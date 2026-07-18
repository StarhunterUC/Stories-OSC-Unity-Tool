# GitHub release setup

Repository:

```text
https://github.com/StarhunterUC/Stories-OSC-Unity-Tool
```

Copy the updated repository files into the local cloned repository, then run:

```powershell
git add .
git commit -m "Stories OSC Unity Tool v0.5.2"
git push origin main

git tag -a v0.5.2 -m "Stories OSC Unity Tool v0.5.2"
git push origin v0.5.2
```

The included workflow publishes:

```text
StoriesOfYggdrasilOSCContactSystem.cs
Stories_Of_Yggdrasil_OSC_Contact_System_v0.5.2.zip
Stories_Of_Yggdrasil_OSC_Contact_System_v0.5.2.zip.sha256
```

The updater only treats a published GitHub Release as an available version.
