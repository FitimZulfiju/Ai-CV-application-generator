<#
.SYNOPSIS
    Adds EF Core migrations for SQL Server, PostgreSQL, or both providers.

.DESCRIPTION
    This script runs 'dotnet ef migrations add' for the specified database provider(s).
    It runs from within each migration project directory to use the design-time factory.

.PARAMETER Name
    The name of the migration to create.

.PARAMETER Provider
    The database provider to target: Both, SqlServer, or PostgreSQL. Default is Both.

.EXAMPLE
    ./Add-Migration.ps1 -Name "Initial"
    Creates migrations for both SQL Server and PostgreSQL.

.EXAMPLE
    ./Add-Migration.ps1 -Name "AddUserTable" -Provider SqlServer
    Creates migration only for SQL Server.
#>
param(
    [Parameter(Mandatory = $true, HelpMessage = "Name of the migration")]
    [string]$Name,
    
    [Parameter(Mandatory = $false, HelpMessage = "Target provider: Both, SqlServer, or PostgreSQL")]
    [ValidateSet("Both", "SqlServer", "PostgreSQL")]
    [string]$Provider = "Both"
)

$ErrorActionPreference = "Stop"
$SolutionRoot = Split-Path -Parent $PSScriptRoot

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  EF Core Migration Generator" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Migration Name: $Name" -ForegroundColor White
Write-Host "Target Provider: $Provider" -ForegroundColor White
Write-Host ""

if ($Provider -eq "Both" -or $Provider -eq "SqlServer") {
    Write-Host "=== SQL Server ===" -ForegroundColor Yellow
    
    $sqlServerProject = Join-Path $SolutionRoot "AiCV.Migrations.SqlServer"
    
    Write-Host "Project: $sqlServerProject" -ForegroundColor Gray
    
    # Run from the migration project directory so design-time factory is used
    Push-Location $sqlServerProject
    dotnet ef migrations add $Name
    $exitCode = $LASTEXITCODE
    Pop-Location
    
    if ($exitCode -eq 0) {
        Write-Host "[OK] SQL Server migration created successfully!" -ForegroundColor Green
    }
    else {
        Write-Host "[ERROR] SQL Server migration failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host ""
}

if ($Provider -eq "Both" -or $Provider -eq "PostgreSQL") {
    Write-Host "=== PostgreSQL ===" -ForegroundColor Yellow
    
    $postgresProject = Join-Path $SolutionRoot "AiCV.Migrations.PostgreSQL"
    
    Write-Host "Project: $postgresProject" -ForegroundColor Gray
    
    # Run from the migration project directory so design-time factory is used
    Push-Location $postgresProject
    dotnet ef migrations add $Name
    $exitCode = $LASTEXITCODE
    Pop-Location
    
    if ($exitCode -eq 0) {
        Write-Host "[OK] PostgreSQL migration created successfully!" -ForegroundColor Green
    }
    else {
        Write-Host "[ERROR] PostgreSQL migration failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host ""
}

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Migration '$Name' completed!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
