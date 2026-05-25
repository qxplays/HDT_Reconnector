# Release archives

ZIP files here are **committed to git** (no CI required).

## Update for a new version

```powershell
.\scripts\Package-Release.ps1
git add releases\*.zip
git commit -m "Update release ZIPs"
git push origin main
```

Optional GitHub Release (if Actions enabled, or upload ZIPs manually on github.com → Releases):

```text
git tag v1.3.1
git push origin v1.3.1
```

## Direct download (always works from `main`)

- [HDT_Reconnector.zip](https://github.com/qxplays/HDT_Reconnector/raw/main/releases/HDT_Reconnector.zip)
- [HDT_BgPickAdvisor.zip](https://github.com/qxplays/HDT_Reconnector/raw/main/releases/HDT_BgPickAdvisor.zip)
