using System;
using UnityEngine;

namespace OCB
{
    public static class ElectricityUtils
    {

        // Default values for configurable options
        public const bool LoadVanillaMapDefault = false;
        public const int BatteryPowerPerUseDefault = 25;
        public const int MinPowerForChargingDefault = 20;
        public const int FuelPowerPerUseDefault = 750;
        public const int PowerPerPanelDefault = 30;
        public const int PowerPerEngineDefault = 50;
        public const int PowerPerBatteryDefault = 50;
        public const int ChargePerBatteryDefault = 35;
        public const int GeneratorMaxFuelDefault = 5000;

        // Should we try to load a vanilla map (initialize with defaults)
        public static bool isLoadVanillaMap = LoadVanillaMapDefault;

        // Coefficient to exchange battery uses and watts
        public static int batteryPowerPerUse = BatteryPowerPerUseDefault;

        // Minimum excess power before we start charging batteries
        // This avoids too much charge/discharge ping-pong
        public static int minPowerForCharging = MinPowerForChargingDefault;

        // Coefficient to exchange fuel into watts
        public static int fuelPowerPerUse = FuelPowerPerUseDefault;

        public static int powerPerPanel = PowerPerPanelDefault;
        public static int powerPerEngine = PowerPerEngineDefault;
        public static int powerPerBattery = PowerPerBatteryDefault;
        public static int chargePerBattery = ChargePerBatteryDefault;

        // Get solar cell power by quality
        static public ushort GetCellPowerByQuality(int quality)
        {
            return (ushort)((double)powerPerPanel * (double)Mathf.Lerp(0.5f, 1f, (float)quality / 6f));
        }

        // Get discharge power by battery quality
        static public ushort GetDischargeByQuality(int quality)
        {
            return (ushort)((double)powerPerBattery * (double)Mathf.Lerp(0.5f, 1f, (float)quality / 6f));
        }

        // Get charging power by battery quality
        static public ushort GetChargeByQuality(int quality)
        {
            return (ushort)((double)chargePerBattery * (double)Mathf.Lerp(0.5f, 1f, (float)quality / 6f));
        }

        // Check if given `source` has a parent power source
        // Return `null` if any trigger group between is off
        static public PowerSource GetParentSources(PowerSource source)
        {
            for (PowerItem parent = source.Parent; parent != null; parent = parent.Parent)
            {
                if (parent is PowerTrigger trigger)
                {
                    bool isActive = false;
                    while (parent.Parent is PowerTrigger child)
                    {
                        isActive = isActive || child.IsActive;
                        parent = parent.Parent;
                    }
                    if (!isActive) return null;
                }
                if (parent is PowerSource res) return res;
            }

            return null;
        }

        // Check if the item is connected to a power source
        // Does correctly handle trigger groups via parents
        // Note: items have various states, which can be confusing:
        // isTriggered: Target is in view (triggered the item)
        // isActive: True if power should flow (e.g. delayed trigger)
        static public bool IsTriggerActive(PowerItem item)
        {
            bool isActive = true;
            PowerItem parent = item;
            while (parent != null)
            {
                // Most downstream trigger
                // Group with all parents
                if (parent is PowerTrigger)
                {
                    bool SubActive = false;
                    while (parent is PowerTrigger upstream)
                    {
                        SubActive = SubActive ||
                                    upstream.IsActive;
                        parent = upstream.Parent;
                        // Abort at next power source
                        if (parent is PowerSource)
                        {
                            return isActive && SubActive;
                        }
                    }

                    isActive = isActive && SubActive;
                }
                // Skip over other parents
                if (parent != null)
                    parent = parent.Parent;
                // Abort at next power source
                if (parent is PowerSource)
                {
                    return isActive;
                }
            }
            return isActive;
        }

