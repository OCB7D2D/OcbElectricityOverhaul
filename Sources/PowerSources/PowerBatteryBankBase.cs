using UnityEngine;

// ####################################################################
// This file is a verbatim copy of the decompiled original class.
// We only changed certain accessor types, e.g. made them public.
// Implement changes/adjustements in the specialized class.
// This should allow us to update vanilla code more easily.
// ####################################################################

public class PowerBatteryBankBase : OcbPowerSource
{

    // ####################################################################
    // ####################################################################

    public ushort LastInputAmount;
    public ushort LastPowerReceived;
    public ushort InputPerTick;
    public ushort ChargePerInput;
    public ushort OutputPerCharge;

    // ####################################################################
    // ####################################################################

    public override PowerItem.PowerItemTypes PowerItemType => PowerItem.PowerItemTypes.BatteryBank;

    public override string OnSound => "batterybank_start";

    public override string OffSound => "batterybank_stop";

    public override bool CanParent(PowerItem parent) => true;

    public override bool IsPowered => this.isOn || this.isPowered;

    // ####################################################################
    // ####################################################################

    public override bool PowerChildren() => true;

    // ####################################################################
    // ####################################################################

    protected bool ParentPowering
    {
        get
        {
            if (this.Parent == null)
                return false;
            if (this.Parent is PowerSolarPanel)
            {
                PowerSolarPanel parent = this.Parent as PowerSolarPanel;
                return parent.HasLight && parent.IsOn;
            }
            if (this.Parent is PowerSource)
                return (this.Parent as PowerSource).IsOn;
            if (!(this.Parent is PowerTrigger))
                return this.Parent.IsPowered;
            return this.Parent.IsPowered && (this.Parent as PowerTrigger).IsActive;
        }
    }

    // ####################################################################
    // ####################################################################

    public override void Update()
    {
        if (this.Parent != null && this.LastPowerReceived > (ushort)0)
        {
            if (this.LastInputAmount <= (ushort)0 || !this.IsOn)
                return;
            this.AddPowerToBatteries((int)this.LastInputAmount);
        }
        else
            base.Update();
    }

    // ####################################################################
    // ####################################################################

    public override void HandleSendPower()
    {
        if (!this.IsOn || this.ParentPowering)
            return;
        if ((int)this.CurrentPower < (int)this.MaxPower)
            this.TickPowerGeneration();
        else if ((int)this.CurrentPower > (int)this.MaxPower)
            this.CurrentPower = this.MaxPower;
        if (this.CurrentPower <= (ushort)0)
        {
            this.CurrentPower = (ushort)0;
            if (this.isPowered)
            {
                this.HandleDisconnect();
                this.hasChangesLocal = true;
            }
        }
        else
            this.isPowered = true;
        if (this.hasChangesLocal)
        {
            this.LastPowerUsed = (ushort)0;
            ushort power = (ushort)Mathf.Min((int)this.MaxOutput, (int)this.CurrentPower);
            World world = GameManager.Instance.World;
            for (int index = 0; index < this.Children.Count; ++index)
            {
                ushort num = power;
                this.Children[index].HandlePowerReceived(ref power);
                this.LastPowerUsed += (ushort)(uint)(ushort)((uint)num - (uint)power);
            }
        }
        this.CurrentPower -= (ushort)(uint)(ushort)Mathf.Min((int)this.CurrentPower, (int)this.LastPowerUsed);
    }

    // ####################################################################
    // ####################################################################

    public override void HandlePowerReceived(ref ushort power)
    {
        this.LastPowerUsed = (ushort)0;
        if ((int)this.LastPowerReceived != (int)power)
        {
            this.LastPowerReceived = power;
            this.hasChangesLocal = true;
            for (int index = 0; index < this.Children.Count; ++index)
                this.Children[index].HandleDisconnect();
        }
        if (power <= (ushort)0)
            return;
        if (this.IsOn && power > (ushort)0)
        {
            this.AddPowerToBatteries((int)(ushort)Mathf.Min((int)this.InputPerTick, (int)power));
            power -= this.LastInputAmount;
        }
        if (!this.PowerChildren())
            return;
        for (int index = 0; index < this.Children.Count; ++index)
        {
            this.Children[index].HandlePowerReceived(ref power);
            if (power <= (ushort)0)
                break;
        }
    }

