public class OcbPowerBatteryBank : PowerBatteryBankBase
{
    protected override void TickPowerGeneration()
    {
        OCB.ElectricityUtils.TickBatteryBankPowerGeneration(this);
    }

    public override void AddPowerToBatteries(int power)
    {
        OCB.ElectricityUtils.AddPowerToBatteries(this, power);
    }

}
