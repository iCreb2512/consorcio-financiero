@echo off
cd /d "%~dp0"
if exist "ejecutable\Consorcio.WinForms.exe" (
  start "" "ejecutable\Consorcio.WinForms.exe"
) else (
  dotnet run --project "src\Consorcio.WinForms\Consorcio.WinForms.csproj" --configuration Release
)
