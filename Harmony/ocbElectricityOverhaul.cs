using System;
using System.Xml;
using System.IO;
using HarmonyLib;
using System.Reflection;
using System.Collections.Generic;
using XMLData.Parsers;
using UnityEngine;

using static OCB.ElectricityUtils;

public class OcbElectricityOverhaul : IModApi
{

    // Entry class for A20 patching
    public void InitMod(Mod mod)
    {
        Log.Out("Loading OCB Electricity Overhaul Patch: " + GetType().ToString());

        // Check if BepInEx was loaded and did its job correctly
        if (AccessTools.Field(typeof(PowerSource), "LentConsumed") == null) {
            BepInExAutoInstall.TryToInstallBepInEx(mod);
            // Log.Error("Required BepInEx patches not found, exiting!");
            // Application.Quit();
            return;
        }

        var harmony = new Harmony(GetType().ToString());
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }

    // Patch PowerManager to return a different (our) instance
    // This is the main hook to get our manager into place
    [HarmonyPatch(typeof(PowerManager))]
    [HarmonyPatch("get_Instance")]
    public class PowerManager_Get_Instance
    {
        static bool Prefix()
        {
            if (PowerManager.instance == null)
            {
                PowerManager.instance = new OcbPowerManager();
            }

            return true;
        }
    }

    // Main overload to allow wire connections between power sources
    [HarmonyPatch(typeof(TileEntityPowerSource))]
    [HarmonyPatch("CanHaveParent")]
    public class TileEntityPowerSource_CanHaveParent
    {
        static bool Prefix(TileEntityPowerSource __instance, ref bool __result, IPowered powered)
        {
            __result = __instance.PowerItemType == PowerItem.PowerItemTypes.BatteryBank ||
                       __instance.PowerItemType == PowerItem.PowerItemTypes.SolarPanel ||
                       __instance.PowerItemType == PowerItem.PowerItemTypes.Generator;
            return false;
        }
    }

    private static void RecalcChargingDemand(PowerBatteryBank bank)
    {
        bank.ChargingDemand = 0;
        float factor = bank.OutputPerStack / (float)PowerPerBattery;
        foreach (var slot in bank.Stacks)
        {
            if (slot.IsEmpty()) continue;
            // Check if battery could use some charging
            if (slot.itemValue.UseTimes <= 0) continue;
            // ToDo: should we cap at what is actually needed?
            bank.ChargingDemand += (ushort)(factor *
                GetChargeByQuality(slot.itemValue));
        }
    }

    private static void PowerSourceUpdateSlots(PowerSource source)
    {
        float powerPerSlot = 1f, defaultPower = 1f;
        if (source is PowerSolarPanel)
        {
            defaultPower = PowerPerPanelDefault;
            powerPerSlot = PowerPerPanel;
        }
        else if (source is PowerGenerator)
        {
            defaultPower = PowerPerEngineDefault;
            powerPerSlot = PowerPerEngine;
        }
        else if (source is PowerBatteryBank bank)
        {
            defaultPower = PowerPerBatteryDefault;
            powerPerSlot = PowerPerBattery;
            RecalcChargingDemand(bank);
        }
        source.StackPower = 0;
        float factor = source.OutputPerStack / powerPerSlot;
        foreach (var stack in source.Stacks)
        {
            if (stack.IsEmpty()) continue;
            source.StackPower += (ushort)(factor *
                GetSlotPowerByQuality(stack.itemValue,
                    powerPerSlot, defaultPower));
        }
        source.StackPower = (ushort)Mathf.Min(
            source.StackPower, source.MaxPower);
    }

    [HarmonyPatch(typeof(PowerSource))]
    [HarmonyPatch(MethodType.Constructor)]
    public class PowerSource_Ctor
    {
        static void Postfix(PowerSource __instance)
        {
            __instance.ChargeFromSolar = true;
        }
    }

    [HarmonyPatch(typeof(PowerSource))]
    [HarmonyPatch("SetSlots")]
    public class PowerSource_SetSlots
    {
        static void Postfix(PowerSource __instance)
        {
            PowerSourceUpdateSlots(__instance);
        }
    }

    [HarmonyPatch(typeof(PowerSource))]
    [HarmonyPatch("TryAddItemToSlot")]
    public class PowerSource_TryAddItemToSlot
    {
        static void Postfix(PowerSource __instance)
        {
            PowerSourceUpdateSlots(__instance);
        }
    }

    [HarmonyPatch(typeof(PowerSource))]
    [HarmonyPatch("write")]
    public class PowerSource_write
    {
        static void Postfix(PowerSource __instance, BinaryWriter _bw)
        {
            _bw.Write(__instance.ChargeFromSolar);
            _bw.Write(__instance.ChargeFromGenerator);
            _bw.Write(__instance.ChargeFromBattery);
        }
    }

