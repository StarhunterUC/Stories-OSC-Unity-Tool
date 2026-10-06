# GitHub Release Setup — v0.5.10 TB16

The repository keeps `dist/` generated locally or by GitHub Actions; release build output is not committed.

## Verify and build locally

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
Set-Location "$HOME\OneDrive\Desktop\Github Stories of Yggdrasil\Stories-OSC-Unity-Tool-Repo"
python .\tools\verify_repo.py --repo-root .
python .\tools\build_release.py --repo-root .
```

The release assets are created under:

```text
dist/v0.5.10-TB16/
```

## Commit, push, tag, and publish

```powershell
$Repo = "$HOME\OneDrive\Desktop\Github Stories of Yggdrasil\Stories-OSC-Unity-Tool-Repo"
$Tag = "v0.5.10-TB16"
$Title = "Stories OSC Unity Tool v0.5.10 TB16"
$GitHubRepo = "StarhunterUC/Stories-OSC-Unity-Tool"

Set-Location $Repo
python .\tools\verify_repo.py --repo-root .
if ($LASTEXITCODE -ne 0) { throw "Repository verification failed." }

python .\tools\build_release.py --repo-root .
if ($LASTEXITCODE -ne 0) { throw "Release build failed." }

git status --short
git add -A
git commit -m "Release Unity Tool v0.5.10 TB16"
git push origin main

git tag -a $Tag -m $Title
git push origin $Tag

$Dist = ".\dist\$Tag"
$Notes = ".\RELEASE_NOTES_$Tag.md"

gh release create $Tag `
  "$Dist\StoriesOfYggdrasilOSCContactSystem.cs" `
  "$Dist\StoriesOfYggdrasilOSCContactSystem.cs.sha256" `
  "$Dist\Stories-OSC-Unity-Tool-$Tag.unitypackage" `
  "$Dist\Stories-OSC-Unity-Tool-$Tag.unitypackage.sha256" `
  "$Dist\Stories-OSC-Unity-Tool-$Tag.zip" `
  "$Dist\Stories-OSC-Unity-Tool-$Tag.zip.sha256" `
  "$Dist\SHA256SUMS.txt" `
  --repo $GitHubRepo `
  --title $Title `
  --notes-file $Notes `
  --prerelease `
  --verify-tag

gh release view $Tag --repo $GitHubRepo
```

TB16 is a test build/prerelease. Unity Tool users who want prerelease updates should keep the updater channel set to **Test Builds**.
