# run-dev.ps1
# Sobe o backend (SPI.Api) e o frontend (Next.js) juntos, cada um na sua
# propria janela de terminal -- exatamente como voce ja vinha rodando
# manualmente em duas janelas, so que com um comando so.
#
# Uso:
#   .\run-dev.ps1
#
# Requisitos (ja configurados neste projeto):
#   - User Secrets do SPI.Api com ConnectionStrings:SpiDb e Jwt:Key definidos
#   - frontend\.env.local com NEXT_PUBLIC_API_URL
#   - MySQL rodando com o schema (database\01 a 09) aplicado

$ErrorActionPreference = "Stop"
$repoRoot = $PSScriptRoot

Write-Host "Subindo backend (SPI.Api) em uma nova janela..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList @(
    "-NoExit",
    "-Command",
    "cd '$repoRoot'; dotnet run --project src/SPI.Api/SPI.Api.csproj --launch-profile https"
)

Write-Host "Subindo frontend (Next.js) em uma nova janela..." -ForegroundColor Cyan
Start-Process powershell -ArgumentList @(
    "-NoExit",
    "-Command",
    "cd '$repoRoot\frontend'; npm run dev"
)

Write-Host ""
Write-Host "Duas janelas novas foram abertas: backend (https://localhost:7002) e frontend (http://localhost:3000)." -ForegroundColor Green
Write-Host "Feche as janelas (ou Ctrl+C dentro delas) para parar cada processo." -ForegroundColor Green
Write-Host ""
Write-Host "Aguardando o front subir para abrir o navegador..." -ForegroundColor Cyan
Start-Sleep -Seconds 8
Start-Process "http://localhost:3000"
