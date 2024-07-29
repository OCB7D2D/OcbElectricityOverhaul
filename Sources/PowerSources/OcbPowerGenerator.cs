using static OCB.ElectricityUtils;

public class OcbPowerGenerator : PowerGeneratorBase
{

    // ####################################################################
    // ####################################################################

    public override void TickPowerGeneration()
    {
        TickBatteryDieselPowerGeneration(this);
    }

    // ####################################################################
    // ####################################################################

}
