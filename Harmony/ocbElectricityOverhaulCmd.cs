using OCB;
using System;
using System.Collections.Generic;

class OcbElectricityOverhaulCmd : ConsoleCmdAbstract
{

    private static string info = "ElectricityOverhaul";

    public override string[] GetCommands()
    {
        return new string[2] { info, "eo" };
    }

    public override bool IsExecuteOnClient => false;
    public override bool AllowedInMainMenu => false;

    public override string GetDescription() => "Plant Settings";

    public override string GetHelp() => "Adjust some settings and \n";

    public string GridStats(PowerSource grid)
    {
        int sources = 0;
        int triggers = 0;
        int consumers = 0;
        int others = 0;
        Queue<PowerItem> queue =
            new Queue<PowerItem>();
        queue.Enqueue(grid);
        while (queue.Count > 0)
        {
            PowerItem pw = queue.Dequeue();
            if (pw is PowerSource) sources++;
            else if (pw is PowerTrigger) triggers++;
            else if (pw is PowerConsumer) consumers++;
            else others++;
        }
        return string.Format(
            "sources: {0}, triggers: {1}, consumers: {2}, others: {3}",
            sources, triggers, consumers, others);
    }

    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {

        OcbPowerManager pm = PowerManager.Instance as OcbPowerManager;
        if (pm == null) throw new Exception("Invalid Power Manager type");

        if (_params.Count == 1)
        {
            switch (_params[0])
            {
                case "config":
                    Log.Out("Interval: {0}", pm.Interval);
                    Log.Out("BatteryPowerPerUse: {0}", ElectricityUtils.batteryPowerPerUse);
                    Log.Out("FuelPowerPerUse: {0}", ElectricityUtils.fuelPowerPerUse);
                    Log.Out("PowerPerPanel: {0}", ElectricityUtils.powerPerPanel);
                    Log.Out("PowerPerEngine: {0}", ElectricityUtils.powerPerEngine);
                    Log.Out("PowerPerBattery: {0}", ElectricityUtils.powerPerBattery);
                    Log.Out("ChargePerBattery: {0}", ElectricityUtils.chargePerBattery);
                    Log.Out("MinPowerForCharging: {0}", ElectricityUtils.minPowerForCharging);
                    break;
                case "stats":
                    Log.Out("Reporting statistics of {0} grids", pm.Grids.Count);
                    for (int i = 0; i < pm.Grids.Count; i++)
                        Log.Out("Grid {0}: {1}", i,
                            GridStats(pm.Grids[i]));
                    break;
                default:
                    Log.Warning("Unknown command " + _params[0]);
                    break;
            }
        }

        else if (_params.Count == 2)
        {
            switch (_params[0])
            {
                case "Interval":
                    pm.Interval = float.Parse(_params[1]);
                    Log.Out("Interval: {0}", pm.Interval);
                    break;
                case "BatteryPowerPerUse":
                    ElectricityUtils.batteryPowerPerUse = int.Parse(_params[1]);
                    Log.Out("BatteryPowerPerUse: {0}", ElectricityUtils.batteryPowerPerUse);
                    break;
                case "FuelPowerPerUse":
                    ElectricityUtils.fuelPowerPerUse = int.Parse(_params[1]);
                    Log.Out("FuelPowerPerUse: {0}", ElectricityUtils.fuelPowerPerUse);
                    break;
                case "PowerPerPanel":
                    ElectricityUtils.powerPerPanel = int.Parse(_params[1]);
                    Log.Out("PowerPerPanel: {0}", ElectricityUtils.powerPerPanel);
                    break;
                case "PowerPerEngine":
                    ElectricityUtils.powerPerEngine = int.Parse(_params[1]);
                    Log.Out("PowerPerEngine: {0}", ElectricityUtils.powerPerEngine);
                    break;
                case "PowerPerBattery":
                    ElectricityUtils.powerPerBattery = int.Parse(_params[1]);
                    Log.Out("PowerPerBattery: {0}", ElectricityUtils.powerPerBattery);
                    break;
                case "ChargePerBattery":
                    ElectricityUtils.chargePerBattery = int.Parse(_params[1]);
                    Log.Out("ChargePerBattery: {0}", ElectricityUtils.chargePerBattery);
                    break;
                case "MinPowerForCharging":
                    ElectricityUtils.minPowerForCharging = int.Parse(_params[1]);
                    Log.Out("MinPowerForCharging: {0}", ElectricityUtils.minPowerForCharging);
                    break;
                default:
                    Log.Warning("Unknown command " + _params[0]);
                    break;
            }
        }
    }

}
