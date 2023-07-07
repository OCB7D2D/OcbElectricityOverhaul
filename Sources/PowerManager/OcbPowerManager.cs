using System.Collections.Generic;
using UnityEngine;
using XMLData.Parsers;
using static OCB.ElectricityUtils;
using Random = UnityEngine.Random;

public class OcbPowerManager : PowerManagerBase
{

    public float Interval = .16f;
    static readonly HarmonyFieldProxy<bool> PowerItemHasLocalChanges =
        new HarmonyFieldProxy<bool>(typeof(PowerItem), "hasChangesLocal");

    // Upstream power sources when we go down
    // the tree via `ProcessPowerSource`.
    public Stack<OcbPowerSource> lenders
        = new Stack<OcbPowerSource>();

    // Power Sources representing root nodes
    public readonly List<OcbPowerSource> Grids
        = new List<OcbPowerSource>();

    // Reuse container on internal function
    private static readonly Queue<PowerItem> collect
        = new Queue<PowerItem>();

    // Store the current/last timer tick count
    public ulong Ticks { get; private set; }

    // Global light for solar panels
    public float GlobalLight = 1f;

    bool IsDirty = true;

    // Constructor
    public OcbPowerManager() : base()
    {
    }

    public override void LoadPowerManager()
    {
        // Call base implementation
        base.LoadPowerManager();

        // Update configuration once when loaded from game preferences
        // Shouldn't change during runtime (unsure if this is the right spot?)

        // We resolve enum dynamically on runtime, since we don't want to
        // hard-code a specific value into our own runtime. This allows
        // compatibility even if game dll alters the enum between version.

        IsLoadVanillaMap = GamePrefs.GetBool(EnumParser.Parse<EnumGamePrefs>("LoadVanillaMap"));
        IsPreferFuelOverBattery = GamePrefs.GetBool(EnumParser.Parse<EnumGamePrefs>("PreferFuelOverBattery"));
        BatteryPowerPerUse = GamePrefs.GetInt(EnumParser.Parse<EnumGamePrefs>("BatteryPowerPerUse"));
        MinPowerForCharging = GamePrefs.GetInt(EnumParser.Parse<EnumGamePrefs>("MinPowerForCharging"));
        FuelPowerPerUse = GamePrefs.GetInt(EnumParser.Parse<EnumGamePrefs>("FuelPowerPerUse"));
        PowerPerPanel = GamePrefs.GetInt(EnumParser.Parse<EnumGamePrefs>("PowerPerPanel"));
        PowerPerEngine = GamePrefs.GetInt(EnumParser.Parse<EnumGamePrefs>("PowerPerEngine"));
        PowerPerBattery = GamePrefs.GetInt(EnumParser.Parse<EnumGamePrefs>("PowerPerBattery"));
        BatteryChargePercentFull = GamePrefs.GetInt(EnumParser.Parse<EnumGamePrefs>("BatteryChargePercentFull"));
        BatteryChargePercentEmpty = GamePrefs.GetInt(EnumParser.Parse<EnumGamePrefs>("BatteryChargePercentEmpty"));

        // Give one debug message for now (just to be sure we are running)
        Log.Out("Loaded OCB PowerManager (" +
                IsLoadVanillaMap + "/" + IsPreferFuelOverBattery + "/" +
                BatteryPowerPerUse + "/" + MinPowerForCharging + ")");
        Log.Out("  Factors " + FuelPowerPerUse + "/" + PowerPerPanel +
                "/" + PowerPerEngine + "/" + PowerPerBattery);
    }

    // Main function called by game manager per tick
    // Ticks seem to be in a strictly timely manner
    // ToDo: check exactly how tick updates are called
    // I think this implementation would act like crazy if
    // we are not called regularly (e.g. if deltaTime is
    // very big). We only are able to work off 0.16f per
    // call, but at least we are perfectly quantized.

    public void CollectGridChidren(OcbPowerSource root)
    {
        collect.Enqueue(root);
        while (collect.Count > 0)
        {
            PowerItem item = collect.Dequeue();
            if (item is PowerTrigger trigger)
            {
                root.PowerTriggers.Add(trigger);
            }
            else if (item is OcbPowerSource source)
            {
                root.PowerSources.Add(source);
            }
            foreach (var child in item.Children)
            {
                collect.Enqueue(child);
            }
        }
    }

