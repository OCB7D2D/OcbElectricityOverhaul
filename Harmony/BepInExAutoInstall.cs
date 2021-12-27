using System.IO;
using UnityEngine;

public static class BepInExAutoInstall
{

    public static bool TryToCreateDirectory(string pwd, string dst)
    {
        // Bail out early if file already exists
        if (Directory.Exists(pwd + "/" + dst)) return true;
        try
        {
            Directory.CreateDirectory(pwd + "/" + dst);
            return true;
        }
        catch (IOException err)
        {
            Log.Warning("Could not copy " + dst);
            Log.Warning(err.ToString());
        }
        return false;
    }

    public static bool TryToCopyFile(string pwd, string src, string dst)
    {
        // Bail out early if file already exists
        if (File.Exists(pwd + "/" + dst)) return true;
        try
        {
            File.Copy(pwd + "/" + src, pwd + "/" + dst);
            return true;
        }
        catch (IOException err)
        {
            Log.Warning("Could not copy " + dst);
            Log.Warning(err.ToString());
        }
        return false;
    }

    // Method that tries to install necessary files into the main game folder
    // If it succeeds, you will need to restart the game to take advantage of it
    public static bool TryToInstallBepInEx(Mod mod)
    {
        bool rv = true;
        string pwd = Directory.GetCurrentDirectory();
        Log.Warning("BepInEx not found, trying to install necessary files, restart if successful!");
        rv &= TryToCreateDirectory(pwd, "/BepInEx");
        rv &= TryToCreateDirectory(pwd, "/BepInEx/core");
        rv &= TryToCreateDirectory(pwd, "/BepInEx/config");
        rv &= TryToCreateDirectory(pwd, "/BepInEx/patchers");
        rv &= TryToCopyFile(pwd, "Mods/" + mod.FolderName + "/BepInEx/doorstop_config.ini", "/doorstop_config.ini");
        rv &= TryToCopyFile(pwd, "Mods/" + mod.FolderName + "/BepInEx/core/BepInEx.dll", "/BepInEx/core/BepInEx.dll");
        rv &= TryToCopyFile(pwd, "Mods/" + mod.FolderName + "/BepInEx/core/BepInEx.xml", "/BepInEx/core/BepInEx.xml");
        rv &= TryToCopyFile(pwd, "Mods/" + mod.FolderName + "/BepInEx/core/BepInEx.Preloader.dll", "/BepInEx/core/BepInEx.Preloader.dll");
        rv &= TryToCopyFile(pwd, "Mods/" + mod.FolderName + "/BepInEx/core/BepInEx.Preloader.xml", "/BepInEx/core/BepInEx.Preloader.xml");
        rv &= TryToCopyFile(pwd, "Mods/" + mod.FolderName + "/BepInEx/core/HarmonyXInterop.dll", "/BepInEx/core/HarmonyXInterop.dll");
        rv &= TryToCopyFile(pwd, "Mods/" + mod.FolderName + "/BepInEx/config/BepInEx.cfg", "/BepInEx/config/BepInEx.cfg");
        rv &= TryToCopyFile(pwd, "Mods/" + mod.FolderName + "/BepInEx/patchers/BepInEx.MultiFolderLoader.dll", "/BepInEx/patchers/BepInEx.MultiFolderLoader.dll");

        if (Application.platform == RuntimePlatform.WindowsPlayer)
        {
            rv &= TryToCopyFile(pwd, "Mods/" + mod.FolderName + "/BepInEx/doorstop_win/winhttp.dll", "/winhttp.dll");
        }
        if (Application.platform == RuntimePlatform.LinuxPlayer)
        {
            rv &= TryToCreateDirectory(pwd, "/doorstop_libs");
            rv &= TryToCopyFile(pwd, "Mods/" + mod.FolderName + "/BepInEx/doorstop_nix/doorstop_libs/libdoorstop_x64.so", "/doorstop_libs/libdoorstop_x64.so");
            rv &= TryToCopyFile(pwd, "Mods/" + mod.FolderName + "/BepInEx/doorstop_nix/doorstop_libs/libdoorstop_x86.so", "/doorstop_libs/libdoorstop_x86.so");
            if (File.Exists(pwd + "/7DaysToDieServer.x86_64")) rv &= TryToCopyFile(pwd, "Mods/" + mod.FolderName + "/BepInEx/doorstop_nix/startmodserver.sh", "/startmodserver.sh");
            if (File.Exists(pwd + "/7DaysToDie.x86_64")) rv &= TryToCopyFile(pwd, "Mods/" + mod.FolderName + "/BepInEx/doorstop_nix/startmodclient.sh", "/startmodclient.sh");
        }
        if (Application.platform == RuntimePlatform.OSXPlayer)
        {
            rv &= TryToCreateDirectory(pwd, "/doorstop_libs");
            rv &= TryToCopyFile(pwd, "Mods/" + mod.FolderName + "/BepInEx/doorstop_nix/doorstop_libs/libdoorstop_x64.dylib", "/doorstop_libs/libdoorstop_x64.dylib");
            rv &= TryToCopyFile(pwd, "Mods/" + mod.FolderName + "/BepInEx/doorstop_nix/doorstop_libs/libdoorstop_x86.dylib", "/doorstop_libs/libdoorstop_x86.dylib");
            if (File.Exists(pwd + "/7DaysToDieServer.x86_64")) rv &= TryToCopyFile(pwd, "Mods/" + mod.FolderName + "/BepInEx/doorstop_nix/startmodserver.sh", "/startmodserver.sh");
            if (File.Exists(pwd + "/7DaysToDie.x86_64")) rv &= TryToCopyFile(pwd, "Mods/" + mod.FolderName + "/BepInEx/doorstop_nix/startmodclient.sh", "/startmodclient.sh");
        }
        if (rv) Log.Warning("BepInEx installed successfully, please restart the game and this message should go away!");
        else
        {
            Log.Error("!! BepInEx installed seems to have failed, please restart the game and if you still see this message again !!");
            Log.Error("!! In case you do, remove this mod and also uninstall orphaned BepInEx files from the game folder !!");
        }
        return rv;
    }

}
