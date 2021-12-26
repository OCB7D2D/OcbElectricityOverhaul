using UnityEngine;
using System.Reflection;

using System;
using System.Collections.Generic;
using System.IO;

using static OCB.ElectricityUtils;

public class OcbPowerManager : PowerManager
{

    // Upstream power sources when we go down
    // the tree via `ProcessPowerSource`.
    public Stack<PowerSource> lenders;

    // Global light for solar panels
    float globalLight = 1f;

    // Constructor
    public OcbPowerManager() : base()
    {
        // Allocate lenders stack where we register
        // upstream power sources when we go down
        // the tree via `ProcessPowerSource`.
        lenders = new Stack<PowerSource>();

    }

    public override void LoadPowerManager()
    {
        // Call base implementation
        base.LoadPowerManager();

        // Update configuration once when loaded from game preferences
        // Shouldn't change during runtime (unsure if this is the right spot?)
        isLoadVanillaMap = GamePrefs.GetBool(EnumGamePrefs.LoadVanillaMap);
        batteryPowerPerUse = GamePrefs.GetInt(EnumGamePrefs.BatteryPowerPerUse);
        minPowerForCharging = GamePrefs.GetInt(EnumGamePrefs.MinPowerForCharging);
        fuelPowerPerUse = GamePrefs.GetInt(EnumGamePrefs.FuelPowerPerUse);
        powerPerPanel = GamePrefs.GetInt(EnumGamePrefs.PowerPerPanel);
        powerPerEngine = GamePrefs.GetInt(EnumGamePrefs.PowerPerEngine);
        powerPerBattery = GamePrefs.GetInt(EnumGamePrefs.PowerPerBattery);
        chargePerBattery = GamePrefs.GetInt(EnumGamePrefs.ChargePerBattery);

        // Give one debug message for now (just to be sure we are running)
        Log.Out("Loaded OCB PowerManager (" + isLoadVanillaMap + "/" +
                batteryPowerPerUse + "/" + minPowerForCharging + ")");
        Log.Out("  Factors " + fuelPowerPerUse + "/" + powerPerPanel + "/" +
            powerPerEngine + "/" + powerPerBattery + "/" + chargePerBattery);
    }

