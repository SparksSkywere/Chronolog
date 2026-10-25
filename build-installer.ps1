# Builds Chronolog installers for both editions:
#   - Chronolog-Modern-Setup-<version>.exe
#   - Chronolog-Classic-Setup-<version>.exe
#
# Requires: .NET SDK and Inno Setup 6 (ISCC.exe).
# Version is read from src\Chronolog\Chronolog.csproj.
# Publish output is staged under %LOCALAPPDATA%\ChronologBuild, then copied
# to artifacts\payload and dist\ (this repo lives on a cloud drive).

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $Root

$AppProj = Join-Path $Root "src\Chronolog\Chronolog.csproj"
$ClassicProj = Join-Path $Root "src\Chronolog\Chronolog.Classic.csproj"
$IssPath = Join-Path $Root "installer\Chronolog.iss"
$IconSrc = Join-Path $Root "assets\installer.ico"
$AppIcon = Join-Path $Root "src\Chronolog\Assets\chronolog.ico"
$PayloadRoot = Join-Path $Root "artifacts\payload"
$DistDir = Join-Path $Root "dist"
$InstallerCopy = Join-Path $Root "installer"

$csprojText = Get-Content $AppProj -Raw
if ($csprojText -match '<Version>([^<]+)</Version>') {
    $Version = $Matches[1].Trim()
} else {
    $Version = "1.0.0"
}

$Configuration  = "Release"
$LocalRoot      = Join-Path $env:LOCALAPPDATA "ChronologBuild"
$PublishRoot    = Join-Path $LocalRoot "publish"
$InstallerLocal = Join-Path $LocalRoot "installer"
$LogsDir        = Join-Path $LocalRoot "logs"

function Write-Step {
    param([string]$Msg)
    Write-Host ""
    Write-Host ("==> " + $Msg) -ForegroundColor Cyan
}

function Find-Iscc {
    $candidates = @(
        (Join-Path $env:LocalAppData "Programs\Inno Setup 6\ISCC.exe"),
        (Join-Path ${env:ProgramFiles(x86)} "Inno Setup 6\ISCC.exe"),
        (Join-Path $env:ProgramFiles "Inno Setup 6\ISCC.exe")
    )
    foreach ($c in $candidates) {
        if ($c -and (Test-Path $c)) { return $c }
    }
    $cmd = Get-Command iscc -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    return $null
}

function Copy-IfPossible {
    param([string]$Src, [string]$Dest)
    try {
        New-Item -ItemType Directory -Force -Path $Dest | Out-Null
        Copy-Item -Path (Join-Path $Src "*") -Destination $Dest -Recurse -Force -ErrorAction Stop
        return $true
    } catch {
        Write-Warning ("Could not copy to " + $Dest + " : " + $_)
        return $false
    }
}

$skus = @(
    @{
        Id            = "modern"
        Name          = "Chronolog Modern"
        Project       = $AppProj
        SetupBase     = ("Chronolog-Modern-Setup-" + $Version)
        SelfContained = $true
        Runtime       = "win-x64"
        MinVersion    = "10.0"
        ArchAllowed   = "x64compatible"
        Arch64        = "x64compatible"
        RequireNet48  = "0"
        AppId         = "{{A8E2C1D4-9F3B-4E6A-8C1D-2B7F0A5E9D31}"
        OsLabel       = "Windows 10 and Windows 11 (x64)"
    },
    @{
        Id            = "classic"
        Name          = "Chronolog Classic"
        Project       = $ClassicProj
        SetupBase     = ("Chronolog-Classic-Setup-" + $Version)
        SelfContained = $false
        Runtime       = ""
        MinVersion    = "6.1sp1"
        ArchAllowed   = "x86compatible x64compatible"
        Arch64        = "x64compatible"
        RequireNet48  = "1"
        AppId         = "{{B7F3D2E5-0A4C-5F7B-9D2E-3C801B6F0E42}"
        OsLabel       = "Windows 7 SP1, 8, 8.1 (needs .NET Framework 4.8)"
    }
)

Write-Host "=== Chronolog installers (Modern + Classic) ===" -ForegroundColor Cyan
Write-Host ("Root:      " + $Root)
Write-Host ("Version:   " + $Version)
Write-Host ("Local:     " + $LocalRoot)
Write-Host "Publisher: Skywere Industries"

if (-not (Test-Path $IconSrc)) {
    throw "Production icon missing: $IconSrc"
}

New-Item -ItemType Directory -Force -Path (Split-Path $AppIcon) | Out-Null
Copy-Item $IconSrc $AppIcon -Force
Write-Host ("Icon synced: assets\installer.ico -> src\Chronolog\Assets\chronolog.ico") -ForegroundColor Green

$iscc = Find-Iscc
if (-not $iscc) {
    throw "Inno Setup (ISCC.exe) not found. Install JRSoftware.InnoSetup via winget."
}
Write-Host ("ISCC: " + $iscc)

New-Item -ItemType Directory -Force -Path $LocalRoot, $PublishRoot, $InstallerLocal, $LogsDir, $PayloadRoot, $DistDir | Out-Null

