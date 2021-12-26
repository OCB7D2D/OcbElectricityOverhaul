public class XUiC_OcbPowerSourceWindowGroup : XUiC_PowerSourceWindowGroup
{
    private XUiC_OcbPowerSourceStats PowerSourceStats;

    public override void Init()
    {
        base.Init();
        if (GetChildByType<XUiC_OcbPowerSourceStats>() is XUiC_OcbPowerSourceStats stats)
        {
            PowerSourceStats = stats;
            PowerSourceStats.Owner = this;
        }
    }

}
