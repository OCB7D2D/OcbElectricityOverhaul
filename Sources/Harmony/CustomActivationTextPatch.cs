using HarmonyLib;
using System;
using UnityEngine;
using static OCB.ElectricityUtils;

public static class CustomActivationTextPatch
{

    // ####################################################################
    // Programmatically patch block.xml
    // ####################################################################

    [HarmonyPatch(typeof(Block))]
    [HarmonyPatch("AssignIds")]
    public class Block_AssignIds
    {
        static void Postfix()
        {
            foreach (Block block in Block.list)
            {
                if (block is BlockPowerSource || block is BlockPowered || block is BlockPoweredLight)
                {
                    // Patch display info for power blocks (show more info)
                    block.Properties.Values["RemoteDescription"] = "true";
                }
            }
        }
    }

    // ####################################################################
    // Hook into remote description mod to give more details on item hover
    // We need to redirect the activation text to custom description in order
    // to let us utilize the actual remote description functionality.
    // A bit more convoluted than wanted but works none the less.
    // ####################################################################

    [HarmonyPatch(typeof(BlockPowered))]
    [HarmonyPatch("GetActivationText")]
    public class BlockPowered_GetActivationText
    {
        public static void Postfix(
            BlockValue _blockValue,
            Vector3i _blockPos,
            ref string __result)
        {
            string desc = _blockValue.Block.GetCustomDescription(_blockPos, _blockValue);
            if (!string.IsNullOrEmpty(desc)) __result += "\n";
            __result += desc;
        }
    }

    [HarmonyPatch(typeof(BlockPoweredLight))]
    [HarmonyPatch("GetActivationText")]
    public class BlockPoweredLight_GetActivationText
    {
        public static void Postfix(
            BlockValue _blockValue,
            Vector3i _blockPos,
            ref string __result)
        {
            string desc = _blockValue.Block.GetCustomDescription(_blockPos, _blockValue);
            if (!string.IsNullOrEmpty(desc)) __result += "\n";
            __result += desc;
        }
    }

    [HarmonyPatch(typeof(BlockPowerSource))]
    [HarmonyPatch("GetActivationText")]
    public class BlockPowerSource_GetActivationText
    {
        public static void Postfix(
            BlockValue _blockValue,
            Vector3i _blockPos,
            ref string __result)
        {
            string desc = _blockValue.Block.GetCustomDescription(_blockPos, _blockValue);
            if (!string.IsNullOrEmpty(desc)) __result += "\n";
            __result += desc;
        }
    }

    // ####################################################################
    // This is the hook that will be queried on the server only
    // Here we access the stuff we need to send back to clients
    // ####################################################################

    [HarmonyPatch(typeof(Block))]
    [HarmonyPatch("GetCustomDescription")]
    public class Block_GetCustomDescription
    {

        private static string[] stateColors = new string[]
        {
            "FF0000","FF0400","FF0900","FF0E00","FF1200","FF1700","FF1C00","FF2100","FF2500","FF2A00","FF2F00","FF3300","FF3800","FF3D00","FF4200","FF4600","FF4B00","FF5000","FF5400","FF5900",
            "FF5E00","FF6300","FF6700","FF6C00","FF7100","FF7500","FF7A00","FF7F00","FF8400","FF8800","FF8D00","FF9200","FF9600","FF9B00","FFA000","FBA600","F7A700","F3A900","EFAA00","EBAB00",
            "E7AD00","E3AE00","DFB000","DBB100","D7B200","D3B400","CFB500","CCB700","C8B800","C4B900","C0BB00","BCBC00","B8BD00","B4BF00","B0C000","ACC200","A8C300","A4C400","A0C600","9CC700",
            "99C900","95CA00","91CB00","8DCD00","89CE00","85CF00","81D100","7DD200","79D400","75D500","71D600","6DD800","69D900","65DB00","62DC00","5EDD00","5ADF00","56E000","52E100","4EE300",
            "4AE400","46E600","42E700","3EE800","3AEA00","36EB00","32ED00","2FEE00","2BEF00","27F100","23F200","1FF300","1BF500","17F600","13F800","0FF900","0BFA00","07FC00","03FD00","00FF00",
        };

