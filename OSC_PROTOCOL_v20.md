# OSC Protocol 20 — Split Canonical / External Damage Alignment

Protocol 20 is introduced with TB17 to prevent legacy compatibility aliases from poisoning canonical Stories alignment.

TB16/Protocol 19 allowed the incoming `SoY_DamageSourceEnemy` receiver to listen to `SoY Caster Enemy` together with `Sword`, `Weapon`, and `Hands`. On avatars that carried one of those external tags, a Friendly Stories attack could be misclassified as Enemy/NPC.

Protocol 20 separates those concerns:

| Parameter | Type | Meaning | Synced |
| --- | --- | --- | --- |
| `SoY_DamageSourceEnemy` | Bool | Canonical Stories damage/debuff alignment. Driven only by `SoY Caster Enemy`. | No |
| `SoY_ExternalDamageSource` | Bool | Legacy/external compatibility source. Driven only by `Sword`, `Weapon`, and `Hands`. | No |
| `SoY_ProtocolVersion` | Int | `20` | No |
| `SoY_UnitySchemaValid` | Bool | True only after the current managed schema validates. | No |

The remaining Unity Tool version marker fields are unchanged from Protocol 19.

## Migration

TB17 repair/migration detects an older mixed `SoY_DamageSourceEnemy` receiver and repairs it in place. The original receiver keeps its GameObject, transform, and volume geometry but is restricted to `SoY Caster Enemy`. A second receiver using the same geometry is created for `SoY_ExternalDamageSource` with `Sword / Weapon / Hands`.

Both **Safe Repair All** and **Repair Existing Action Alignment** perform this split. A schema is not marked valid until the mixed legacy receiver is gone.

## Desktop requirement

Stories-generated gameplay Contacts require Desktop `0.8.21-prebuild.2` or newer during TB17 testing. The Desktop distinguishes canonical Stories alignment from external/unknown compatibility Contacts and fails closed on older Unity protocols.