$results = @()
foreach ($s in $skus) {
    $publishDir = Join-Path $PublishRoot $s.Id
    New-Item -ItemType Directory -Force -Path $publishDir | Out-Null
    Get-ChildItem $publishDir -Force -ErrorAction SilentlyContinue |
        Remove-Item -Recurse -Force -ErrorAction SilentlyContinue

    Write-Step ("Publish " + $s.Name)
    $log = Join-Path $LogsDir ("publish-" + $s.Id + ".log")
    $publishArgs = @(
        "publish", $s.Project,
        "-c", $Configuration,
        ("-p:Version=" + $Version),
        ("-p:AssemblyVersion=" + $Version + ".0"),
        ("-p:FileVersion=" + $Version + ".0"),
        ("-p:InformationalVersion=" + $Version),
        "-o", $publishDir
    )
    if ($s.SelfContained) {
        $publishArgs += @(
            "-r", $s.Runtime,
            "--self-contained", "true",
            "-p:PublishSingleFile=true",
            "-p:IncludeNativeLibrariesForSelfExtract=true",
            "-p:EnableCompressionInSingleFile=true",
            "-p:PublishReadyToRun=true",
            "-p:DebugType=none",
            "-p:DebugSymbols=false"
        )
    } else {
        $publishArgs += @(
            "--self-contained", "false",
            "-p:DebugType=none",
            "-p:DebugSymbols=false"
        )
    }

    & dotnet @publishArgs 2>&1 | Tee-Object -FilePath $log
    if ($LASTEXITCODE -ne 0) {
        throw ("publish failed for " + $s.Name + " - see " + $log)
    }

    $exe = Join-Path $publishDir "Chronolog.exe"
    if (-not (Test-Path $exe)) {
        throw ("EXE missing: " + $exe)
    }
    $sizeMb = [math]::Round((Get-Item $exe).Length / 1MB, 1)
    Write-Host ("Published: " + $exe + " (" + $sizeMb + " MB)")

    Write-Step ("Installer " + $s.SetupBase)
    $issLog = Join-Path $LogsDir ("iscc-" + $s.Id + ".log")
    & $iscc @(
        $IssPath,
        ("/DMyAppName=" + $s.Name),
        ("/DMyAppVersion=" + $Version),
        ("/DMyAppId=" + $s.AppId),
        "/DMyAppExeName=Chronolog.exe",
        ("/DPublishDir=" + $publishDir),
        ("/DOutputDir=" + $InstallerLocal),
        ("/DOutputBaseFilename=" + $s.SetupBase),
        ("/DMinVersion=" + $s.MinVersion),
        ("/DArchitecturesAllowed=" + $s.ArchAllowed),
        ("/DArchitecturesInstallIn64BitMode=" + $s.Arch64),
        ("/DRequireNet48=" + $s.RequireNet48),
        "/Q"
    ) 2>&1 | Tee-Object -FilePath $issLog
    if ($LASTEXITCODE -ne 0) {
        throw ("ISCC failed for " + $s.Name + " - see " + $issLog)
    }

    $setup = Join-Path $InstallerLocal ($s.SetupBase + ".exe")
    if (-not (Test-Path $setup)) {
        throw ("Installer missing: " + $setup)
    }

    $results += [pscustomobject]@{
        Id         = $s.Id
        Name       = $s.Name
        Os         = $s.OsLabel
        PublishDir = $publishDir
        Installer  = $setup
        Bytes      = (Get-Item $setup).Length
    }
}

Write-Step "Copying payload to artifacts\ and installers to dist\"
foreach ($r in $results) {
    $payloadDest = Join-Path $PayloadRoot $r.Id
    if (Copy-IfPossible -Src $r.PublishDir -Dest $payloadDest) {
        Write-Host ("  Payload   -> " + $payloadDest)
    }
    $leaf = Split-Path $r.Installer -Leaf
    foreach ($destDir in @($DistDir, $InstallerCopy)) {
        try {
            Copy-Item $r.Installer (Join-Path $destDir $leaf) -Force -ErrorAction Stop
            Write-Host ("  Installer -> " + (Join-Path $destDir $leaf))
        } catch {
            Write-Warning ("Installer left at " + $r.Installer + " (could not copy to " + $destDir + ")")
        }
    }
}

Write-Host ""
Write-Host "=== Build succeeded ===" -ForegroundColor Green
foreach ($r in $results) {
    $mb = [math]::Round($r.Bytes / 1MB, 1)
    $leaf = Split-Path $r.Installer -Leaf
    Write-Host ("  " + $r.Name + "  (" + $r.Os + ")")
    Write-Host ("    dist\" + $leaf + "  (" + $mb + " MB)")
}
Write-Host ""
Write-Host "Icon source: assets\installer.ico"
Write-Host "Nothing was copied to the Desktop." -ForegroundColor DarkGray

$manifest = @{
    version = $Version
    builtAt = (Get-Date).ToString("o")
    skus    = @($results | ForEach-Object {
        @{
            id         = $_.Id
            name       = $_.Name
            os         = $_.Os
            publishDir = $_.PublishDir
            installer  = $_.Installer
            bytes      = $_.Bytes
        }
    })
} | ConvertTo-Json -Depth 5

$manifestLocal = Join-Path $LocalRoot "build-manifest.json"
Set-Content -Path $manifestLocal -Value $manifest -Encoding ASCII
try {
    Copy-Item $manifestLocal (Join-Path $Root "artifacts\build-manifest.json") -Force
} catch {}
Write-Host ("Manifest: " + $manifestLocal)
