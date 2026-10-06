```text
Stories Of Yggdrasil OSC Unity Tool v0.5.10 TB16
Desktop v0.8.21 / OSC Protocol 19
TEST PLAN
======================================================================

Sam.py is NOT changed by this build.

INSTALL
-------
1. Remove/replace the previous StoriesOfYggdrasilOSCContactSystem_TB15.cs.
2. Put StoriesOfYggdrasilOSCContactSystem_TB16.cs in the same Unity Editor folder.
3. Let Unity fully recompile before opening the tool.
4. Use a COPY of a test avatar first.

A. UNITY COMPILE / CONTEXT
--------------------------
[ ] Unity compiles with zero C# errors.
[ ] Existing TB15.1 Avatar Context restores correctly.
[ ] Avatar Descriptor / FX / Parameters / Menu / Avatar Root remain populated.

B. PROTOCOL 19 MIGRATION / MARKER
---------------------------------
1. Open Avatar Setup.
2. Run "MIGRATE / VALIDATE AVATAR FOR PROTOCOL 19".
3. Allow the managed repair snapshot/repair if the tool finds old SoY contacts.

Expected Expression Parameters (local/unsynced marker fields):
  SoY_UnityToolPresent
  SoY_UnityToolMajor
  SoY_UnityToolMinor
  SoY_UnityToolPatch
  SoY_UnityToolTB
  SoY_UnityToolTBRevision
  SoY_ProtocolVersion
  SoY_UnitySchemaValid

Expected FX layer:
  Stories Of Yggdrasil | Unity Tool Marker

Expected values in Play/VRChat:
  SoY_UnityToolPresent   = true
  SoY_UnityToolMajor     = 0
  SoY_UnityToolMinor     = 5
  SoY_UnityToolPatch     = 10
  SoY_UnityToolTB        = 16
  SoY_UnityToolTBRevision= 0
  SoY_ProtocolVersion    = 19
  SoY_UnitySchemaValid   = true

If the tool still finds broken/outdated managed systems, SchemaValid must remain false.

C. DESKTOP v0.8.21 MARKER REPORTING
------------------------------------
With the migrated TB16 avatar loaded:
[ ] Connection page reports: Unity Tool v0.5.10 TB16.
[ ] Protocol reports 19.
[ ] Schema reports valid / Supported.
[ ] Normal SoY hit/action OSC reaches the existing controller.

D. LEGACY FAIL-CLOSED TEST
--------------------------
Use a COPY of an avatar that has old SoY-generated contacts and has NOT been migrated.
[ ] Desktop reports AVATAR UPDATE REQUIRED when direct SoY gameplay input arrives.
[ ] SoY_HitWeak / SoY_HitAverage / action selectors are ignored.
[ ] No old direct SoY contact causes damage/action handling.
[ ] Recent Activity logs the update/migration reason once rather than spamming.

E. EXTERNAL COMPATIBILITY REGRESSION
------------------------------------
Use an avatar that only supplies external-compatible parameters.
Examples:
  Health
  Hit By Weak Attack T0
  Hit By Average Attack T0
  Hit Blocked
[ ] These continue to reach the compatibility path without a TB16 marker.
[ ] Desktop output parameters such as SoY_HPPercent do NOT falsely mark the avatar as legacy.

F. FUTURE / INVALID PROTOCOL SAFETY
-----------------------------------
Optional manual OSC tests:
[ ] Protocol 18 -> Avatar Update Required.
[ ] Protocol 20 -> Desktop Update Required.
[ ] Protocol 19 + SoY_UnitySchemaValid=false -> Migration / validation required.

G. EVASION AUTHORING
--------------------
Avatar FX -> Evasion Animations
Assign one or more clips:
  1 Evade Forward
  2 Evade Backward
  3 Evade Left
  4 Evade Right
  5 Roll Forward
  6 Roll Backward
  7 Roll Left
  8 Roll Right
  9 Generic Evade

Build/repair the Evasion Animation Layer.
Expected layer:
  Stories Of Yggdrasil | Evasion Animations

Expected parameters:
  SoY_EvadeType (Int)
  SoY_Evading (Bool)

Test:
[ ] Selector 0 = Ready / Idle.
[ ] Each installed selector plays the correct clip.
[ ] Another selector cannot interrupt the active evade clip.
[ ] State goes to Wait For Release after the clip.
[ ] Returning SoY_EvadeType to 0 returns to Ready.
[ ] SoY_Evading is true only while the evade animation is active.
[ ] KO prevents entering a new evade.
[ ] SoY_Evading does NOT itself provide gameplay invulnerability.

H. HP / MP FLOAT REGRESSION
---------------------------
[ ] Existing Resource FX still uses normalized SoY_HPPercent 0..1.
[ ] Existing Resource FX still uses normalized SoY_MPPercent 0..1.
[ ] Existing Simple1D blend points still interpolate correctly.
[ ] Existing TB15 action gates still build and animate correctly.

I. REPORTING A FAILURE
----------------------
For Unity compile failures send:
- FIRST Console error in full
- file + line number

For Animator failures send:
- screenshot of the affected Animator layer
- Parameters panel
- selected transition/state Inspector
- which SoY parameter/value was active

For Desktop compatibility failures send:
- Connection page screenshot
- Diagnostics page screenshot
- Recent Activity entries around avatar load
- whether the avatar was migrated with TB16
```
