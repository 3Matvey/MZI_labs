@echo off
chcp 65001 >nul
pushd "%~dp0"
dotnet run --project Demo\ConsoleUi.Demo.csproj
if errorlevel 1 pause
popd
