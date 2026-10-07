# OSC Protocol 19 — Unity Avatar Marker

Protocol 19 was introduced with TB16. TB17 retains the same Desktop transport protocol and writes the current Unity Tool build marker for OSC Desktop v0.8.21+ to read from the loaded avatar.

| Parameter | Type | Expected TB17 value | Synced |
|---|---:|---:|---|
| `SoY_UnityToolPresent` | Bool | `true` | No |
| `SoY_UnityToolMajor` | Int | `0` | No |
| `SoY_UnityToolMinor` | Int | `5` | No |
| `SoY_UnityToolPatch` | Int | `10` | No |
| `SoY_UnityToolTB` | Int | `17` | No |
| `SoY_UnityToolTBRevision` | Int | `0` | No |
| `SoY_ProtocolVersion` | Int | `19` | No |
| `SoY_UnitySchemaValid` | Bool | `true` after validation | No |

`SoY_UnitySchemaValid` must not be considered current merely because the parameter exists. TB17 rebuilds the marker layer after auditing the current managed schema and publishes false when recognized outdated/broken managed systems remain.

The marker is intentionally separate from the contact compatibility revision. TB17 still accepts the revision-17 external Contact aliases while protocol 19 governs Stories-generated avatar compatibility with Desktop.
