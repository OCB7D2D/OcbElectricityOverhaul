using System.Reflection;
using DMT;
using HarmonyLib;
using UnityEngine;

public class XUiC_OcbPowerSourceWindowGroup : XUiC_PowerSourceWindowGroup
{
    private XUiC_OcbPowerSourceStats PowerSourceStats;

    public override void Init()
    {
        base.Init();
        XUiController childByType1 = (XUiController) this.GetChildByType<XUiC_OcbPowerSourceStats>();
        if (childByType1 != null)
        {
            this.PowerSourceStats = (XUiC_OcbPowerSourceStats) childByType1;
            this.PowerSourceStats.Owner = this;
        }
    }

}
