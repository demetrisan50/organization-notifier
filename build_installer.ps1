# Build and Package Script for Organization Notifier
# This script publishes the app and (optionally) builds the installer.

Write-Host "--- Starting Build Process ---" -ForegroundColor Cyan

# 1. Clean previous build
if (Test-Path "./publish") { Remove-Item "./publish" -Recurse -Force }
if (Test-Path "./Output") { Remove-Item "./Output" -Recurse -Force }

# 2. Publish the application
Write-Host "Publishing .NET application..." -ForegroundColor Yellow
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishReadyToRun=true -o ./publish

# Note: We set PublishSingleFile=false for the installer to ensure the DLLs are explicitly included, 
# which is more reliable for installers. If you want a single file, change it to true.

# 3. Check for Inno Setup Compiler (ISCC.exe)
$iscc = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if (Test-Path $iscc) {
    Write-Host "Inno Setup found! Compiling installer..." -ForegroundColor Green
    & $iscc "installer.iss"
    Write-Host "Installer created successfully in the 'Output' folder." -ForegroundColor Green
} else {
    Write-Host "Inno Setup compiler (ISCC.exe) not found at standard path." -ForegroundColor Red
    Write-Host "Please download it from https://jrsoftware.org/isdl.php" -ForegroundColor White
    Write-Host "Once installed, you can open 'installer.iss' and click 'Compile' manually." -ForegroundColor Cyan
}

Write-Host "--- Build Process Complete ---" -ForegroundColor Cyan
