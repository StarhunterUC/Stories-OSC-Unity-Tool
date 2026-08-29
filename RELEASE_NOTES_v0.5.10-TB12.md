# Stories OSC Unity Tool v0.5.10 TB12

## External Contact Compatibility

TB12 expands incoming compatibility with outside VRChat Contact Sender conventions while preserving the existing Stories Desktop/Sam.py transport.

### New accepted sender aliases

- `Sword` → Average hit (`SoY_HitAverage`)
- `Weapon` → Average hit (`SoY_HitAverage`)
- `Hands` → Weak hit (`SoY_HitWeak`)
- `Blockable` → block/parry compatible
- `Hit Blocked` → block/parry compatible
- `Parry_Detect` → block/parry compatible

The aliases are exact and case-sensitive.

### Alignment and authority

`Sword`, `Weapon`, and `Hands` also drive `SoY_DamageSourceEnemy` as hostile/unknown outside sources. They still travel through the existing Desktop OSC bridge and Sam.py DM Gate. TB12 does not introduce a separate damage authority path.

### Repair behavior

Create/Repair and Managed Repair can expand existing Stories-managed Weak/Average/block receivers with the compatibility aliases and consolidate redundant managed receivers without moving or replacing foreign objects.

### Existing systems retained

TB12 retains the v0.5.10 Raycast/world-drop, Technick world targeting, installed-only menu, unique animation naming, status-gauge and safe repair systems from the current test line.

### Warning about `Hands`

`Hands` is intentionally broad. While an incoming body receiver is active, casual hand contact from another avatar can be interpreted as a Weak hit. Gate combat receivers when this is undesirable.

### Validation

Repository verification checks source parity, build/version metadata, all TB12 aliases, grouped Weak/Average receivers, external alignment, block aliases, current Raycast/world-drop support and current registries. A full Unity + current VRChat Avatars SDK compile/Build & Test is still required before stable promotion.
