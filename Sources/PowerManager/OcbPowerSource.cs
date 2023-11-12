using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using UnityEngine;
using XMLData.Parsers;
using static OCB.ElectricityUtils;

public class OcbPowerSource : PowerSource
{

    // ####################################################################
    // ####################################################################

    public ushort StackPower;
    public ushort MaxProduction;
    public ushort MaxGridProduction;
    public ushort LentConsumed;
    public ushort LentCharging;
    public ushort ChargingUsed;
    public ushort ChargingDemand;
    public ushort ConsumerUsed;
    public ushort ConsumerDemand;
    public ushort GridConsumerDemand;
    public ushort GridChargingDemand;
    public ushort GridConsumerUsed;
    public ushort GridChargingUsed;
    public ushort LentConsumerUsed;
    public ushort LentChargingUsed;
    public bool ChargeFromSolar = true;
    public bool ChargeFromGenerator;
    public bool ChargeFromBattery;

    public List<OcbPowerSource> PowerSources;
    public List<PowerTrigger> PowerTriggers;

    public float UpdateTime;
    public float AvgTime;
    public ulong LastTick;

    // ####################################################################
    // ####################################################################

    public void DoPowerGenerationTick()
        => TickPowerGeneration();

    public bool ShouldItAutoTurnOff()
        => ShouldAutoTurnOff();

    // ####################################################################
    // ####################################################################

    public override void read(BinaryReader _br, byte _version)
    {
        base.read(_br, _version);
        // We resolve enum dynamically on runtime, since we don't want to
        // hard-code a specific value into our own runtime. This allows
        // compatibility even if game dll alters the enum between version.
        if (OcbPowerManager.LoadVanillaMap)
        {
            ChargeFromSolar = true;
            ChargeFromGenerator = true;
            ChargeFromBattery = false;
        }
        else
        {
            ChargeFromSolar = _br.ReadBoolean();
            ChargeFromGenerator = _br.ReadBoolean();
            ChargeFromBattery = _br.ReadBoolean();
        }
        UpdateSlots();
    }

    // ####################################################################
    // ####################################################################

    public override void write(BinaryWriter _bw)
    {
        base.write(_bw);
        if (OcbPowerManager.StoreVanillaMap) return;
        _bw.Write(ChargeFromSolar);
        _bw.Write(ChargeFromGenerator);
        _bw.Write(ChargeFromBattery);
    }

    // ####################################################################
    // ####################################################################

    private static void RecalcChargingDemand(OcbPowerBatteryBank bank)
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

    // ####################################################################
    // ####################################################################

    public void UpdateSlots()
    {
        float powerPerSlot = 1f, defaultPower = 1f;
        if (this is OcbPowerSolarPanel)
        {
            defaultPower = PowerPerPanelDefault;
            powerPerSlot = PowerPerPanel;
        }
        else if (this is OcbPowerGenerator)
        {
            defaultPower = PowerPerEngineDefault;
            powerPerSlot = PowerPerEngine;
        }
        else if (this is OcbPowerBatteryBank bank)
        {
            defaultPower = PowerPerBatteryDefault;
            powerPerSlot = PowerPerBattery;
            RecalcChargingDemand(bank);
        }
        this.StackPower = 0;
        float factor = this.OutputPerStack / powerPerSlot;
        foreach (var stack in this.Stacks)
        {
            if (stack.IsEmpty()) continue;
            if (stack.itemValue.MaxUseTimes > 0
                && stack.itemValue.UseTimes >=
                stack.itemValue.MaxUseTimes) continue;
            this.StackPower += (ushort)(factor *
                GetSlotPowerByQuality(stack.itemValue,
                    powerPerSlot, defaultPower));
        }
        this.StackPower = (ushort)Mathf.Min(
            this.StackPower, this.MaxPower);
        // Turn off when all is defect
        if (this.StackPower <= 0)
            this.IsOn = false;
    }

    // ####################################################################
    // ####################################################################

    [HarmonyPatch(typeof(PowerSource))]
    [HarmonyPatch("SetSlots")]
    public class PowerSource_SetSlots
    {
        static void Postfix(PowerSource __instance)
        {
            if (__instance is OcbPowerSource source)
                source.UpdateSlots();
        }
    }

    [HarmonyPatch(typeof(PowerSource))]
    [HarmonyPatch("TryAddItemToSlot")]
    public class PowerSource_TryAddItemToSlot
    {
        static void Postfix(PowerSource __instance)
        {
            if (__instance is OcbPowerSource source)
                source.UpdateSlots();
        }
    }

    // ####################################################################
    // ####################################################################

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

    // ####################################################################
    // ####################################################################

    // Allow config defaults to be set via xml
    [HarmonyPatch(typeof(MiscFromXml))]
    [HarmonyPatch("Create")]
    public class MiscFromXmlCreatePatch
    {

        static void SetBool(string name, string value)
        {
            if (bool.TryParse(value, out bool result))
                GamePrefs.Set(EnumParser.Parse<EnumGamePrefs>(name), result);
            else Log.Error("Could not parse boolean `{0}` for {1}", value, name);
        }

        static void SetInt(string name, string value)
        {
            if (int.TryParse(value, out int result))
                GamePrefs.Set(EnumParser.Parse<EnumGamePrefs>(name), result);
            else Log.Error("Could not parse integer `{0}` for {1}", value, name);
        }

        static void Prefix(XmlFile _xmlFile)
        {
            if (!GameManager.IsDedicatedServer) return;
            foreach (XElement node in _xmlFile.XmlDoc.Root.Elements())
            {
                if (node.Name.LocalName != "electricity-overhaul") continue;
                foreach (XElement el in node.Elements("property"))
                {
                    string name = el.GetAttribute("name");
                    string value = el.GetAttribute("value");
                    switch (name)
                    {
                        case "DegradationFactor": SetInt("DegradationFactor", value); break;
                        case "PreferFuelOverBattery": SetBool("PreferFuelOverBattery", value); break;
                        case "BatteryPowerPerUse": SetInt("BatteryPowerPerUse", value); break;
                        case "FuelPowerPerUse": SetInt("FuelPowerPerUse", value); break;
                        case "PowerPerPanel": SetInt("PowerPerPanel", value); break;
                        case "PowerPerEngine": SetInt("PowerPerEngine", value); break;
                        case "PowerPerBattery": SetInt("PowerPerBattery", value); break;
                        case "MinPowerForCharging": SetInt("MinPowerForCharging", value); break;
                        case "BatteryChargePercentFull": SetInt("BatteryChargePercentFull", value); break;
                        case "BatteryChargePercentEmpty": SetInt("BatteryChargePercentEmpty", value); break;
                    }
                }
            }
        }
    }

    // ####################################################################
    // ####################################################################

}
