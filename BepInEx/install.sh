echo Copying doorstop and helper scripts
echo Make sure to start the game via these!

if [ ! -f "../../../doorstop_config.ini" ]; then
	cp "doorstop_config.ini" "../../../doorstop_config.ini"
fi

if [ ! -d "../../../doorstop_libs" ]; then
	mkdir "../../../doorstop_libs"
fi
if [ ! -f "../../../doorstop_libs/libdoorstop_x64.so" ]; then
	cp "doorstop_nix/doorstop_libs/libdoorstop_x64.so" "../../../doorstop_libs/libdoorstop_x64.so"
fi
if [ ! -f "../../../doorstop_libs/libdoorstop_x64.dylib" ]; then
	cp "doorstop_nix/doorstop_libs/libdoorstop_x64.dylib" "../../../doorstop_libs/libdoorstop_x64.dylib"
fi
if [ ! -f "../../../doorstop_libs/libdoorstop_x86.so" ]; then
	cp "doorstop_nix/doorstop_libs/libdoorstop_x86.so" "../../../doorstop_libs/libdoorstop_x86.so"
fi
if [ ! -f "../../../doorstop_libs/libdoorstop_x86.dylib" ]; then
	cp "doorstop_nix/doorstop_libs/libdoorstop_x86.dylib" "../../../doorstop_libs/libdoorstop_x86.dylib"
fi

if [ -f "../../../7DaysToDie.x86_64" ]; then
	if [ ! -f "../../../startmodclient.sh" ]; then
		cp "doorstop_nix/startmodclient.sh" "../../../startmodclient.sh"
	fi
fi
if [ -f "../../../7DaysToDieServer.x86_64" ]; then
	if [ ! -f "../../../startmodserver.sh" ]; then
		cp "doorstop_nix/startmodserver.sh" "../../../startmodserver.sh"
	fi
fi

echo Copying BepInEx to be invoked by doorstop

if [ ! -d "../../../BepInEx" ]; then
	mkdir "../../../BepInEx"
fi
if [ ! -d "../../../BepInEx/core" ]; then
	mkdir "../../../BepInEx/core"
fi

if [ ! -f "../../../BepInEx/core/BepInEx.dll" ]; then
	cp "core/BepInEx.dll" "../../../BepInEx/core/BepInEx.dll"
fi
if [ ! -f "../../../BepInEx/core/BepInEx.xml" ]; then
	cp "core/BepInEx.xml" "../../../BepInEx/core/BepInEx.xml"
fi
if [ ! -f "../../../BepInEx/core/BepInEx.Preloader.dll" ]; then
	cp "core/BepInEx.Preloader.dll" "../../../BepInEx/core/BepInEx.Preloader.dll"
fi
if [ ! -f "../../../BepInEx/core/BepInEx.Preloader.xml" ]; then
	cp "core/BepInEx.Preloader.xml" "../../../BepInEx/core/BepInEx.Preloader.xml"
fi
if [ ! -f "../../../BepInEx/core/HarmonyXInterop.dll" ]; then
	cp "core/HarmonyXInterop.dll" "../../../BepInEx/core/HarmonyXInterop.dll"
fi
if [ ! -f "../../../BepInEx/core/HarmonyXInterop.dll" ]; then
	cp "core/HarmonyXInterop.dll" "../../../BepInEx/core/HarmonyXInterop.dll"
fi

if [ ! -d "../../../BepInEx/config" ]; then
	mkdir "../../../BepInEx/config"
fi
if [ ! -f "../../../BepInEx/config/BepInEx.cfg" ]; then
	cp "config/BepInEx.cfg" "../../../BepInEx/config/BepInEx.cfg"
fi

if [ ! -d "../../../BepInEx/patchers" ]; then
	mkdir "../../../BepInEx/patchers"
fi
if [ ! -f "../../../BepInEx/patchers/BepInEx.MultiFolderLoader.dll" ]; then
	cp "patchers/BepInEx.MultiFolderLoader.dll" "../../../BepInEx/patchers/BepInEx.MultiFolderLoader.dll"
fi

echo Installed BepInEx files into game folder
