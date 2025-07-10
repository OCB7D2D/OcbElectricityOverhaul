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
xcopy %FOLDER%\*.dll build\%NAME%\
xcopy %FOLDER%\README.md build\%NAME%\
xcopy %FOLDER%\Config build\%NAME%\Config\ /S
xcopy %FOLDER%\Resources build\%NAME%\Resources\ /S
xcopy %FOLDER%\UIAtlases build\%NAME%\UIAtlases\ /S

cd build
echo Packaging %NAME%-%VERSION%.zip
powershell Compress-Archive %NAME% %NAME%-%VERSION%-V2.0.zip -Force
cd ..

SET RV=%ERRORLEVEL%
if "%CI%"=="" pause
exit /B %RV%

:MAIN

SET FOLDER=.
SET NAME=OcbElectricityOverhaul
call :BUILD 

REM SET FOLDER=Addons\ZMXuiCPOCBEO
REM SET NAME=ZMXuiCPOCBEO
REM call :BUILD 

