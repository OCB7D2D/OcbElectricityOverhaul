using System;
using System.IO;
using HarmonyLib;
using UnityEngine;
using System.Reflection;

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
        ModEvents.GameAwake.RegisterHandler(GameAwakeHandler);
    }

    public static void GameAwakeHandler()
    {
        GamePrefs.m_Instance.initPropertyDecl();
    }

    [HarmonyPatch(typeof(GamePrefs))]
    [HarmonyPatch("initPropertyDecl")]
    public class GamePrefs_initPropertyDecl
    {
        static void Postfix(ref GamePrefs.PropertyDecl[] ___propertyList, ref object[] ___propertyValues)
        {
            int size = ___propertyList.Length;
            Array.Resize(ref ___propertyList, size + 8);
            Array.Resize(ref ___propertyValues, ___propertyValues.Length + 8);
            ___propertyList[size + 0] = new GamePrefs.PropertyDecl(EnumGamePrefs.LoadVanillaMap, false, GamePrefs.EnumType.Bool, LoadVanillaMapDefault, null, null);
            ___propertyList[size + 1] = new GamePrefs.PropertyDecl(EnumGamePrefs.BatteryPowerPerUse, true, GamePrefs.EnumType.Int, BatteryPowerPerUseDefault, null, null);
            ___propertyList[size + 2] = new GamePrefs.PropertyDecl(EnumGamePrefs.MinPowerForCharging, true, GamePrefs.EnumType.Int, MinPowerForChargingDefault, null, null);
            ___propertyList[size + 3] = new GamePrefs.PropertyDecl(EnumGamePrefs.FuelPowerPerUse, true, GamePrefs.EnumType.Int, FuelPowerPerUseDefault, null, null);
            ___propertyList[size + 4] = new GamePrefs.PropertyDecl(EnumGamePrefs.PowerPerPanel, true, GamePrefs.EnumType.Int, PowerPerPanelDefault, null, null);
            ___propertyList[size + 5] = new GamePrefs.PropertyDecl(EnumGamePrefs.PowerPerEngine, true, GamePrefs.EnumType.Int, PowerPerEngineDefault, null, null);
            ___propertyList[size + 6] = new GamePrefs.PropertyDecl(EnumGamePrefs.PowerPerBattery, true, GamePrefs.EnumType.Int, PowerPerBatteryDefault, null, null);
            ___propertyList[size + 7] = new GamePrefs.PropertyDecl(EnumGamePrefs.ChargePerBattery, true, GamePrefs.EnumType.Int, ChargePerBatteryDefault, null, null);
        }
    }

    [HarmonyPatch(typeof(EnvironmentAudioManager))]
    [HarmonyPatch("Start")]
    public class EnvironmentAudioManager_Start
    {
        static void Prefix(EnvironmentAudioManager __instance)
        {
            Log.Warning("EnvironmentAudioManager " + __instance);
            Log.Warning("  " + __instance.gameObject);
            Log.Warning("  " + __instance.gameObject.name);
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
            __result[size + 0] = new GameMode.ModeGamePref(EnumGamePrefs.LoadVanillaMap, GamePrefs.EnumType.Bool, (bool) LoadVanillaMapDefault);
            __result[size + 1] = new GameMode.ModeGamePref(EnumGamePrefs.BatteryPowerPerUse, GamePrefs.EnumType.Int, (int) BatteryPowerPerUseDefault);
            __result[size + 2] = new GameMode.ModeGamePref(EnumGamePrefs.MinPowerForCharging, GamePrefs.EnumType.Int, (int) MinPowerForChargingDefault);
            __result[size + 3] = new GameMode.ModeGamePref(EnumGamePrefs.FuelPowerPerUse, GamePrefs.EnumType.Int, (int) FuelPowerPerUseDefault);
            __result[size + 4] = new GameMode.ModeGamePref(EnumGamePrefs.PowerPerPanel, GamePrefs.EnumType.Int, (int) PowerPerPanelDefault);
            __result[size + 5] = new GameMode.ModeGamePref(EnumGamePrefs.PowerPerEngine, GamePrefs.EnumType.Int, (int) PowerPerEngineDefault);
            __result[size + 6] = new GameMode.ModeGamePref(EnumGamePrefs.PowerPerBattery, GamePrefs.EnumType.Int, (int) PowerPerBatteryDefault);
            __result[size + 7] = new GameMode.ModeGamePref(EnumGamePrefs.ChargePerBattery, GamePrefs.EnumType.Int, (int) ChargePerBatteryDefault);
        }
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
                OcbPowerManager instance = new OcbPowerManager();
                PowerManager.instance = (PowerManager) instance;
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
            if (GamePrefs.GetBool(EnumGamePrefs.LoadVanillaMap)) {
                __instance.ChargeFromSolar = true;
                __instance.ChargeFromGenerator = true;
                __instance.ChargeFromBattery = false;
                return;
            }
            __instance.ChargeFromSolar = _br.ReadBoolean();
            __instance.ChargeFromGenerator = _br.ReadBoolean();
            __instance.ChargeFromBattery = _br.ReadBoolean();
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
            PowerSource powerItem = ___PowerItem as PowerSource;
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
                    _bw.Write(powerItem != null);
                    if (powerItem == null)
                        break;
                    // ToDo: check if we need em all (now 180 bytes)
                    _bw.Write(powerItem.MaxProduction);
                    _bw.Write(powerItem.ChargingUsed);
                    _bw.Write(powerItem.ChargingDemand);
                    _bw.Write(powerItem.ConsumerUsed);
                    _bw.Write(powerItem.ConsumerDemand);
                    _bw.Write(powerItem.LentConsumed);
                    _bw.Write(powerItem.LentCharging);
                    _bw.Write(powerItem.GridConsumerDemand);
                    _bw.Write(powerItem.GridChargingDemand);
                    _bw.Write(powerItem.GridConsumerUsed);
                    _bw.Write(powerItem.GridChargingUsed);
                    _bw.Write(powerItem.LentConsumerUsed);
                    _bw.Write(powerItem.LentChargingUsed);
                    _bw.Write(powerItem.ChargeFromSolar);
                    _bw.Write(powerItem.ChargeFromGenerator);
                    _bw.Write(powerItem.ChargeFromBattery);
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
                    break;
            }
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
                    trigger.SetTriggeredByParent(__instance.IsActive);
                    if (trigger.IsActive) {
                        continue;
                    }
                    trigger.HandleDisconnectChildren();
                }
                else {
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

    public static ushort GetCurrentPower(TileEntityPowerSource instance)
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

    public static ushort GetMaxProduction(TileEntityPowerSource instance)
    {
        return !SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer ?
            instance.ClientData.MaxProduction : (instance.PowerItem as PowerSource).MaxProduction;
    }

    public static ushort getBattery1Left(TileEntityPowerSource instance)
    {
        return !SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer ?
            instance.ClientData.MaxProduction : (instance.PowerItem as PowerSource).MaxProduction;
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

    public static ushort GetLocalConsumerUsed(TileEntityPowerSource instance)
    {
        return (ushort)(GetConsumerUsed(instance) + GetGridConsumerUsed(instance));
    }
    public static ushort GetLocalConsumerDemand(TileEntityPowerSource instance)
    {
        return (ushort)(GetConsumerDemand(instance) + GetGridConsumerDemand(instance));
    }
    public static ushort GetLocalChargingUsed(TileEntityPowerSource instance)
    {
        return (ushort)(GetChargingUsed(instance) + GetGridChargingUsed(instance));
    }
    public static ushort GetLocalChargingDemand(TileEntityPowerSource instance)
    {
        return (ushort)(GetChargingDemand(instance) + GetGridChargingDemand(instance));
    }

    public static string GetPercent(XUiC_PowerSourceStats __instance, int amount, int off)
    {
        return off == 0 ? "0" : __instance.maxoutputFormatter.Format((ushort)(100f * (float)amount / (float)off));
    }

    public static string GetFill(XUiC_PowerSourceStats __instance, int amount, int off)
    {
        return off == 0 ? "0" : __instance.powerFillFormatter.Format((float)amount / (float)off);
    }

    [HarmonyPatch(typeof(XUiC_PowerSourceStats))]
    [HarmonyPatch("GetBindingValue")]
    public class XUiC_PowerSourceStats_GetBindingValue
    {
        static void Postfix(XUiC_PowerSourceStats __instance, TileEntityPowerSource ___tileEntity, ref string value, string bindingName, ref bool __result)
        {
            switch (bindingName)
            {

                // Only available for local debugging
                // Doesn't transfer info from server to client
                case "batteryLeftA":
                    value = __instance.powerSource == null ? "n/a"
                        : __instance.maxoutputFormatter.Format(getBatteryLeft(___tileEntity, 0));
                    __result = true;
                    break;
                case "batteryLeftB":
                    value = __instance.powerSource == null ? "n/a"
                        : __instance.maxoutputFormatter.Format(getBatteryLeft(___tileEntity, 1));
                    __result = true;
                    break;
                case "batteryLeftC":
                    value = __instance.powerSource == null ? "n/a"
                        : __instance.maxoutputFormatter.Format(getBatteryLeft(___tileEntity, 2));
                    __result = true;
                    break;
                case "batteryLeftD":
                    value = __instance.powerSource == null ? "n/a"
                        : __instance.maxoutputFormatter.Format(getBatteryLeft(___tileEntity, 3));
                    __result = true;
                    break;
                case "batteryLeftE":
                    value = __instance.powerSource == null ? "n/a"
                        : __instance.maxoutputFormatter.Format(getBatteryLeft(___tileEntity, 4));
                    __result = true;
                    break;
                case "batteryLeftF":
                    value = __instance.powerSource == null ? "n/a"
                        : __instance.maxoutputFormatter.Format(getBatteryLeft(___tileEntity, 5));
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
                case "LocalConsumerDemand": // used
                    value = ___tileEntity == null ? "n/a" : __instance.maxoutputFormatter.Format(GetLocalConsumerDemand(___tileEntity));
                    __result = true;
                    break;
                case "LocalChargingDemand": // used
                    value = ___tileEntity == null ? "n/a" : __instance.maxoutputFormatter.Format(GetLocalChargingDemand(___tileEntity));
                    __result = true;
                    break;
                case "LocalConsumerUsed": // used
                    value = ___tileEntity == null ? "n/a" : __instance.maxoutputFormatter.Format(GetLocalConsumerUsed(___tileEntity));
                    __result = true;
                    break;
                case "LocalChargingUsed": // used
                    value = ___tileEntity == null ? "n/a" : __instance.maxoutputFormatter.Format(GetLocalChargingUsed(___tileEntity));
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
                        GetFill(__instance, GetLentConsumed(___tileEntity), GetMaxProduction(___tileEntity));
                    __result = true;
                    break;
                case "UsedChargingFill": // used
                    value = ___tileEntity == null ? "0" :
                        GetFill(__instance, GetLentConsumed(___tileEntity) + GetLentCharging(___tileEntity), GetMaxProduction(___tileEntity));
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

                // Overload to return MaxProduction instead of MaxOutput
                // Reports actual capacity for BatteryBank if some cells are empty
                case "MaxProduction":
                    value = ___tileEntity == null ? "n/a" :
                        __instance.maxoutputFormatter.Format(GetMaxProduction(___tileEntity));
                    __result = true;
                    break;
                case "MaxOutput":
                    value = ___tileEntity == null ? "n/a" :
                        __instance.maxoutputFormatter.Format(GetMaxOutput(___tileEntity));
                    __result = true;
                    break;
                case "Flow":
                    value = ___tileEntity == null ? "n/a" :
                        __instance.maxoutputFormatter.Format((ushort)(GetLentConsumerUsed(___tileEntity) + GetLentChargingUsed(___tileEntity)));
                    __result = true;
                    break;

                // Conditional for generator
                case "NotGenerator":
                    value = ___tileEntity == null ? "true" :
                        (___tileEntity.PowerItemType != PowerItem.PowerItemTypes.Generator).ToString();
                    __result = true;
                    break;
                // Conditional for generator
                case "IsGenerator":
                    value = ___tileEntity == null ? "false" :
                        (___tileEntity.PowerItemType == PowerItem.PowerItemTypes.Generator).ToString();
                    __result = true;
                    break;
                // Conditional for battery banks
                case "IsBatteryBank":
                    value = ___tileEntity == null ? "false" :
                        (___tileEntity.PowerItemType == PowerItem.PowerItemTypes.BatteryBank).ToString();
                    __result = true;
                    break;

                default:
                    break;
            }
        }
    }

}
