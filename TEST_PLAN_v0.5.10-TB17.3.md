# TB17.3 Test Plan — Marker State-Name Hotfix

## 1. Compile / header
- Confirm Unity compiles TB17.3 without errors.
- Confirm the tool header shows v0.5.10 / TB17.3.

## 2. Existing TB17.2 controller
- Open the same avatar that showed the TB17.2 marker loop.
- Confirm REPAIR ANIMATOR INTEGRITY still reports a healthy graph or safely removes only unreachable transitions.
- Confirm no `Broken ext PPtr` errors return.

## 3. Marker repair
- Run Audit Avatar.
- Confirm the Unity Tool Compatibility Marker is the only repair if the rest of the avatar is healthy.
- Run Repair All Safe.
- Confirm Unity emits no "'/' is not allowed in State name" warnings.
- Confirm marker states are created with Unity-safe names using ` - ` separators.
- Confirm a second audit reports zero safe repairs.

## 4. Protocol validation
- Run MIGRATE / VALIDATE AVATAR FOR PROTOCOL 20.
- Confirm `SoY_UnityToolTB = 17`, `SoY_UnityToolTBRevision = 3`, `SoY_ProtocolVersion = 20`, and `SoY_UnitySchemaValid = true`.
- Confirm the marker panel reports Installed / Valid.
- Confirm the 117/118 local beacon continues alternating.

## 5. Repair convergence reporting
- Force a test repairable condition that cannot be resolved in one transaction.
- Confirm the tool reports the remaining finding explicitly instead of saying repair completed cleanly.

## 6. Regression
- Verify Spell/Technick/Item authoring, Raycast gates/world drop, Evasion, Resource FX, and external Contact compatibility remain functional.

Sam.py is not modified by TB17.3.