    public void ResetGrids()
    {
        Grids.Clear();
        // Reset all power sources and collect root nodes
        foreach (OcbPowerSource source in PowerSources)
        {
            // Reset or create lists to hold (potential) grid children
            if (source.PowerSources != null) source.PowerSources.Clear();
            else source.PowerSources = new List<OcbPowerSource>();
            if (source.PowerTriggers != null) source.PowerTriggers.Clear();
            else source.PowerTriggers = new List<PowerTrigger>();
            // Skip all power sources that have parent sources
            if (GetParentSources(source) != null) continue;
            // Accumulate all root power sources
            Grids.Add(source);
        }
        // Distribute update times and collect children
        for (int i = 0; i < Grids.Count; i += 1)
        {
            // Update each grid at a different frame
            Grids[i].UpdateTime = Interval / Grids.Count * i;
            // Collect sources and triggers
            CollectGridChidren(Grids[i]);
        }
    }

    public void UpdateLight()
    {
        // Calculate light levels once
        var world = GameManager.Instance.World;
        // Partially copied from `IsDark`
        float time = (world.worldTime % 24000UL) / 1000f;
        if (time > world.DawnHour && time < world.DuskHour)
        {
            // Give a more natural sunrise and sunset effect
            float span = (world.DuskHour - world.DawnHour) / 2f;
            float halfTime = (world.DuskHour + world.DawnHour) / 2f;
            float distance = span - Mathf.Abs(time - halfTime);
            GlobalLight = Mathf.SmoothStep(0f, 1f, distance / 2f);
        }
    }

    public void UpdateGrid(OcbPowerSource root)
    {

        // Take overhead hit to have some idea about performance
        var watch = System.Diagnostics.Stopwatch.StartNew();

        // Check timers only for power triggers
        // Makes the whole thing a bit more in sync!?
        // foreach (PowerTrigger trigger in root.PowerTriggers)
        // {
        //     bool changes = trigger.hasChangesLocal;
        //     // Disables single use checks
        //     trigger.hasChangesLocal = true;
        //     trigger.CachedUpdateCall();
        //     trigger.hasChangesLocal = changes;
        // }

        // Re-generate all power sources first to enable full capacity
        foreach (OcbPowerSource source in root.PowerSources)
            RegeneratePowerSource(source);
        // Distribute from root down
        ProcessPowerSource(root);
        // Update triggers and see how they affect groups
        foreach (PowerTrigger trigger in root.PowerTriggers)
            trigger.CachedUpdateCall();
        // Not sure if this does much at all currently
        foreach (OcbPowerSource source in root.PowerSources)
            FinalizePowerSource(source);

        watch.Stop();

        // Once we reach this level, we certainly have an issue!
        if (watch.Elapsed.TotalMilliseconds > 20) Log.Warning(
            "PowerManager took " + watch.Elapsed.TotalMilliseconds + " ms");

        // For debugging purposes only (as exposed by electricity overhaul admin mod)
        // This is a simple approximation and should at least give a well enough
        // ball-park number to estimate how long the power grid calculations took.
        // https://github.com/OCB7D2D/ElectricityOverhaulAdmin
        root.AvgTime = 0.9f * root.AvgTime + 0.1f * (float)watch.Elapsed.TotalMilliseconds;

    }

