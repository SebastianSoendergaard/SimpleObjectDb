@echo off
set /p "version=Enter version (major.minor.build):"

dotnet tool update --global dotnet-validate --version 0.0.1-preview.304


echo ========================================================================================
echo    Building: Basses.SimpleDocumentStore
echo ========================================================================================
@echo on
dotnet pack Basses.SimpleDocumentStore/Basses.SimpleDocumentStore.csproj -p:PackageVersion=%version% --include-source --output nupkgs /p:ContinuousIntegrationBuild=true
dotnet validate package local "nupkgs/Basses.SimpleDocumentStore.%version%.nupkg"
@echo off


echo ========================================================================================
echo    Building: Basses.SimpleDocumentStore.SqlServer
echo ========================================================================================
@echo on
dotnet pack Basses.SimpleDocumentStore.SqlServer/Basses.SimpleDocumentStore.SqlServer.csproj -p:PackageVersion=%version% --include-source --output nupkgs /p:ContinuousIntegrationBuild=true
dotnet validate package local "nupkgs/Basses.SimpleDocumentStore.SqlServer.%version%.nupkg"
@echo off


echo ========================================================================================
echo    Building: Basses.SimpleDocumentStore.PostgreSql
echo ========================================================================================
@echo on
dotnet pack Basses.SimpleDocumentStore.PostgreSql/Basses.SimpleDocumentStore.PostgreSql.csproj -p:PackageVersion=%version% --include-source --output nupkgs /p:ContinuousIntegrationBuild=true
dotnet validate package local "nupkgs/Basses.SimpleDocumentStore.PostgreSql.%version%.nupkg"
@echo off


echo ----------------------------------------------------------------------------------------
echo Files done!
pause
