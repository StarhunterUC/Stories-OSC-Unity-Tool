# GitHub release setup

Repository:

```text
https://github.com/StarhunterUC/Stories-OSC-Unity-Tool
```

Copy this overlay into the local `Stories-OSC-Unity-Tool-Repo` clone, then run:

```powershell
git status
git add .
git commit -m "Stories OSC Unity Tool v0.5.7"
git push origin main

git tag -a v0.5.7 -m "Stories OSC Unity Tool v0.5.7"
git push origin v0.5.7
```

The tag workflow validates that all three versions agree:

```text
Git tag
StoriesOfYggdrasilOSCContactSystem.cs
version.json
```

It then publishes:

```text
StoriesOfYggdrasilOSCContactSystem.cs
Stories_Of_Yggdrasil_OSC_Contact_System_v0.5.7.unitypackage
Stories_Of_Yggdrasil_OSC_Contact_System_v0.5.7.unitypackage.sha256
Stories_Of_Yggdrasil_OSC_Contact_System_v0.5.7.zip
Stories_Of_Yggdrasil_OSC_Contact_System_v0.5.7.zip.sha256
```

The `.unitypackage` is for normal installation. The standalone `.cs` file is deliberately retained as the auto-updater payload.
