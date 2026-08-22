# Stories OSC Unity Tool v0.5.10 TB2

**Test Build 2 — Filtered Action Mapping Hotfix**

TB2 is a focused correctness update on top of v0.5.10 TB1. It preserves the
new Direct Impact and Spell Ground Placement Raycast systems while repairing a
selection-index mismatch discovered during live Regen testing.

## Fixed — Regen creating Cure Contacts

When Search filtered a Spell list, the Editor popup index referred to the
**filtered** list, but Contact creation reused that same numeric index against
the **unfiltered** school list. For example:

- Search: `Regen`
- Visible selection index: `0` = Regen (ID 8)
- Raycast gate: correctly generated `Spell_8_Regen`
- Old Contact creation: incorrectly treated unfiltered index `0` as Cure (ID 1)

TB2 centralizes visible/selected action resolution so the UI, Raycast gate,
Raycast prefix, and generated Contact bus all resolve the exact same action.
Regen now generates Spell ID 8 Contact senders rather than Cure ID 1 senders.

## Same fix applied to Technicks and Items

Technick and Item search UIs used the same filtered-index pattern. TB2 fixes
those paths proactively so searching for an action cannot create Contacts for a
different unfiltered action with the same popup index.

## Raycast behavior retained from TB1

- Direct Impact for bullets/arrows/beams/projectile actions.
- Spell Ground Placement with remote-player targeting and a world-only downward
  floor probe.
- Local-only targeting icon using `IsLocal`.
- Shared Spell Aim Origin.
- Player layer 9 targeting with safe SDK fallback.
- TB3 legacy managed Raycast migration guard.
- Per-avatar generated asset workspace.

## Upgrade / repair note

If TB1 already created a Regen Raycast with Cure children, run **Create / Repair**
for Regen again after importing TB2. TB2 removes mismatched Stories-managed Spell
children inside that managed Raycast action host with Undo support, then rebuilds
the correct Regen ID 8 Contact bus. User-authored/foreign children are untouched.

## Validation

Static repository verification includes an explicit filtered-selection
consistency guard for Spells, Technicks, and Items. A full Unity/VRChat SDK
compile and Build & Test remains required before stable promotion.
