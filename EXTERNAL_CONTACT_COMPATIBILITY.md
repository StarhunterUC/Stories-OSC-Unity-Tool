# External Contact Compatibility — TB12

All tags are exact and case-sensitive.

| External sender tag | Result |
|---|---|
| `Sword` | `SoY_HitAverage` |
| `Weapon` | `SoY_HitAverage` |
| `Hands` | `SoY_HitWeak` |
| `Blockable` | accepted by active Stories block/parry receiver |
| `Hit Blocked` | accepted by active Stories block/parry receiver |
| `Parry_Detect` | accepted by active Stories block/parry receiver |

`Sword`, `Weapon`, and `Hands` also contribute to `SoY_DamageSourceEnemy` so outside hits that lack Stories caster alignment are treated as hostile/unknown by the normal Desktop/Sam.py path.

The canonical Stories sender names remain unchanged:

- `Hit By Weak Attack`
- `Hit By Average Attack`
- `Hit By Strong Attack`
- `Hit By Critical Attack`
- `Blockable`
- `Hit Blocked`

`Hands` may be present during non-combat avatar interaction. If passive touching should not count as damage, keep the incoming combat receiver gated off outside combat.
