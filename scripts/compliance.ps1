# AUTH:DEVNEUROSIM:7A3F9E2B | Legacy/scripts/compliance.ps1
# NeuroSim compliance check
# docCompliant: docs populated, README, CHANGELOG
# testCompliant: unit + system tests pass
# dirCompliant: expected directory structure
# State: Perfect(11), Good(10), Okay(01), Bad(00)

param(
    [string]$Root = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = "Stop"
$script:Root = $Root
$cfgPath = Join-Path $script:Root "compliance.config.json"

function Get-ComplianceConfig {
    if (-not (Test-Path $cfgPath)) { throw "Missing compliance config: $cfgPath" }
    return (Get-Content $cfgPath -Raw | ConvertFrom-Json)
}

# ── Helpers ────────────────────────────────────────────────────────────────

function Test-DocsPopulated {
    $cfg = Get-ComplianceConfig

    foreach ($rel in $cfg.requiredDocs) {
        $p = Join-Path $script:Root $rel
        if (-not (Test-Path $p)) { return $false }
    }

    $docsPath = Join-Path $script:Root "docs"
    if (-not (Test-Path $docsPath)) { return $false }
    $min = 2
    if ($null -ne $cfg.minimumDocsMarkdownFilesInDocsDir) { $min = [int]$cfg.minimumDocsMarkdownFilesInDocsDir }
    $files = Get-ChildItem $docsPath -File -Filter "*.md" -ErrorAction SilentlyContinue
    return $files.Count -ge $min
}

function Test-ReadmeDone {
    $readme = Join-Path $script:Root "README.md"
    if (-not (Test-Path $readme)) { return $false }
    $content = Get-Content $readme -Raw
    return $content.Length -gt 200 -and $content -match "NeuroSim|Run it|Tests"
}

function Test-ChangelogDone {
    $changelog = Join-Path $script:Root "CHANGELOG.md"
    if (-not (Test-Path $changelog)) { return $false }
    $content = Get-Content $changelog -Raw
    return $content.Length -gt 100 -and $content -match "Changelog|Added|Changed"
}

function Test-DirStructure {
    $cfg = Get-ComplianceConfig
    foreach ($dir in $cfg.requiredDirectories) {
        $path = Join-Path $script:Root $dir
        if (-not (Test-Path $path)) { return $false }
    }
    return $true
}

function Test-UnitTests {
    $cfg = Get-ComplianceConfig
    foreach ($rel in $cfg.unitTestProjects) {
        $proj = Join-Path $script:Root $rel
        if (-not (Test-Path $proj)) { return $false }
        dotnet test $proj --verbosity quiet 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0) { return $false }
    }
    return $true
}

function Test-SystemTests {
    $cfg = Get-ComplianceConfig
    foreach ($rel in $cfg.systemTestProjects) {
        $proj = Join-Path $script:Root $rel
        if (-not (Test-Path $proj)) { return $false }
        dotnet test $proj --verbosity quiet 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0) { return $false }
    }
    return $true
}

# ── Main ───────────────────────────────────────────────────────────────────

# Ensure we're in repo root
Push-Location $script:Root
try {
    dotnet build --verbosity quiet 2>&1 | Out-Null
} catch { }
finally {
    Pop-Location
}

$docsOk = (Test-DocsPopulated) -and (Test-ReadmeDone) -and (Test-ChangelogDone)
$dirOk = Test-DirStructure
$docCompliant = [int]($docsOk -and $dirOk)  # 1 or 0

$unitOk = Test-UnitTests
$sysOk = Test-SystemTests
$testCompliant = [int]($unitOk -and $sysOk)  # 1 or 0

# State: bit0=docCompliant, bit1=testCompliant
$stateBits = ($testCompliant -shl 1) -bor $docCompliant
$stateName = switch ($stateBits) {
    0 { "Bad" }
    1 { "Okay" }
    2 { "Good" }
    3 { "Perfect" }
    default { "Unknown" }
}

# Output
function Out-01 { param($x) if ($x) { '01' } else { '00' } }
Write-Host ''
Write-Host 'NeuroSim Compliance Check' -ForegroundColor Cyan
Write-Host '-------------------------'
Write-Host ('docs populated:    ' + (Out-01 $docsOk))
Write-Host ('README done:       ' + (Out-01 (Test-ReadmeDone)))
Write-Host ('CHANGELOG done:    ' + (Out-01 (Test-ChangelogDone)))
Write-Host ('dir structure:     ' + (Out-01 $dirOk))
Write-Host ('dirCompliant:      ' + (Out-01 $dirOk))
Write-Host ('docCompliant:      ' + (Out-01 $docCompliant))
Write-Host ''
Write-Host ('unit tests:        ' + $(if ($unitOk) { 'pass' } else { 'fail' }))
Write-Host ('system tests:      ' + $(if ($sysOk) { 'pass' } else { 'fail' }))
Write-Host ('testCompliant:     ' + (Out-01 $testCompliant))
Write-Host ''
$color = switch ($stateBits) { 3 { 'Green' } 2 { 'Yellow' } 1 { 'Yellow' } default { 'Red' } }
$bitsDisplay = [Convert]::ToString($stateBits, 2).PadLeft(2, '0')  # 00, 01, 10, 11
$msg = 'State: ' + $stateName + ' (' + $bitsDisplay + ')'
Write-Host $msg -ForegroundColor $color
Write-Host ''

$cfg = Get-ComplianceConfig
$threshold = "Good"
if ($null -ne $cfg.passThreshold) { $threshold = $cfg.passThreshold.ToString() }
$need = switch ($threshold) { "Perfect" { 3 } "Good" { 2 } "Okay" { 1 } default { 2 } }
if ($stateBits -ge $need) { exit 0 } else { exit 1 }
