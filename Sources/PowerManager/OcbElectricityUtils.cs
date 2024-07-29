using HarmonyLib;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace OCB
{
    public static class ElectricityUtils
    {

        // ####################################################################
        // ####################################################################

        // Default values for configurable options
        public const bool PreferFuelOverBatteryDefault = false;
        public const int BatteryPowerPerUseDefault = 25;
        public const int MinPowerForChargingDefault = 20;
        public const int DegradationFactorDefault = 100;
        public const int FuelPowerPerUseDefault = 750;
        public const int PowerPerPanelDefault = 30;
        public const int PowerPerEngineDefault = 100;
        public const int PowerPerBatteryDefault = 50;
        public const int BatteryChargePercentFullDefault = 60;
        public const int BatteryChargePercentEmptyDefault = 130;

        // ####################################################################
        // ####################################################################

        // Should we prefer burning fuel to fill up batteries or use those up first
        public static bool PreferFuelOverBattery = PreferFuelOverBatteryDefault;

        // Coefficient to exchange battery uses and watts
        public static int BatteryPowerPerUse = BatteryPowerPerUseDefault;

        // How fast should items in power sources degrade (wind/solar)
        public static int DegradationFactor = DegradationFactorDefault;

        // Coefficient to exchange fuel into watts
        public static int FuelPowerPerUse = FuelPowerPerUseDefault;

        public static int PowerPerPanel = PowerPerPanelDefault;
        public static int PowerPerEngine = PowerPerEngineDefault;
        public static int PowerPerBattery = PowerPerBatteryDefault;
        public static int BatteryChargePercentFull = BatteryChargePercentFullDefault;
        public static int BatteryChargePercentEmpty = BatteryChargePercentEmptyDefault;

        // Property not yet configurable via regular game options (or xml config)
        public static float WearMinInterval = 10f;
        public static float WearMaxInterval = 20f;
        public static float WearThreshold = 0.15f;

        // Minimum excess power before we start charging batteries
        // This avoids too much charge/discharge ping-pong
        public static int MinPowerForCharging = MinPowerForChargingDefault;

        // ####################################################################
        // ####################################################################

        // Check for optional passive `PowerOutput` effect (e.g. Undead Legacy defines this)
        // Note: make sure we don't put a warning to the console as `AccessTools.Field` would 
        static readonly FieldInfo PowerOutputEffect = AccessTools
            .TypeByName(nameof(PassiveEffects))?.GetFields()?
            .FirstOrDefault(field => field.Name == "PowerOutput");

        // ####################################################################
        // ####################################################################

        // Get charging power by battery quality
        static public ushort GetChargeByQuality(ItemValue item)
        {
            float used = item.MaxUseTimes == 0 ? 0f : item.UseTimes / item.MaxUseTimes;
            float factor = Mathf.SmoothStep(BatteryChargePercentFull / 100f, BatteryChargePercentEmpty / 100f, used);
            return (ushort)(factor * GetSlotPowerByQuality(item, PowerPerBattery, 50f));
        }

        static public ushort GetSlotPowerByQuality(ItemValue item, float powerPerSlot, float defaultPower)
        {
            // Support for Undead Legacy (adds a passive effect for power)
            if (PowerOutputEffect?.GetValue(null) is PassiveEffects effect)
            {
                return (ushort)(powerPerSlot / defaultPower * EffectManager.GetValue(
                    effect, item, 0.0f, null, null, new FastTags<TagGroup.Global>(),
                    false, false, false, false, false, 1, false));
            }
            // Support for vanilla (just lerping the power for quality)
            return (ushort)(powerPerSlot * Mathf.Lerp(0.5f, 1f, item.Quality / 6f));
        }

        // ####################################################################
        // ####################################################################

        // Check if given `source` has a parent power source
        // Return `null` if any trigger group between is off
        static public OcbPowerSource GetParentSources(OcbPowerSource source)
        {
            for (PowerItem parent = source.Parent; parent != null; parent = parent.Parent)
            {
                if (parent is OcbPowerSource res) return res;
            }

            return null;
        }

        // ####################################################################
        // ####################################################################

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
                        if (parent is OcbPowerSource)
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
                if (parent is OcbPowerSource)
                {
                    return isActive;
                }
            }
            return isActive;
        }

        // ####################################################################
        // ####################################################################

        // Used for reporting information about overall state of the block
        // Worst item gives a good indication when a user wants to repair it
        static public float GetWorstStackItemUseState(OcbPowerSource source)
        {
            float state = 0f; bool hasLeft = false; bool isEmpty = true;
            for (int index = 0; index < source.Stacks.Length; ++index)
            {
                // Skip over empty battery slots
                if (source.Stacks[index].IsEmpty()) continue;
                // Check if item has quality, otherwise it always "just" works
                // if (!source.Stacks[index].itemValue.HasQuality) return 1f;
                // Avoid potential division by zero (play safe) 
                if (source.Stacks[index].itemValue.MaxUseTimes == 0) continue;
                // Calculate usage state (0 => not used, 1 => fully used)
                float used = source.Stacks[index].itemValue.UseTimes
                    / source.Stacks[index].itemValue.MaxUseTimes;
                state = Mathf.Max(state, used);
                if (used < 1f) hasLeft = true;
                isEmpty = false;
            }
            if (isEmpty) return -2f;
            if (!hasLeft) return -1f;
            return 1f - state;
        }

        // ####################################################################
        // ####################################################################

        // Fill `CurrentPower` of BatteryBank by draining batteries.
        // We try to distribute the power among all batteries. This
        // implementation has some funky behavior, but that seems to
        // be fine, since batteries in real-life can behave similarly.
        // E.g. lights may flicker if batteries are running out.
        static public void TickBatteryBankPowerGeneration(OcbPowerBatteryBank bank)
        {
            // MaxPower - theoretical maximum power if all batteries have energy
            // CurrentPower - amount of power currently available in the bank
            // CurrentPower acts as a internal buffer, where excess energy is stored.
            if (bank.CurrentPower < bank.MaxOutput)
            {
                float neededToMax = bank.MaxOutput - bank.CurrentPower;
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

                    if (neededToMax <= usesLeftOver * BatteryPowerPerUse)
                    {
                        bank.Stacks[index].itemValue.UseTimes += neededToMax / BatteryPowerPerUse;
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
                        bank.Stacks[index].itemValue.UseTimes += usesLeftOver / BatteryPowerPerUse;
                        bank.CurrentPower += (ushort)usesLeftOver;

                    }
                }
            }
        }

        // ####################################################################
        // ####################################################################

        // Mostly copied from original dll to insert our configurable conversion factor
        static public void TickBatteryDieselPowerGeneration(OcbPowerGenerator generator)
        {
            // Check if buffer has enough energy to fulfill max power
            if (generator.CurrentPower >= generator.MaxProduction) return;
            ushort neededToMax = (ushort)(generator.MaxProduction - generator.CurrentPower);
            ushort consume = (ushort)Mathf.Ceil((float)neededToMax / FuelPowerPerUse);
            consume = (ushort)Mathf.Min(consume, generator.CurrentFuel);
            generator.CurrentPower += (ushort)(consume * FuelPowerPerUse);
            generator.CurrentFuel -= consume;
        }

        // ####################################################################
        // ####################################################################

        // We get some amount of power that can be put into our batteries.
        // There are multiple ways to distribute that power (and how much)
        // in many ways. In the vanilla implementation only the first battery
        // that is not full receives the full power load. In our implementation
        // we try to only load batteries that are not full, so the charging
        // power can vary if only one or all batteries are not fully charged.
        // For this to work, we need to discharge batteries in the same way!
        static public void AddPowerToBatteries(OcbPowerBatteryBank bank, int power)
        {
            int used = 0;
            if (bank.IsOn)
            {

                // Distribute power to batteries from left to right
                // This might be unnatural, but is easily optimized
                for (int index = 0; index < bank.Stacks.Length; ++index)
                {
                    // Check if slot actually holds a battery
                    if (bank.Stacks[index].IsEmpty()) continue;
                    // Fix battery uses out of range (play safe)
                    bank.Stacks[index].itemValue.UseTimes =
                    MathUtils.Max(0, MathUtils.Min(
                        (int)bank.Stacks[index].itemValue.UseTimes,
                        bank.Stacks[index].itemValue.MaxUseTimes));
                    // Check if the battery slot can hold any more power
                    if (bank.Stacks[index].itemValue.UseTimes <= 0) continue;

                    // Get demand of local battery or what is left
                    ushort demand = (ushort)Mathf.Min(GetChargeByQuality(
                        bank.Stacks[index].itemValue), power);
                    float demandUses = demand / (float)BatteryPowerPerUse;
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
                        power -= (ushort)(bank.Stacks[index].itemValue.UseTimes * BatteryPowerPerUse);
                        used += (ushort)(bank.Stacks[index].itemValue.UseTimes * BatteryPowerPerUse);
                        bank.Stacks[index].itemValue.UseTimes = 0;
                    }

                }
            }
            // Copied from original DLL
            if (bank.LastInputAmount == used) return;
            bank.SendHasLocalChangesToRoot();
            bank.LastInputAmount = (ushort)used;
        }

    }

    // ####################################################################
    // ####################################################################

}

