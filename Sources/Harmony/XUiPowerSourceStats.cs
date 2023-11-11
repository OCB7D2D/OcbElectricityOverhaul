using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

public static class XUiPowerSourceStats
{

    // ####################################################################
    // ####################################################################

    static readonly CachedStringFormatterFloat powerFillFormatter = new CachedStringFormatterFloat();
    static readonly CachedStringFormatter<ushort> maxOutputFormatter = new CachedStringFormatter<ushort>((ushort _i) => _i.ToString());

    // ####################################################################
    // ####################################################################

    public static ushort GetMaxPower(OcbTileEntityPowerSource instance)
    {
        return !SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer ?
            (ushort)0 : (instance.GetPowerItem() as OcbPowerSource).MaxPower;
    }

    public static ushort GetLentConsumerUsed(OcbTileEntityPowerSource instance)
    {
        return !SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer ?
            instance.ClientData.LentConsumerUsed : (instance.GetPowerItem() as OcbPowerSource).LentConsumerUsed;
    }

    public static ushort GetLentChargingUsed(OcbTileEntityPowerSource instance)
    {
        return !SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer ?
            instance.ClientData.LentChargingUsed : (instance.GetPowerItem() as OcbPowerSource).LentChargingUsed;
    }

    public static ushort GetMaxOutput(OcbTileEntityPowerSource instance)
    {
        return !SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer ?
            instance.ClientData.MaxOutput : (instance.GetPowerItem() as OcbPowerSource).MaxOutput;
    }

    public static ushort GetMaxGridProduction(OcbTileEntityPowerSource instance)
    {
        return !SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer ?
            instance.ClientData.MaxGridProduction : (instance.GetPowerItem() as OcbPowerSource).MaxGridProduction;
    }

    public static ushort GetMaxProduction(OcbTileEntityPowerSource instance)
    {
        return !SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer ?
            instance.ClientData.MaxProduction : (instance.GetPowerItem() as OcbPowerSource).MaxProduction;
    }

    public static ushort GetStackPower(OcbTileEntityPowerSource instance)
    {
        return !SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer ?
            (ushort)0 : (instance.GetPowerItem() as OcbPowerSource).StackPower;
    }

    public static ushort GetLentConsumed(OcbTileEntityPowerSource instance)
    {
        return !SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer ?
            instance.ClientData.LentConsumed : (instance.GetPowerItem() as OcbPowerSource).LentConsumed;
    }

    public static ushort GetLentCharging(OcbTileEntityPowerSource instance)
    {
        return !SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer ?
            instance.ClientData.LentCharging : (instance.GetPowerItem() as OcbPowerSource).LentCharging;
    }

    public static ushort GetConsumerDemand(OcbTileEntityPowerSource instance)
    {
        return !SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer ?
            instance.ClientData.ConsumerDemand : (instance.GetPowerItem() as OcbPowerSource).ConsumerDemand;
    }

    public static ushort GetChargingDemand(OcbTileEntityPowerSource instance)
    {
        return !SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer ?
            instance.ClientData.ChargingDemand : (instance.GetPowerItem() as OcbPowerSource).ChargingDemand;
    }

    public static ushort GetConsumerUsed(OcbTileEntityPowerSource instance)
    {
        return !SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer ?
            instance.ClientData.ConsumerUsed : (instance.GetPowerItem() as OcbPowerSource).ConsumerUsed;
    }

    public static ushort GetChargingUsed(OcbTileEntityPowerSource instance)
    {
        return !SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer ?
            instance.ClientData.ChargingUsed : (instance.GetPowerItem() as OcbPowerSource).ChargingUsed;
    }

    public static ushort GetGridConsumerDemand(OcbTileEntityPowerSource instance)
    {
        return !SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer ?
            instance.ClientData.GridConsumerDemand : (instance.GetPowerItem() as OcbPowerSource).GridConsumerDemand;
    }

    public static ushort GetGridChargingDemand(OcbTileEntityPowerSource instance)
    {
        return !SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer ?
            instance.ClientData.GridChargingDemand : (instance.GetPowerItem() as OcbPowerSource).GridChargingDemand;
    }

