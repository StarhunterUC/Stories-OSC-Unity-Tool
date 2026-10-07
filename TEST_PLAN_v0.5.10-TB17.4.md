# TB17.4 Test Plan — Marker Convergence Hotfix

## 1. Compile / header
- Confirm Unity compiles TB17.4 without errors.
- Confirm the tool header shows v0.5.10 / TB17.4.

## 2. Existing TB17.3 avatar
- Open the avatar that previously ended with one remaining Unity Tool Compatibility Marker repair.
- Run Audit Avatar.
- Run Repair All Safe.
- Confirm missing Stories bridge Animator/Expression parameters are restored if any are absent.
- Confirm the marker is rebuilt with Unity-safe state names.

## 3. Convergence
- Run Audit Avatar again.
- Confirm Safe repairs becomes 0.
- Confirm no repeat Unity Tool Compatibility Marker repair remains.

## 4. Protocol validation
- Run MIGRATE / VALIDATE AVATAR FOR PROTOCOL 20.
- Confirm `SoY_UnityToolTB = 17`, `SoY_UnityToolTBRevision = 4`, `SoY_ProtocolVersion = 20`.
- Confirm the compatibility card reports Marker Layer Installed and Schema Valid.
- Confirm the 117/118 marker beacon continues alternating.

## 5. Failure diagnostics
- On a controlled test copy, create a missing/wrong Stories bridge parameter.
- Confirm repair restores missing parameters where safe.
- If schema remains invalid, confirm the operation log lists the exact failing parameter or managed finding.

## 6. Integrity regression
- Confirm REPAIR ANIMATOR INTEGRITY still protects the controller.
- Confirm no `Broken ext PPtr` errors return.
- Confirm Spell/Technick/Item authoring, Raycast/world drop, Evasion, Resource FX, and external Contact compatibility remain intact.

Sam.py is not modified by TB17.4.
