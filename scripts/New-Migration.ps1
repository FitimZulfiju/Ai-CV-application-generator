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
