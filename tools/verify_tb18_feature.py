from pathlib import Path

root = Path(__file__).resolve().parents[1]
canonical = (root / "StoriesOfYggdrasilOSCContactSystem.cs").read_text(encoding="utf-8")
asset = (root / "Assets" / "Stories Of Yggdrasil" / "Editor" / "StoriesOfYggdrasilOSCContactSystem.cs").read_text(encoding="utf-8")

if canonical != asset:
    raise SystemExit("Canonical Unity source and Assets copy differ.")

required = {
    'build': 'private const string BuildNumber = "TB18";',
    'protocol': 'private const int OscProtocolVersion = 21;',
    'beacon_a': 'ConfigureUnityMarkerDriver(stateA, true, 121)',
    'beacon_b': 'ConfigureUnityMarkerDriver(stateB, true, 122)',
    'self_touch': 'SoY_HelpItemSelfTouch',
    'other_touch': 'SoY_HelpItemOtherTouch',
    'help_bus': 'SoY_HelpItemActive',
    'use_result': 'SoY_ItemUseResult',
    'receive_result': 'SoY_ItemReceiveResult',
    'head_receiver': 'EnsureHelpfulItemHeadReceiverBus',
    'humanoid_head': 'HumanBodyBones.Head',
    'gesture_ui': 'Grab / Toggle Gesture',
    'gesture_reader': 'GestureParameterForHand',
    'toggle_latch': 'Grab Toggle On — Wait Release',
    'use_window': 'Use — Head Contacts Active',
    'self_split': 'allowSelf',
    'others_split': 'allowOthers',
    'faceemo_safety': 'FaceEmo assets were not modified',
    'helpful_registry': 'HelpfulPhysicalItemIds',
    'pvp_attempt_weak': 'SoY_PvPAttemptWeak',
    'pvp_attempt_average': 'SoY_PvPAttemptAverage',
    'pvp_attempt_strong': 'SoY_PvPAttemptStrong',
    'pvp_attempt_critical': 'SoY_PvPAttemptCritical',
    'pvp_remote_body_tags': 'PvpRemoteBodyTags',
    'pvp_attempt_receiver': 'ConfigurePvpAttemptReceiver',
    'pvp_existing_attack_host': 'ConfigurePvpAttemptReceiver(\n                    host,\n                    attackTier,',
}
for label, marker in required.items():
    if marker not in canonical:
        raise SystemExit(f"Missing TB18 feature marker {label}: {marker}")

# These harmful/special item IDs must not be in the physical whitelist.
registry_start = canonical.index("HelpfulPhysicalItemIds")
registry_end = canonical.index("private static bool IsHelpfulPhysicalItem", registry_start)
registry = canonical[registry_start:registry_end]
for forbidden in ("23,", "26,", "33,"):
    if forbidden in registry:
        raise SystemExit(f"Harmful item ID unexpectedly enabled in physical helpful registry: {forbidden}")

print("TB18 physical helpful-item feature verification passed.")
