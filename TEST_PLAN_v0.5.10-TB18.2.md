# TB18 Test Plan — Protocol 21

## Compile / migration
- Confirm Unity compiles with the current VRChat Avatars SDK.
- Confirm header reports TB18 / Protocol 21.
- Migrate/Validate an avatar and confirm the 121/122 beacon.
- Confirm Animator Integrity remains healthy.

## Helpful item authoring
- Select Potion or another whitelisted helpful item.
- Assign a prop beneath the avatar root.
- Choose gesture hand, Grab gesture, Use gesture, Self/Others.
- Build the physical interaction.
- Confirm FaceEmo assets and gesture expression assets are unchanged.
- Confirm Grab toggles prop on, releasing Grab keeps it visible, and a second Grab toggles it off.
- Confirm Use enables contacts only during the Use gesture.
- Confirm Self Head contact produces `SoY_HelpItemSelfTouch`.
- Confirm other-player Head contact produces actor attempt + target item bus.
- Confirm full HP/no-effect and no-inventory failures do not consume items.
- Confirm success consumes exactly one actor inventory item.

## Automatic PvP attribution
- Build a Weak/Average/Strong/Critical Attack contact.
- Confirm the same attack volume contains a local-only remote-body receiver.
- Hit one Protocol 21 target and confirm automatic actor/target attribution.
- Confirm Sam.py applies actor stats against target defenses/armor/augments.
- Confirm DM gate blocks damage with no HP mutation when inactive.
- Create two simultaneous same-tier attackers and confirm ambiguity fails closed.
- Confirm Protocol 20 avatars still use the legacy/manual attribution path.

## Regression
- Spell/Technick/Item action authoring.
- Raycast/world drop.
- Evasion.
- Resource FX.
- External Sword/Weapon/Hands compatibility.
- TB17.5 Animator repair protections.

## TB18.2: Incoming native/external separation

- Confirm FX/Expression parameters include unsynced `SoY_ExternalHitWeak` and `SoY_ExternalHitAverage`.
- Canonical Weak/Average receivers contain only `Hit By Weak Attack` / `Hit By Average Attack` respectively.
- `Sword` and `Weapon` are routed exclusively to `SoY_ExternalHitAverage`; `Hands` to `SoY_ExternalHitWeak`.
- A Strong sender also carrying `Sword` must not emit a canonical Average hit.
- TB18.1 avatar Safe Repair All converts mixed receiver without changing geometry, parent or foreign Contact components.
- Desktop prebuild.7+ receives external provenance as external; .6 clients are not compatible with external-only hits.
- Single- and multi-attacker tests: Sam.py still rejects genuinely ambiguous candidate matches.
- Confirm spell buses, raycast, helpful items, avatar marker, Animator integrity and preexisting menu functionality remain unaffected.
