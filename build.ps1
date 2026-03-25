# AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/build.ps1
<#
.SYNOPSIS
    NeuroSim — full build script (C++ DLL + C# WPF + Python bindings)
.DESCRIPTION
    1. Builds NeuroSim.Core C++ DLL via CMake
    2. Copies DLL to NeuroSim.UI output directory
    3. Builds NeuroSim.UI WPF application via dotnet
    4. (Optional) Builds Python pybind11 module

.PARAMETER Config
    Build configuration: Debug or Release (default: Release)
.PARAMETER NoPython
    Skip building the Python bindings
.PARAMETER Clean
    Delete previous build artefacts before building

.EXAMPLE
    .\build.ps1 -Config Debug
    .\build.ps1 -NoPython
#>

param(
    [ValidateSet("Debug","Release")]
    [string]$Config = "Release",
    [switch]$NoPython,
    [switch]$Clean
)

$ErrorActionPreference = "Stop"
$Root  = $PSScriptRoot
$Build = Join-Path $Root "build_cpp"

Write-Host ""
Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host "  NeuroSim Build  —  $Config" -ForegroundColor Cyan
Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host ""

# ── 1. CMake configure + build (C++ DLL) ─────────────────────────────────────
if ($Clean -and (Test-Path $Build)) {
    Write-Host "[1/3] Cleaning previous C++ build..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force $Build
}

if (-not (Test-Path $Build)) { New-Item -ItemType Directory -Path $Build | Out-Null }

Write-Host "[1/3] Configuring C++ with CMake..." -ForegroundColor Green
$cmakeArgs = @(
    "-S", $Root,
    "-B", $Build,
    "-DCMAKE_BUILD_TYPE=$Config",
    "-DNEUROSIM_BUILD_PYTHON=$(if($NoPython){'OFF'}else{'ON'})"
)
cmake @cmakeArgs
if ($LASTEXITCODE -ne 0) { Write-Error "CMake configure failed." }

Write-Host "[1/3] Building C++ core..." -ForegroundColor Green
cmake --build $Build --config $Config --parallel
if ($LASTEXITCODE -ne 0) { Write-Error "CMake build failed." }

# ── 2. Copy DLL to C# UI output ───────────────────────────────────────────────
$dllSrc = Join-Path $Build "bin\$Config\NeuroSimCore.dll"
if (-not (Test-Path $dllSrc)) {
    # Some generators put it flat
    $dllSrc = Join-Path $Build "bin\NeuroSimCore.dll"
}

$uiOut = Join-Path $Root "NeuroSim.UI\bin\$Config\net8.0-windows"
if (Test-Path $dllSrc) {
    if (-not (Test-Path $uiOut)) { New-Item -ItemType Directory -Path $uiOut | Out-Null }
    Write-Host "[2/3] Copying NeuroSimCore.dll -> UI output..." -ForegroundColor Green
    Copy-Item -Force $dllSrc $uiOut
} else {
    Write-Warning "NeuroSimCore.dll not found at expected path — the UI will run without the C++ backend."
}

# ── 3. dotnet build (C# WPF) ──────────────────────────────────────────────────
Write-Host "[3/3] Building NeuroSim.UI (dotnet)..." -ForegroundColor Green
$slnPath = Join-Path $Root "devNeuroSim.sln"
dotnet build $slnPath -c $Config
if ($LASTEXITCODE -ne 0) { Write-Error "dotnet build failed." }

Write-Host ""
Write-Host "Build complete." -ForegroundColor Green
$exePath = Join-Path $Root "NeuroSim.UI\bin\$Config\net8.0-windows\NeuroSim.UI.exe"
if (Test-Path $exePath) {
    Write-Host "Run: $exePath" -ForegroundColor Cyan
}