    public static ushort GetGridConsumerUsed(OcbTileEntityPowerSource instance)
    {
        return !SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer ?
            instance.ClientData.GridConsumerUsed : (instance.GetPowerItem() as OcbPowerSource).GridConsumerUsed;
    }

    public static ushort GetGridChargingUsed(OcbTileEntityPowerSource instance)
    {
        return !SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer ?
            instance.ClientData.GridChargingUsed : (instance.GetPowerItem() as OcbPowerSource).GridChargingUsed;
    }

    public static ushort getBatteryLeft(OcbTileEntityPowerSource instance, int index)
    {
        if (!SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer) return (ushort)0;
        OcbPowerSource source = instance.GetPowerItem() as OcbPowerSource;
        if (source.Stacks[index].IsEmpty()) return (ushort)0;
        return (ushort)(source.Stacks[index].itemValue.MaxUseTimes
            - source.Stacks[index].itemValue.UseTimes);
    }

    public static ushort GetLightLevel(OcbTileEntityPowerSource instance)
    {
        if (SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
        {
            if (instance.GetPowerItem() is OcbPowerSolarPanel panel) return panel.LightLevel;
        }
        else
        {
            return instance.ClientData.LightLevel;
        }
        return 0;
    }

    public static ushort GetLocalGridConsumerUsed(OcbTileEntityPowerSource instance)
    {
        return (ushort)(GetConsumerUsed(instance) + GetGridConsumerUsed(instance));
    }
    public static ushort GetLocalGridConsumerDemand(OcbTileEntityPowerSource instance)
    {
        return (ushort)(GetConsumerDemand(instance) + GetGridConsumerDemand(instance));
    }
    public static ushort GetLocalGridChargingUsed(OcbTileEntityPowerSource instance)
    {
        return (ushort)(GetChargingUsed(instance) + GetGridChargingUsed(instance));
    }
    public static ushort GetLocalGridChargingDemand(OcbTileEntityPowerSource instance)
    {
        return (ushort)(GetChargingDemand(instance) + GetGridChargingDemand(instance));
    }

    public static ushort GetLocalGridUsed(OcbTileEntityPowerSource instance)
    {
        return (ushort)(GetLocalGridConsumerUsed(instance) + GetLocalGridChargingUsed(instance));
    }

    public static ushort GetLocalGridDemand(OcbTileEntityPowerSource instance)
    {
        return (ushort)(GetLocalGridConsumerDemand(instance) + GetLocalGridChargingDemand(instance));
    }

    public static string GetPercent(XUiC_PowerSourceStats __instance, float amount, float off)
    {
        return off == 0 ? "0" : maxOutputFormatter.Format((ushort)(100f * amount / off));
    }

    public static string GetFill(XUiC_PowerSourceStats __instance, float amount, float off)
    {
        return off == 0 ? "0" : powerFillFormatter.Format(amount / off);
    }

    // ####################################################################
    // ####################################################################

    [HarmonyPatch]
    class PatchPowerSourceStats
    {

        private static readonly string[] PowerSourceStatClasses = new string[]
            { "XUiC_PowerSourceStats", "XUiC_ULM_PowerSourceStats" };

        static IEnumerable<MethodBase> TargetMethods()
        {
            List<MethodBase> targets = new List<MethodBase>();
            foreach (string klass in PowerSourceStatClasses)
            {
                if (ReflectionUtils.TypeByName(klass) is Type vanilla)
                {
                    foreach (MethodInfo method in vanilla.GetMethods())
                    {
                        if (method.Name != "GetBindingValue") continue;
                        targets.Add(method);
                        break;
                    }
                }
            }
            return targets;
        }

        static float GetValue(string name, TileEntityPowerSource source)
        {
            if (!(source is OcbTileEntityPowerSource te)) return -1;
            if (float.TryParse(name, out float val)) return val;
            switch (name)
            {
                case "MaxOutput": return GetMaxOutput(te);
                case "MaxProduction": return GetMaxProduction(te);
                case "MaxGridProduction": return GetMaxGridProduction(te);
                case "LentConsumed": return GetLentConsumed(te);
                case "LentCharging": return GetLentCharging(te);
                case "LentConsumerUsed": return GetLentConsumerUsed(te);
                case "LentChargingUsed": return GetLentChargingUsed(te);
                case "ConsumerDemand": return GetConsumerDemand(te);
                case "ChargingDemand": return GetChargingDemand(te);
                case "ConsumerUsed": return GetConsumerUsed(te);
                case "ChargingUsed": return GetChargingUsed(te);
                case "GridConsumerDemand": return GetGridConsumerDemand(te);
                case "GridChargingDemand": return GetGridChargingDemand(te);
                case "GridConsumerUsed": return GetGridConsumerUsed(te);
                case "GridChargingUsed": return GetGridChargingUsed(te);
                case "LocalGridDemand": return GetLocalGridDemand(te);
                case "LocalConsumerDemand": return GetLocalGridConsumerDemand(te);
                case "LocalChargingDemand": return GetLocalGridChargingDemand(te);
                case "LocalGridUsed": return GetLocalGridUsed(te);
                case "LocalConsumerUsed": return GetLocalGridConsumerUsed(te);
                case "LocalChargingUsed": return GetLocalGridChargingUsed(te);
                case "LightLevel": return GetLightLevel(te);
                case "ushort.MaxValue": return ushort.MaxValue;
                default: Log.Error("Invalid Filler Argument {0}", name); break;
            }
            return -1;
        }

        static float GetValues(string name, TileEntityPowerSource te)
        {
            float sum = 1f;
            foreach (string multiplier in name.Split('*'))
            {
                sum *= GetValue(multiplier, te);
            }
            return sum;
        }

        // prefix all methods in someAssembly with a non-void return type and beginning with "Player"
        static void Postfix(
            XUiC_PowerSourceStats __instance,
            TileEntityPowerSource ___tileEntity,
            ref string value,
            string bindingName,
            ref bool __result)
        {
            // Special macro to create fillers
            // Probably not best for performance
            // Consider baking the ones you use!
            if (bindingName.StartsWith("Filler:"))
            {
                var parts = bindingName.Substring(7).Split('/');
                float dividend = 0; float divisor = 0;
                foreach (string part in parts[0].Split('+'))
                    dividend += GetValues(part, ___tileEntity);
                foreach (string part in parts[1].Split('+'))
                    divisor += GetValues(part, ___tileEntity);
                value = GetFill(__instance, dividend, divisor);
                __result = true;
                return;
            }
            else if (bindingName.StartsWith("Percent:"))
            {
                var parts = bindingName.Substring(8).Split('/');
                float dividend = 0; float divisor = 0;
                foreach (string part in parts[0].Split('+'))
                    dividend += GetValues(part, ___tileEntity);
                foreach (string part in parts[1].Split('+'))
                    divisor += GetValues(part, ___tileEntity);
                value = GetPercent(__instance, dividend, divisor);
                __result = true;
                return;
            }

            var te = ___tileEntity as OcbTileEntityPowerSource;

            // Regular code path
            switch (bindingName)
            {

                case "MaxPower": // unused
                    value = te == null ? "n/a" : maxOutputFormatter.Format(GetMaxPower(te));
                    __result = true;
                    break;
                case "MaxProduction":
                    value = te == null ? "n/a" : maxOutputFormatter.Format(GetMaxProduction(te));
                    __result = true;
                    break;
                case "MaxOutput":
                    value = te == null ? "n/a" : maxOutputFormatter.Format(GetMaxOutput(te));
                    __result = true;
                    break;
                case "StackPower": // unused
                    value = te == null ? "n/a" : maxOutputFormatter.Format(GetStackPower(te));
                    __result = true;
                    break;
                case "LightLevel": // unused
                    value = te == null ? "n/a" : maxOutputFormatter.Format(GetLightLevel(te));
                    __result = true;
                    break;

                // Grid values (information only)
                // ToDo: really needed (overhead)
                // ToDo: maybe only for single-player
                case "LentConsumed": // unused
                    value = te == null ? "n/a" : maxOutputFormatter.Format(GetLentConsumed(te));
                    __result = true;
                    break;
                case "LentCharging": // unused
                    value = te == null ? "n/a" : maxOutputFormatter.Format(GetLentCharging(te));
                    __result = true;
                    break;
                case "ConsumerDemand": // unused
                    value = te == null ? "n/a" : maxOutputFormatter.Format(GetConsumerDemand(te));
                    __result = true;
                    break;
                case "ChargingDemand": // unused
                    value = te == null ? "n/a" : maxOutputFormatter.Format(GetChargingDemand(te));
                    __result = true;
                    break;
                case "ConsumerUsed": // unused
                    value = te == null ? "n/a" : maxOutputFormatter.Format(GetConsumerUsed(te));
                    __result = true;
                    break;
                case "ChargingUsed": // battery only
                    value = te == null ? "n/a" : maxOutputFormatter.Format(GetChargingUsed(te));
                    __result = true;
                    break;
                case "GridConsumerDemand": // unused
                    value = te == null ? "n/a" : maxOutputFormatter.Format(GetGridConsumerDemand(te));
                    __result = true;
                    break;
                case "GridChargingDemand": // unused
                    value = te == null ? "n/a" : maxOutputFormatter.Format(GetGridChargingDemand(te));
                    __result = true;
                    break;
                case "GridConsumerUsed": // unused
                    value = te == null ? "n/a" : maxOutputFormatter.Format(GetGridConsumerUsed(te));
                    __result = true;
                    break;
                case "GridChargingUsed": // unused
                    value = te == null ? "n/a" : maxOutputFormatter.Format(GetGridChargingUsed(te));
                    __result = true;
                    break;
                case "LocalGridDemand": // used
                    value = te == null ? "n/a" : maxOutputFormatter.Format(GetLocalGridDemand(te));
                    __result = true;
                    break;
                case "LocalConsumerDemand": // used
                    value = te == null ? "n/a" : maxOutputFormatter.Format(GetLocalGridConsumerDemand(te));
                    __result = true;
                    break;
                case "LocalChargingDemand": // used
                    value = te == null ? "n/a" : maxOutputFormatter.Format(GetLocalGridChargingDemand(te));
                    __result = true;
                    break;
                case "LocalGridUsed": // used
                    value = te == null ? "n/a" : maxOutputFormatter.Format(GetLocalGridUsed(te));
                    __result = true;
                    break;
                case "LocalConsumerUsed": // used
                    value = te == null ? "n/a" : maxOutputFormatter.Format(GetLocalGridConsumerUsed(te));
                    __result = true;
                    break;
                case "LocalChargingUsed": // used
                    value = te == null ? "n/a" : maxOutputFormatter.Format(GetLocalGridChargingUsed(te));
                    __result = true;
                    break;

                case "LightLevelTitle":
                    value = Localization.Get("xuiLightLevel");
                    __result = true;
                    break;
                case "WindLevelTitle":
                    value = Localization.Get("xuiWindLevel");
                    __result = true;
                    break;
                case "OutputTitle":
                    value = Localization.Get("xuiOutput");
                    __result = true;
                    break;

                case "ChargingTitle":
                    value = Localization.Get("xuiCharge");
                    __result = true;
                    break;
                case "LocalSupplyTitle":
                    value = Localization.Get("xuiSupplied");
                    __result = true;
                    break;
                case "LocalChargingTitle":
                    value = Localization.Get("xuiCharged");
                    __result = true;
                    break;
                case "FlowTitle":
                    value = Localization.Get("xuiFlow");
                    __result = true;
                    break;

                case "PowerTooltip":
                    value = Localization.Get("xuiPowerTooltip");
                    __result = true;
                    break;
                case "OutputTooltip":
                    value = Localization.Get("xuiOutputTooltip");
                    __result = true;
                    break;
                case "ChargingTooltip":
                    value = Localization.Get("xuiChargeTooltip");
                    __result = true;
                    break;
                case "LocalConsumerTooltip":
                    value = Localization.Get("xuiConsumerTooltip");
                    __result = true;
                    break;
                case "LocalChargingTooltip":
                    value = Localization.Get("xuiChargingTooltip");
                    __result = true;
                    break;

                case "UsedConsumerFill": // used
                    value = te == null ? "0" :
                        GetFill(__instance, GetLentConsumed(te), GetMaxOutput(te));
                    __result = true;
                    break;
                case "UsedChargingFill": // used
                    value = te == null ? "0" :
                        GetFill(__instance, GetLentConsumed(te) + GetLentCharging(te), GetMaxOutput(te));
                    __result = true;
                    break;

                case "LentConsumerFill": // used
                    value = te == null ? "0" :
                        GetFill(__instance, GetLentConsumerUsed(te), GetLentConsumerUsed(te) + GetLentChargingUsed(te));
                    __result = true;
                    break;
                case "LentChargingFill": // used
                    value = te == null ? "0" :
                        GetFill(__instance, GetLentConsumerUsed(te) + GetLentChargingUsed(te), GetLentConsumerUsed(te) + GetLentChargingUsed(te));
                    __result = true;
                    break;

                case "ChargingFill": // battery only
                    value = te == null ? "0" :
                        GetFill(__instance, GetChargingUsed(te), GetChargingDemand(te));
                    __result = true;
                    break;
                case "GridConsumerFill": // unused
                    value = te == null ? "0" :
                        GetFill(__instance, GetGridConsumerUsed(te), GetGridConsumerDemand(te));
                    __result = true;
                    break;
                case "GridChargingFill": // unused
                    value = te == null ? "0" :
                        GetFill(__instance, GetGridChargingUsed(te), GetGridChargingDemand(te));
                    __result = true;
                    break;

                case "MaxGridProduction":
                    value = te == null ? "n/a" :
                        maxOutputFormatter.Format(GetMaxGridProduction(te));
                    __result = true;
                    break;

                case "Flow":
                    value = te == null ? "n/a" :
                        maxOutputFormatter.Format((ushort)(GetLentConsumerUsed(te) + GetLentChargingUsed(te)));
                    __result = true;
                    break;

                case "IsGenerator":
                    value = ___tileEntity == null ? "false" :
                        (___tileEntity.PowerItemType == PowerItem.PowerItemTypes.Generator).ToString();
                    __result = true;
                    break;
                case "NotGenerator":
                    value = ___tileEntity == null ? "false" :
                        (!(___tileEntity.PowerItemType == PowerItem.PowerItemTypes.Generator)).ToString();
                    __result = true;
                    break;
                case "IsBatteryBank":
                    value = ___tileEntity == null ? "false" :
                        (___tileEntity.PowerItemType == PowerItem.PowerItemTypes.BatteryBank).ToString();
                    __result = true;
                    break;
                case "NotBatteryBank":
                    value = ___tileEntity == null ? "false" :
                        (!(___tileEntity.PowerItemType == PowerItem.PowerItemTypes.BatteryBank)).ToString();
                    __result = true;
                    break;
                case "IsSolarBank":
                    value = ___tileEntity == null ? "false" :
                        (___tileEntity.PowerItemType == PowerItem.PowerItemTypes.SolarPanel &&
                        !___tileEntity.blockValue.Block.Properties.Contains("IsWindmill")).ToString();
                    __result = true;
                    break;
                case "NotSolarBank":
                    value = ___tileEntity == null ? "false" :
                        (!(___tileEntity.PowerItemType == PowerItem.PowerItemTypes.SolarPanel &&
                        !___tileEntity.blockValue.Block.Properties.Contains("IsWindmill"))).ToString();
                    __result = true;
                    break;
                case "IsWindMill":
                    value = ___tileEntity == null ? "false" :
                        (___tileEntity.PowerItemType == PowerItem.PowerItemTypes.SolarPanel &&
                        ___tileEntity.blockValue.Block.Properties.Contains("IsWindmill")).ToString();
                    __result = true;
                    break;
                case "NotWindMill":
                    value = ___tileEntity == null ? "false" :
                        (!(___tileEntity.PowerItemType == PowerItem.PowerItemTypes.SolarPanel &&
                        ___tileEntity.blockValue.Block.Properties.Contains("IsWindmill"))).ToString();
                    __result = true;
                    break;

                default:
                    break;
            }
        }
    }

    // ####################################################################
    // ####################################################################

}
