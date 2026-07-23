# Changelog

## v0.5.7

- Marks `SoY_SpellType`, `SoY_TechnickType`, and `SoY_ItemType` as synchronized Int parameters so remote players can see cast/action FX.
- Keeps `SoY_SpellActive`, `SoY_TechnickActive`, `SoY_ItemActive`, and all Contact bus bits local and unsynced.
- Repairs existing Expression Parameter sync flags without exceeding VRChat's 256-bit synchronized parameter budget.
- Confirms that existing momentary Int-button FX graphs, including Clover's First Aid animation on Technick ID 42, do not require a separate CastActive parameter.
- Adds `.unitypackage` creation to the GitHub Actions release workflow.
- Keeps the canonical `.cs` release asset so the existing in-tool auto-updater continues to work.
- Adds SHA-256 files for both ZIP and UnityPackage artifacts.
- Adds release tag, source constant, and `version.json` consistency validation.

## v0.5.6

- Adds Ally/Enemy variants to melee Attack and standalone Debuff senders.
- Adds `SoY_DamageSourceEnemy` to incoming damage receivers.
- Repairs legacy Attack, Debuff, Technick, and Item alignment in one action.
- Starts I-Frames from accepted `SoY_Damaged` feedback, not raw rejected Contacts.
- Extends the action alignment FX layer to Spells, Technicks, Items, Attacks, and Debuffs.

## v0.5.5

- Adds Ally/Enemy sender pairs for Technick and Item Contacts.
- Incoming Technick and Item buses now receive caster alignment through the existing `SoY_HealingSourceEnemy` Bool.
- The `SoY_IsEnemy` toggle now switches Spell, Technick, and Item sender pairs together.
- Adds **REPAIR EXISTING TECHNICK / ITEM ALIGNMENT** for v0.5.4 senders.
- Updates incoming-bus diagnostics from 9 to 10 receivers because alignment is now included.
- Notes that OSC actions can operate independently from Sam.py encounter state.

## v0.5.4

- Added outgoing Technick and Item Contact authoring with temporary move/resize previews.
- Added `TECHNICK_ID_REGISTRY_v1.json` with 99 IDs from the current Sam.py license catalog.
- Added `ITEM_ID_REGISTRY_v1.json` with 61 current combat-usable item IDs.
- Added compact incoming Technick and Item receiver buses using one Active Bool plus eight ID bits each.
- Added Stories RP menu pages for Technick and Item ID selection.
- Added `OSC_CONTRACT_v10.json` and requires Desktop v0.8.4+.
- All action bus parameters are unsynced, so the three Spell/Technick/Item buses consume no synchronized parameter budget.
- Sam.py remains authoritative for Technick licenses, active encounters, target selection, item ownership, item usability, and consumption.

## v0.5.3

- Replaced broken per-spell Constant Int receivers with a compact eight-bit Contact bus.
- Added `SoY_SpellActive` and `SoY_SpellBit0` through `SoY_SpellBit7`.
- Every spell ID from 1-255 now uses one Active tag plus only its set bit tags.
- Reduced incoming spell identification from as many as 144 receiver components to nine spell bus receivers plus one caster-alignment receiver.
- Added a one-click repair for v0.5.1/v0.5.2 spell senders and receivers.
- Removed legacy `SoY Spell <id>` tags from repaired senders so broken Int receivers can no longer report every spell as Cure.
- Added diagnostics for legacy broken receivers and v0.5.3 spell bus coverage.
- Added `OSC_CONTRACT_v9.json`; requires Desktop v0.8.1+ for binary spell-ID reconstruction.

## v0.5.2

- Added 144 stable spell IDs across 12 Magick schools.
- Preserved all White and Black Magick IDs from v0.5.1.
- Added Green, Time, Arcane, Synergist, Illusion, Dream, Nature, Chaos, Abyssal, and Yggdrasil Light registries.
- Added Core, Specialized, and Forbidden/Custom spell-menu groups.
- Added Offensive, Healing, Revival, Cleanse, Support, Status, and Utility spell categories.
- Added category-specific Contact tags.
- Added incoming school selection with shared-ID deduplication and large-set warnings.
- Added `SPELL_ID_REGISTRY_v2.json`, `OSC_CONTRACT_v8.json`, and the unresolved-name audit.

## v0.5.1

- Added generated Stories RP submenu.
- Added RP Combat and Enemy Mode toggles.
- Added Mist Charge and Curse Of Diablos radial gauges.
- Added Curse warning states at 25%, 50%, 90%, and 98%.
- Added White and Black Magick Int spell registry and paginated menu pages.
- Added spell Contact Sender previews with Ally/Enemy alignment variants.
- Added incoming spell receivers using one `SoY_SpellType` Int.
- Added one-second incoming-hit I-Frames.
- Added GitHub Release updater with confirmation and automatic local backup.
- Preserved safe FX-copy behavior and temporary contact previews.