    [HarmonyPatch(typeof(PowerSource))]
    [HarmonyPatch("read")]
    public class PowerSource_read
    {
        static void Postfix(PowerSource __instance, BinaryReader _br)
        {
            // We resolve enum dynamically on runtime, since we don't want to
            // hard-code a specific value into our own runtime. This allows
            // compatibility even if game dll alters the enum between version.
            if (GamePrefs.GetBool(EnumParser.Parse<EnumGamePrefs>("LoadVanillaMap"))) {
                __instance.ChargeFromSolar = true;
                __instance.ChargeFromGenerator = true;
                __instance.ChargeFromBattery = false;
                return;
            }
            __instance.ChargeFromSolar = _br.ReadBoolean();
            __instance.ChargeFromGenerator = _br.ReadBoolean();
            __instance.ChargeFromBattery = _br.ReadBoolean();
            PowerSourceUpdateSlots(__instance);
        }
    }

    [HarmonyPatch(typeof(PowerSolarPanel))]
    [HarmonyPatch("RefreshPowerStats")]
    public class PowerSolarPanel_RefreshPowerStats
    {
        static void Postfix(PowerSolarPanel __instance)
        {
            Block block = Block.list[__instance.BlockID];
            if (!block.Properties.Values.ContainsKey("MaxPower")) return;
            __instance.MaxPower = ushort.Parse(block.Properties.Values["MaxPower"]);
        }
    }

    [HarmonyPatch(typeof(PowerSource))]
    [HarmonyPatch("RefreshPowerStats")]
    public class PowerSource_RefreshPowerStats
    {
        static void Postfix(PowerSolarPanel __instance)
        {
            Block block = Block.list[__instance.BlockID];
            if (!block.Properties.Values.ContainsKey("MaxPower")) return;
            __instance.MaxPower = ushort.Parse(block.Properties.Values["MaxPower"]);
        }
    }

    // Main overload to allow wire connections between power sources
    [HarmonyPatch(typeof(TileEntityPowerSource))]
    [HarmonyPatch("write")]
    public class TileEntityPowerSource_write
    {
        static void Postfix(PooledBinaryWriter _bw, TileEntity.StreamModeWrite _eStreamMode,
            TileEntityPowerSource __instance, PowerItem.PowerItemTypes ___PowerItemType, PowerItem ___PowerItem)
        {
            PowerSource source = ___PowerItem as PowerSource;
            switch (_eStreamMode)
            {
                case TileEntity.StreamModeWrite.Persistency:
                    break;
                case TileEntity.StreamModeWrite.ToServer:
                    _bw.Write(__instance.ClientData.ChargeFromSolar);
                    _bw.Write(__instance.ClientData.ChargeFromGenerator);
                    _bw.Write(__instance.ClientData.ChargeFromBattery);
                    break;
                default: // ToClient
                    _bw.Write(source != null);
                    if (source == null)
                        break;
                    // ToDo: check if we need em all (now 180 bytes)
                    _bw.Write(source.MaxProduction);
                    _bw.Write(source.MaxGridProduction);
                    _bw.Write(source.ChargingUsed);
                    _bw.Write(source.ChargingDemand);
                    _bw.Write(source.ConsumerUsed);
                    _bw.Write(source.ConsumerDemand);
                    _bw.Write(source.LentConsumed);
                    _bw.Write(source.LentCharging);
                    _bw.Write(source.GridConsumerDemand);
                    _bw.Write(source.GridChargingDemand);
                    _bw.Write(source.GridConsumerUsed);
                    _bw.Write(source.GridChargingUsed);
                    _bw.Write(source.LentConsumerUsed);
                    _bw.Write(source.LentChargingUsed);
                    _bw.Write(source.ChargeFromSolar);
                    _bw.Write(source.ChargeFromGenerator);
                    _bw.Write(source.ChargeFromBattery);
                    if (source is PowerSolarPanel panel)
                    {
                        _bw.Write(panel.LightLevel);
                    }
                    else
                    {
                        _bw.Write(false);
                    }
                    break;
            }
        }
    }

