@echo off
dotnet build --nologo -v quiet -clp:ErrorsOnly && dotnet run --no-build || pause
