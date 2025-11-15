param(
    [switch]$SkipTests,
    [switch]$SkipPreCommit
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repoRoot 'OpenAIDictate.sln'

Write-Host '[CLEAN] dotnet clean' -ForegroundColor Cyan
& dotnet clean $solution | Out-Host

Write-Host '[FORMAT] dotnet format' -ForegroundColor Cyan
& dotnet format $solution --no-restore | Out-Host

if (-not $SkipTests) {
    Write-Host '[TEST] dotnet test' -ForegroundColor Cyan
    & dotnet test $solution --no-restore --verbosity minimal | Out-Host
}
else {
    Write-Host '[SKIP] Tests skipped via flag.' -ForegroundColor Yellow
}

if (-not $SkipPreCommit) {
    if (Get-Command pre-commit -ErrorAction SilentlyContinue) {
        Write-Host '[HOOK] pre-commit run --all-files' -ForegroundColor Cyan
        & pre-commit run --all-files | Out-Host
    }
    else {
        Write-Host '[WARN] pre-commit not installed; skipping.' -ForegroundColor Yellow
    }
}
else {
    Write-Host '[SKIP] pre-commit skipped via flag.' -ForegroundColor Yellow
}

Write-Host '[DONE] Workspace cleanup completed.' -ForegroundColor Green