    // Main function called by game manager per tick
    // Ticks seem to be in a strictly timely manner
    // ToDo: check exactly how tick updates are called
    // I think this implementation would act like crazy if
    // we are not called regularly (e.g. if deltaTime is
    // very big). We only are able to work off 0.16f per
    // call, but at least we are perfectly quantized.
    public override void Update()
    {

        if (GameManager.Instance.World == null) return;
        if (GameManager.Instance.World.Players == null) return;
        if (GameManager.Instance.World.Players.Count == 0) return;

        this.globalLight = 0f;
        var startTime = Environment.TickCount;
        var watch = System.Diagnostics.Stopwatch.StartNew();

        // PowerManager only runs on server instance, for clients the
        // only connection is via TileEntities and their DTO, e.g.
        // `TileEntityPowerSource::ClientPowerData` (this data is synced
        // via the write and read methods of the corresponding tile).
        if (SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
        {
            if (GameManager.Instance.gameStateManager.IsGameStarted())
            {
                // Update absolute delta of our last update call to check if tick
                // interval is due. We do updates only once per quantized time interval.
                // If we get called to fast, we skip calculation altogether, if we have
                // a very big delta, we must run as many time as needed to catch up.
                base.updateTime -= Time.deltaTime;
                if ((double)this.updateTime <= 0.0)
                {

                    // Calculate light levels once
                    var world = GameManager.Instance.World;
                    // Partially copied from `IsDark`
                    float time = (float)(world.worldTime % 24000UL) / 1000f;
                    if ((double)time > (double)world.DawnHour && (double)time < (double)world.DuskHour)
                    {
                        // Give a more natural sunrise and sunset effect
                        float span = (world.DuskHour - world.DawnHour) / 2f;
                        float halfTime = (world.DuskHour + world.DawnHour) / 2f;
                        float distance = span - Mathf.Abs(time - halfTime);
                        this.globalLight = Mathf.SmoothStep(0f, 1f, distance / 2f);
                    }

                    // Reset all parent triggers
                    // for (int index = 0; index < this.PowerTriggers.Count; ++index)
                    //     this.PowerTriggers[index].SetTriggeredByParent(false);

                    // Re-generate all power source first to enable full capacity
                    for (int index = 0; index < this.PowerSources.Count; ++index)
                    {
                        // Severe FPS drop only after 100'000 times per tick
                        RegeneratePowerSource(this.PowerSources[index]);
                    }
                    // Then start to distribute from root power sources down
                    for (int index = 0; index < this.PowerSources.Count; ++index)
                    {
                        // ToDo: This check could probably be optimized/cached
                        if (GetParentSources(this.PowerSources[index]) != null) continue;
                        // Will be called recursively for substream sources
                        ProcessPowerSource(this.PowerSources[index]);
                    }

                    // This triggers e.g. motions sensors with given delay
                    for (int index = 0; index < this.PowerTriggers.Count; ++index)
                        this.PowerTriggers[index].CachedUpdateCall();

                    // Doesn't do much anymore since caching is not enabled yet
                    for (int index = 0; index < this.PowerSources.Count; ++index)
                        FinalizePowerSource(this.PowerSources[index]);

                    if (Environment.TickCount - startTime > 40)
                        Log.Warning("PowerManager Tick took " + (Environment.TickCount - startTime) + " ms");
                    watch.Stop(); if (watch.Elapsed.Milliseconds > 40)
                        Log.Warning("PowerManager Watch took " + watch.Elapsed.Milliseconds + " ms");

                    // Add another tick interval
                    // Guessing this means every 160ms
                    // Which is around 6 times per second
                    // Note: easiest factor to adjust for better performance
                    // Although it makes the whole grid a bit less responsive
                    // None the less, even two updates per second seems fair enough
                    this.updateTime = 0.16f;
                }

                // Suppose this saves data to disk from time to time
                // Simply copied from original vanilla implementation
                this.saveTime -= Time.deltaTime;
                if ((double)this.saveTime <= 0.0 &&
                    (this.dataSaveThreadInfo == null || this.dataSaveThreadInfo.HasTerminated()))
                {
                    // Means every 2 minutes!?
                    this.saveTime = 120f;
                    this.SavePowerManager();
                }
            }
        }

        // No idea what this does exactly, copied from vanilla code
        for (int index = 0; index < this.ClientUpdateList.Count; ++index)
            this.ClientUpdateList[index].ClientUpdate();

    }

    // Ensure that enough power is available in `CurrentPower`
    // The `CurrentPower` property acts like a buffer
    // When fuel is burned we add it to `CurrentPower`
    // When we consume power we take it from `CurrentPower`
    // Note: this is called at the start of each tick
    // Batteries will over-load `CurrentPower` in order
    // to drain all batteries evenly (power is buffered).
    public void RegeneratePowerSource(PowerSource source)
    {

        source.MaxProduction = source.MaxOutput;

        // Code is coped from vanilla game dll
        if (source is PowerSolarPanel solar)
        {
            source.MaxPower = 0;
            source.MaxOutput = 0;
            source.MaxProduction = 0;
            source.RequiredPower = 0;
            if (source.IsOn)
            {
                if ((double)Time.time > (double)solar.lightUpdateTime)
                {
                    solar.lightUpdateTime = Time.time + 2f;
                    // ToDo: maybe add a bit more elaborate sun-light detection
                    // Currently it will simply switch on/off between day/night
                    // HasLight should probably be a float to achieve this
                    solar.CheckLightLevel();
                }
            }
            for (int index = source.Stacks.Length - 1; index >= 0; --index)
            {
                if (!source.Stacks[index].IsEmpty())
                {
                    ItemStack slot = source.Stacks[index];
                    ushort cellPower = GetCellPowerByQuality(slot.itemValue.Quality);
                    if (source.IsOn && solar.HasLight)
                    {
                        source.MaxProduction += (ushort)Mathf.Ceil(cellPower * globalLight);
                    }
                    source.MaxOutput += cellPower;
                    source.MaxPower += cellPower;
                }
            }
        }

        if (source.IsOn)
        {
            // Code directly copied from decompiled dll
            if ((int)source.CurrentPower < (int)source.MaxPower)
                source.TickPowerGeneration();
            // else if ((int)source.CurrentPower > (int)source.MaxPower)
            //     source.CurrentPower = source.MaxPower;
        }

        // We introduce `MaxProduction`, since vanilla code expects `MaxOutput`
        // not to be zero in order to activate a power source.
        source.ChargingDemand = (ushort)0;

        // BatteryBanks are a bit CPU intensive!
        // Calculate demand and production on each tick
        // ToDo: find out how caching could work with this!
        if (source is PowerBatteryBank bank)
        {
            source.MaxPower = 0;
            source.MaxOutput = 0;
            source.MaxProduction = 0;
            source.RequiredPower = 0;
            for (int index = source.Stacks.Length - 1; index >= 0; --index)
            {
                if (!source.Stacks[index].IsEmpty())
                {
                    ItemStack slot = source.Stacks[index];
                    ushort discharge = GetDischargeByQuality(slot.itemValue.Quality);
                    if (source.IsOn)
                    {
                        if (slot.itemValue.UseTimes < slot.itemValue.MaxUseTimes)
                        {
                            // ToDo: should we cap at what is actually available?
                            source.MaxProduction += discharge;
                        }
                        if (slot.itemValue.UseTimes > 0)
                        {
                            // ToDo: should we cap at what is actually needed?
                            bank.ChargingDemand += GetChargeByQuality(slot.itemValue.Quality);
                        }
                    }
                    // Production if all batteries are loaded
                    source.MaxOutput += discharge;
                    source.MaxPower += discharge;
                }
            }
            // Power needed to charge batteries not fully loaded
            source.RequiredPower = bank.ChargingDemand;
        }
        else if (source is PowerGenerator generator)
        {
            source.MaxPower = 0;
            source.MaxOutput = 0;
            source.MaxProduction = 0;
            source.RequiredPower = 0;
            generator.MaxFuel = GeneratorMaxFuelDefault;
            for (int index = source.Stacks.Length - 1; index >= 0; --index)
            {
                if (!source.Stacks[index].IsEmpty())
                {
                    if (source.IsOn)
                    {
                        source.MaxProduction += (ushort)powerPerEngine;
                    }
                    source.MaxOutput += (ushort)powerPerEngine;
                    source.MaxPower += (ushort)powerPerEngine;
                }
            }
        }

        // Code directly copied from decompiled dll
        if (source.ShouldAutoTurnOff())
        {
            source.CurrentPower = (ushort)0;
            source.IsOn = false;
        }

    }

    // Borrow as much power from `lender` as possible to fulfill `distribute` requirement
    public void BorrowPowerFromSource(PowerSource lender, ref ushort distribute, PowerBatteryBank battery = null)
    {
        if (distribute <= 0) return;
        int lenderPower = Mathf.Min(lender.MaxProduction, lender.CurrentPower);
        ushort lenderLeftOver = (ushort)(lenderPower - lender.LastPowerUsed);
        if (lenderLeftOver <= 0) return;
        // Lender has enough energy
        if (lenderLeftOver >= distribute)
        {
            lender.LastPowerUsed += distribute;
            if (battery != null)
            {
                lender.LentCharging += distribute;
            }
            else
            {
                lender.LentConsumed += distribute;
            }
            distribute = 0;
        }
        // Only partially fulfilled
        else
        {
            lender.LastPowerUsed += lenderLeftOver;
            if (battery != null)
            {
                lender.LentCharging += lenderLeftOver;
            }
            else
            {
                lender.LentConsumed += lenderLeftOver;
            }
            distribute -= lenderLeftOver;
        }
    }

    // Accumulate data for statistic purposes to show how much
    // energy is actually flowing through any given node. For that
    // we register our energy use on every parent power source,
    // until we reach the target that provided us that energy.
    public void AccountPowerUse(PowerSource target, ushort used, PowerBatteryBank battery = null)
    {
        if (used == 0) return;
        var enumerator = lenders.GetEnumerator();
        while (enumerator.MoveNext())
        {
            PowerSource lender = enumerator.Current;
            // Distinguish power consumption
            if (battery != null)
            {
                lender.LentChargingUsed += used;
            }
            else
            {
                lender.LentConsumerUsed += used;
            }
            if (lender == target) break;
        }

    }

    // Distribute the `distribute` load to all lenders
    // ToDo: loop and casts seems semi optimal
    // but doesn't seem to be any bottleneck.
    // Also a bit much code-repetition for my liking.
    // But do we really gain much by optimizing this?
    public ushort BorrowPower(ref ushort distribute, PowerBatteryBank battery = null)
    {
        ushort used = 0;
        ushort before = distribute;
        if (distribute <= 0) return used;
        var enumerator = lenders.GetEnumerator();
        int i = 0;
        while (enumerator.MoveNext())
        {
            PowerSource lender = enumerator.Current;
            if (!lender.isOn) continue;
            if (!(lender is PowerSolarPanel)) continue;
            if (battery != null && !battery.ChargeFromSolar)
                continue;
            ushort distributing = distribute;
            BorrowPowerFromSource(lender, ref distribute, battery);
            AccountPowerUse(lender, (ushort)(distributing - distribute), battery);
            if (distribute == 0) break;
            i++;
        }

        enumerator = lenders.GetEnumerator();
        i = 0;
        while (enumerator.MoveNext())
        {
            PowerSource lender = enumerator.Current;
            if (!lender.isOn) continue;

            if (lender is PowerBatteryBank)
            {
                if (battery != null && !battery.ChargeFromBattery)
                    continue;
            }
            else if (lender is PowerGenerator)
            {
                if (battery != null && !battery.ChargeFromGenerator)
                    continue;
            }
            else
            {
                continue;
            }
            ushort distributing = distribute;
            BorrowPowerFromSource(lender, ref distribute, battery);
            AccountPowerUse(lender, (ushort)(distributing - distribute), battery);
            if (distribute == 0) break;
            i++;
        }
        // Return how much power we used
        return (ushort)(before - distribute);
    }

    // Our bread and butter function that drives the whole electricity grid.
    public void ProcessPowerSource(PowerSource root)
    {
        // Power used from local power source
        root.LastPowerUsed = (ushort)0;
        // Power put into local battery bank
        root.ChargingUsed = (ushort)0;
        // Maximum local charging capacity
        // Calculated by `RegeneratePowerSource`
        // root.ChargingDemand = (ushort)0;
        // Power local consumers have used
        root.ConsumerUsed = (ushort)0;
        // Power local consumers are demanding
        root.ConsumerDemand = (ushort)0;
        // Power local source has lent to other consumers
        root.LentConsumed = (ushort)0;
        // Power local source has lent to charge other batteries
        root.LentCharging = (ushort)0;
        // Power downstream consumers are using
        root.GridConsumerUsed = (ushort)0;
        // Power downstream consumers are demanding
        root.GridConsumerDemand = (ushort)0;
        // Power downstream batteries are using
        root.GridChargingUsed = (ushort)0;
        // Power downstream batteries are demanding
        root.GridChargingDemand = (ushort)0;
        // Power flowing through this grid node
        root.LentConsumerUsed = (ushort)0;
        root.LentChargingUsed = (ushort)0;

        // Add ourself as a lender
        lenders.Push(root);

        // Check how much power is available
        int lendable = 0;

        // Calculate lendable power from upstream lenders
        var enumerator = lenders.GetEnumerator();
        while (enumerator.MoveNext())
        {
            PowerSource lender = enumerator.Current;
            if (!lender.isOn) continue;
            int produced = Mathf.Min(lender.MaxProduction, lender.CurrentPower);
            lendable += produced - lender.LastPowerUsed;
        }

        // Need to get all children recursively, therefore we need a stack
        // Fill the stack with the initial Children of the given PowerSource
        Queue<PowerItem> children = new Queue<PowerItem>();
        for (int i = 0; i < root.Children.Count; ++i)
            children.Enqueue(root.Children[i]);

        // Store all sub PowerSource nodes for further processing
        // We will give them our excessive energy to be borrowed
        Queue<PowerSource> subnodes = new Queue<PowerSource>();

        // Range of ushort is up to 65'535
        ushort power = (ushort)(lendable);

        // Loop all children non recursive
        // Adds further children to collection
        while (children.Count > 0)
        {
            ushort num = (ushort)(power);
            // Shift the first item of the queue
            PowerItem child = children.Dequeue();

            // Account for consumer demand
            if (child is PowerConsumerToggle toggle)
            {
                if (toggle.IsToggled)
                    root.ConsumerDemand += child.RequiredPower;
            }
            else if (child is PowerSource)
            {
                // Intentionally nothing we do here
                // ToDo: Generator has RequiredPower?
            }
            else
            {
                root.ConsumerDemand += child.RequiredPower;
            }

            // Enqueue all grand children to be processed
            if (child is PowerSource source)
            {
                // source.SendHasLocalChangesToRoot();
                subnodes.Enqueue(source);
            }
            // Process all any other PowerItem
            else
            {
                // Send power to the consumer item
                child.HandlePowerReceived(ref power);
                ushort distribute = (ushort)((uint)num - (uint)power);
                // Distribute consumption to all possible lenders
                ushort used = BorrowPower(ref distribute, null);
                // Accounting for used power
                lendable -= used;
                // Account for consumed power
                root.ConsumerUsed += used;
                // Process further children of consumers
                for (int i = 0; i < child.Children.Count; ++i)
                {
                    // Check translates to `is PowerConsumer`
                    if (!(child.Children[i] is PowerTrigger))
                    {
                        // Check if consumer has no "off" trigger group upstream
                        // ToDo: could probably be optimized (low hanging fruit)
                        if (!IsTriggerActive(child.Children[i]))
                        {
                            continue;
                        }
                    }

                    // Add further children to be processed
                    children.Enqueue(child.Children[i]);
                }

                if (child.WasPowered != child.isPowered)
                {
                    child.WasPowered = child.isPowered;
                    child.HandlePowerUpdate(used > 0);
                }

                child.hasChangesLocal = false;
            }
        }

        // Process downstream power sources
        // Since all local consumers have been asked to
        // consume energy, we now should know how much
        // energy is left for any substream consumers.
        while (subnodes.Count > 0)
        {
            ProcessPowerSource(subnodes.Dequeue());
        }

        // Remove from stack
        this.lenders.Pop();

        if (root is PowerBatteryBank bank)
            DistributeLeftToBank(bank);

        // Now distribute power statistics up the chain
        // Note: this is only for statistics (remove?)
        PowerSource up = GetParentSources(root);
        while (up != null)
        {
            up.GridConsumerDemand += root.ConsumerDemand;
            up.GridChargingDemand += root.ChargingDemand;
            up.GridConsumerUsed += root.ConsumerUsed;
            up.GridChargingUsed += root.ChargingUsed;
            up = GetParentSources(up);
        }

        // Copied from original dll (power graph)
        root.CurrentPower -= root.LastPowerUsed;

        // Not really used, but do it anyway
        root.hasChangesLocal = false;
    }

    private void DistributeLeftToBank(PowerBatteryBank bank)
    {
        if (!bank.IsOn) return;

        int lendableSolar = 0;
        int lendableBattery = 0;
        int lendableGenerator = 0;

        // Re-Calculate lendable power from upstream each time
        // This is the rest power that can go into batteries
        var enumerator = lenders.GetEnumerator();
        while (enumerator.MoveNext())
        {
            if (!enumerator.Current.isOn) continue;
            PowerSource lender = enumerator.Current;
            int produced = Mathf.Min(lender.MaxProduction, lender.CurrentPower);
            int powerLeftOver = produced - lender.LastPowerUsed;
            switch (lender.PowerItemType)
            {
                case PowerItem.PowerItemTypes.SolarPanel:
                    lendableSolar += powerLeftOver;
                    break;
                case PowerItem.PowerItemTypes.BatteryBank:
                    lendableBattery += powerLeftOver;
                    break;
                case PowerItem.PowerItemTypes.Generator:
                    lendableGenerator += powerLeftOver;
                    break;
            }
        }

        int lendable = 0;
        // Any left-over energy can be used to charge batteries.
        // The (configurable) limit to start charging batteries ensures
        // that we don't get too much ping-pong charging/discharging.
        if (bank.ChargeFromSolar) lendable += lendableSolar;
        if (bank.ChargeFromBattery) lendable += lendableBattery;
        if (bank.ChargeFromGenerator) lendable += lendableGenerator;

        bank.ChargingUsed = 0;

        if (lendable >= minPowerForCharging)
        {
            // Get demand or what is available
            ushort demand = (ushort)Mathf.Min(
                lendable, bank.ChargingDemand);
            // This updates bank.LastInputAmount
            bank.AddPowerToBatteries(demand);
            // Get how much power was added to batteries
            // Need to distribute this now to power sources
            ushort charged = bank.LastInputAmount;
            // Nothing to do if nothing charged
            if (charged == 0) return;
            // Try to borrow the power from all sources
            bank.ChargingUsed = BorrowPower(ref charged, bank);
            // Check if everything was calculated as expected
            if (charged != 0) Log.Error("Phantom charging detected");
        }

    }

    public void FinalizePowerSource(PowerSource source)
    {
        if (!source.hasChangesLocal)
            return;
        // This disables e.g. the Motion Sensor trigger if needed
        // for (int index = 0; index < source.Children.Count; ++index)
        //     source.Children[index].HandlePowerUpdate(source.IsOn);
        source.hasChangesLocal = false;
    }

}