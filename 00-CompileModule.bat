@echo off

SET NAME=ElectricityOverhaul

call CM7D2D %NAME% Harmony\*.cs PatchScripts\*.cs

SET RV=%ERRORLEVEL%
if "%CI%"=="" pause
exit /B %RV%