@echo off

call MC7D2D ElectricityOverhaul.dll /reference:"%PATH_7D2D_MANAGED%\Assembly-CSharp.dll" ^
  -recurse:Sources\*.cs && ^
echo Successfully compiled ElectricityOverhaul.dll

SET RV=%ERRORLEVEL%
if "%CI%"=="" pause
exit /B %RV%