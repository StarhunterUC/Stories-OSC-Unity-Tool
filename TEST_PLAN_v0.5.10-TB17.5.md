# TB17.5 Test Plan — Marker Metadata Contract

## 1. Compile / header
- Confirm Unity compiles TB17.5 without errors.
- Confirm the tool header shows v0.5.10 / TB17.5.

## 2. Existing affected avatar
- Open the same avatar that still showed the compatibility-marker repair loop under TB17.4.
- Run REPAIR ANIMATOR INTEGRITY first and confirm no Broken ext PPtr errors are introduced.
- Run Audit Avatar, then Repair All Safe.

## 3. Marker construction
- Confirm no Unity "'/' is not allowed in State name" warnings are emitted during marker rebuild.
- Confirm the marker contains exactly the fixed state names `SoY Marker Beacon A`, `SoY Marker Beacon B`, or `SoY Marker INVALID`.
- Confirm state A is the default state for a valid marker.
- Confirm A transitions to B and B transitions back to A.

## 4. Marker metadata validation
- Confirm the state drivers publish the expected Unity Tool version/build, Protocol 20, schema-valid flag, and beacon values.
- Confirm `SoY_UnityToolTBRevision = 5`.
- Confirm valid state A publishes beacon 117 and state B publishes beacon 118.

## 5. Convergence
- Run Audit Avatar again after Repair All Safe.
- Confirm Safe repairs becomes 0.
- Run MIGRATE / VALIDATE AVATAR FOR PROTOCOL 20.
- Confirm Marker Layer Installed and Schema Valid.

## 6. Diagnostics / regression
- If schema remains invalid on a test copy, confirm the operation log reports the exact core-schema reason.
- Confirm Spell/Technick/Item authoring, Raycast/world drop, Evasion, Resource FX, and external Contact compatibility remain functional.
- Confirm Sam.py requires no changes.

Sam.py is not modified by TB17.5.
