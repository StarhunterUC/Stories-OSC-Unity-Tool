# Stories Of Yggdrasil OSC Contact System — Unity Tool v0.5.7

## Install

Download and import:

```text
Stories_Of_Yggdrasil_OSC_Contact_System_v0.5.7.unitypackage
```

The package installs the Editor tool at:

```text
Assets/Stories Of Yggdrasil/Editor/StoriesOfYggdrasilOSCContactSystem.cs
```

Open:

```text
Tools → Stories Of Yggdrasil → OSC Contact System
```

Assign the Avatar Descriptor, press **Load From Avatar**, then press **PREPARE AVATAR FOR STORIES OSC**. Animator changes are applied to a copied FX controller under:

```text
Assets/Stories Of Yggdrasil/FX/
```

The original FX controller is not edited.

## v0.5.7 remote spell, Technick, and item animations

The generated action selector parameters are now synchronized:

```text
SoY_SpellType
SoY_TechnickType
SoY_ItemType
```

These are Int parameters used by the generated Expressions Menu buttons and by avatar-specific FX transitions. Synchronizing them allows remote players to see the same cast or action animation.

The incoming Contact buses remain local and unsynced:

```text
SoY_SpellActive + SoY_SpellBit0-7
SoY_TechnickActive + SoY_TechnickBit0-7
SoY_ItemActive + SoY_ItemBit0-7
```

Those parameters are target-local collision telemetry for the Desktop bridge. They are intentionally not rebroadcast.

Existing avatars can be repaired by selecting their assigned Expression Parameters asset and pressing:

```text
Add Missing SoY Expression Parameters
```

or:

```text
INSTALL / REPAIR ALL MISSING OSC HOOKS
```

The repair checks the 256-bit synchronized parameter budget before enabling the three selector Ints.

## Clover / First Aid example

Clover's FX controller already uses:

```text
Healing END → Healing 1
SoY_TechnickType Equals 42
```

and returns through the animation chain after the menu button releases and the Int stops equaling `42`. No separate `SoY_CastActive` parameter is required for that controller. The selector only needed to be marked Synced.

## Technick and item Contacts

Each action uses a stable registry ID and a compact binary Contact pattern:

```text
SoY Technick Active + SoY Technick Bit 0-7
SoY Item Active      + SoY Item Bit 0-7
```

Sam.py remains authoritative for Technick licenses, encounter rules, target selection, item ownership, item usability, and consumption.

## Release files

Every tagged release now publishes:

```text
StoriesOfYggdrasilOSCContactSystem.cs
Stories_Of_Yggdrasil_OSC_Contact_System_vX.Y.Z.unitypackage
Stories_Of_Yggdrasil_OSC_Contact_System_vX.Y.Z.unitypackage.sha256
Stories_Of_Yggdrasil_OSC_Contact_System_vX.Y.Z.zip
Stories_Of_Yggdrasil_OSC_Contact_System_vX.Y.Z.zip.sha256
```

The `.unitypackage` is the normal manual installation path. The canonical `.cs` release asset remains present for the in-tool auto-updater.

## Auto-updater

The updater checks the latest published release from:

```text
StarhunterUC/Stories-OSC-Unity-Tool
```

It never updates silently. It asks first, backs up the current script under:

```text
Assets/Stories Of Yggdrasil/Backups/Unity Tool/
```

and then replaces the current Editor script with the canonical `.cs` release asset before Unity recompiles.
