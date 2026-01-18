<#
.SYNOPSIS
    Starts a local PostgreSQL instance using Docker for development testing.

.DESCRIPTION
    This script starts a PostgreSQL container for local development.
    Reads DB password from .env file (same as production).
    Use this when you want to test PostgreSQL locally without a full deployment.

.PARAMETER Stop
    If specified, stops and removes the PostgreSQL container instead of starting it.

.EXAMPLE
    ./Start-LocalPostgres.ps1
    Starts the local PostgreSQL container.

.EXAMPLE
    ./Start-LocalPostgres.ps1 -Stop
    Stops and removes the local PostgreSQL container.
#>
param(
    [Parameter(Mandatory = $false)]
    [switch]$Stop
)

$SolutionRoot = Split-Path -Parent $PSScriptRoot
$EnvFile = Join-Path $SolutionRoot "AiCV.Web\deploy\.env"

# Read password from .env file
$Password = "postgres"  # default fallback
$Database = "aicv_db"
$User = "sa"

if (Test-Path $EnvFile) {
    Get-Content $EnvFile | ForEach-Object {
        if ($_ -match "^DB_PASSWORD='?([^']+)'?$") {
            $Password = $matches[1]
        }
        if ($_ -match "^DB_NAME=(.+)$") {
            $Database = $matches[1]
        }
        if ($_ -match "^DB_USER=(.+)$") {
            $User = $matches[1]
        }
    }
    Write-Host "Loaded settings from .env" -ForegroundColor Gray
}

$ContainerName = "aicv-postgres-dev"
$Port = 5432

if ($Stop) {
    Write-Host "Stopping PostgreSQL container..." -ForegroundColor Yellow
    docker stop $ContainerName 2>$null
    docker rm $ContainerName 2>$null
    Write-Host "PostgreSQL container stopped and removed." -ForegroundColor Green
    exit 0
}

# Check if container already exists
$existing = docker ps -a --filter "name=$ContainerName" --format "{{.Names}}"
if ($existing -eq $ContainerName) {
    Write-Host "Starting existing PostgreSQL container..." -ForegroundColor Yellow
    docker start $ContainerName
}
else {
    Write-Host "Creating new PostgreSQL container..." -ForegroundColor Yellow
    Write-Host "  Database: $Database" -ForegroundColor Gray
    Write-Host "  User: $User" -ForegroundColor Gray
    docker run -d `
        --name $ContainerName `
        -e POSTGRES_USER=$User `
        -e POSTGRES_PASSWORD=$Password `
        -e POSTGRES_DB=$Database `
        -p "${Port}:5432" `
        postgres:16-alpine
}

# Wait for PostgreSQL to be ready
Write-Host "Waiting for PostgreSQL to be ready..." -ForegroundColor Yellow
for ($i = 0; $i -lt 30; $i++) {
    docker exec $ContainerName pg_isready -U $User 2>$null | Out-Null
    if ($LASTEXITCODE -eq 0) {
        break
    }
    Start-Sleep -Seconds 1
}

$ConnectionString = "Host=localhost;Port=$Port;Database=$Database;Username=$User;Password=$Password;"

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  PostgreSQL is ready!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Connection String:" -ForegroundColor White
Write-Host $ConnectionString -ForegroundColor Gray
Write-Host ""
Write-Host "To run the app with PostgreSQL:" -ForegroundColor White
Write-Host '$env:DB_PROVIDER = "PostgreSQL"' -ForegroundColor Gray
Write-Host "`$env:ConnectionStrings__DefaultConnection = `"$ConnectionString`"" -ForegroundColor Gray
Write-Host "dotnet run --project AiCV.Web" -ForegroundColor Gray
Write-Host ""
