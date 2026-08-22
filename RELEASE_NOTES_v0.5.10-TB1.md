# Stories OSC Unity Tool v0.5.10 TB1

**Test Build 1 — Raycast Spell Placement & Workspace Cleanup**

This test build starts from the v0.5.9 TB3 source line and focuses on the
reported Raycast failures, spell-placement authoring, generated asset
organization, and menu/help usability.

## Raycast Studio

### Direct Impact

Use Direct Impact for bullets, arrows, beams, projectile-like Technicks/Items,
and debuffs. The result transform receives the contact payload at the first
configured player/world collision.

Remote-player targeting prefers a custom collision mask containing only
VRChat's **Player** layer. This excludes the local avatar. If the installed SDK
does not expose the expected custom-layer member, the tool falls back to
`Hit Players` and emits a visible warning instead of leaving the Raycast in a
custom mode with an empty/unknown mask.

Raycast settings now write through reflected SDK members first and fall back to
Unity serialized properties when those member names differ between SDK revisions.

### Spell Ground Placement

Spell delivery now uses a shared two-stage Raycast rig:

1. Aim from the selected hand/focus/weapon toward a **remote player**.
2. A local-only targeting crosshair appears at the valid player hit.
3. Press the selected Spell button.
4. The crosshair hides immediately.
5. A second Raycast starts above the tracked player hit and casts
   **straight down** into world geometry.
6. The spell Contact/VFX action is enabled at that ground result while the
   selected spell action is active.

The ground result enables rotation and aligns local +Y with the world surface
normal. Place conventional +Z-facing particles/effects beneath:

```text
FX — Faces Down (Place Particle Here)
```

The generated holder rotates the effect into the floor direction while retaining
the floor-normal orientation.

The two shared target/ground rays are reused across spell definitions to avoid
burning two VRCRaycast components per spell. **Create / Repair** also enforces a
single managed Spell Aim Origin per avatar. If an earlier TB1 repair left
multiple managed aim origins, extras are disabled with Undo support; selecting a
different hand/focus moves the shared origin instead of creating competing rays
that write the same `SoY_SpellTarget_*` parameters.

## Local targeting

The generated crosshair is controlled through VRChat's built-in `IsLocal`
Animator parameter. Other players do not see the targeting helper.

It is hidden when:
- the cast/fire action is active,
- the Raycast no longer has a valid target,
- the wearer is KO,
- or the avatar instance is not local.

## Generated avatar workspace

New generated content is now stored under the model/avatar name:

```text
Assets/Stories Of Yggdrasil/<Avatar>/
├─ FX/
├─ Menus/
├─ Animations/
├─ Profiles/
└─ Backups/
```

Older generated locations are recognized but are not moved automatically.

## Menu cleanup

The generated root menu now reads:

- Combat
- Spells
- Actions
- Targeting
- Status
- Quick Access (optional)

`Targeting` contains the projectile fire action used by direct-impact
Attack/Debuff Raycasts. Spell Raycasts use the selected Spell button itself as
the cast action.

## Moving-target effects

This build intentionally does not auto-install a persistent third-party target
tracker. VRCRaycast follows the current hit while aiming. Effects that must stay
attached to a player after aim is lost are treated as a separate tracker system
so the Unity Tool does not silently inject foreign dependencies.

## Upgrade notes

- Existing v0.5.9 TB3 managed objects remain repairable.
- Existing generated asset paths are left in place.
- Original FX controllers remain untouched.
- The canonical root source and Unity `Assets/.../Editor` source are identical.
- Requires a VRChat Avatars SDK version that includes `VRCRaycast`
  (SDK 3.10.3 or newer).

## Validation limitation

This repository is statically audited and release-packaged here, but a full
Unity/VRC SDK compile and in-client Build & Test must be performed in an actual
Unity project before treating TB1 as stable.
