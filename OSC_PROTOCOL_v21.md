# Stories Of Yggdrasil OSC Protocol 21

Protocol 21 extends Protocol 20 without removing the existing combat/contact contract.

## Compatibility marker

- Unity Tool: v0.5.10 TB18
- Desktop minimum: v0.8.22-prebuild.1
- Marker protocol: 21
- Local marker beacon: 121 / 122
- Marker metadata remains local/unsynced and is validated from Avatar Parameter Drivers.

## Physical helpful items

The Unity Tool can author a real prop interaction for explicitly whitelisted helpful items.

Actor-side parameters:

- `SoY_HelpItemSelfTouch` — selected item touched the actor's own Head.
- `SoY_HelpItemOtherTouch` — selected item touched another humanoid Head.
- `SoY_ItemUseResult` — local Sam.py result code for success/failure presentation.

Target-side incoming bus:

- `SoY_HelpItemActive`
- `SoY_HelpItemBit0..7`
- `SoY_ItemReceiveResult`

The outgoing item bus identifies the item only. It does **not** claim the remote player's identity. Desktop/Sam.py authenticate each side and pair actor attempt + target receipt. Ambiguous pairings fail closed and consume nothing.

Result codes:

- 0 Idle
- 1 Success
- 2 No item in actor inventory
- 3 Invalid target/item
- 4 No gameplay effect
- 5 Blocked by authority/gate
- 6 Expired or ambiguous identity

## Automatic Player-to-Player source attribution

TB18 adds local attacker-side Contact Receivers to the existing Stories Attack volume:

- `SoY_PvPAttemptWeak`
- `SoY_PvPAttemptAverage`
- `SoY_PvPAttemptStrong`
- `SoY_PvPAttemptCritical`

These local receivers listen for VRChat built-in humanoid body tags and prove only that the local attack volume touched another humanoid avatar. They do not identify the target.

The target continues to report the actual received Weak/Average/Strong/Critical Contact. Sam.py API v0.8.19 pairs one authenticated attack attempt with one authenticated target receipt inside a short time window. Ambiguous matches fail closed.

## Authority

- Unity/VRChat detects physical interaction only.
- Desktop authenticates the linked actor or target.
- Sam.py remains authoritative for inventory, target state, PvP stats, armor/augments, and the DM gate.
- Protocol 20 remains supported by Desktop v0.8.22 for existing TB17.5 avatars; the new physical identity features require Protocol 21.
