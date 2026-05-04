# Build release artifacts for Gestore PDF
# Usage: .\build\publish.ps1
# Output: .\artifacts\win-x64\   (folder install)
#         .\artifacts\installer\ (GestorePDF-Setup-1.0.0.exe via Inno Setup)

$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent

# ---- Publish ----
Write-Host "Publishing self-contained win-x64..." -ForegroundColor Cyan
dotnet publish "$root\src\PdfManager.App\PdfManager.App.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:DebugType=embedded `
    -o "$root\artifacts\win-x64"

Write-Host "Publish complete: $root\artifacts\win-x64" -ForegroundColor Green

# ---- Inno Setup ----
$iscc = @(
    "$env:ProgramFiles(x86)\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
    "ISCC.exe"
) | Where-Object { (Get-Command $_ -ErrorAction SilentlyContinue) -or (Test-Path $_) } | Select-Object -First 1

if ($iscc) {
    Write-Host "Running Inno Setup..." -ForegroundColor Cyan
    & $iscc "$root\build\installer.iss"
    Write-Host "Installer built: $root\artifacts\installer\GestorePDF-Setup-1.0.0.exe" -ForegroundColor Green
} else {
    Write-Warning "Inno Setup (ISCC.exe) not found. Install from https://jrsoftware.org/isinfo.php and re-run."
    Write-Host "Folder ready for manual Inno Setup compilation: $root\artifacts\win-x64" -ForegroundColor Yellow
}
