# Builds release plugin ZIPs (no raw DLL on GitHub — unpack into HDT Plugins folder).
param(
    [string]$Configuration = "Release",
    [string]$RepoRoot = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
    $RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
}

$releasesDir = Join-Path $RepoRoot "releases"
New-Item -ItemType Directory -Force -Path $releasesDir | Out-Null

Write-Host "Building HDT_Reconnector and HDT_BgPickAdvisor ($Configuration)..."
dotnet msbuild (Join-Path $RepoRoot "HDT_Reconnector\HDT_Reconnector.csproj") /p:Configuration=$Configuration /v:minimal
dotnet msbuild (Join-Path $RepoRoot "HDT_BgPickAdvisor\HDT_BgPickAdvisor.csproj") /p:Configuration=$Configuration /v:minimal

$reconnectorDll = Join-Path $RepoRoot "HDT_Reconnector\bin\$Configuration\HDT_Reconnector.dll"
$pickAdvisorDll = Join-Path $RepoRoot "HDT_BgPickAdvisor\bin\$Configuration\HDT_BgPickAdvisor.dll"
$metaExample = Join-Path $releasesDir "meta-api.url.example"

foreach ($path in @($reconnectorDll, $pickAdvisorDll, $metaExample)) {
    if (-not (Test-Path $path)) {
        throw "Missing build output: $path"
    }
}

Remove-Item (Join-Path $releasesDir "*.zip") -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $releasesDir "*.dll") -Force -ErrorAction SilentlyContinue

function New-PluginZip {
    param(
        [string]$ZipName,
        [hashtable[]]$Files
    )

    $staging = Join-Path ([System.IO.Path]::GetTempPath()) ("hdt-rel-" + [Guid]::NewGuid().ToString("N"))
    New-Item -ItemType Directory -Force -Path $staging | Out-Null
    try {
        foreach ($entry in $Files) {
            $dest = Join-Path $staging $entry.Name
            Copy-Item -Path $entry.Source -Destination $dest -Force
        }

        $zipPath = Join-Path $releasesDir ($ZipName + ".zip")
        if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
        Compress-Archive -Path (Join-Path $staging "*") -DestinationPath $zipPath -CompressionLevel Optimal
        Write-Host "Created $zipPath"
    }
    finally {
        Remove-Item $staging -Recurse -Force -ErrorAction SilentlyContinue
    }
}

$installReconnector = Join-Path $releasesDir "INSTALL.HDT_Reconnector.txt"
$installPickAdvisor = Join-Path $releasesDir "INSTALL.HDT_BgPickAdvisor.txt"

New-PluginZip -ZipName "HDT_Reconnector" -Files @(
    @{ Name = "HDT_Reconnector.dll"; Source = $reconnectorDll },
    @{ Name = "INSTALL.txt"; Source = $installReconnector }
)

New-PluginZip -ZipName "HDT_BgPickAdvisor" -Files @(
    @{ Name = "HDT_BgPickAdvisor.dll"; Source = $pickAdvisorDll },
    @{ Name = "meta-api.url.example"; Source = $metaExample },
    @{ Name = "INSTALL.txt"; Source = $installPickAdvisor }
)

Write-Host "Done. Release artifacts in $releasesDir"
Write-Host ""
Write-Host "Next (no CI required):"
Write-Host "  git add releases/*.zip"
Write-Host "  git commit -m ""Update release ZIPs"""
Write-Host "  git tag vX.Y.Z && git push origin main && git push origin vX.Y.Z"
Write-Host "Or download from: https://github.com/qxplays/HDT_Reconnector/raw/main/releases/<name>.zip"
