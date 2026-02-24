# ============================================
# MIGRATION SCRIPT
# ============================================
# Edit the migration name below, then run this script

$MigrationName = "Initial"  # <-- CHANGE THIS NAME

# ============================================
# DO NOT EDIT BELOW THIS LINE
# ============================================
Set-Location $PSScriptRoot
.\Add-Migration.ps1 -Name $MigrationName -Provider Both

# Full path to the migration file ============
& "c:\Ai-CV-application-generator\scripts\Add-Migration.ps1" -Name "InitialCreate" -Provider Both
# ============================================