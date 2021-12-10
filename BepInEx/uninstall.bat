echo off

echo Deleting BepInEx to be invoked by doorstop

if exist "..\..\..\BepInEx\cache\chainloader_typeloader.dat" ( 
	del "..\..\..\BepInEx\cache\chainloader_typeloader.dat"
)
if exist "..\..\..\BepInEx\cache\harmony_interop_cache.dat" ( 
	del "..\..\..\BepInEx\cache\harmony_interop_cache.dat"
)
if exist "..\..\..\BepInEx\cache" ( 
	rmdir "..\..\..\BepInEx\cache"
)

if exist "..\..\..\BepInEx\patchers\BepInEx.MultiFolderLoader.dll" ( 
	del "..\..\..\BepInEx\patchers\BepInEx.MultiFolderLoader.dll"
)
if exist "..\..\..\BepInEx\patchers" ( 
	rmdir "..\..\..\BepInEx\patchers"
)

if exist "..\..\..\BepInEx\config\BepInEx.cfg" ( 
	del "..\..\..\BepInEx\config\BepInEx.cfg"
)
if exist "..\..\..\BepInEx\config" ( 
	rmdir "..\..\..\BepInEx\config"
)

if exist "..\..\..\BepInEx\core\HarmonyXInterop.dll" ( 
	del "..\..\..\BepInEx\core\HarmonyXInterop.dll"
)
if exist "..\..\..\BepInEx\core\HarmonyXInterop.dll" ( 
	del "..\..\..\BepInEx\core\HarmonyXInterop.dll"
)
if exist "..\..\..\BepInEx\core\BepInEx.Preloader.xml" ( 
	del "..\..\..\BepInEx\core\BepInEx.Preloader.xml"
)
if exist "..\..\..\BepInEx\core\BepInEx.Preloader.dll" ( 
	del "..\..\..\BepInEx\core\BepInEx.Preloader.dll"
)
if exist "..\..\..\BepInEx\core\BepInEx.xml" ( 
	del "..\..\..\BepInEx\core\BepInEx.xml"
)
if exist "..\..\..\BepInEx\core\BepInEx.dll" ( 
	del "..\..\..\BepInEx\core\BepInEx.dll"
)

if exist "..\..\..\BepInEx\core" ( 
	rmdir "..\..\..\BepInEx\core"
)

if exist "..\..\..\BepInEx\plugins" ( 
	rmdir "..\..\..\BepInEx\plugins"
)

if exist "..\..\..\BepInEx\LogOutput.log" ( 
	del "..\..\..\BepInEx\LogOutput.log"
)

if exist "..\..\..\BepInEx" ( 
	rmdir "..\..\..\BepInEx"
)

echo Deleting doorstop concealed as winhttp.dll

if exist "..\..\..\doorstop_config.ini" ( 
	del "..\..\..\doorstop_config.ini"
)
if exist "..\..\..\winhttp.dll" ( 
	del "..\..\..\winhttp.dll"
)

echo Removed BepInEx files from game folder
