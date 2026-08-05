# Stories OSC Unity Tool v0.5.9 TB3

**Tag:** `v0.5.9-TB3`  
**Channel:** Test Builds / prerelease  
**Canonical updater asset:** `StoriesOfYggdrasilOSCContactSystem.cs`

## Primary TB3 fix

The managed-system audit now distinguishes retired numeric spell receiver tags
such as `SoY Spell 24` from the current compact spell bus:

- `SoY Spell Active`
- `SoY Spell Bit 0` through `SoY Spell Bit 7`

Only numeric suffixes are classified as legacy. This prevents current spell-bus
receivers from being incorrectly reported or migrated as obsolete.

## Current v0.5.9 tool capabilities included

- Self-healing audit and repair center for Stories-managed contacts.
- Transactional repair snapshots and Unity Undo rollback.
- Preservation of parent, sibling order, local/world transforms, active state,
  constraints, and prefab modifications.
- Safe FX-copy workflow that leaves third-party health systems untouched.
- Official VRChat raycast delivery support.
- Contact, weapon-root, and VRC Parent Constraint attachment modes.
- Spell, Technick, and Item compact incoming buses.
- One-second incoming-hit I-Frames.
- Health, action-animation, menu, accessibility, diagnostics, backup, and
  updater tooling.
- SHA-256-verified GitHub updater installation.

## Release assets required by the built-in updater

- `StoriesOfYggdrasilOSCContactSystem.cs`
- `StoriesOfYggdrasilOSCContactSystem.cs.sha256`

The `.zip` and `.unitypackage` assets are supplied for manual installation.
The release must remain marked as a **prerelease** so it appears on the tool's
**Test Builds** update channel rather than Stable.

## Registry snapshot

- 144 unique Spell IDs from 151 school/category rows
- 99 Technicks
- 61 Items

## Validation performed for this upload package

- Version, build, repository, and updater constants checked.
- C# directive and lexical delimiter balance checked.
- Spell, Technick, and Item registry continuity checked.
- Current spell-bus legacy-classification guard checked.
- Updater `.cs`, `.zip`, and `.sha256` handling checked.
- Release assets and repository files SHA-256 manifested.

A full Unity/VRChat SDK compile was not run in this environment. Import the
generated `.unitypackage` into a clean Unity test project before promoting the
build beyond prerelease.