    // Main overload to allow wire connections between power sources
    [HarmonyPatch(typeof(TileEntityPowerSource))]
    [HarmonyPatch("read")]
    public class TileEntityPowerSource_read
    {
        static void Postfix(PooledBinaryReader _br, TileEntity.StreamModeRead _eStreamMode,
            TileEntityPowerSource __instance, PowerItem.PowerItemTypes ___PowerItemType)
        {
            switch (_eStreamMode)
            {
                case TileEntity.StreamModeRead.Persistency:
                    break;
                case TileEntity.StreamModeRead.FromClient:
                    __instance.ClientData.ChargeFromSolar = _br.ReadBoolean();
                    __instance.ClientData.ChargeFromGenerator = _br.ReadBoolean();
                    __instance.ClientData.ChargeFromBattery = _br.ReadBoolean();
                    PowerSource powerItem = __instance.PowerItem as PowerSource;
                    powerItem.ChargeFromSolar = __instance.ClientData.ChargeFromSolar;
                    powerItem.ChargeFromGenerator = __instance.ClientData.ChargeFromGenerator;
                    powerItem.ChargeFromBattery = __instance.ClientData.ChargeFromBattery;
                    break;
                default: // FromServer
                    if (!_br.ReadBoolean())
                        break;
                    // ToDo: check if we need em all (now 180 bytes)
                    __instance.ClientData.MaxProduction = _br.ReadUInt16();
                    __instance.ClientData.MaxGridProduction = _br.ReadUInt16();
                    __instance.ClientData.ChargingUsed = _br.ReadUInt16();
                    __instance.ClientData.ChargingDemand = _br.ReadUInt16();
                    __instance.ClientData.ConsumerUsed = _br.ReadUInt16();
                    __instance.ClientData.ConsumerDemand = _br.ReadUInt16();
                    __instance.ClientData.LentConsumed = _br.ReadUInt16();
                    __instance.ClientData.LentCharging = _br.ReadUInt16();
                    __instance.ClientData.GridConsumerDemand = _br.ReadUInt16();
                    __instance.ClientData.GridChargingDemand = _br.ReadUInt16();
                    __instance.ClientData.GridConsumerUsed = _br.ReadUInt16();
                    __instance.ClientData.GridChargingUsed = _br.ReadUInt16();
                    __instance.ClientData.LentConsumerUsed = _br.ReadUInt16();
                    __instance.ClientData.LentChargingUsed = _br.ReadUInt16();
                    __instance.ClientData.ChargeFromSolar = _br.ReadBoolean();
                    __instance.ClientData.ChargeFromGenerator = _br.ReadBoolean();
                    __instance.ClientData.ChargeFromBattery = _br.ReadBoolean();
                    __instance.ClientData.LightLevel = _br.ReadUInt16();
                    break;
            }
        }
    }

    // Allow config defaults to be set via xml
    [HarmonyPatch(typeof(XUiFromXml))]
    [HarmonyPatch("loadWindows")]
    public class XUiFromXml_loadWindows
    {
        static void Prefix(XmlFile _xmlFile)
        {
            foreach (XmlNode node in _xmlFile.XmlDoc.DocumentElement.ChildNodes)
            {
                if (node.NodeType != XmlNodeType.Element) continue;
                if (!node.Name.Equals("electricity-overhaul")) continue;
                foreach (XmlNode child in node.ChildNodes)
                {
                    if (child.NodeType != XmlNodeType.Element) continue;
                    if (!child.Name.Equals("property")) continue;
                    var el = child as XmlElement;
                    string name = el.GetAttribute("name");
                    string value = el.GetAttribute("value");
                    switch (name)
                    {
                        case "IsPreferFuelOverBattery": IsPreferFuelOverBattery = bool.Parse(value); break;
                        case "BatteryPowerPerUse": BatteryPowerPerUse = int.Parse(value); break;
                        case "FuelPowerPerUse": FuelPowerPerUse = int.Parse(value); break;
                        case "PowerPerPanel": PowerPerPanel = int.Parse(value); break;
                        case "PowerPerEngine": PowerPerEngine = int.Parse(value); break;
                        case "PowerPerBattery": PowerPerBattery = int.Parse(value); break;
                        case "MinPowerForCharging": MinPowerForCharging = int.Parse(value); break;
                        case "BatteryChargePercentFull": BatteryChargePercentFull = int.Parse(value); break;
                        case "BatteryChargePercentEmpty": BatteryChargePercentEmpty = int.Parse(value); break;
                    }
                }
            }
        }
    }

    // Force defaults when being asked for it
    [HarmonyPatch(typeof(GameMode))]
    [HarmonyPatch("GetGamePrefs")]
    public class GameMode_GetGamePrefs
    {

        static void UpdateDefault(Dictionary<EnumGamePrefs, GameMode.ModeGamePref> prefs, string name, object value)
        {
            var id = EnumParser.Parse<EnumGamePrefs>(name);
            var cpy = prefs[id];
            cpy.DefaultValue = value;
            prefs[id] = cpy;
        }

        static void Postfix(ref Dictionary<EnumGamePrefs, GameMode.ModeGamePref> __result)
        {
            UpdateDefault(__result, "PreferFuelOverBattery", IsPreferFuelOverBattery);
            UpdateDefault(__result, "BatteryPowerPerUse", BatteryPowerPerUse);
            UpdateDefault(__result, "FuelPowerPerUse", FuelPowerPerUse);
            UpdateDefault(__result, "PowerPerPanel", PowerPerPanel);
            UpdateDefault(__result, "PowerPerEngine", PowerPerEngine);
            UpdateDefault(__result, "PowerPerBattery", PowerPerBattery);
            UpdateDefault(__result, "MinPowerForCharging", MinPowerForCharging);
            UpdateDefault(__result, "BatteryChargePercentFull", BatteryChargePercentFull);
            UpdateDefault(__result, "BatteryChargePercentEmpty", BatteryChargePercentEmpty);
        }
    }

