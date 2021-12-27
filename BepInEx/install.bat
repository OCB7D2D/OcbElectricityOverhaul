echo off

echo Copying doorstop concealed as winhttp.dll

if not exist "..\..\..\winhttp.dll" ( 
	copy "doorstop_win\winhttp.dll" "..\..\..\winhttp.dll"
)
if not exist "..\..\..\doorstop_config.ini" ( 
	copy "doorstop_config.ini" "..\..\..\doorstop_config.ini"
)

echo Copying BepInEx to be invoked by doorstop

if not exist "..\..\..\BepInEx" ( 
	mkdir "..\..\..\BepInEx"
)
if not exist "..\..\..\BepInEx\core" ( 
	mkdir "..\..\..\BepInEx\core"
)

if not exist "..\..\..\BepInEx\core\BepInEx.dll" ( 
	copy "core\BepInEx.dll" "..\..\..\BepInEx\core\BepInEx.dll"
)
if not exist "..\..\..\BepInEx\core\BepInEx.xml" ( 
	copy "core\BepInEx.xml" "..\..\..\BepInEx\core\BepInEx.xml"
)
if not exist "..\..\..\BepInEx\core\BepInEx.Preloader.dll" ( 
	copy "core\BepInEx.Preloader.dll" "..\..\..\BepInEx\core\BepInEx.Preloader.dll"
)
if not exist "..\..\..\BepInEx\core\BepInEx.Preloader.xml" ( 
	copy "core\BepInEx.Preloader.xml" "..\..\..\BepInEx\core\BepInEx.Preloader.xml"
)
if not exist "..\..\..\BepInEx\core\HarmonyXInterop.dll" ( 
	copy "core\HarmonyXInterop.dll" "..\..\..\BepInEx\core\HarmonyXInterop.dll"
)
if not exist "..\..\..\BepInEx\core\HarmonyXInterop.dll" ( 
	copy "core\HarmonyXInterop.dll" "..\..\..\BepInEx\core\HarmonyXInterop.dll"
)

if not exist "..\..\..\BepInEx\config" ( 
	mkdir "..\..\..\BepInEx\config"
)
if not exist "..\..\..\BepInEx\config\BepInEx.cfg" ( 
	copy "config\BepInEx.cfg" "..\..\..\BepInEx\config\BepInEx.cfg"
)

if not exist "..\..\..\BepInEx\patchers" ( 
	mkdir "..\..\..\BepInEx\patchers"
)
if not exist "..\..\..\BepInEx\patchers\BepInEx.MultiFolderLoader.dll" ( 
	copy "patchers\BepInEx.MultiFolderLoader.dll" "..\..\..\BepInEx\patchers\BepInEx.MultiFolderLoader.dll"
)

echo Installed BepInEx files into game folder
