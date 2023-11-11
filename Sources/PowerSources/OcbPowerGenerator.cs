using static OCB.ElectricityUtils;

public class OcbPowerGenerator : PowerGeneratorBase
{

    // ####################################################################
    // ####################################################################

    protected override void TickPowerGeneration()
    {
        TickBatteryDieselPowerGeneration(this);
    }

    // ####################################################################
    // ####################################################################

}
