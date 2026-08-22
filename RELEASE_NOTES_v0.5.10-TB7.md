# Stories OSC Unity Tool v0.5.10 TB7

TB7 is a focused compatibility hotfix for VRChat Avatars SDK 3.10.5-beta.1.

## Fixed

TB6 could resolve and add `VRC.SDK3.Dynamics.Constraint.Components.VRCParentConstraint`, but failed while reflecting the `Sources` keyable list. TB7 now compiles against and uses VRChat's documented public API directly:

```csharp
while (constraint.Sources.Count > 0)
    constraint.Sources.RemoveAt(constraint.Sources.Count - 1);
constraint.Sources.Add(new VRCConstraintSource(sourceTransform, 1f));
constraint.ApplyConfigurationChanges();
```

TB7 verifies that the source was actually inserted and points at `[SoY Spell Ground Result]` before keeping the world-drop component.

## Retained

- Native `FreezeToWorld` / `RebakeOffsetsWhenUnfrozen` world drop.
- TB4 latched player + ground placement.
- TB3 installed-only menus and explicit targeting toggle.
- TB2 filtered selection / Regen ID 8 fix.

Full Unity + VRChat SDK compile/test still needs to be run in the target project.
