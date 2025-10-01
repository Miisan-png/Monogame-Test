@echo off
echo Building Snow Engine...
dotnet build
if %errorlevel% neq 0 (
    echo Build failed!
    pause
    exit /b %errorlevel%
)

echo.
echo Running Snow Engine...
dotnet run
pause
