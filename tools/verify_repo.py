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
    if not (root / '.git').exists():
        return []
    try:
        proc = subprocess.run(['git', '-C', str(root), 'ls-files', '--', pathspec], check=False, capture_output=True, text=True)
    except OSError:
        return []
    if proc.returncode != 0:
        return []
    return [line.strip() for line in proc.stdout.splitlines() if line.strip()]


def git_is_ignored(root: Path, pathspec: str) -> bool:
    if not (root / '.git').exists():
        return False
    try:
        proc = subprocess.run(['git', '-C', str(root), 'check-ignore', '-q', pathspec], check=False, capture_output=True, text=True)
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
    tag = version['tag']
    source = root / version['canonical_source_asset']
    unity_source = root / version['unity_asset_path']
    contract = root / 'contracts' / f'UNITY_TOOL_CONTRACT_{tag}.json'
    external_contract = root / 'contracts' / 'EXTERNAL_CONTACT_COMPATIBILITY_v18.json'
    release_notes = root / f'RELEASE_NOTES_{tag}.md'
    raycast_guide = root / f'RAYCAST_GUIDE_{tag}.md'
    source_audit = root / f'SOURCE_AUDIT_{tag}.json'
    test_plan = root / f'TEST_PLAN_{tag}.md'
    protocol_doc = root / 'OSC_PROTOCOL_v21.md'

    required = [source, unity_source, contract, external_contract, release_notes, raycast_guide, source_audit, test_plan, protocol_doc]
    missing = [str(p.relative_to(root)) for p in required if not p.is_file()]
    if missing:
        raise SystemExit('Verification failed: missing ' + ', '.join(missing))

    text = source.read_text(encoding='utf-8-sig')
    checks = {
        'version': version.get('version') == '0.5.10',
        'build_number': version.get('build_number') == 'TB18',
        'tag': tag == 'v0.5.10-TB18',
        'protocol_metadata': version.get('osc_protocol_version') == 21,
        'desktop_metadata': version.get('minimum_desktop_version') == '0.8.22-prebuild.1',
        'source_header_version': 'private const string Version = "0.5.10";' in text,
        'source_header_build': 'private const string BuildNumber = "TB18";' in text,
        'source_header_label': 'Physical Helpful Items' in text,
        'source_protocol': 'private const int OscProtocolVersion = 21;' in text,
        'canonical_and_unity_source_identical': source.read_bytes() == unity_source.read_bytes(),

        # Protocol 21 marker.
        'marker_layer': 'Stories Of Yggdrasil | Unity Tool Marker' in text,
        'marker_present': 'SoY_UnityToolPresent' in text,
        'marker_version_fields': all(x in text for x in ('SoY_UnityToolMajor','SoY_UnityToolMinor','SoY_UnityToolPatch','SoY_UnityToolTB','SoY_UnityToolTBRevision')),
        'marker_protocol': 'SoY_ProtocolVersion' in text,
        'marker_schema': 'SoY_UnitySchemaValid' in text,
        'marker_beacon': 'SoY_UnityMarkerBeacon' in text and 'SOY_UnityMarker_Beacon_A.anim' in text and 'SOY_UnityMarker_Beacon_B.anim' in text,
        'marker_repair': 'ManagedRepairKind.UnityCompatibilityMarker' in text and 'periodic marker beacon' in text,
        'animator_integrity_repair': 'REPAIR ANIMATOR INTEGRITY' in text and 'RepairAnimatorControllerIntegrity' in text,
        'orphan_transition_cleanup': 'CleanupOrphanedAnimatorTransitions' in text and 'FindOrphanedAnimatorTransitions' in text,
        'animator_validation_before_save': 'InspectAnimatorControllerIntegrity' in text and 'Animator integrity validation failed before save' in text,
        'controller_backup_before_repair': 'CreateAnimatorIntegrityBackup' in text and 'Backups/Animator Integrity' in text,
        'animator_state_name_sanitizer': 'SanitizeAnimatorStateName' in text and 'name.Replace("/", " - ")' in text,
        'repair_convergence_reporting': 'Repair transaction committed, but ' in text and 'Repair Needs Another Look' in text,
        'marker_restores_bridge_parameters': 'AddMissingAnimatorParameters(fxController)' in text and 'AddMissingExpressionParameters(expressionParameters)' in text,
        'marker_schema_failure_diagnostics': 'ManagedSchemaCoreFailureSummary' in text and 'Marker remains schema INVALID' in text,
        'marker_driver_metadata_validation': 'MarkerStatePublishes' in text and 'DriverPublishesValue' in text,
        'marker_fixed_safe_state_names': 'SoY Marker Beacon A' in text and 'SoY Marker Beacon B' in text and 'SoY Marker INVALID' in text,
        'marker_transition_validation': 'destinationState == stateB' in text and 'destinationState == stateA' in text,
        'migration_ui': 'MIGRATE / VALIDATE AVATAR FOR PROTOCOL ' in text,
        'schema_validation': 'CurrentSchemaIsValid()' in text and 'RebuildUnityToolMarkerLayer' in text,
        # TB18 Protocol 21 physical helpful-item interaction.
        'helpful_item_self_touch': 'SoY_HelpItemSelfTouch' in text,
        'helpful_item_other_touch': 'SoY_HelpItemOtherTouch' in text,
        'helpful_item_bus': 'SoY_HelpItemActive' in text and 'HelpfulItemBitParameterPrefix' in text,
        'helpful_item_results': 'SoY_ItemUseResult' in text and 'SoY_ItemReceiveResult' in text,
        'helpful_item_head_receiver': 'EnsureHelpfulItemHeadReceiverBus' in text and 'HumanBodyBones.Head' in text,
        'helpful_item_gesture_toggle': 'Grab Toggle On — Wait Release' in text and 'Use — Head Contacts Active' in text,
        'helpful_item_faceemo_safe': 'FaceEmo assets were not modified' in text,
        'helpful_item_registry': 'HelpfulPhysicalItemIds' in text,
        # Protocol 21 authenticated PvP source handshake.
        'pvp_attempt_parameters': all(x in text for x in ('SoY_PvPAttemptWeak','SoY_PvPAttemptAverage','SoY_PvPAttemptStrong','SoY_PvPAttemptCritical')),
        'pvp_builtin_body_tags': 'PvpRemoteBodyTags' in text and all(x in text for x in ('"Head"','"Torso"','"Hand"','"Foot"')),
        'pvp_attempt_receiver': 'ConfigurePvpAttemptReceiver' in text and 'allowOthers' in text and 'localOnly' in text,
        'protocol21_beacon': 'ConfigureUnityMarkerDriver(stateA, true, 121)' in text and 'ConfigureUnityMarkerDriver(stateB, true, 122)' in text,

        # TB15 action gates retained.
        'spell_approved': 'SoY_SpellApproved' in text,
        'technick_approved': 'SoY_TechnickApproved' in text,
        'item_approved': 'SoY_ItemApproved' in text,
        'raycast_approved': 'SoY_RaycastApproved' in text,
        'recovery_defaults': all(x in text for x in ('DefaultSpellRecoverySeconds = 8f','DefaultTechnickRecoverySeconds = 15f','DefaultItemRecoverySeconds = 5f','DefaultRaycastRecoverySeconds = 1f')),

        # TB17 automated action authoring.
        'automated_action_sync': 'SyncInstalledActionAnimationProfile' in text and 'RebuildAllAutomatedActionLayers' in text,
        'optional_presentation': 'generated automatic timer (no manual animation required)' in text and 'AutoPresentation.anim' in text,
        'spell_presentation_presets': all(x in text for x in ('spellSchoolPresentationClips','spellCategoryPresentationClips','defaultSpellPresentationClipPath')),
        'shared_action_presentation': all(x in text for x in ('defaultTechnickPresentationClipPath','defaultItemPresentationClipPath')),
        'managed_visual_holders': all(x in text for x in ('FX — Spell Visuals (Place Here)','FX — Technick Visuals (Place Here)','FX — Item Visuals (Place Here)')),
        'legacy_placeholder_migration': 'IsLegacyGeneratedEmptyPresentation' in text and '__Cast.anim' in text,
        'presentation_does_not_own_approval': 'presentation layers do not write SoY_SpellApproved' in text and 'presentation does not own SoY_*Approved' in text,

        # Evasion retained and automated fallback expanded in TB17.
        'evasion_layer': 'Stories Of Yggdrasil | Evasion Animations' in text,
        'evade_selector': 'SoY_EvadeType' in text,
        'evading_flag': 'SoY_Evading' in text,
        'evasion_builder': 'DrawEvasionAnimationBuilder' in text and 'RebuildEvasionAnimationLayer' in text,
        'generic_evade': 'Generic Evade' in text and 'useGenericEvadeFallback' in text,

        # Resource FX / external contact compatibility retained.
        'resource_hp': 'SoY_HPPercent' in text,
        'resource_mp': 'SoY_MPPercent' in text,
        'resource_blendtree': 'BlendTreeType.Simple1D' in text,
        'external_sword': 'TagExternalSword = "Sword"' in text,
        'external_weapon': 'TagExternalWeapon = "Weapon"' in text,
        'external_hands': 'TagExternalHands = "Hands"' in text,
        'external_parry': 'TagExternalParryDetect = "Parry_Detect"' in text,
        'external_source_parameter': 'SoY_ExternalDamageSource' in text,
        'canonical_alignment_split': 'SetCollisionTags(receiver, new[] { CasterEnemyTag });' in text and 'new ReceiverMapping(ExternalDamageContactTags, ExternalDamageSourceParameter)' in text,
        'native_world_drop': 'FreezeToWorld' in text,

        # Current registries.
        'registries': all((root / p).is_file() for p in (
            'registries/SPELL_ID_REGISTRY_v2.json',
            'registries/TECHNICK_ID_REGISTRY_v1.json',
            'registries/ITEM_ID_REGISTRY_v1.json',
        )),

        # Repository current-head hygiene.
        'no_tracked_dist': len(git_tracked_paths(root, 'dist')) == 0 if (root / '.git').exists() else not (root / 'dist').exists(),
        'dist_ignored_in_git_clone': git_is_ignored(root, 'dist/') if (root / '.git').exists() else True,
        'no_tracked_python_cache': (len(git_tracked_paths(root, 'tools/__pycache__')) == 0 and len(git_tracked_paths(root, '**/__pycache__')) == 0) if (root / '.git').exists() else not any(x.name == '__pycache__' for x in root.rglob('__pycache__')),
        'single_current_release_notes': len(list(root.glob('RELEASE_NOTES_*.md'))) == 1,
        'single_current_raycast_guide': len(list(root.glob('RAYCAST_GUIDE_*.md'))) == 1,
        'single_current_source_audit': len(list(root.glob('SOURCE_AUDIT_*.json'))) == 1,
        'single_current_test_plan': len(list(root.glob('TEST_PLAN_*.md'))) == 1,
        'current_contract_only': len(list((root / 'contracts').glob('UNITY_TOOL_CONTRACT_*.json'))) == 1,
        'no_legacy_root_osc_contracts': len(list(root.glob('OSC_CONTRACT_v*.json'))) == 0,
        'no_duplicate_root_registries': not any((root / name).exists() for name in ('SPELL_ID_REGISTRY_v1.json','SPELL_ID_REGISTRY_v2.json','TECHNICK_ID_REGISTRY_v1.json','ITEM_ID_REGISTRY_v1.json')),
    }

    failed = [name for name, passed in checks.items() if not passed]
    if failed:
        raise SystemExit('Verification failed: ' + ', '.join(failed))

    actual = sha256(source)
    expected = version['source_sha256']
    if actual != expected:
        raise SystemExit(f'Source SHA mismatch: expected {expected}, received {actual}')

    contract_data = json.loads(contract.read_text(encoding='utf-8'))
    if contract_data.get('osc_protocol_version') != 21 or contract_data.get('build_number') != 'TB18':
        raise SystemExit('Verification failed: current Unity contract metadata mismatch')

    audit_data = json.loads(source_audit.read_text(encoding='utf-8'))
    if audit_data.get('source_sha256') != actual:
        raise SystemExit('Verification failed: source audit SHA mismatch')

    # Only the canonical root source and Unity Assets copy should define the EditorWindow script.
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
    expected_copies = sorted([version['canonical_source_asset'], version['unity_asset_path']])
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
