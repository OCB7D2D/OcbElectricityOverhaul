@echo off

SET VERSION=snapshot

if not "%1"=="" (
  SET VERSION=%1
)

goto MAIN

:BUILD

if not exist build\ (
  mkdir build
)

if exist build\%NAME%\ (
  echo remove existing directory
  rmdir build\%NAME% /S /Q
)

mkdir build\%NAME%

echo create %VERSION%

xcopy %FOLDER%\*.xml build\%NAME%\
xcopy %FOLDER%\*.md build\%NAME%\
xcopy %FOLDER%\*.dll build\%NAME%\
xcopy %FOLDER%\Config build\%NAME%\Config\ /S
xcopy %FOLDER%\Resources build\%NAME%\Resources\ /S
xcopy %FOLDER%\UIAtlases build\%NAME%\UIAtlases\ /S
xcopy %FOLDER%\BepInEx build\%NAME%\BepInEx\ /S

xcopy %FOLDER%\patchers\*.dll build\%NAME%\patchers\
xcopy %FOLDER%\98-install-bepinex.sh build\%NAME%\
xcopy %FOLDER%\98-install-bepinex.bat build\%NAME%\
xcopy %FOLDER%\99-uninstall-bepinex.sh build\%NAME%\
xcopy %FOLDER%\99-uninstall-bepinex.bat build\%NAME%\

cd build
echo Packaging %NAME%-%VERSION%.zip
powershell Compress-Archive %NAME% %NAME%-%VERSION%.zip -Force
cd ..

SET RV=%ERRORLEVEL%
if "%CI%"=="" pause
exit /B %RV%

:MAIN

SET FOLDER=.
SET NAME=ElectricityOverhaul
call :BUILD 

SET FOLDER=Addons\ZMXuiCPOCBEO
SET NAME=ZMXuiCPOCBEO
call :BUILD 