        // Fill `CurrentPower` of BatteryBank by draining batteries.
        // We try to distribute the power among all batteries. This
        // implementation has some funky behavior, but that seems to
        // be fine, since batteries in real-life can behave similarly.
        // E.g. lights may flicker if batteries are running out.
        static public void TickBatteryBankPowerGeneration(PowerBatteryBank bank)
        {
            // MaxPower - theoretical maximum power if all batteries have energy
            // CurrentPower - amount of power currently available in the bank
            // CurrentPower acts as a internal buffer, where excess energy is stored.
            if (bank.CurrentPower < bank.MaxPower)
            {
                float neededToMax = bank.MaxPower - bank.CurrentPower;
                for (int index = 0; index < bank.Stacks.Length; ++index)
                {
                    // Skip over empty battery slots
                    if (bank.Stacks[index].IsEmpty()) continue;

                    // Get how many uses are left over for this battery (related to quality)
                    // Each battery quality has a different maximum use time (use times are
                    // inherited from e.g. tools), so empty batteries have `UseTimes` equal
                    // to `MaxUseTimes`, while full batteries have `UseTimes` equal to `0`.
                    float usesLeftOver = bank.Stacks[index].itemValue.MaxUseTimes
                                         - (int)bank.Stacks[index].itemValue.UseTimes;

                    if (neededToMax <= usesLeftOver * batteryPowerPerUse)
                    {
                        bank.Stacks[index].itemValue.UseTimes += neededToMax / batteryPowerPerUse;
                        bank.CurrentPower += (ushort)neededToMax;
                        // break; // Don't break in order to drain all batteries, this means we add
                        // more power than needed to the `CurrentPower` buffer, which means the initial
                        // condition will be true for multiple ticks (equals amount of slots filled).
                        // This is the simplest way to drain all batteries equally, although since we
                        // take the same amount on each tick, lower quality batteries are faster empty
                        // than high quality batteries (which seems an acceptable game condition).
                        // This also leads to a "quite" efficient implementation in contrast to
                        // solutions that try to evenly distribute the energy demand on each tick.
                    }
                    else
                    {
                        // If battery can only sustain the required demand partially, take what is left.
                        bank.Stacks[index].itemValue.UseTimes += usesLeftOver / batteryPowerPerUse;
                        bank.CurrentPower += (ushort)usesLeftOver;

                    }
                }
            }
        }

        // Mostly copied from original dll to insert our configurable conversion factor
        static public void TickBatteryDieselPowerGeneration(PowerGenerator generator)
        {
            // Check if buffer has enough energy to fulfill max power
            if (generator.CurrentPower >= generator.MaxProduction) return;
            ushort neededToMax = (ushort)(generator.MaxProduction - generator.CurrentPower);
            ushort consume = (ushort)Mathf.Ceil((float)neededToMax / (float)fuelPowerPerUse);
            consume = (ushort)Mathf.Min(consume, generator.CurrentFuel);
            generator.CurrentPower += (ushort)(consume * fuelPowerPerUse);
            generator.CurrentFuel -= consume;
        }

        // We get some amount of power that can be put into our batteries.
        // There are multiple ways to distribute that power (and how much)
        // in many ways. In the vanilla implementation only the first battery
        // that is not full receives the full power load. In our implementation
        // we try to only load batteries that are not full, so the charging
        // power can vary if only one or all batteries are not fully charged.
        // For this to work, we need to discharge batteries in the same way!
        static public void AddPowerToBatteries(PowerBatteryBank bank, int power)
        {
            int used = 0;
            if (bank.isOn)
            {

                // Distribute power to batteries from left to right
                // This might be unnatural, but is easily optimized
                for (int index = 0; index < bank.Stacks.Length; ++index)
                {
                    // Check if slot actually holds a battery
                    if (bank.Stacks[index].IsEmpty()) continue;
                    // Check if the battery slot can hold any more power
                    if (bank.Stacks[index].itemValue.UseTimes <= 0) continue;

                    // Get demand of local battery or what is left
                    ushort demand = (ushort)Mathf.Min(GetChargeByQuality(
                        bank.Stacks[index].itemValue.Quality), power);
                    float demandUses = demand / (float)batteryPowerPerUse;
                    // Check if current battery can take the full charge load
                    if (bank.Stacks[index].itemValue.UseTimes >= demandUses)
                    {
                        bank.Stacks[index].itemValue.UseTimes -= demandUses;
                        power -= demand;
                        used += demand;
                    }
                    // Can only take partial load (battery is fully charged)
                    else
                    {
                        power -= (ushort)(bank.Stacks[index].itemValue.UseTimes * batteryPowerPerUse);
                        used += (ushort)(bank.Stacks[index].itemValue.UseTimes * batteryPowerPerUse);
                        bank.Stacks[index].itemValue.UseTimes = 0;
                    }

                }
            }
            // Copied from original DLL
            if ((int)bank.LastInputAmount == used) return;
            bank.SendHasLocalChangesToRoot();
            bank.LastInputAmount = (ushort)used;
        }

    }
}

