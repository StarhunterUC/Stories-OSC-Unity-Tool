# GitHub release setup

Repository:

```text
https://github.com/StarhunterUC/Stories-OSC-Unity-Tool
```

Copy this update over the local cloned repository folder, then run:

```powershell
git status
git add .
git commit -m "Stories OSC Unity Tool v0.5.4"
git push origin main

git tag -a v0.5.4 -m "Stories OSC Unity Tool v0.5.4"
git push origin v0.5.4
```

The included workflow publishes:

```text
StoriesOfYggdrasilOSCContactSystem.cs
Stories_Of_Yggdrasil_OSC_Contact_System_v0.5.4.zip
Stories_Of_Yggdrasil_OSC_Contact_System_v0.5.4.zip.sha256
```

The updater only treats a published GitHub Release as an available version.
