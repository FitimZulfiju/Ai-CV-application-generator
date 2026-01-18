<#
.SYNOPSIS
    Applies EF Core migrations to the database for the specified provider.

.DESCRIPTION
    This script runs 'dotnet ef database update' for the specified database provider.
    Make sure the appropriate database server is running before executing.

.PARAMETER Provider
    The database provider to target: SqlServer or PostgreSQL.

.EXAMPLE
    ./Update-Database.ps1 -Provider SqlServer
    Applies migrations to the SQL Server database.

.EXAMPLE
    ./Update-Database.ps1 -Provider PostgreSQL
    Applies migrations to the PostgreSQL database.
#>
param(
    [Parameter(Mandatory = $true, HelpMessage = "Target provider: SqlServer or PostgreSQL")]
    [ValidateSet("SqlServer", "PostgreSQL")]
    [string]$Provider
)

$ErrorActionPreference = "Stop"
$SolutionRoot = Split-Path -Parent $PSScriptRoot

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  EF Core Database Update" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Target Provider: $Provider" -ForegroundColor White
Write-Host ""

$projectName = if ($Provider -eq "PostgreSQL") { 
    "AiCV.Migrations.PostgreSQL" 
}
else { 
    "AiCV.Migrations.SqlServer" 
}

$migrationProject = Join-Path $SolutionRoot $projectName

Write-Host "Migration Project: $migrationProject" -ForegroundColor Gray
Write-Host ""

Write-Host "Applying migrations..." -ForegroundColor Yellow

# Run from the migration project directory so design-time factory is used
Push-Location $migrationProject
dotnet ef database update
$exitCode = $LASTEXITCODE
Pop-Location

if ($exitCode -eq 0) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  Database updated successfully!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Cyan
}
else {
    Write-Host ""
    Write-Host "[ERROR] Database update failed!" -ForegroundColor Red
    exit 1
}
