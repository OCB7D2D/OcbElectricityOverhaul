using DMT;
using System;
using HarmonyLib;
using UnityEngine;
using System.Reflection;

using static OCB.ElectricityUtils;

public class OcbElectricityOption
{

    // Entry class for Harmony patching
    public class OcbElectricityOverhaul_Init : IHarmony
    {
        public void Start()
        {
            Debug.Log("Loading OCB Electricity Option Patch: " + GetType().ToString());
            var harmony = new Harmony(GetType().ToString());
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(GamePrefs))]
    [HarmonyPatch("initPropertyDecl")]
    public class GamePrefs_initPropertyDecl
    {
        static void Postfix(GamePrefs __instance)
        {
            int size = __instance.propertyList.Length;
            // Only apply once, check for our key
            for (int i = 0; i < size; i++)
            {
                GamePrefs.PropertyDecl pref = __instance.propertyList[i];
                if (pref.name == EnumGamePrefs.MinPowerForCharging) return;
            }

            // Otherwise add three new items
            Array.Resize(ref __instance.propertyList, size + 8);
            __instance.propertyList[size + 0] = new GamePrefs.PropertyDecl(EnumGamePrefs.BatterySelfCharge,
                true, GamePrefs.EnumType.Bool, (bool)false, (object)null, (object)null);
            __instance.propertyList[size + 1] = new GamePrefs.PropertyDecl(EnumGamePrefs.BatteryPowerPerUse,
                true, GamePrefs.EnumType.Int, (int)10, (object)null, (object)null);
            __instance.propertyList[size + 2] = new GamePrefs.PropertyDecl(EnumGamePrefs.MinPowerForCharging,
                true, GamePrefs.EnumType.Int, (int)20, (object)null, (object)null);
            __instance.propertyList[size + 3] = new GamePrefs.PropertyDecl(EnumGamePrefs.FuelPowerPerUse,
                true, GamePrefs.EnumType.Int, (int)20, (object)null, (object)null);

            __instance.propertyList[size + 4] = new GamePrefs.PropertyDecl(EnumGamePrefs.PowerPerPanel,
                true, GamePrefs.EnumType.Int, (int)30, (object)null, (object)null);
            __instance.propertyList[size + 5] = new GamePrefs.PropertyDecl(EnumGamePrefs.PowerPerEngine,
                true, GamePrefs.EnumType.Int, (int)50, (object)null, (object)null);
            __instance.propertyList[size + 6] = new GamePrefs.PropertyDecl(EnumGamePrefs.PowerPerBattery,
                true, GamePrefs.EnumType.Int, (int)50, (object)null, (object)null);
            __instance.propertyList[size + 7] = new GamePrefs.PropertyDecl(EnumGamePrefs.ChargePerBattery,
                true, GamePrefs.EnumType.Int, (int)35, (object)null, (object)null);

            size = __instance.propertyValues.Length;
            Array.Resize(ref __instance.propertyValues, size + 8);
            __instance.propertyValues[size + 0] = (bool) false;
            __instance.propertyValues[size + 1] = (int) 10;
            __instance.propertyValues[size + 2] = (int) 20;
            __instance.propertyValues[size + 3] = (int) 750;
            __instance.propertyValues[size + 4] = (int) 30;
            __instance.propertyValues[size + 5] = (int) 50;
            __instance.propertyValues[size + 6] = (int) 50;
            __instance.propertyValues[size + 7] = (int) 35;
}
    }

    [HarmonyPatch(typeof(GameModeSurvival))]
    [HarmonyPatch("GetSupportedGamePrefsInfo")]
    public class GameModeSurvival_GetSupportedGamePrefsInfo
    {
        static void Postfix(GameModeSurvival __instance,
            ref GameMode.ModeGamePref[] __result)
        {
            int size = __result.Length;
            // Only apply once, check for our key
            for (int i = 0; i < size; i++)
            {
                GameMode.ModeGamePref pref = __result[i];
                if (pref.GamePref == EnumGamePrefs.MinPowerForCharging) return;
            }
            // Otherwise add three new keys
            Array.Resize(ref __result, size + 8);
            __result[size + 0] = new GameMode.ModeGamePref(EnumGamePrefs.BatterySelfCharge, GamePrefs.EnumType.Bool, (bool) false);
            __result[size + 1] = new GameMode.ModeGamePref(EnumGamePrefs.BatteryPowerPerUse, GamePrefs.EnumType.Int, (int) 10);
            __result[size + 2] = new GameMode.ModeGamePref(EnumGamePrefs.MinPowerForCharging, GamePrefs.EnumType.Int, (int) 20);
            __result[size + 3] = new GameMode.ModeGamePref(EnumGamePrefs.FuelPowerPerUse, GamePrefs.EnumType.Int, (int) 750);
            __result[size + 4] = new GameMode.ModeGamePref(EnumGamePrefs.PowerPerPanel, GamePrefs.EnumType.Int, (int) 30);
            __result[size + 5] = new GameMode.ModeGamePref(EnumGamePrefs.PowerPerEngine, GamePrefs.EnumType.Int, (int) 50);
            __result[size + 6] = new GameMode.ModeGamePref(EnumGamePrefs.PowerPerBattery, GamePrefs.EnumType.Int, (int) 50);
            __result[size + 7] = new GameMode.ModeGamePref(EnumGamePrefs.ChargePerBattery, GamePrefs.EnumType.Int, (int) 35);
        }
    }

    

}
