# TB17 Test Plan — Automated Action Authoring

Use a copy of an avatar/project. Do not keep an older StoriesOfYggdrasilOSCContactSystem.cs in the project alongside TB17.

## 1. Compile / header

- Replace the older Unity Tool source with TB17.
- Confirm Unity reports zero compiler errors.
- Confirm the window header reports `v0.5.10 • TB17`.
- Confirm no unused-field warning was introduced by the TB17 automation UI.

## 2. Existing TB16 avatar migration

Use an avatar that already contains managed Spell/Technick/Item Contacts and TB16 animation profile data.

- Open Avatar Setup and migrate/validate.
- Press `AUTOMATE / REPAIR ALL INSTALLED ACTIONS` in Animation Setup.
- Confirm existing managed actions are discovered automatically.
- Confirm empty TB16 generated `__Cast.anim` placeholders no longer appear as required per-action overrides.
- If a generated TB16 Cast clip was manually edited and contains curves, confirm TB17 preserves it.

## 3. Spell with no presentation clip

- Create/repair one normal Spell Contact.
- Do not assign any Spell animation.
- Confirm TB17 automatically creates the Spell profile binding and functional gate.
- Confirm the Spell presentation state uses `SOY_Spell_<ID>_AutoPresentation.anim`.
- Confirm the action Contact becomes active only during the configured contact window.
- Confirm releasing the selector enters recovery and the Spell cannot immediately retrigger.

## 4. Managed visual holder

- Confirm the managed Spell host contains `FX — Spell Visuals (Place Here)`.
- Put a temporary visible object under this holder.
- Trigger the Spell.
- Confirm the visual follows the parent action's generated Active window without manually authoring an animation curve.
- Repeat for Technick and Item.

## 5. Shared Spell presentation

- Assign one clip to `Shared Spell Cast`.
- Leave per-spell override blank.
- Rebuild.
- Confirm all installed Spells without a more specific preset use the shared clip while their functional Contact timing remains independent.

## 6. Spell school preset

- Assign a Black Magick school clip and a different shared Spell default.
- Use an installed Black Magick spell.
- Confirm the school preset wins over the shared default.

## 7. Spell category preset

- Remove the applicable school preset.
- Assign a Healing or Damage category clip.
- Confirm the category preset wins over the shared default.

## 8. Per-spell override

- Assign a unique per-spell override.
- Confirm it wins over school, category, and shared defaults.
- Clear it and confirm resolution falls back automatically.

## 9. Technick automation

- Create/repair multiple Technick Contacts.
- Leave all per-Technick clips blank.
- Confirm they are auto-discovered and build with generated timer presentation.
- Assign one shared Technick animation and confirm every non-overridden Technick uses it.
- Confirm default recovery remains 15 seconds unless changed.

## 10. Item automation

- Repeat the Technick test for Items.
- Confirm default recovery remains 5 seconds unless changed.

## 11. Approval ownership

While in Play Mode, watch Animator parameters:

- Trigger a normal Spell Contact action.
- Confirm `SoY_SpellApproved` is driven by the Spell Contact Gate active window.
- Confirm changing/removing the presentation animation does not independently toggle `SoY_SpellApproved`.
- Repeat for Technick and Item.

## 12. Raycast direct impact

- Build a selector-driven Raycast Spell/Technick/Item.
- Confirm TB17 does not require or inject an empty `__Cast.anim` into the action profile.
- Confirm Raycast Ready → Armed → Cast → Wait For Release → Recovery still works.
- Confirm `SoY_RaycastApproved` and the category approved pulse occur only in the approved Cast state.

## 13. Spell world drop

- Build/repair a ground-placement Spell.
- Confirm the generated WorldDrop clips still automatically animate action Active state and `FreezeToWorld`.
- Confirm the placed effect resets on release and remains hidden during recovery.

## 14. Generic evade fallback

- Assign only `Generic Evade` (ID 9).
- Enable `Use Generic Evade for every unassigned direction/roll`.
- Rebuild Evasion.
- Confirm IDs 1–8 are present and use the Generic clip where no directional override exists.
- Add a custom Left Evade clip and confirm only that direction overrides Generic.

## 15. No presentation clips at all

- Clear all shared, school, category, and per-action presentation clips.
- Rebuild all installed actions.
- Confirm functional Spell/Technick/Item Contacts and Raycasts remain operational.

## 16. KO / interruption

- Begin an action, then set KO.
- Confirm the presentation state exits toward Wait/Recovery as designed.
- Confirm no new action can enter from Ready while KO is true.
- Confirm selector changes cannot jump directly from one active action into another.

## 17. Protocol marker

- Migrate/validate the avatar.
- Confirm the Unity marker reports Tool `v0.5.10 TB17`, Protocol `19`, and valid schema after the audit passes.
- Desktop v0.8.21-prebuild.2-prebuild.2 should accept Protocol 20.

## Known boundary

The Unity-local recovery state can still be reset by resetting/reloading the avatar. Persistent authoritative cooldown enforcement remains a later Desktop/Sam.py responsibility. Sam.py is not modified by TB17.

## Protocol 20 alignment migration test

1. Audit an avatar authored under TB16/Protocol 19 whose `SoY_DamageSourceEnemy` receiver contains `SoY Caster Enemy`, `Sword`, `Weapon`, and/or `Hands`.
2. Confirm the audit reports **Canonical Stories Enemy Alignment** as Repairable.
3. Run **Safe Repair All**. Confirm the original receiver is preserved but now contains only `SoY Caster Enemy`.
4. Confirm a second receiver exists on the same incoming-damage host: `SoY_ExternalDamageSource` with exactly `Sword`, `Weapon`, and `Hands`.
5. Repeat using **Repair Existing Action Alignment** on a rollback/copy and confirm it performs the same split.
6. Run Migrate / Validate. Confirm the avatar publishes Protocol 20 and Schema Valid.
7. With Desktop 0.8.21-prebuild.2, Friendly Stories attacks must not become NPC/unattributed merely because `Sword`, `Weapon`, or `Hands` is present.
8. External Sword/Weapon/Hands compatibility should still be treated as external/unknown hostile input and require normal attribution.
