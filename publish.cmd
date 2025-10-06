@echo off
set /p "version=Enter version (major.minor.build):"
set /p "apikey=Enter Nuget API key:"

dotnet tool update --global dotnet-validate --version 0.0.1-preview.304


echo ========================================================================================
echo    Publishing: Basses.SimpleDocumentStore
echo ========================================================================================
@echo on
dotnet nuget push nupkgs/Basses.SimpleDocumentStore.%version%.nupkg --api-key %apikey% --source https://api.nuget.org/v3/index.json
@echo off


echo ========================================================================================
echo    Publishing: Basses.SimpleDocumentStore.SqlServer
echo ========================================================================================
@echo on
dotnet nuget push nupkgs/Basses.SimpleDocumentStore.SqlServer.%version%.nupkg --api-key %apikey% --source https://api.nuget.org/v3/index.json
@echo off


echo ========================================================================================
echo    Publishing: Basses.SimpleDocumentStore.PostgreSql
echo ========================================================================================
@echo on
dotnet nuget push nupkgs/Basses.SimpleDocumentStore.PostgreSql.%version%.nupkg --api-key %apikey% --source https://api.nuget.org/v3/index.json
@echo off


echo ----------------------------------------------------------------------------------------
echo Files done!
pause