        public static string GetRepairText(OcbPowerSource source)
        {
            string str;
            float state = GetWorstStackItemUseState(source);
            if (state < -1) str = Localization.Get("xuiPowerStackConditionEmpty");
            else if (state < 0) str = Localization.Get("xuiPowerStackConditionBroken");
            else if (state < 0.05) str = Localization.Get("xuiPowerStackConditionBad");
            else if (state < 0.15) str = Localization.Get("xuiPowerStackConditionPoor");
            else if (state < 0.35) str = Localization.Get("xuiPowerStackConditionMedium");
            else if (state < 0.75) str = Localization.Get("xuiPowerStackConditionGood");
            else str = Localization.Get("xuiPowerStackConditionPerfect");
            string color = stateColors[(int)Mathf.Clamp(state * 99, 0, 99)];
            return string.Format("[{0}]{1}[-]", color, str);
        }

        public static string GetBatteryState(OcbPowerBatteryBank bank)
        {
            string str;
            float state = GetWorstStackItemUseState(bank);
            if (state < -1) str = Localization.Get("xuiBatteryStackConditionEmpty");
            else if (state < 0) str = Localization.Get("xuiBatteryStackConditionBroken");
            else if (state < 0.05) str = Localization.Get("xuiBatteryStackConditionBad");
            else if (state < 0.15) str = Localization.Get("xuiBatteryStackConditionPoor");
            else if (state < 0.35) str = Localization.Get("xuiBatteryStackConditionMedium");
            else if (state < 0.75) str = Localization.Get("xuiBatteryStackConditionGood");
            else str = Localization.Get("xuiBatteryStackConditionPerfect");
            string color = stateColors[(int)Mathf.Clamp(state * 99, 0, 99)];
            return string.Format("[{0}]{1}[-]", color, str);
        }

        public static string GetFuelState(OcbPowerGenerator generator)
        {
            string tmpl = Localization.Get("xuiGeneratorFuelCondition");
            float state = 100f * generator.CurrentFuel / generator.MaxFuel;
            string color = stateColors[(int)Mathf.Clamp(state, 0, 99)];
            return String.Format("[{0}]{1}[-]", color, String.Format(tmpl, state));
        }

        private static void AppendLine(ref string result, string str)
        {
            if (string.IsNullOrEmpty(result)) result = str;
            else result += "\n" + str;
        }

        public static void Postfix(
            Vector3i _blockPos,
            BlockValue _bv,
            ref string __result)
        {
            // Check if we are the server (clients don't have world)
            if (!(GameManager.Instance.World is WorldBase)) return;
            if (_bv.Block is BlockPowerSource block)
            {
                if (OcbPowerManager.Instance.PowerItemDictionary
                    .TryGetValue(_blockPos, out PowerItem pw))
                {
                    if (pw is OcbPowerSolarPanel solar)
                    {
                        var te = solar?.TileEntity as TileEntityPowerSource;
                        bool hasQuality = (bool)te?.SlotItem?.HasQuality;
                        var label = "xuiPowerSolarHoverNoQuality";
                        if (hasQuality) label = "xuiPowerSolarHover";
                        if (block.Properties.GetBool("IsWindmill"))
                        {
                            label = "xuiPowerWindHoverNoQuality";
                            if (hasQuality) label = "xuiPowerWindHover";
                        }
                        AppendLine(ref __result, string.Format(
                            Localization.Get(label),
                            solar.LastPowerUsed, solar.MaxProduction,
                            100f * solar.LightLevel / ushort.MaxValue,
                            hasQuality ? GetRepairText(solar) : ""));
                    }
                    else if (pw is OcbPowerGenerator generator)
                    {
                        AppendLine(ref __result, string.Format(
                            Localization.Get("xuiPowerGeneratorHover"),
                            generator.LastPowerUsed, generator.MaxProduction,
                            GetFuelState(generator)));
                    }
                    else if (pw is OcbPowerBatteryBank bank)
                    {
                        AppendLine(ref __result, string.Format(
                            Localization.Get("xuiPowerBatteryHover"),
                            bank.LastPowerUsed, bank.MaxProduction,
                            bank.ChargingUsed, bank.ChargingDemand,
                            GetBatteryState(bank)));
                    }
                }
            }
        }
    }

    // ####################################################################
    // ####################################################################

}