    public override void Update()
    {

        if (GameManager.Instance.World == null) return;
        if (GameManager.Instance.World.Players == null) return;
        if (GameManager.Instance.World.Players.Count == 0) return;

        // Do nothing when the game is paused
        if (Ticks == GameTimer.Instance.ticks) return;

        // Update ticks counter first
        Ticks = GameTimer.Instance.ticks;

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
                // a very big delta, we must run as many time as needed to catch up?

                if (IsDirty)
                {
                    ResetGrids();
                    IsDirty = false;
                }
                updateTime -= Time.deltaTime;
                if (updateTime <= 0.0)
                {
                    GlobalLight = 0f;
                    UpdateLight();
                    updateTime = Interval;
                }
                foreach (OcbPowerSource root in Grids)
                {
                    root.UpdateTime -= Time.deltaTime;
                    if (root.UpdateTime > 0) continue;
                    while (root.UpdateTime <= 0)
                        root.UpdateTime += Interval;
                    UpdateGrid(root);
                }

                // Suppose this saves data to disk from time to time
                // Simply copied from original vanilla implementation
                saveTime -= Time.deltaTime;
                if (saveTime <= 0.0 &&
                    (dataSaveThreadInfo == null || dataSaveThreadInfo.HasTerminated()))
                {
                    // Means every 2 minutes!?
                    saveTime = 120f;
                    SavePowerManager();
                }
            }
        }

        // No idea what this does exactly, copied from vanilla code
        // Note: seems to be only used by `TileEntityPoweredRangedTrap`
        for (int index = 0; index < ClientUpdateList.Count; ++index)
            ClientUpdateList[index].ClientUpdate();

    }

    // Ensure that enough power is available in `CurrentPower`
    // The `CurrentPower` property acts like a buffer
    // When fuel is burned we add it to `CurrentPower`
    // When we consume power we take it from `CurrentPower`
    // Note: this is called at the start of each tick
    // Batteries will over-load `CurrentPower` in order
    // to drain all batteries evenly (power is buffered).
    public void RegeneratePowerSource(OcbPowerSource source)
    {

        // Code is coped from vanilla game dll
        if (source is OcbPowerSolarPanel solar)
        {
            source.RequiredPower = 0;
            if (source.OutputPerStack == 0) source
                .OutputPerStack = (ushort)PowerPerPanel;
            if (Time.time > solar.wearUpdateTime)
            {
                solar.wearUpdateTime = Time.time + Random
                    .Range(WearMinInterval, WearMaxInterval);
                foreach (var slot in source.Stacks)
                {
                    if (slot.IsEmpty()) continue;
                    // Check if item in slot can degrade
                    if (slot.itemValue.MaxUseTimes > 0)
                    {
                        // Check if item is not yet completely used up
                        if (slot.itemValue.UseTimes < slot.itemValue.MaxUseTimes)
                        {
                            // Add more randomness to it
                            if (Random.Range(0f, 1f) < WearThreshold)
                            {
                                // Slightly damage the item
                                slot.itemValue.UseTimes += Random.Range(1f, WearFactor);
                                // Slot has newly reached the max use times
                                if (slot.itemValue.UseTimes >= slot.itemValue.MaxUseTimes)
                                {
                                    slot.itemValue.UseTimes = slot.itemValue.MaxUseTimes;
                                    solar.StackPower -= (ushort)(
                                        source.OutputPerStack / PowerPerPanel *
                                        GetSlotPowerByQuality(slot.itemValue,
                                            PowerPerPanel, PowerPerPanelDefault));
                                    solar.TileEntity?.MarkChanged();
                                    break; // Only decrement one per tick
                                }
                            }
                        }

                        if (slot.itemValue.UseTimes >= slot.itemValue.MaxUseTimes)
                            slot.itemValue.UseTimes = slot.itemValue.MaxUseTimes;
                    }
                }
                // Turn off when all is defect
                if (solar.StackPower <= 0)
                    solar.IsOn = false;
            }
            if (Time.time > solar.lightUpdateTime)
            {
                solar.lightUpdateTime = Time.time + 2f;
                // ToDo: maybe add a bit more elaborate sun-light detection
                // Currently it will simply switch on/off between day/night
                // HasLight should probably be a float to achieve this
                solar.CheckLightLevel();
            }

            var props = Block.list[source.BlockID].Properties;
            if (!props.Values.ContainsKey("IsWindmill"))
            {
                solar.LightLevel = (ushort)(GlobalLight * ushort.MaxValue);
                if (!solar.HasLight) solar.LightLevel = 0;
            }

            float factor = (float)solar.LightLevel / ushort.MaxValue;
            float production = !source.IsOn ? 0 : solar.StackPower;
            source.MaxOutput = solar.StackPower;

            // Round solar power always up
            production = Mathf.Ceil(factor * production);
            source.MaxProduction = (ushort)production;
        }
        else
        {
            source.MaxProduction = source.MaxOutput;
        }

        // Limit MaxOutput according to MaxPower
        source.MaxOutput = (ushort)Mathf.Min(
            source.MaxOutput, source.MaxPower);
        source.MaxProduction = (ushort)Mathf.Min(
            source.MaxProduction, source.MaxOutput);

        if (source.IsOn)
        {
            // Code directly copied from decompiled dll
            if (source.CurrentPower < source.MaxOutput)
                source.DoPowerGenerationTick();
            // else if ((int)source.CurrentPower > (int)source.MaxOutput)
            //     source.CurrentPower = source.MaxOutput;
        }

        // We introduce `MaxProduction`, since vanilla code expects
        // `MaxOutput` to not be zero in order to activate power source.
        source.ChargingDemand = 0;
        // source.ChargingUsed = 0;

        // BatteryBanks are a bit CPU intensive!
        // Calculate demand and production on each tick
        // ToDo: find out how caching could work with this!
        if (source is OcbPowerBatteryBank bank)
        {
            ushort capacity = 0, discharging = 0;
            if (source.OutputPerStack == 0) source
                .OutputPerStack = (ushort)PowerPerBattery;
            float factor = source.OutputPerStack / (float)PowerPerBattery;
            foreach (var slot in source.Stacks)
            {
                if (slot.IsEmpty()) continue;
                ushort discharge = (ushort)(factor *
                    GetSlotPowerByQuality(slot.itemValue,
                        PowerPerBattery, PowerPerBatteryDefault));
                if (source.IsOn)
                {
                    // Check if battery has some juice left
                    if (slot.itemValue.UseTimes < slot.itemValue.MaxUseTimes)
                    {
                        // ToDo: should we cap at what is actually available?
                        // source.MaxProduction += discharge;
                        discharging += discharge;
                    }
                    // Check if battery could use some charging
                    if (slot.itemValue.UseTimes > 0)
                    {
                        // ToDo: should we cap at what is actually needed?
                        bank.ChargingDemand += (ushort)(factor *
                            GetChargeByQuality(slot.itemValue));
                    }
                }
                // Production if all batteries are loaded
                capacity += discharge;
            }
            source.MaxOutput = capacity;
            source.MaxProduction = discharging;
            // Power needed to charge batteries not fully loaded
            source.RequiredPower = bank.ChargingDemand;
        }
        else if (source is OcbPowerGenerator)
        {
            // Calculate the maximum power will all the filled engine slots
            if (source.OutputPerStack == 0) source
                .OutputPerStack = (ushort)PowerPerEngine;
            float factor = source.OutputPerStack / (float)PowerPerEngine;
            float power = source.StackPower * factor;
            source.RequiredPower = 0;
            source.MaxProduction = (ushort)
                (source.IsOn ? power : 0);
            source.MaxOutput = (ushort)power;

        }

        // Limit MaxOutput according to MaxPower
        source.MaxOutput = (ushort)Mathf.Min(
            source.MaxOutput, source.MaxPower);
        source.MaxProduction = (ushort)Mathf.Min(
            source.MaxProduction, source.MaxOutput);

        // Code directly copied from decompiled dll
        if (source.ShouldItAutoTurnOff())
        {
            source.CurrentPower = 0;
            source.IsOn = false;
        }

    }

    // Borrow as much power from `lender` as possible to fulfill `distribute` requirement
    public void BorrowPowerFromSource(OcbPowerSource lender, ref ushort distribute, OcbPowerBatteryBank battery = null)
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
    public void AccountPowerUse(OcbPowerSource target, ushort used, OcbPowerBatteryBank battery = null)
    {
        if (used == 0) return;
        var enumerator = lenders.GetEnumerator();
        while (enumerator.MoveNext())
        {
            OcbPowerSource lender = enumerator.Current;
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
    public ushort BorrowPower(ref ushort distribute, OcbPowerBatteryBank battery = null)
    {
        ushort used = 0;
        ushort before = distribute;
        if (distribute <= 0) return used;
        var enumerator = lenders.GetEnumerator();
        while (enumerator.MoveNext())
        {
            OcbPowerSource lender = enumerator.Current;
            if (!lender.IsOn) continue;
            if (!(lender is OcbPowerSolarPanel)) continue;
            if (battery != null && !battery.ChargeFromSolar)
                continue;
            ushort distributing = distribute;
            BorrowPowerFromSource(lender, ref distribute, battery);
            AccountPowerUse(lender, (ushort)(distributing - distribute), battery);
            if (distribute == 0) break;
        }

        if (IsPreferFuelOverBattery)
        {

            enumerator = lenders.GetEnumerator();
            while (enumerator.MoveNext())
            {
                OcbPowerSource lender = enumerator.Current;
                if (!lender.IsOn) continue;
                if (!(lender is OcbPowerGenerator)) continue;
                if (battery != null && !battery.ChargeFromGenerator)
                    continue;
                ushort distributing = distribute;
                BorrowPowerFromSource(lender, ref distribute, battery);
                AccountPowerUse(lender, (ushort)(distributing - distribute), battery);
                if (distribute == 0) break;
            }

            enumerator = lenders.GetEnumerator();
            while (enumerator.MoveNext())
            {
                OcbPowerSource lender = enumerator.Current;
                if (!lender.IsOn) continue;
                if (!(lender is OcbPowerBatteryBank)) continue;
                if (battery != null && !battery.ChargeFromBattery)
                    continue;
                ushort distributing = distribute;
                BorrowPowerFromSource(lender, ref distribute, battery);
                AccountPowerUse(lender, (ushort)(distributing - distribute), battery);
                if (distribute == 0) break;
            }

        }
        else
        {

            enumerator = lenders.GetEnumerator();
            while (enumerator.MoveNext())
            {
                OcbPowerSource lender = enumerator.Current;
                if (!lender.IsOn) continue;

                if (lender is OcbPowerBatteryBank)
                {
                    if (battery != null && !battery.ChargeFromBattery)
                        continue;
                }
                else if (lender is OcbPowerGenerator)
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
            }

        }

        // Return how much power we used
        return (ushort)(before - distribute);
    }

    // Our bread and butter function that drives the whole electricity grid.
    public void ProcessPowerSource(OcbPowerSource root)
    {
        // Check if this root was already ticked
        if (root.LastTick >= Ticks) return;
        // Register us as being ticked
        root.LastTick = Ticks;
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
            OcbPowerSource lender = enumerator.Current;
            if (!lender.IsOn) continue;
            int produced = Mathf.Min(lender.MaxProduction, lender.CurrentPower);
            lendable += produced - lender.LastPowerUsed;
        }

        // Need to get all children recursively, therefore we need a stack
        // Fill the stack with the initial Children of the given PowerSourceOcb
        Queue<PowerItem> children = new Queue<PowerItem>();
        for (int i = 0; i < root.Children.Count; ++i)
            children.Enqueue(root.Children[i]);

        // Store all sub PowerSource nodes for further processing
        // We will give them our excessive energy to be borrowed
        Queue<OcbPowerSource> subnodes = new Queue<OcbPowerSource>();

        // Range of ushort is up to 65'535
        ushort power = (ushort)(lendable);

        // Register the max grid production
        root.MaxGridProduction = (ushort)lendable;

        // Loop all children non recursive
        // Adds further children to collection
        while (children.Count > 0)
        {
            ushort num = (ushort)(power);
            // Shift the first item of the queue
            PowerItem child = children.Dequeue();

            bool wasPowered = child.IsPowered;

            // Account for consumer demand
            if (child is PowerConsumerToggle toggle)
            {
                if (toggle.IsToggled)
                    root.ConsumerDemand += child.RequiredPower;
            }
            else if (child is OcbPowerSource)
            {
                // Intentionally nothing we do here
                // ToDo: Generator has RequiredPower?
            }
            else
            {
                root.ConsumerDemand += child.RequiredPower;
            }

            // Enqueue all grand children to be processed
            if (child is OcbPowerSource source)
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

                if (wasPowered != child.IsPowered)
                {
                    child.HandlePowerUpdate(used > 0);
                }

                PowerItemHasLocalChanges.Set(child, false);
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
        lenders.Pop();

        if (root is OcbPowerBatteryBank bank)
            DistributeLeftToBank(bank);

        // Now distribute power statistics up the chain
        // Note: this is only for statistics (remove?)
        OcbPowerSource up = GetParentSources(root);
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
        PowerItemHasLocalChanges.Set(root, false);
    }

    private void DistributeLeftToBank(OcbPowerBatteryBank bank)
    {
        bank.ChargingUsed = 0;
        if (!bank.IsOn) return;

        int lendableSolar = 0;
        int lendableBattery = 0;
        int lendableGenerator = 0;

        // Re-Calculate lendable power from upstream each time
        // This is the rest power that can go into batteries
        var enumerator = lenders.GetEnumerator();
        while (enumerator.MoveNext())
        {
            if (!enumerator.Current.IsOn) continue;
            OcbPowerSource lender = enumerator.Current;
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

        if (lendable >= MinPowerForCharging)
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

    public void FinalizePowerSource(OcbPowerSource source)
    {
        // Tick disconnected source
        // Bails out if already ticked
        ProcessPowerSource(source);
        // Following the vanilla code
        if (!PowerItemHasLocalChanges.Get(source)) return;
        // This disables e.g. the Motion Sensor trigger if needed
        // for (int index = 0; index < source.Children.Count; ++index)
        //     source.Children[index].HandlePowerUpdate(source.IsOn);
        PowerItemHasLocalChanges.Set(source, false);
    }

    public override void RemoveParent(PowerItem node)
    {
        base.RemoveParent(node);
        IsDirty = true;
        // ResetGrids();
    }

    public override void SetParent(PowerItem child, PowerItem parent)
    {
        base.SetParent(child, parent);
        IsDirty = true;
        // ResetGrids();
    }

}