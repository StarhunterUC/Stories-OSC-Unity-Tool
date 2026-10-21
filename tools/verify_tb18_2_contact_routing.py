from pathlib import Path
s=(Path(__file__).resolve().parents[1]/"StoriesOfYggdrasilOSCContactSystem.cs").read_text(encoding="utf-8")
checks={
  "version": 'private const string BuildNumber = "TB18.2";',
  "external_weak": 'private const string ExternalHitWeakParameter = "SoY_ExternalHitWeak";',
  "external_average": 'private const string ExternalHitAverageParameter = "SoY_ExternalHitAverage";',
  "weak_native_only": "IncomingWeakContactTags =\n        {\n            TagWeak\n        };",
  "average_native_only": "IncomingAverageContactTags =\n        {\n            TagAverage\n        };",
  "new_weak_receiver": "new ReceiverMapping(ExternalWeakContactTags, ExternalHitWeakParameter)",
  "new_average_receiver": "new ReceiverMapping(ExternalAverageContactTags, ExternalHitAverageParameter)",
  "managed_geometry_preserved": "ConfigureIncomingReceiverPreservingGeometry(receiver.gameObject,",
  "mixed_alias_audit": "var needsRepair = missing.Length > 0 || unexpected.Length > 0;",
  "tier_priority": 'if (set.Contains(TagStrong)) { tier = AttackTier.Strong; return true; }'
}
for name,frag in checks.items():
    assert frag in s, f"Missing {name}"
print("TB18.2 contact provenance source checks passed. Unity compilation not tested.")
