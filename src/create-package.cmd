@echo off

set PROJECT=%~dp0IxMilia.Config\IxMilia.Config.csproj
dotnet restore %PROJECT%
if errorlevel 1 exit /b 1
dotnet pack --configuration Release %PROJECT%