    // Copied from original dll
    // Removed dispatching to children
    // Only consume energy if fully powered
    [HarmonyPriority(Priority.VeryHigh)]
    [HarmonyPatch(typeof(PowerItem))]
    [HarmonyPatch("HandlePowerReceived")]
    public class PowerItem_HandlePowerReceived
    {
        static bool Prefix(PowerItem __instance, ref ushort power)
        {
            ushort num = __instance.RequiredPower <= power ? __instance.RequiredPower : (ushort)0;
            bool newPowered = (int)num == (int)__instance.RequiredPower;
            if (newPowered != __instance.isPowered)
            {
                __instance.isPowered = newPowered;
                __instance.IsPoweredChanged(newPowered);
                if (__instance.TileEntity != null)
                    __instance.TileEntity.SetModified();
            }

            power -= num;
            return false;
        }
    }

    // Copied from original dll
    // Removed dispatching to children
    // Only consume energy if fully powered
    [HarmonyPriority(Priority.VeryHigh)]
    [HarmonyPatch(typeof(PowerBatteryBank))]
    [HarmonyPatch("HandlePowerReceived")]
    public class PowerBatteryBank_HandlePowerReceived
    {
        static bool Prefix(PowerItem __instance, ref ushort power)
        {
            ushort num = __instance.RequiredPower <= power ? __instance.RequiredPower : (ushort)0;
            bool newPowered = (int)num == (int)__instance.RequiredPower;
            if (newPowered != __instance.isPowered)
            {
                __instance.isPowered = newPowered;
                __instance.IsPoweredChanged(newPowered);
                if (__instance.TileEntity != null)
                    __instance.TileEntity.SetModified();
            }

            power -= num;
            return false;
        }
    }

    // Copied from original dll
    // Removed dispatching to children
    // Only consume energy if fully powered
    [HarmonyPriority(Priority.VeryHigh)]
    [HarmonyPatch(typeof(PowerTrigger))]
    [HarmonyPatch("HandlePowerReceived")]
    public class PowerTrigger_HandlePowerReceived
    {
        static bool Prefix(PowerTrigger __instance, ref ushort power)
        {
            ushort num = __instance.RequiredPower <= power ? __instance.RequiredPower : (ushort)0;
            __instance.isPowered = (int)num == (int)__instance.RequiredPower;
            power -= num;
            if (power <= (ushort)0)
                return false;
            __instance.CheckForActiveChange();
            for (int index = 0; index < __instance.Children.Count; ++index)
            {
                if (__instance.Children[index] is PowerTrigger child)
                {
                    __instance.HandleParentTriggering(child);
                }
            }
            return false;
        }
    }

    // Copied from original dll
    // Removed dispatching to children
    // Only consume energy if fully powered
    [HarmonyPriority(Priority.VeryHigh)]
    [HarmonyPatch(typeof(PowerTimerRelay))]
    [HarmonyPatch("HandlePowerReceived")]
    public class PowerTimerRelay_HandlePowerReceived
    {
        static bool Prefix(PowerTimerRelay __instance, ref ushort power)
        {
            ushort num = __instance.RequiredPower <= power ? __instance.RequiredPower : (ushort)0;
            bool newPowered = (int)num == (int)__instance.RequiredPower;
            __instance.isPowered = newPowered;
            power -= num;
            __instance.CheckForActiveChange();
            // Fix weird vanilla implementation "bug"
            __instance.isActive = __instance.isTriggered;
            return false;
        }
    }

    // Copied from original dll
    // Removed dispatching to children
    // Only consume energy if fully powered
    [HarmonyPriority(Priority.VeryHigh)]
    [HarmonyPatch(typeof(PowerConsumerToggle))]
    [HarmonyPatch("HandlePowerReceived")]
    public class PowerConsumerToggle_HandlePowerReceived
    {
        static bool Prefix(PowerConsumerToggle __instance, ref ushort power)
        {
            ushort num = __instance.RequiredPower <= power ? __instance.RequiredPower : (ushort)0;
            bool newPowered = (int)num == (int)__instance.RequiredPower;
            if (__instance.IsToggled == false) newPowered = false;
            if (newPowered != __instance.isPowered)
            {
                __instance.isPowered = newPowered;
                __instance.IsPoweredChanged(newPowered);
                if (__instance.TileEntity != null)
                    __instance.TileEntity.SetModified();
            }

            power -= __instance.isToggled ? num : (ushort)0;
            return false;
        }
    }

