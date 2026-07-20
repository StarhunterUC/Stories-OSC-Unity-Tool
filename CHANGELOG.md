# Changelog

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
