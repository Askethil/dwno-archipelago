[CmdletBinding()]
param(
    [string]$WorldCataloguePath,
    [string]$JsonOutputPath,
    [string]$ReportPath,
    [switch]$Check
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$analyzerProject = Join-Path $repoRoot 'tools/AnalyzeFlags/AnalyzeFlags.csproj'
$analyzerArgs = @()
if ($WorldCataloguePath) { $analyzerArgs += @('--input', $WorldCataloguePath) }
if ($JsonOutputPath) { $analyzerArgs += @('--output', $JsonOutputPath) }
if ($ReportPath) { $analyzerArgs += @('--report', $ReportPath) }
if ($Check) { $analyzerArgs += '--check' }
Push-Location $repoRoot
try {
    & dotnet run --project $analyzerProject -c Release -- @analyzerArgs
    if ($LASTEXITCODE -ne 0) { throw 'Flag analysis failed; check the error above.' }
} finally { Pop-Location }
