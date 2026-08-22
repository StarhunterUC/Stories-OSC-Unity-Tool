# Raycast Guide — v0.5.10 TB3


## TB3 menu + targeting workflow

The generated menu is now based on **installed managed Contacts**, not the whole
action catalog. Create/install the action first, then rebuild the menu.

For a Spell Ground Placement action:

1. Create / Repair the Spell Raycast.
2. Generate / Repair Stories RP Menus.
3. Open `Stories RP -> Targeting`.
4. Toggle **Targeting Crosshair** ON.
5. Aim at the remote player until the local icon appears.
6. Press the installed Spell button (for example **Regen**). That selector is the
   fire trigger for the Raycast Contact and the managed cast-animation state.

Direct Technick and Item Raycasts work the same way with their action buttons.
Attack/Debuff Direct Impact Raycasts receive **Projectile Fire** only when they
actually need the standalone `SoY_RaycastFire` trigger.

The targeting icon uses unsynchronized `SoY_RaycastTargeting` plus VRChat
`IsLocal`, so the toggle/icon are local to the wearer.

### Generated animation location

Raycast clips now follow the generated object hierarchy under:

```text
Assets/Stories Of Yggdrasil/<Avatar>/Animations/Raycasts/<Hierarchy...>/
```

Selector Raycasts automatically receive a `Cast_<ID>_<Name>.anim` binding when
no custom action animation is already assigned. Editing that clip is the normal
place to add the avatar-specific casting pose/motion; pressing the action button
plays it.

## Which mode should I use?

### Direct Impact

Use for:
- bullets,
- arrows,
- beams,
- projectile-like Technicks,
- projectile-like Items,
- impact debuffs.

Select the muzzle/weapon/hand/focus object that defines the Raycast's local
forward direction, choose the current action, select Raycast delivery, then
Create / Repair Raycast.

For Attack and Debuff direct-impact Raycasts, use:

```text
Stories RP → Targeting → Projectile Fire
```

Technick and Item Raycasts use their action button/selector as the gate.

### Spell Ground Placement

Use when the spell should appear **on the floor beneath a target** rather than
directly on the target collider.

Typical examples:
- Cure / Cura / Curaga ground circles,
- buffs placed beneath a party member,
- healing fields,
- ground telegraphs,
- magic seals and casting circles.

Workflow:

1. Select the hand, focus, staff, weapon muzzle, or other aim origin.
2. Select the Spell in the Unity Tool.
3. Switch delivery to Raycast.
4. Press **Create / Repair Raycast**.
5. Put the visual effect under:
   `FX — Faces Down (Place Particle Here)`.
6. In VRChat, aim at another player.
7. Wait for the local targeting crosshair.
8. Press the Spell button.
9. The target icon disappears and the spell placement activates on world
   geometry beneath that player.

## How the spell rig works

```text
Aim Origin
  └─ VRCRaycast: remote Player target
       ↓
Stories Raycast Systems
  └─ Spell Ground Placement
      ├─ Spell Target Anchor
      │   └─ Downward Ground Probe (+1.75 m)
      │       └─ VRCRaycast: Vector3.down / world only
      └─ Spell Ground Result
          └─ Spell Action
              └─ FX — Faces Down (Place Particle Here)
```

The Player-target Raycast and world-floor Raycast are shared across spells.
Each spell gets its own action/contact host under the shared ground result.
There is intentionally only **one** managed `[SoY Spell Aim Origin]` per avatar.
Running **Create / Repair** from a different hand/focus moves that shared origin
to the new selected source rather than creating another Raycast that writes the
same target parameters.

## Collision behavior

Recommended player targeting uses:
- `Hit Custom Layers`
- Player layer **9**
- excludes PlayerLocal layer **10**

If the current SDK's serialized/member naming is incompatible with the tool's
reflection compatibility layer, it falls back to `Hit Players` and logs a
warning instead of creating a non-functional custom-layer Raycast.

The downward floor Raycast uses world collision only.

## Orientation

The ground result:
- enables `Apply Rotation`,
- aligns +Y away from the contacted surface,
- places the generated VFX holder with +Z facing back toward the floor.

This lets common forward-facing particle effects be dropped into the holder
without manually solving floor orientation for each spell.

## Targeting icon

The target crosshair is:
- local-only (`IsLocal`),
- shown only on a valid hit,
- hidden while firing/casting,
- hidden while KO,
- hidden on remote avatar instances.

## Persistent target following

VRCRaycast continuously updates its result while the ray remains on the target.

If an effect must keep following a player after you stop aiming, that is a
different tracking problem. A Contact-Tracker-style system can be integrated
later, but v0.5.10 TB3 deliberately does not inject a third-party tracker or
FinalIK-based package automatically.

## Troubleshooting

### Crosshair never appears
- Confirm the project uses a VRChat Avatars SDK with `VRCRaycast`.
- Confirm the Raycast origin points along the expected **local** direction.
- Increase Maximum Aim Distance.
- Check the Unity Console for the custom-layer fallback warning.
- Select the generated VRCRaycast and enable Scene gizmos to inspect its ray.

### Spell hits player but nothing appears on the floor
- Confirm the target has world geometry underneath them.
- Inspect `SoY_SpellGround_Hit` in Play Mode.
- Confirm the second ray is world-only and points `Vector3.down`.
- Test with a normal flat floor before testing stairs/complex colliders.

### Effect points up instead of toward the floor
- Put the VFX under the generated
  `FX — Faces Down (Place Particle Here)` holder.
- Avoid rotating the generated ground result itself.
- Correct the particle's own local orientation only if that asset does not use
  conventional +Z forward.

### Existing old assets are still in global folders
That is intentional. The tool does not auto-move old generated Unity assets
because moving them can break references. New output uses the avatar workspace.

## VRCRaycast properties used by the tool

The generated rig intentionally configures the official component rather than
simulating a ray with stretched Contacts:

| VRCRaycast property | Stories OSC use |
| --- | --- |
| Raycast Direction | Local aim vector for Direct Impact; fixed `Vector3.down` for the floor probe |
| Distance | User-configured aim range; 6 m floor-probe range |
| Apply Transform Scale | Disabled so avatar transform scale does not unexpectedly multiply range |
| Collision Mode | Remote Player custom layer, Worlds, or Worlds + Players depending on mode |
| Custom Collision Layers | Player layer 9 for remote-player-only targeting |
| Result Transform | Separate generated target/ground result objects |
| Apply Rotation | Enabled on spell ground result |
| Alignment Axis | +Y away from the floor normal; downward VFX holder points back into the surface |
| Behavior On Miss | Snap To Start so stale hit positions are not left active |
| Parameter | Generates `_Hit`, `_Ratio`, and `_Distance` Animator outputs |

VRChat limits avatars to 80 `VRCRaycast` components, shared with FinalIK
components. The tool displays this budget in Preflight and Raycast Studio.

## VRLabs references

The design is compatible with the ideas behind **VRLabs Contact Tracker** and
the older **VRLabs Raycast Prefab**, but neither package is embedded or modified
by this release.

- Contact Tracker-style logic is useful when an effect must remain attached to
  a moving player after the caster stops aiming.
- The older Raycast Prefab is useful conceptual background for downward
  placement, but v0.5.10 TB3 uses VRChat's native `VRCRaycast` instead of
  automatically adding a FinalIK-based raycast dependency.


## TB2 filtered-selection correctness

Raycast action creation now resolves the selected Spell/Technick/Item from the
same filtered list displayed in the Editor. Search terms such as `Regen` can no
longer cause the Raycast gate to target one ID while the generated Contact bus
uses the same numeric popup index from an unfiltered list.
