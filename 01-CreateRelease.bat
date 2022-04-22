@echo off

SET NAME=ElectricityOverhaul

if not exist build\ (
  mkdir build
)

if exist build\%NAME%\ (
  echo remove existing directory
  rmdir build\%NAME% /S /Q
)

mkdir build\%NAME%

SET VERSION=snapshot

if not "%1"=="" (
  SET VERSION=%1
)

echo create %VERSION%

xcopy *.xml build\%NAME%\
xcopy *.md build\%NAME%\
xcopy *.dll build\%NAME%\
xcopy Config build\%NAME%\Config\ /S
xcopy Resources build\%NAME%\Resources\ /S
xcopy UIAtlases build\%NAME%\UIAtlases\ /S
xcopy BepInEx build\%NAME%\BepInEx\ /S

xcopy patchers\*.dll build\%NAME%\patchers\
xcopy 98-install-bepinex.sh build\%NAME%\
xcopy 98-install-bepinex.bat build\%NAME%\
xcopy 99-uninstall-bepinex.sh build\%NAME%\
xcopy 99-uninstall-bepinex.bat build\%NAME%\


cd build
echo Packaging %NAME%-%VERSION%.zip
powershell Compress-Archive %NAME% %NAME%-%VERSION%.zip -Force
cd ..

SET RV=%ERRORLEVEL%
if "%CI%"=="" pause
exit /B %RV%
