# Quick .NET SDK Installer
Write-Host "Downloading .NET 8.0 SDK..." -ForegroundColor Yellow

$sdkUrl = "https://download.visualstudio.microsoft.com/download/pr/b280d97f-25a9-4ab7-8a12-8291aa3af117/a7d8f93b5fc5775a77f0e3d5288c6c6b/dotnet-sdk-8.0.404-win-x64.exe"
$installerPath = "$env:TEMP\dotnet-sdk-8.0.404-win-x64.exe"

try {
    Invoke-WebRequest -Uri $sdkUrl -OutFile $installerPath -UseBasicParsing
    Write-Host "Download complete!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Starting installer..." -ForegroundColor Yellow
    Write-Host "Please follow the installation wizard." -ForegroundColor Yellow
    Write-Host ""

    Start-Process -FilePath $installerPath -Wait

    Write-Host ""
    Write-Host "Installation complete!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Please close this terminal and open a new one, then run:" -ForegroundColor Yellow
    Write-Host "  .\build.ps1" -ForegroundColor White
    Write-Host ""
}
catch {
    Write-Host "Error: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please download manually from:" -ForegroundColor Yellow
    Write-Host "  https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor White
}

pause
