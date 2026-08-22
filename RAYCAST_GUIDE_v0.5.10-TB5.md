# Raycast Guide — v0.5.10 TB5

TB5 adds **World-Dropped Spell Ground Placement** on top of the TB4 latched targeting flow.

## Spell Ground Placement flow

For Regen and other ground-placement spells:

1. Turn **Stories RP → Targeting → Targeting Crosshair** on.
2. Aim at a remote player.
3. The shared player Raycast tracks the target and the shared ground Raycast probes straight downward.
4. The crosshair appears only when both a remote player and a valid floor beneath them are available.
5. Press and hold the installed Spell button, such as **Regen**.
6. The cast arms for up to 1.25 seconds while waiting for the Raycast confirmation.
7. Once confirmed, the spell payload is enabled and its World Drop carrier sets the VRChat Parent Constraint's **Freeze To World** property.
8. You can move away or lose the target; the spell remains locked at the world position where it was placed.
9. Releasing the Spell button unfreezes the carrier, hides the placement payload, and lets the carrier follow the live Raycast result again. If a future action uses a Toggle rather than a Button, turning that Toggle off performs the same reset.

## Generated hierarchy

A spell such as Regen now uses a hierarchy similar to:

```text
Stories Raycast Systems
└─ Spell Ground Placement
   ├─ [SoY Spell Target Anchor]
   │  ├─ [SoY Downward Ground Probe]
   │  └─ [LOCAL ONLY] Spell Targeting Icon
   ├─ [SoY Spell Ground Result]
   └─ [SoY Spell World Drop] Spell_8_Regen
      └─ [SoY Spell Placement] Spell_8_Regen
         ├─ FX — Faces Down (Place Particle Here)
         └─ Stories Spell - 8 Regen
            ├─ [SoY Spell Ally] 8 Regen
            └─ [SoY Spell Enemy] 8 Regen
```

The **World Drop carrier stays active** and follows `[SoY Spell Ground Result]` through a `VRCParentConstraint`. The spell payload beneath it is what is shown/hidden by the cast state.

## Why native VRChat Freeze To World

TB5 does not require VRCFury or VRLabs. It uses VRChat's native `VRCParentConstraint` and animates `FreezeToWorld`. A real ground-result transform is always assigned as the constraint source, and `RebakeOffsetsWhenUnfrozen` is enabled so repeat placements can reset cleanly instead of relying on a source-less world-lock trick.

This design is conceptually similar to VRCFury's Droppable World Constraint and VRLabs' World Constraint, but keeps the Stories tool self-contained.

## Existing TB4 Regen migration

Run **Create / Repair** for Regen after importing TB5. The tool moves the existing generated `[SoY Spell Placement] Spell_8_Regen` object into the new World Drop carrier rather than replacing it, so user-added children beneath the generated placement object remain intact.

## Direct Impact rays

Bullets, arrows, beams, Technicks, Items, and debuff projectiles keep the TB4 direct-impact pulse behavior. World Drop is only applied to **Spell Ground Placement**.

## Unity preview note

The local crosshair also depends on VRChat's built-in `IsLocal` parameter. Gesture Manager and ordinary editor preview may not emulate that parameter exactly like the live VRChat client. Use Build & Test / live VRChat as the authoritative crosshair check.
