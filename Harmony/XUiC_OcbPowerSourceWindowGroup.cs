public class XUiC_OcbPowerSourceWindowGroup : XUiC_PowerSourceWindowGroup
{
    private XUiC_OcbPowerSourceStats PowerSourceStats = null;

    private XUiC_WindowNonPagingHeader header = null;

    public override void Init()
    {
        base.Init();
        if (GetChildByType<XUiC_OcbPowerSourceStats>() is XUiC_OcbPowerSourceStats stats)
        {
            PowerSourceStats = stats;
            PowerSourceStats.Owner = this;
            header = GetChildByType<XUiC_WindowNonPagingHeader>();
        }
    }

    public override void OnOpen()
    {
        base.OnOpen();
        if (header == null) return;
        switch (TileEntity.PowerItemType)
        {
            case PowerItem.PowerItemTypes.SolarPanel:
                // player.PlayerJournal.AddJournalEntry("windMillTip");
                DynamicProperties props = TileEntity?.blockValue.Block?.Properties;
                if (props != null && props.Contains("IsWindmill"))
                    header.SetHeader(Localization.Get("windmill"));
                break;
        }
    }
}
