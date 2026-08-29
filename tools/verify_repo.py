#!/usr/bin/env python3
from __future__ import annotations

from pathlib import Path
import argparse
import hashlib
import json
import re
import subprocess


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()



def git_tracked_paths(root: Path, pathspec: str) -> list[str]:
    """Return tracked files matching pathspec when root is a Git worktree."""
    git_dir = root / '.git'
    if not git_dir.exists():
        return []
    try:
        proc = subprocess.run(
            ['git', '-C', str(root), 'ls-files', '--', pathspec],
            check=False,
            capture_output=True,
            text=True,
        )
    except OSError:
        return []
    if proc.returncode != 0:
        return []
    return [line.strip() for line in proc.stdout.splitlines() if line.strip()]


def git_is_ignored(root: Path, pathspec: str) -> bool:
    """Return True when Git ignores pathspec; False outside a Git worktree."""
    if not (root / '.git').exists():
        return False
    try:
        proc = subprocess.run(
            ['git', '-C', str(root), 'check-ignore', '-q', pathspec],
            check=False,
            capture_output=True,
            text=True,
        )
    except OSError:
        return False
    return proc.returncode == 0

def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument('--repo-root', default='.')
    args = parser.parse_args()

    root = Path(args.repo_root).resolve()
    version_path = root / 'version.json'
    if not version_path.is_file():
        raise SystemExit('Verification failed: missing version.json')

    version = json.loads(version_path.read_text(encoding='utf-8'))
    source = root / version['canonical_source_asset']
    unity_source = root / version['unity_asset_path']
    contract = root / 'contracts' / 'UNITY_TOOL_CONTRACT_v0.5.10-TB12.json'
    external_contract = root / 'contracts' / 'EXTERNAL_CONTACT_COMPATIBILITY_v17.json'
    release_notes = root / 'RELEASE_NOTES_v0.5.10-TB12.md'
    raycast_guide = root / 'RAYCAST_GUIDE_v0.5.10-TB12.md'
    source_audit = root / 'SOURCE_AUDIT_v0.5.10-TB12.json'

    required = [source, unity_source, contract, external_contract, release_notes, raycast_guide, source_audit]
    missing = [str(p.relative_to(root)) for p in required if not p.is_file()]
    if missing:
        raise SystemExit('Verification failed: missing ' + ', '.join(missing))

    text = source.read_text(encoding='utf-8-sig')

    checks = {
        'version': version.get('version') == '0.5.10',
        'build_number': version.get('build_number') == 'TB12',
        'tag': version.get('tag') == 'v0.5.10-TB12',
        'source_header_version': 'private const string Version = "0.5.10";' in text,
        'source_header_build': 'private const string BuildNumber = "TB12";' in text,
        'source_header_label': 'External Contact Compatibility' in text,
        'canonical_and_unity_source_identical': source.read_bytes() == unity_source.read_bytes(),

        # TB12 aliases.
        'external_sword': 'TagExternalSword = "Sword"' in text,
        'external_weapon': 'TagExternalWeapon = "Weapon"' in text,
        'external_hands': 'TagExternalHands = "Hands"' in text,
        'external_parry': 'TagExternalParryDetect = "Parry_Detect"' in text,
        'grouped_weak_receiver': 'IncomingWeakContactTags' in text and 'TagExternalHands' in text,
        'grouped_average_receiver': 'IncomingAverageContactTags' in text and 'TagExternalSword' in text and 'TagExternalWeapon' in text,
        'external_damage_alignment': 'ExternalDamageContactTags' in text and 'SoY_DamageSourceEnemy' in text,
        'compatible_block_group': 'CompatibleBlockContactTags' in text and 'TagHitBlocked' in text and 'TagExternalParryDetect' in text,
        'canonical_mapping_weak': 'set.Contains(TagWeak) || set.Contains(TagExternalHands)' in text,
        'canonical_mapping_average': 'set.Contains(TagAverage) || set.Contains(TagExternalSword) || set.Contains(TagExternalWeapon)' in text,
        'managed_upgrade_path': 'Repair Stories External Contact Compatibility' in text,

        # Existing v0.5.10 systems retained.
        'raycast_direct': 'CreateDirectRaycastDelivery' in text,
        'raycast_spell_world': 'CreateSpellGroundPlacementRaycastDelivery' in text,
        'raycast_technick_world': 'CreateTechnickGroundPlacementRaycastDelivery' in text,
        'raycast_downward_probe': 'Vector3.down' in text and 'SpellGroundPrefix' in text,
        'native_parent_constraint': 'VRCParentConstraint' in text and 'new VRCConstraintSource' in text,
        'world_drop_zero_offset_release': 'RebakeOffsetsWhenUnfrozen = false' in text,
        'targeting_toggle': 'SoY_RaycastTargeting' in text,
        'installed_only_menus': 'GetInstalledManagedActionIds' in text,
        'flat_unique_raycast_animations': 'Animations/Raycasts' in text and 'SOY_Raycast_' in text and 'RaycastAnimationClipPath' in text,
        'mist_gauge': 'SoY_MistPercent' in text,
        'diablos_gauge': 'SoY_DiablosPercent' in text,
        'arousal_gauge': 'SoY_ArousalPercent' in text,

        # Repo cleanliness/current-head policy.
        #
        # A real working clone is expected to contain .git and may contain a
        # locally generated dist/ after build_release.py runs. What matters is
        # that generated build/cache output is not committed.
        'no_tracked_dist': len(git_tracked_paths(root, 'dist')) == 0
            if (root / '.git').exists() else not (root / 'dist').exists(),
        'dist_ignored_in_git_clone': git_is_ignored(root, 'dist')
            if (root / '.git').exists() else True,
        'no_tracked_python_cache': (
            len(git_tracked_paths(root, 'tools/__pycache__')) == 0 and
            len(git_tracked_paths(root, '**/__pycache__')) == 0
        ) if (root / '.git').exists()
          else not any(p.name == '__pycache__' for p in root.rglob('__pycache__')),
        'single_current_release_notes': len(list(root.glob('RELEASE_NOTES_*.md'))) == 1,
        'single_current_raycast_guide': len(list(root.glob('RAYCAST_GUIDE_*.md'))) == 1,
        'single_current_source_audit': len(list(root.glob('SOURCE_AUDIT_*.json'))) == 1,
        'current_contract_only': len(list((root / 'contracts').glob('UNITY_TOOL_CONTRACT_*.json'))) == 1,
        'no_legacy_root_osc_contracts': len(list(root.glob('OSC_CONTRACT_v*.json'))) == 0,
        'no_duplicate_root_registries': not any((root / name).exists() for name in [
            'SPELL_ID_REGISTRY_v1.json','SPELL_ID_REGISTRY_v2.json','TECHNICK_ID_REGISTRY_v1.json','ITEM_ID_REGISTRY_v1.json'
        ]),
        'current_registry_set': all((root / 'registries' / name).is_file() for name in [
            'SPELL_ID_REGISTRY_v2.json','TECHNICK_ID_REGISTRY_v1.json','ITEM_ID_REGISTRY_v1.json'
        ]),
    }

    failed = [name for name, passed in checks.items() if not passed]
    if failed:
        raise SystemExit('Verification failed: ' + ', '.join(failed))

    actual = sha256(source)
    expected = version['source_sha256']
    if actual != expected:
        raise SystemExit(f'Source SHA mismatch: expected {expected}, received {actual}')

    # Only the canonical root source and Unity Assets copy should define the EditorWindow script.
    # Ignore generated release/build/cache output such as dist/, .git/, and Python caches.
    copies = []
    excluded_top_level = {'dist', '.git', '.pytest_cache'}
    for path in root.rglob('*.cs'):
        relative = path.relative_to(root)
        if any(part in excluded_top_level or part == '__pycache__' for part in relative.parts):
            continue
        try:
            t = path.read_text(encoding='utf-8-sig')
        except UnicodeDecodeError:
            continue
        if 'class StoriesOfYggdrasilOSCContactSystem' in t:
            copies.append(relative.as_posix())
    expected_copies = sorted([
        version['canonical_source_asset'],
        version['unity_asset_path'],
    ])
    if sorted(copies) != expected_copies:
        raise SystemExit('Verification failed: unexpected Unity Tool script copies: ' + ', '.join(copies))

    print('VERIFY PASSED')
    for name in checks:
        print(f' - {name}: OK')
    print(f' - source_sha256: {actual}')
    print(' - tool_script_copies: ' + ', '.join(copies))
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
