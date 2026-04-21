# SmartCutScheduler – SonarQube analysis with code coverage
#
# Prerequisites:
#   1. SonarQube running: docker compose up sonarqube -d
#      (first start takes ~1 min; open http://localhost:9000, login admin/admin,
#       change password, then create a new project key "SmartCutScheduler" and
#       generate a user token)
#   2. Install dotnet-sonarscanner:
#      dotnet tool install --global dotnet-sonarscanner
#
# Usage:
#   .\run-sonar.ps1 -Token <your_sonarqube_token>
#
param(
    [Parameter(Mandatory=$true)]
    [string]$Token,

    [string]$SonarHost = "http://localhost:9000",
    [string]$ProjectKey = "SmartCutScheduler"
)

$ErrorActionPreference = "Stop"

Write-Host "=== Starting SonarQube analysis ===" -ForegroundColor Cyan

# 1 – Begin scanner
Write-Host "[1/4] Begin SonarScanner..." -ForegroundColor Yellow
dotnet sonarscanner begin `
    /k:"$ProjectKey" `
    /d:sonar.host.url="$SonarHost" `
    /d:sonar.token="$Token" `
    /d:sonar.cs.opencover.reportsPaths="**/coverage.opencover.xml" `
    /d:sonar.exclusions="**/Migrations/**,**/obj/**,**/bin/**,frontend/**" `
    /d:sonar.coverage.exclusions="**/Migrations/**,**/Program.cs,**/DependencyInjection.cs,**/Seeding/**"

if ($LASTEXITCODE -ne 0) { throw "SonarScanner begin failed" }

# 2 – Build (without test project so step 3 can build+instrument in one pass)
Write-Host "[2/4] Building solution (excluding test project)..." -ForegroundColor Yellow
dotnet build SmartCutScheduler.sln --no-incremental -c Release `
    --ignore-failed-sources

if ($LASTEXITCODE -ne 0) { throw "Build failed" }

# 3 – Run tests with coverage (dotnet test builds the test project itself so coverlet finds the DLL)
Write-Host "[3/4] Running tests with coverage..." -ForegroundColor Yellow
dotnet test tests/SmartCutScheduler.Tests/SmartCutScheduler.Tests.csproj `
    /p:CollectCoverage=true `
    -c Release

if ($LASTEXITCODE -ne 0) { throw "Tests failed" }

# 4 – End scanner / upload results
Write-Host "[4/4] Uploading results to SonarQube..." -ForegroundColor Yellow
dotnet sonarscanner end /d:sonar.token="$Token"

if ($LASTEXITCODE -ne 0) { throw "SonarScanner end failed" }

Write-Host ""
Write-Host "=== Analysis complete! Open $SonarHost/dashboard?id=$ProjectKey ===" -ForegroundColor Green
