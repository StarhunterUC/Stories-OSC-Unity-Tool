# GitHub release setup — v0.5.10 TB8

Repository:

```text
https://github.com/StarhunterUC/Stories-OSC-Unity-Tool
```

TB8 fixes VRChat Parent Constraint discovery/source configuration on current SDK builds while retaining TB5 World Drop behavior.

## Review first

```powershell
python tools/verify_repo.py --repo-root .
python tools/build_release.py --repo-root .
```

## Commit and tag

```powershell
git status
git add .
git commit -m "Stories OSC Unity Tool v0.5.10 TB8"
git push origin main

git tag -a v0.5.10-TB8 -m "Stories OSC Unity Tool v0.5.10 TB8"
git push origin v0.5.10-TB8
```

Expected release assets:

```text
StoriesOfYggdrasilOSCContactSystem.cs
StoriesOfYggdrasilOSCContactSystem.cs.sha256
Stories-OSC-Unity-Tool-v0.5.10-TB8.unitypackage
Stories-OSC-Unity-Tool-v0.5.10-TB8.unitypackage.sha256
Stories-OSC-Unity-Tool-v0.5.10-TB8.zip
Stories-OSC-Unity-Tool-v0.5.10-TB8.zip.sha256
SHA256SUMS.txt
```
