# GitHub release setup

Repository:

```text
https://github.com/StarhunterUC/Stories-OSC-Unity-Tool
```

Upload this repository package to the repository root and push it. Then create and push a version tag:

```powershell
git add .
git commit -m "Stories OSC Unity Tool v0.5.1"
git push origin main
git tag -a v0.5.1 -m "Stories OSC Unity Tool v0.5.1"
git push origin v0.5.1
```

The included workflow publishes these updater-compatible assets:

```text
StoriesOfYggdrasilOSCContactSystem.cs
Stories_Of_Yggdrasil_OSC_Contact_System_v0.5.1.zip
Stories_Of_Yggdrasil_OSC_Contact_System_v0.5.1.zip.sha256
```

The repository currently needs a published Release; the updater does not treat an ordinary source commit as a release.
