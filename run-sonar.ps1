# SmartCutScheduler – SonarQube analysis (single project, multi-language)
#
# Analizează ÎNTREG proiectul (C# backend + Python AI) într-un singur proiect SonarQube.
# dotnet-sonarscanner suportă analiza multi-limbaj: C# via MSBuild + Python via coverage XML.
#
# Prerequisites:
#   1. SonarQube running:
#        docker compose up sonarqube -d
#      (prima pornire durează ~1 min; deschide http://localhost:9000, login admin/admin,
#       schimbă parola, creează un proiect cu cheia "SmartCutScheduler" și generează un token)
#   2. Install dotnet-sonarscanner:
#        dotnet tool install --global dotnet-sonarscanner
#   3. Python env cu pytest-cov:
#        pip install pytest-cov
#
# Usage:
#   .\run-sonar.ps1 -Token <your_sonarqube_token>
#
param(
    [Parameter(Mandatory=$true)]
    [string]$Token,

    [string]$SonarHost  = "http://localhost:9000",
    [string]$ProjectKey = "SmartCutScheduler"
)

$ErrorActionPreference = "Stop"
$Root = $PSScriptRoot

Write-Host ""
Write-Host "=== SmartCutScheduler – SonarQube full analysis ===" -ForegroundColor Cyan

# ---------------------------------------------------------------------------
# Step 1 – Generate Python coverage report (must be done BEFORE sonarscanner begin)
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "[1/5] Running Python tests + generating coverage XML..." -ForegroundColor Yellow

Push-Location "$Root\ai_service"
try {
    python -m pytest tests/ `
        --cov=. `
        --cov-report=xml:coverage.xml `
        --cov-report=term-missing `
        -q

    if ($LASTEXITCODE -ne 0) { throw "Python tests failed" }
}
finally {
    Pop-Location
}

# ---------------------------------------------------------------------------
# Step 2 – Begin sonarscanner (C# + Python in one project)
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "[2/5] Begin SonarScanner (multi-language)..." -ForegroundColor Yellow

Push-Location $Root
dotnet sonarscanner begin `
    /k:"$ProjectKey" `
    /d:sonar.host.url="$SonarHost" `
    /d:sonar.token="$Token" `
    /d:sonar.cs.opencover.reportsPaths="**/coverage.opencover.xml" `
    /d:sonar.python.coverage.reportPaths="ai_service/coverage.xml" `
    /d:sonar.exclusions="**/Migrations/**,**/obj/**,**/bin/**,frontend/**,**/__pycache__/**,**/*.pyc" `
    /d:sonar.coverage.exclusions="**/Migrations/**,**/Program.cs,**/DependencyInjection.cs,**/Seeding/**,ai_service/tests/**"

if ($LASTEXITCODE -ne 0) { throw "SonarScanner begin failed" }

# ---------------------------------------------------------------------------
# Step 3 – Build .NET solution
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "[3/5] Building .NET solution..." -ForegroundColor Yellow

dotnet build SmartCutScheduler.sln --no-incremental -c Release --ignore-failed-sources

if ($LASTEXITCODE -ne 0) { throw "Build failed" }

# ---------------------------------------------------------------------------
# Step 4 – Run .NET tests with coverage
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "[4/5] Running .NET tests with coverage..." -ForegroundColor Yellow

dotnet test tests/SmartCutScheduler.Tests/SmartCutScheduler.Tests.csproj `
    /p:CollectCoverage=true `
    -c Release

if ($LASTEXITCODE -ne 0) { throw ".NET tests failed" }

# ---------------------------------------------------------------------------
# Step 5 – End scanner and upload all results
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "[5/5] Uploading results to SonarQube..." -ForegroundColor Yellow

dotnet sonarscanner end /d:sonar.token="$Token"

if ($LASTEXITCODE -ne 0) { throw "SonarScanner end failed" }

Pop-Location

Write-Host ""
Write-Host "=== Analysis complete! ===" -ForegroundColor Green
Write-Host "    Dashboard: $SonarHost/dashboard?id=$ProjectKey" -ForegroundColor Green