    // Copied from original dll
    // Removed dispatching to children
    // Doesn't obey `PowerChildren` flag!?
    [HarmonyPatch(typeof(PowerTimerRelay))]
    [HarmonyPatch("HandlePowerUpdate")]
    public class PowerTimerRelay_HandlePowerUpdate
    {
        static bool Prefix(PowerTimerRelay __instance, bool parentIsOn)
        {

            if (__instance.TileEntity != null)
            {
                ((TileEntityPoweredTrigger)__instance.TileEntity).Activate(
                    __instance.isPowered & parentIsOn, __instance.isTriggered);
                __instance.TileEntity.SetModified();
            }

            __instance.hasChangesLocal = true;
            return false;
        }
    }

    // Copied from original dll
    // Removed dispatching to children
    // Doesn't obey `PowerChildren` flag!?
    [HarmonyPatch(typeof(PowerTrigger))]
    [HarmonyPatch("HandlePowerUpdate")]
    public class PowerTrigger_HandlePowerUpdate
    {
        static bool Prefix(PowerTrigger __instance, bool parentIsOn)
        {

            if (__instance.TileEntity != null)
            {
                ((TileEntityPoweredTrigger)__instance.TileEntity).Activate(
                    __instance.isPowered & parentIsOn, __instance.isTriggered);
                __instance.TileEntity.SetModified();
            }

            __instance.hasChangesLocal = true;
            __instance.HandleSingleUseDisable();

            return false;
        }
    }

    // Copied from original dll
    // Removed dispatching to children
    // Obeys to `PowerChildren` flag!!
    [HarmonyPatch(typeof(PowerConsumerToggle))]
    [HarmonyPatch("HandlePowerUpdate")]
    public class PowerConsumerToggle_HandlePowerUpdate
    {
        static bool Prefix(PowerConsumerToggle __instance, bool isOn)
        {
            bool activated = __instance.isPowered & isOn && __instance.isToggled;
            if (__instance.TileEntity != null)
            {
                __instance.TileEntity.Activate(activated);
                if (activated && __instance.lastActivate != activated)
                    __instance.TileEntity.ActivateOnce();
            }
            __instance.lastActivate = activated;
            return false;
        }
    }

    // Copied from original dll
    // Removed dispatching to children
    // Obeys to `PowerChildren` flag!!
    [HarmonyPatch(typeof(PowerConsumer))]
    [HarmonyPatch("HandlePowerUpdate")]
    public class PowerConsumer_HandlePowerUpdate
    {
        static bool Prefix(PowerConsumerToggle __instance, bool isOn)
        {
            bool activated = __instance.isPowered & isOn;
            if (__instance.TileEntity != null)
            {
                __instance.TileEntity.Activate(activated);
                if (activated && __instance.lastActivate != activated)
                    __instance.TileEntity.ActivateOnce();
                __instance.TileEntity.SetModified();
            }
            __instance.lastActivate = activated;
            return false;
        }
    }

    // Only consumers obey this flag!?
    [HarmonyPatch(typeof(PowerItem))]
    [HarmonyPatch("HandleDisconnect")]
    public class PowerItem_HandleDisconnect
    {
        static void Postfix(PowerItem __instance)
        {
            __instance.WasPowered = false;
        }
    }

    // Only consumers obey this flag!?
    [HarmonyPatch(typeof(PowerItem))]
    [HarmonyPatch("PowerChildren")]
    public class PowerItem_PowerChildren
    {
        static void Postfix(ref bool __result)
        {
            __result = false;
        }
    }

    [HarmonyPatch(typeof(PowerBatteryBank))]
    [HarmonyPatch("AddPowerToBatteries")]
    public class PowerBatteryBank_AddPowerToBatteries
    {
        static bool Prefix(PowerBatteryBank __instance, int power)
        {
            AddPowerToBatteries(__instance, power);
            return false;
        }
    }

    [HarmonyPatch(typeof(PowerBatteryBank))]
    [HarmonyPatch("TickPowerGeneration")]
    public class PowerBatteryBank_TickPowerGeneration
    {
        static bool Prefix(PowerBatteryBank __instance)
        {
            TickBatteryBankPowerGeneration(__instance);
            return false;
        }
    }

    [HarmonyPatch(typeof(PowerGenerator))]
    [HarmonyPatch("TickPowerGeneration")]
    public class Powergenerator_TickPowerGeneration
    {
        static bool Prefix(PowerGenerator __instance)
        {
            TickBatteryDieselPowerGeneration(__instance);
            return false;
        }
    }

    // [HarmonyPatch(typeof(PowerBatteryBank))]
    // [HarmonyPatch("get_IsPowered")]
    // public class PowerBatteryBank_IsPowered
    // {
    //     static bool Prefix(ref bool __result, bool ___isPowered, bool ___isOn)
    //     {
    //         __result = ___isPowered;
    //         return false;
    //     }
    // }


