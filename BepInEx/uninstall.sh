echo Deleting BepInEx to be invoked by doorstop

if [ -f "../../../BepInEx/cache/chainloader_typeloader.dat" ]; then
	rm "../../../BepInEx/cache/chainloader_typeloader.dat"
fi
if [ -f "../../../BepInEx/cache/harmony_interop_cache.dat" ]; then
	rm "../../../BepInEx/cache/harmony_interop_cache.dat"
fi
if [ -d "../../../BepInEx/cache" ]; then
	rmdir "../../../BepInEx/cache"
fi

if [ -f "../../../BepInEx/patchers/BepInEx.MultiFolderLoader.dll" ]; then
	rm "../../../BepInEx/patchers/BepInEx.MultiFolderLoader.dll"
fi
if [ -d "../../../BepInEx/patchers" ]; then
	rmdir "../../../BepInEx/patchers"
fi

if [ -f "../../../BepInEx/config/BepInEx.cfg" ]; then
	rm "../../../BepInEx/config/BepInEx.cfg"
fi
if [ -d "../../../BepInEx/config" ]; then
	rmdir "../../../BepInEx/config"
fi

if [ -f "../../../BepInEx/core/HarmonyXInterop.dll" ]; then
	rm "../../../BepInEx/core/HarmonyXInterop.dll"
fi
if [ -f "../../../BepInEx/core/HarmonyXInterop.dll" ]; then
	rm "../../../BepInEx/core/HarmonyXInterop.dll"
fi
if [ -f "../../../BepInEx/core/BepInEx.Preloader.xml" ]; then
	rm "../../../BepInEx/core/BepInEx.Preloader.xml"
fi
if [ -f "../../../BepInEx/core/BepInEx.Preloader.dll" ]; then
	rm "../../../BepInEx/core/BepInEx.Preloader.dll"
fi
if [ -f "../../../BepInEx/core/BepInEx.xml" ]; then
	rm "../../../BepInEx/core/BepInEx.xml"
fi
if [ -f "../../../BepInEx/core/BepInEx.dll" ]; then
	rm "../../../BepInEx/core/BepInEx.dll"
fi

if [ -d "../../../BepInEx/core" ]; then
	rmdir "../../../BepInEx/core"
fi

if [ -d "../../../BepInEx/plugins" ]; then
	rmdir "../../../BepInEx/plugins"
fi

if [ -f "../../../BepInEx/LogOutput.log" ]; then
	rm "../../../BepInEx/LogOutput.log"
fi

if [ -d "../../../BepInEx" ]; then
	rmdir "../../../BepInEx"
fi

echo Deleting doorstop concealed as winhttp.dll

if [ -f "../../../startmodserver.sh" ]; then
	rm "../../../startmodserver.sh"
fi

if [ -f "../../../doorstop_config.ini" ]; then
	rm "../../../doorstop_config.ini"
fi

if [ -f "../../../doorstop_libs/libdoorstop_x86.dylib" ]; then
	rm "../../../doorstop_libs/libdoorstop_x86.dylib"
fi
if [ -f "../../../doorstop_libs/libdoorstop_x86.so" ]; then
	rm "../../../doorstop_libs/libdoorstop_x86.so"
fi
if [ -f "../../../doorstop_libs/libdoorstop_x64.dylib" ]; then
	rm "../../../doorstop_libs/libdoorstop_x64.dylib"
fi
if [ -f "../../../doorstop_libs/libdoorstop_x64.so" ]; then
	rm "../../../doorstop_libs/libdoorstop_x64.so"
fi
if [ -d "../../../doorstop_libs" ]; then
	rmdir "../../../doorstop_libs"
fi

echo Removed BepInEx files from game folder
