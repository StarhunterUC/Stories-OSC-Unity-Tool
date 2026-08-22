# Raycast Guide — v0.5.10 TB6

TB6 retains the TB5 World Drop flow and updates the VRChat Parent Constraint authoring path for current SDK builds.

## Expected Regen hierarchy

```text
[SoY Spell Ground Result]
[SoY Spell World Drop] Spell_8_Regen
  VRC Parent Constraint
    Source: [SoY Spell Ground Result]
    Freeze To World: Off while aiming
    Rebake Offsets When Unfrozen: On
  [SoY Spell Placement] Spell_8_Regen
```

## Runtime flow

1. Targeting is enabled locally.
2. The player Raycast finds a remote target.
3. The downward Raycast finds a world surface beneath the target.
4. Regen is pressed/held.
5. The carrier follows the ground result until placement confirms.
6. `FreezeToWorld` switches on and captures the current world pose.
7. The caster and target may move; the Regen placement remains fixed.
8. Releasing Regen switches `FreezeToWorld` off and the carrier returns to the ground-result source for the next cast.

## SDK 3.10.5-beta.1 compatibility

The public constraint type is `VRC.SDK3.Dynamics.Constraint.Components.VRCParentConstraint`. TB6 uses the current `Sources` public API first, with compatibility fallbacks, and calls `ApplyConfigurationChanges()` after edits.

If Create / Repair still fails, copy the Stories tool log line beginning with `TB6 resolved VRChat Parent Constraint type:` or `TB6 public Sources API configuration failed`. That line now identifies exactly which stage failed.