    // Don't forcefully remove children if one trigger goes inactive
    // Some child might be another trigger, forming a trigger group,
    // thus if one sub-triggers is active, children stay connected.
    [HarmonyPatch(typeof(PowerTrigger))]
    [HarmonyPatch("HandleDisconnectChildren")]
    public class PowerTrigger_HandleDisconnectChildrenOverhaul
    {
        static bool Prefix(PowerTrigger __instance)
        {

            if (__instance.IsActive) return false;

            // Make sure no parent is triggered
            if (__instance.Parent != null) {
                if (__instance.Parent is PowerTrigger upstream) {
                    if (upstream.IsActive) {
                        return false;
                    }
                }
            }
            for (int index = 0; index < __instance.Children.Count; ++index)
            {
                // If child is another trigger, only disconnect if not active
                if (__instance.Children[index] is PowerTrigger trigger)
                {
                    trigger.SetTriggeredByParent(false);
                    if (trigger.IsActive) {
                        continue;
                    }
                    trigger.HandleDisconnectChildren();
                }
                else if (!(__instance.Children[index] is PowerSource)) {
                    __instance.Children[index].HandleDisconnect();
                }
            }
            // Disable power for this instance?
            // Get better results without this?
            // __instance.HandlePowerUpdate(false);
            // Fully replace implementation
            return false;
        }
    }

    public static ushort GetMaxPower(TileEntityPowerSource instance)
    {
        return !SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer ?
            (ushort)0 : (instance.PowerItem as PowerSource).MaxPower;
    }

    public static ushort GetLentConsumerUsed(TileEntityPowerSource instance)
    {
        return !SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer ?
            instance.ClientData.LentConsumerUsed : (instance.PowerItem as PowerSource).LentConsumerUsed;
    }

    public static ushort GetLentChargingUsed(TileEntityPowerSource instance)
    {
        return !SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer ?
            instance.ClientData.LentChargingUsed : (instance.PowerItem as PowerSource).LentChargingUsed;
    }

    public static ushort GetMaxOutput(TileEntityPowerSource instance)
    {
        return !SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer ?
            instance.ClientData.MaxOutput : (instance.PowerItem as PowerSource).MaxOutput;
    }

    public static ushort GetMaxGridProduction(TileEntityPowerSource instance)
    {
        return !SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer ?
            instance.ClientData.MaxGridProduction : (instance.PowerItem as PowerSource).MaxGridProduction;
    }

    public static ushort GetMaxProduction(TileEntityPowerSource instance)
    {
        return !SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer ?
            instance.ClientData.MaxProduction : (instance.PowerItem as PowerSource).MaxProduction;
    }

    public static ushort GetStackPower(TileEntityPowerSource instance)
    {
        return !SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer ?
            (ushort)0 : (instance.PowerItem as PowerSource).StackPower;
    }

    public static ushort GetLentConsumed(TileEntityPowerSource instance)
    {
        return !SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer ?
            instance.ClientData.LentConsumed : (instance.PowerItem as PowerSource).LentConsumed;
    }

    public static ushort GetLentCharging(TileEntityPowerSource instance)
    {
        return !SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer ?
            instance.ClientData.LentCharging : (instance.PowerItem as PowerSource).LentCharging;
    }

    public static ushort GetConsumerDemand(TileEntityPowerSource instance)
    {
        return !SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer ?
            instance.ClientData.ConsumerDemand : (instance.PowerItem as PowerSource).ConsumerDemand;
    }

    public static ushort GetChargingDemand(TileEntityPowerSource instance)
    {
        return !SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer ?
            instance.ClientData.ChargingDemand : (instance.PowerItem as PowerSource).ChargingDemand;
    }

    public static ushort GetConsumerUsed(TileEntityPowerSource instance)
    {
        return !SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer ?
            instance.ClientData.ConsumerUsed : (instance.PowerItem as PowerSource).ConsumerUsed;
    }

    public static ushort GetChargingUsed(TileEntityPowerSource instance)
    {
        return !SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer ?
            instance.ClientData.ChargingUsed : (instance.PowerItem as PowerSource).ChargingUsed;
    }

    public static ushort GetGridConsumerDemand(TileEntityPowerSource instance)
    {
        return !SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer ?
            instance.ClientData.GridConsumerDemand : (instance.PowerItem as PowerSource).GridConsumerDemand;
    }

    public static ushort GetGridChargingDemand(TileEntityPowerSource instance)
    {
        return !SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer ?
            instance.ClientData.GridChargingDemand : (instance.PowerItem as PowerSource).GridChargingDemand;
    }

