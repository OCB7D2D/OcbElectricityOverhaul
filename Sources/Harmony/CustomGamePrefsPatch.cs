using HarmonyLib;
using OCB;
using Platform;
using System;
using System.Collections.Generic;

static public class CustomGamePrefsPatch
{

    // ####################################################################
    // ####################################################################

    // Store two maps for each custom extended enum type.
    // One to map from name to index, the other vice-versa.
    // We could also just use a `NameIdMapping`, but it has
    // some drawbacks in its API, so we just re-implement it.
    // There isn't much rocket science about it anyway ;)
    public static Dictionary<string, object> Name2Int
        = new Dictionary<string, object>();
    public static Dictionary<int, string> Int2Name
        = new Dictionary<int, string>();

    // ####################################################################
    // ####################################################################

    // Overwrite the `Last` identifier, as it isn't used
    static EnumGamePrefs CurrentPref = EnumGamePrefs.Last - 1;

    public static GamePrefs.PropertyDecl NewBoolean(string name, bool value)
    {
        EnumGamePrefs cur = ++CurrentPref;
        Name2Int[name] = cur; Int2Name[(int)cur] = name;
        return new GamePrefs.PropertyDecl(cur, DeviceFlag.None,
            GamePrefs.EnumType.Bool, value, null, null);
    }

    public static GamePrefs.PropertyDecl NewInteger(string name, int value)
    {
        EnumGamePrefs cur = ++CurrentPref;
        Name2Int[name] = cur; Int2Name[(int)cur] = name;
        return new GamePrefs.PropertyDecl(cur, DeviceFlag.None,
            GamePrefs.EnumType.Int, value, null, null);
    }

    // ####################################################################
    // ####################################################################

    static readonly GamePrefs.PropertyDecl[] CustomPrefs = new GamePrefs.PropertyDecl[]
    {
        NewBoolean("PreferFuelOverBattery", ElectricityUtils.PreferFuelOverBatteryDefault),
        NewInteger("BatteryPowerPerUse", ElectricityUtils.BatteryPowerPerUseDefault),
        NewInteger("DegradationFactor", ElectricityUtils.DegradationFactor),
        NewInteger("FuelPowerPerUse", ElectricityUtils.FuelPowerPerUseDefault),
        NewInteger("PowerPerPanel", ElectricityUtils.PowerPerPanelDefault),
        NewInteger("PowerPerEngine", ElectricityUtils.PowerPerEngineDefault),
        NewInteger("PowerPerBattery", ElectricityUtils.PowerPerBatteryDefault),
        NewInteger("MinPowerForCharging", ElectricityUtils.MinPowerForChargingDefault),
        NewInteger("BatteryChargePercentFull", ElectricityUtils.BatteryChargePercentFullDefault),
        NewInteger("BatteryChargePercentEmpty", ElectricityUtils.BatteryChargePercentEmptyDefault),
    };

    // ####################################################################
    // ####################################################################

    // Get some private methods via reflection to mess with vanilla internals
    static readonly HarmonyFieldProxy<GamePrefs.PropertyDecl[]> GamePrefsPropList = new
        HarmonyFieldProxy<GamePrefs.PropertyDecl[]>(typeof(GamePrefs), "propertyList");
    static readonly HarmonyFieldProxy<object[]> GamePrefsPropValues = new
        HarmonyFieldProxy<object[]>(typeof(GamePrefs), "propertyValues");

    static public void InitCustomGamePrefs()
    {
        object[] defaults = GamePrefsPropValues.Get(GamePrefs.Instance);
        GamePrefs.PropertyDecl[] prefs = GamePrefs.Instance.GetPropertyList();
        prefs = prefs.AddRangeToArray(CustomPrefs);
        defaults = defaults.AddRangeToArray(Array.
            ConvertAll(CustomPrefs, x => x.defaultValue));
        GamePrefsPropList.Set(GamePrefs.Instance, prefs);
        GamePrefsPropValues.Set(GamePrefs.Instance, defaults);
    }

    // ####################################################################
    // ####################################################################

    [HarmonyPatch(typeof(GameModeSurvival))]
    [HarmonyPatch("GetSupportedGamePrefsInfo")]

    public class GameModeSurvival_GetSupportedGamePrefsInfo
    {
        static void Postfix(ref GameMode.ModeGamePref[] __result)
        {
            // Extend array by number of additional game prefs
            // Add our new game preferences to result array
            __result = __result.AddRangeToArray(Array.ConvertAll(CustomPrefs,
                p => new GameMode.ModeGamePref(p.name, p.type, p.defaultValue)));
        }
    }

    // ####################################################################
    // ####################################################################

    [HarmonyPatch(typeof(Enum))]
    [HarmonyPatch("GetName")]
    public class CustomEnums_EnumGetName
    {
        static bool Prefix(Type enumType, object value, ref string __result)
        {
            if (!(value is int idx)) return true;
            if (enumType != typeof(EnumGamePrefs)) return true;
            if (Int2Name.TryGetValue(idx, out __result))
                return false;
            return true;
        }
    }

    // ####################################################################
    // ####################################################################

    [HarmonyPatch(typeof(Enum))]
    [HarmonyPatch("Parse")]
    [HarmonyPatch(new Type[] {
        typeof(Type),
        typeof(string),
        typeof(bool) })]

    public class CustomEnums_EnumParse
    {
        static bool Prefix(Type enumType, string value, ref object __result)
        {
            if (enumType != typeof(EnumGamePrefs)) return true;
            if (Name2Int.TryGetValue(value, out __result))
                return false;
            return true;
        }
    }

    // ####################################################################
    // ####################################################################

}
