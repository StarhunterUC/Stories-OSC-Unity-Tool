# Stories Of Yggdrasil OSC Unity Tool

Current prerelease: **v0.5.9 TB3**

This repository contains the Unity Editor tool used to author and repair the
Stories Of Yggdrasil OSC Contact System for VRChat avatars.

## Install

### Built-in updater asset

Download both files from the GitHub Release:

- `StoriesOfYggdrasilOSCContactSystem.cs`
- `StoriesOfYggdrasilOSCContactSystem.cs.sha256`

The tool's updater verifies the checksum before replacing the current script.

### Manual Unity install

Import:

- `Stories-OSC-Unity-Tool-v0.5.9-TB3.unitypackage`

Or copy the canonical script to:

```text
Assets/Stories Of Yggdrasil/Editor/StoriesOfYggdrasilOSCContactSystem.cs
```

Only one copy of `StoriesOfYggdrasilOSCContactSystem` may exist inside the
Unity project's `Assets` folder.

## Safety guarantees

- Existing third-party health Animator systems are not replaced.
- Managed repairs are restricted to strict Stories-owned signatures.
- Repair transactions preserve hierarchy, transforms, constraints, prefab
  changes, and unrelated avatar systems.
- Critical attacks do not carry the `Blockable` tag.
- Weak, Average, and Strong attacks carry `Blockable`.
- Incoming hit I-Frames are one second.

See `v0.5.9-TB3` release notes and the generated contract/registry files for the
complete interface snapshot.
