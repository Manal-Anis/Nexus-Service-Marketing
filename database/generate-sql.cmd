@echo off
REM Creates the complete SQL script of the database from the EF Core migration.
REM Needs the EF tool once:  dotnet tool install --global dotnet-ef
cd /d "%~dp0.."
dotnet ef migrations script --idempotent --output database\NexusServiceMarketingDb.sql
echo Done: database\NexusServiceMarketingDb.sql
pause