    public static ushort GetGridConsumerUsed(TileEntityPowerSource instance)
    {
        return !SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer ?
            instance.ClientData.GridConsumerUsed : (instance.PowerItem as PowerSource).GridConsumerUsed;
    }

    public static ushort GetGridChargingUsed(TileEntityPowerSource instance)
    {
        return !SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer ?
            instance.ClientData.GridChargingUsed : (instance.PowerItem as PowerSource).GridChargingUsed;
    }

    public static ushort getBatteryLeft(TileEntityPowerSource instance, int index)
    {
        if (!SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer) return (ushort)0;
        PowerSource source = instance.PowerItem as PowerSource;
        if (source.Stacks[index].IsEmpty()) return (ushort)0;
        return (ushort)(source.Stacks[index].itemValue.MaxUseTimes
            - source.Stacks[index].itemValue.UseTimes);
    }

    public static ushort GetLightLevel(TileEntityPowerSource instance)
    {
        if (SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
        {
            if (instance.PowerItem is PowerSolarPanel panel) return panel.LightLevel;
        }
        else
        {
            return instance.ClientData.LightLevel;
        }
        return 0;
    }

    public static ushort GetLocalGridConsumerUsed(TileEntityPowerSource instance)
    {
        return (ushort)(GetConsumerUsed(instance) + GetGridConsumerUsed(instance));
    }
    public static ushort GetLocalGridConsumerDemand(TileEntityPowerSource instance)
    {
        return (ushort)(GetConsumerDemand(instance) + GetGridConsumerDemand(instance));
    }
    public static ushort GetLocalGridChargingUsed(TileEntityPowerSource instance)
    {
        return (ushort)(GetChargingUsed(instance) + GetGridChargingUsed(instance));
    }
    public static ushort GetLocalGridChargingDemand(TileEntityPowerSource instance)
    {
        return (ushort)(GetChargingDemand(instance) + GetGridChargingDemand(instance));
    }

    public static ushort GetLocalGridUsed(TileEntityPowerSource instance)
    {
        return (ushort)(GetLocalGridConsumerUsed(instance) + GetLocalGridChargingUsed(instance));
    }

    public static ushort GetLocalGridDemand(TileEntityPowerSource instance)
    {
        return (ushort)(GetLocalGridConsumerDemand(instance) + GetLocalGridChargingDemand(instance));
    }

    public static string GetPercent(XUiC_PowerSourceStats __instance, float amount, float off)
    {
        return off == 0 ? "0" : __instance.maxoutputFormatter.Format((ushort)(100f * amount / off));
    }

    public static string GetFill(XUiC_PowerSourceStats __instance, float amount, float off)
    {
        return off == 0 ? "0" : __instance.powerFillFormatter.Format(amount / off);
    }

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

        static float GetValue(string name, TileEntityPowerSource te)
        {
            if (te == null) return -1;
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
            // Regular code path
            switch (bindingName)
            {

                case "MaxPower": // unused
                    value = ___tileEntity == null ? "n/a" : __instance.maxoutputFormatter.Format(GetMaxPower(___tileEntity));
                    __result = true;
                    break;
                case "MaxProduction":
                    value = ___tileEntity == null ? "n/a" : __instance.maxoutputFormatter.Format(GetMaxProduction(___tileEntity));
                    __result = true;
                    break;
                case "MaxOutput":
                    value = ___tileEntity == null ? "n/a" : __instance.maxoutputFormatter.Format(GetMaxOutput(___tileEntity));
                    __result = true;
                    break;
                case "StackPower": // unused
                    value = ___tileEntity == null ? "n/a" : __instance.maxoutputFormatter.Format(GetStackPower(___tileEntity));
                    __result = true;
                    break;
                case "LightLevel": // unused
                    value = ___tileEntity == null ? "n/a" : __instance.maxoutputFormatter.Format(GetLightLevel(___tileEntity));
                    __result = true;
                    break;

                // Grid values (information only)
                // ToDo: really needed (overhead)
                // ToDo: maybe only for single-player
                case "LentConsumed": // unused
                    value = ___tileEntity == null ? "n/a" : __instance.maxoutputFormatter.Format(GetLentConsumed(___tileEntity));
                    __result = true;
                    break;
                case "LentCharging": // unused
                    value = ___tileEntity == null ? "n/a" : __instance.maxoutputFormatter.Format(GetLentCharging(___tileEntity));
                    __result = true;
                    break;
                case "ConsumerDemand": // unused
                    value = ___tileEntity == null ? "n/a" : __instance.maxoutputFormatter.Format(GetConsumerDemand(___tileEntity));
                    __result = true;
                    break;
                case "ChargingDemand": // unused
                    value = ___tileEntity == null ? "n/a" : __instance.maxoutputFormatter.Format(GetChargingDemand(___tileEntity));
                    __result = true;
                    break;
                case "ConsumerUsed": // unused
                    value = ___tileEntity == null ? "n/a" : __instance.maxoutputFormatter.Format(GetConsumerUsed(___tileEntity));
                    __result = true;
                    break;
                case "ChargingUsed": // battery only
                    value = ___tileEntity == null ? "n/a" : __instance.maxoutputFormatter.Format(GetChargingUsed(___tileEntity));
                    __result = true;
                    break;
                case "GridConsumerDemand": // unused
                    value = ___tileEntity == null ? "n/a" : __instance.maxoutputFormatter.Format(GetGridConsumerDemand(___tileEntity));
                    __result = true;
                    break;
                case "GridChargingDemand": // unused
                    value = ___tileEntity == null ? "n/a" : __instance.maxoutputFormatter.Format(GetGridChargingDemand(___tileEntity));
                    __result = true;
                    break;
                case "GridConsumerUsed": // unused
                    value = ___tileEntity == null ? "n/a" : __instance.maxoutputFormatter.Format(GetGridConsumerUsed(___tileEntity));
                    __result = true;
                    break;
                case "GridChargingUsed": // unused
                    value = ___tileEntity == null ? "n/a" : __instance.maxoutputFormatter.Format(GetGridChargingUsed(___tileEntity));
                    __result = true;
                    break;
                case "LocalGridDemand": // used
                    value = ___tileEntity == null ? "n/a" : __instance.maxoutputFormatter.Format(GetLocalGridDemand(___tileEntity));
                    __result = true;
                    break;
                case "LocalConsumerDemand": // used
                    value = ___tileEntity == null ? "n/a" : __instance.maxoutputFormatter.Format(GetLocalGridConsumerDemand(___tileEntity));
                    __result = true;
                    break;
                case "LocalChargingDemand": // used
                    value = ___tileEntity == null ? "n/a" : __instance.maxoutputFormatter.Format(GetLocalGridChargingDemand(___tileEntity));
                    __result = true;
                    break;
                case "LocalGridUsed": // used
                    value = ___tileEntity == null ? "n/a" : __instance.maxoutputFormatter.Format(GetLocalGridUsed(___tileEntity));
                    __result = true;
                    break;
                case "LocalConsumerUsed": // used
                    value = ___tileEntity == null ? "n/a" : __instance.maxoutputFormatter.Format(GetLocalGridConsumerUsed(___tileEntity));
                    __result = true;
                    break;
                case "LocalChargingUsed": // used
                    value = ___tileEntity == null ? "n/a" : __instance.maxoutputFormatter.Format(GetLocalGridChargingUsed(___tileEntity));
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
                    value = ___tileEntity == null ? "0" :
                        GetFill(__instance, GetLentConsumed(___tileEntity), GetMaxOutput(___tileEntity));
                    __result = true;
                    break;
                case "UsedChargingFill": // used
                    value = ___tileEntity == null ? "0" :
                        GetFill(__instance, GetLentConsumed(___tileEntity) + GetLentCharging(___tileEntity), GetMaxOutput(___tileEntity));
                    __result = true;
                    break;

                case "LentConsumerFill": // used
                    value = ___tileEntity == null ? "0" :
                        GetFill(__instance, GetLentConsumerUsed(___tileEntity), GetLentConsumerUsed(___tileEntity) + GetLentChargingUsed(___tileEntity));
                    __result = true;
                    break;
                case "LentChargingFill": // used
                    value = ___tileEntity == null ? "0" :
                        GetFill(__instance, GetLentConsumerUsed(___tileEntity) + GetLentChargingUsed(___tileEntity), GetLentConsumerUsed(___tileEntity) + GetLentChargingUsed(___tileEntity));
                    __result = true;
                    break;

                case "ChargingFill": // battery only
                    value = ___tileEntity == null ? "0" :
                        GetFill(__instance, GetChargingUsed(___tileEntity), GetChargingDemand(___tileEntity));
                    __result = true;
                    break;
                case "GridConsumerFill": // unused
                    value = ___tileEntity == null ? "0" :
                        GetFill(__instance, GetGridConsumerUsed(___tileEntity), GetGridConsumerDemand(___tileEntity));
                    __result = true;
                    break;
                case "GridChargingFill": // unused
                    value = ___tileEntity == null ? "0" :
                        GetFill(__instance, GetGridChargingUsed(___tileEntity), GetGridChargingDemand(___tileEntity));
                    __result = true;
                    break;

                case "MaxGridProduction":
                    value = ___tileEntity == null ? "n/a" :
                        __instance.maxoutputFormatter.Format(GetMaxGridProduction(___tileEntity));
                    __result = true;
                    break;

                case "Flow":
                    value = ___tileEntity == null ? "n/a" :
                        __instance.maxoutputFormatter.Format((ushort)(GetLentConsumerUsed(___tileEntity) + GetLentChargingUsed(___tileEntity)));
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

}
