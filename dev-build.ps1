# dev-build.ps1
# Run this from the project root to: kill the app, pull latest, build, done.
# Usage: Right-click -> "Run with PowerShell"  OR  pin to taskbar.
# After it finishes, double-click TunnelMonitor.exe to re-launch.

param(
    [string]$Branch = "ui/sidebar-fullscreen-redesign",
    [string]$Config  = "Release"
)

$projectRoot = $PSScriptRoot
$exePath     = Join-Path $projectRoot "bin\$Config\net8.0-windows\TunnelMonitor.exe"

Write-Host ""
Write-Host "=== Oolio Tunnel Monitor - Dev Build ===" -ForegroundColor Cyan
Write-Host ""

# ── 1. Kill running instance ──────────────────────────────────────────────────
Write-Host "Stopping TunnelMonitor..." -ForegroundColor Yellow
Get-Process -Name "TunnelMonitor" -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 800

# ── 2. Git pull ───────────────────────────────────────────────────────────────
Write-Host "Pulling $Branch..." -ForegroundColor Yellow
Set-Location $projectRoot
$currentBranch = git rev-parse --abbrev-ref HEAD 2>&1
if ($currentBranch -ne $Branch) {
    Write-Host "  Switching from '$currentBranch' to '$Branch'..." -ForegroundColor DarkYellow
    git checkout $Branch
    if ($LASTEXITCODE -ne 0) { Write-Host "git checkout failed" -ForegroundColor Red; Read-Host "Press Enter to exit"; exit 1 }
}
git pull
if ($LASTEXITCODE -ne 0) { Write-Host "git pull failed" -ForegroundColor Red; Read-Host "Press Enter to exit"; exit 1 }
Write-Host "  Pull OK" -ForegroundColor Green

# ── 3. Build ──────────────────────────────────────────────────────────────────
Write-Host "Building ($Config)..." -ForegroundColor Yellow
dotnet build -c $Config --nologo -v minimal
if ($LASTEXITCODE -ne 0) { Write-Host "Build FAILED" -ForegroundColor Red; Read-Host "Press Enter to exit"; exit 1 }
Write-Host "  Build OK" -ForegroundColor Green

# ── 4. Done ───────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "Done! Exe: $exePath" -ForegroundColor Cyan
Write-Host ""
Write-Host "Launch now? (Y/N)" -ForegroundColor White -NoNewline
$key = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
Write-Host ""
if ($key.Character -eq 'y' -or $key.Character -eq 'Y') {
    Start-Process $exePath
}
