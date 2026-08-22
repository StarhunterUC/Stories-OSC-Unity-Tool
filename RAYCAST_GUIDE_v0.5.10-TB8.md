# Raycast Guide — v0.5.10 TB8

## World Drop reset behavior

Spell Ground Placement uses a native `VRCParentConstraint` sourced from `[SoY Spell Ground Result]`.

- While unfrozen, the World Drop carrier follows the live ground-result Raycast at its original zero offset.
- When the spell is active and placement confirms, `Freeze To World` locks the carrier in world space.
- When the spell Button is released or Toggle is disabled, `Freeze To World` turns off and the carrier follows the live ground-result source again.
- TB8 keeps `Rebake Offsets When Unfrozen` **OFF** so the dropped displacement is not converted into a new permanent constraint offset.

This is intentional. Turning Rebake on would preserve the dropped displacement when unfreezing, which is the opposite of the desired reusable spell-placement behavior.
