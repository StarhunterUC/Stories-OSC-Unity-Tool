#!/usr/bin/env python3
from __future__ import annotations

from pathlib import Path
import argparse
import hashlib
import json


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo-root", default=".")
    args = parser.parse_args()

    root = Path(args.repo_root).resolve()
    version = json.loads((root / "version.json").read_text(encoding="utf-8"))
    source = root / version["canonical_source_asset"]
    unity_source = root / version["unity_asset_path"]
    text = source.read_text(encoding="utf-8-sig")

    contract = root / "contracts" / f'UNITY_TOOL_CONTRACT_{version["tag"]}.json'
    release_notes = root / f'RELEASE_NOTES_{version["tag"]}.md'
    raycast_guide = root / f'RAYCAST_GUIDE_{version["tag"]}.md'

    checks = {
        "version": f'Version = "{version["version"]}"' in text,
        "build": f'BuildNumber = "{version["build_number"]}"' in text,
        "repository": version["repository"] in text,
        "class": "class StoriesOfYggdrasilOSCContactSystem" in text,
        "editor_guard": text.count("#if UNITY_EDITOR") == 1 and text.count("#endif") == 1,
        "spell_bus_fix": "int.TryParse(suffix, out spellId)" in text,
        "current_spell_bus": "tag == SpellActiveTag" in text and "SpellBitTagPrefix" in text,
        "sha256_updater": 'EndsWith(".sha256"' in text,
        "managed_repair": "Managed-System Repair Center" in text,

        # TB2 filtered-selection consistency hotfix (retained in TB3).
        "filtered_selection_helpers": "GetVisibleSpellDefinitions" in text and
                                      "GetVisibleTechnickDefinitions" in text and
                                      "GetVisibleItemDefinitions" in text and
                                      "TryGetSelectedSpell" in text and
                                      "TryGetSelectedTechnick" in text and
                                      "TryGetSelectedItem" in text,
        "filtered_creation_paths": "if (!TryGetSelectedSpell(out spell))" in text and
                                   "if (!TryGetSelectedTechnick(out technick))" in text and
                                   "if (!TryGetSelectedItem(out item))" in text,
        "no_unfiltered_spell_creation": "var spells = GetSpellsForSchool(spellSchool);\n            if (spells.Length == 0)" not in text,
        "no_unfiltered_action_creation": "TechnickDefinitions[technickSelectionIndex]" not in text and
                                         "ItemDefinitions[itemSelectionIndex]" not in text,

        "filtered_raycast_repair_cleanup": "RemoveMismatchedManagedRaycastActionChildren" in text and
                                           "Undo.DestroyObjectImmediate(child)" in text and
                                           "selectedSpell.Id != spellId" in text,

        # v0.5.10 TB3 Raycast/menu contract.
        "raycast_direct_mode": "CreateDirectRaycastDelivery" in text,
        "raycast_spell_ground_mode": "CreateSpellGroundPlacementRaycastDelivery" in text,
        "raycast_ground_is_down": "Vector3.down" in text and "SpellGroundPrefix" in text,
        "raycast_surface_rotation": "alignmentAxis" in text and "FX — Faces Down (Place Particle Here)" in text,
        "remote_player_layer_9": "1 << 9" in text and "PlayerLocal (10)" in text,
        "custom_layer_safe_fallback": "Fell back to Hit Players" in text,
        "serialized_sdk_fallback": "WriteSerializedMember" in text and "SetSerializedEnumMemberByKeywords" in text,
        "raycast_budget_diagnostic": "CountRaycastComponents" in text and '" / 80"' in text,
        "local_targeting": '"IsLocal"' in text and "[LOCAL ONLY] Spell Targeting Icon" in text,
        "shared_spell_rays": "SpellTargetPrefix" in text and "SpellGroundPrefix" in text,
        "single_shared_spell_aim_origin": "GetOrCreateSharedSpellAimOrigin" in text and
                                          "Disable Duplicate Stories Spell Aim Origin" in text,
        "legacy_tb3_raycast_guard": "DisableLegacyRaycastDelivery" in text and
                                     "LegacyRaycastOriginPrefix" in text and
                                     "LegacyRaycastResultPrefix" in text,
        "raycast_origin_hierarchy_guard": "ValidateRaycastOrigin" in text and
                                           "IsInsideManagedRaycastHierarchy" in text,

        # Organization / safety.
        "avatar_workspace": 'AvatarGeneratedFolder(avatarName, "FX")' in text and
                            'AvatarGeneratedFolder(avatarName, "Menus")' in text and
                            'AvatarGeneratedFolder(avatarName, "Animations/' in text,
        "legacy_paths_preserved": "LegacyFxCopyRoot" in text and "LegacyManifestRoot" in text,
        "parameter_budget_fallback": "as LOCAL ONLY because synchronizing it would exceed" in text,
        "menu_cleanup": 'CreateSubMenuControl("Combat", combatMenu)' in text and
                        'CreateSubMenuControl("Targeting", targetingMenu)' in text,

        # TB3 installed-only menu and Raycast trigger UX.
        "installed_contact_menu_filtering": "GetInstalledManagedActionIds" in text and
                                             "GetInstalledSpellDefinitions" in text and
                                             "GetInstalledTechnickDefinitions" in text and
                                             "GetInstalledItemDefinitions" in text and
                                             'GetInstalledManagedActionIds("Stories Spell - ")' in text,
        "installed_action_page_filtering": 'BuildActionMenuPages(avatarName, "Technicks", "SoY_TechnickType", installedTechnicks)' in text and
                                           'BuildActionMenuPages(avatarName, "Items", "SoY_ItemType", installedItems)' in text and
                                           "installedIds.Contains(spell.Id)" in text,
        "empty_menu_categories_omitted": 'if (spellsMenu.controls != null && spellsMenu.controls.Count > 0)' in text and
                                         'if (actionsMenu.controls != null && actionsMenu.controls.Count > 0)' in text and
                                         'if (targetingMenu.controls != null && targetingMenu.controls.Count > 0)' in text,
        "quick_access_installed_only": "installedSpellIds.Contains(favorite.id)" in text and
                                       "installedTechnickIds.Contains(favorite.id)" in text and
                                       "installedItemIds.Contains(favorite.id)" in text,
        "targeting_toggle_parameter": 'RaycastTargetingParameter = "SoY_RaycastTargeting"' in text and
                                      "EnsureLocalRaycastTargetingExpressionParameter" in text and
                                      'CreateToggleControl("Targeting Crosshair", RaycastTargetingParameter)' in text,
        "targeting_toggle_is_local": "networkSynced = false" in text and
                                     "show.AddCondition(AnimatorConditionMode.If, 0f, RaycastTargetingParameter)" in text and
                                     "targetingDisabled.AddCondition(AnimatorConditionMode.IfNot, 0f, RaycastTargetingParameter)" in text,
        "manual_fire_only_when_needed": "HasManualRaycastFireAction" in text and
                                        'CreateButtonControl("Projectile Fire", RaycastFireParameter, 1f)' in text,
        "selector_raycast_animation_binding": "EnsureRaycastSelectorAnimationBinding" in text and
                                              "RebuildSpellCastAnimationLayer" in text and
                                              "RebuildActionAnimationLayer(ActionAnimationKind.Technick" in text and
                                              "RebuildActionAnimationLayer(ActionAnimationKind.Item" in text,
        "hierarchy_raycast_animation_assets": "RaycastHierarchyAnimationFolder" in text and
                                              '"Animations/Raycasts"' in text and
                                              '"Cast_" + actionId + "_" + actionName' in text and
                                              '/Contact_Ready.anim' in text and
                                              '/Contact_Pulse.anim' in text and
                                              '/Crosshair_Visible.anim' in text,

        # TB4 two-stage spell placement reliability.
        "latched_raycast_gate": "Armed / Waiting For Raycast" in text and
                                "RaycastActionArmSeconds = 1.25f" in text and
                                "RaycastActionPulseSeconds = 0.85f" in text and
                                "Contact_Armed.anim" in text and
                                "Contact_Pulse.anim" in text,
        "spell_ground_ready_targeting": "RebuildRaycastTargetingLayer(" in text and
                                         "SpellGroundPrefix);" in text and
                                         "additionalRequiredHitPrefix + \"_Hit\"" in text,
        "spell_gate_does_not_require_same_frame_selector_and_hit": "var arm = ready.AddTransition(armed);" in text and
                                                                    "var confirm = armed.AddTransition(on);" in text,

        # TB5 native World Drop spell placement.
        "native_world_drop_constraint": "ConfigureWorldDropParentConstraint" in text and
                                        'SpellWorldDropPrefix = "[SoY Spell World Drop] "' in text and
                                        "VRCParentConstraint" in text,
        "world_drop_freeze_to_world": '"FreezeToWorld"' in text and
                                      "CreateOrReplaceWorldDropClip" in text and
                                      'WorldDrop_Placed.anim' in text,
        "world_drop_real_source_required": "ConfigureWorldDropParentConstraint(worldDropCarrier, groundResult.transform)" in text and
                                           "component.Sources.Add(sourceEntry)" in text and
                                           "configuredSource.SourceTransform != source" in text,
        "world_drop_returns_to_live_raycast_on_release": "component.RebakeOffsetsWhenUnfrozen = false;" in text and
                                                       'SetBoolMember(component, false, "RebakeOffsetsWhenUnfrozen", "rebakeOffsetsWhenUnfrozen")' in text,
        "world_drop_holds_until_action_release": "worldDropReleased = on.AddTransition(waitForRelease)" in text and
                                                 "worldDropConstraint == null" in text and
                                                 "future action uses a Toggle" in text,
        "world_drop_migrates_tb4_payload": "Migrate Stories Spell Placement To World Drop" in text and
                                           "without replacing its children" in text,
        "world_drop_is_spell_only": "RebuildRaycastGateLayer(" in text and
                                    "ConfigureWorldDropParentConstraint(worldDropCarrier, groundResult.transform)" in text and
                                    "worldDropConstraint);" in text,

        # TB7/TB8 current VRChat Constraint API compatibility + zero-offset release reset.
        "tb8_direct_constraint_api": "using VRC.SDK3.Dynamics.Constraint.Components;" in text and
                                     "using VRC.Dynamics;" in text and
                                     "new VRCConstraintSource(source, Mathf.Clamp01(weight))" in text,
        "tb8_public_sources_api": "component.Sources.Add(sourceEntry)" in text and
                                  "component.ApplyConfigurationChanges()" in text and
                                  "configuredSource.SourceTransform != source" in text,
        "tb8_constraint_axes": "AffectsPositionX" in text and "AffectsRotationZ" in text and
                               "FreezeToWorld" in text,

        # Repository payload.
        "unity_source_exists": unity_source.is_file(),
        "canonical_and_unity_source_identical": unity_source.is_file() and source.read_bytes() == unity_source.read_bytes(),
        "contract_exists": contract.is_file(),
        "release_notes_exist": release_notes.is_file(),
        "raycast_guide_exists": raycast_guide.is_file(),
    }

    failed = [name for name, passed in checks.items() if not passed]
    if failed:
        raise SystemExit("Verification failed: " + ", ".join(failed))

    actual = sha256(source)
    expected = version["source_sha256"]
    if actual != expected:
        raise SystemExit(
            f"Source SHA mismatch: expected {expected}, received {actual}"
        )

    print("VERIFY PASSED")
    for name in checks:
        print(f" - {name}: OK")
    print(f" - source_sha256: {actual}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