    // ####################################################################
    // ####################################################################

    public virtual void AddPowerToBatteries(int power)
    {
        int num1 = power;
        int b = power / (int)this.InputPerTick * (int)this.ChargePerInput;
        for (int index = this.Stacks.Length - 1; index >= 0; --index)
        {
            if (!this.Stacks[index].IsEmpty())
            {
                int useTimes = (int)this.Stacks[index].itemValue.UseTimes;
                if (useTimes > 0)
                {
                    ushort num2 = (ushort)Mathf.Min(useTimes, b);
                    num1 -= (int)num2 * (int)this.InputPerTick;
                    this.Stacks[index].itemValue.UseTimes -= (float)num2;
                }
                if (num1 == 0)
                    break;
            }
        }
        int num3 = power - num1;
        if ((int)this.LastInputAmount == (int)(ushort)num3)
            return;
        this.SendHasLocalChangesToRoot();
        this.LastInputAmount = (ushort)num3;
    }

    // ####################################################################
    // ####################################################################

    public override void TickPowerGeneration()
    {
        base.TickPowerGeneration();
        int num1 = (int)(ushort)((uint)this.MaxPower - (uint)this.CurrentPower);
        ushort b = (ushort)((uint)num1 / (uint)this.OutputPerCharge);
        if (num1 < (int)this.OutputPerCharge)
            return;
        for (int index = 0; index < this.Stacks.Length; ++index)
        {
            int num2 = (int)Mathf.Min((float)this.Stacks[index].itemValue.MaxUseTimes - this.Stacks[index].itemValue.UseTimes, (float)b);
            if (num2 > 0)
            {
                this.Stacks[index].itemValue.UseTimes += (float)num2;
                this.CurrentPower += (ushort)(uint)(ushort)((uint)num2 * (uint)this.OutputPerCharge);
                break;
            }
        }
    }

    // ####################################################################
    // ####################################################################

    public override void HandlePowerUpdate(bool isOn)
    {
        if (this.Parent == null || this.LastPowerReceived <= (ushort)0 || !this.PowerChildren())
            return;
        for (int index = 0; index < this.Children.Count; ++index)
            this.Children[index].HandlePowerUpdate(isOn);
    }

    // ####################################################################
    // ####################################################################

    public override void HandleDisconnect()
    {
        if (this.isPowered)
            this.IsPoweredChanged(false);
        this.isPowered = false;
        this.HandlePowerUpdate(false);
        for (int index = 0; index < this.Children.Count; ++index)
            this.Children[index].HandleDisconnect();
        this.LastInputAmount = (ushort)0;
        this.LastPowerReceived = (ushort)0;
        if (this.TileEntity == null)
            return;
        this.TileEntity.SetModified();
    }

    // ####################################################################
    // ####################################################################

    public override void SetValuesFromBlock()
    {
        base.SetValuesFromBlock();
        Block block = Block.list[(int)this.BlockID];
        if (block.Properties.Values.ContainsKey("InputPerTick"))
            this.InputPerTick = ushort.Parse(block.Properties.Values["InputPerTick"]);
        if (block.Properties.Values.ContainsKey("ChargePerInput"))
            this.ChargePerInput = ushort.Parse(block.Properties.Values["ChargePerInput"]);
        if (block.Properties.Values.ContainsKey("OutputPerCharge"))
            this.OutputPerCharge = ushort.Parse(block.Properties.Values["OutputPerCharge"]);
        if (!block.Properties.Values.ContainsKey("MaxPower"))
            return;
        this.MaxPower = ushort.Parse(block.Properties.Values["MaxPower"]);
    }

    // ####################################################################
    // ####################################################################

}
