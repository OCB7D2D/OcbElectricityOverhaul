using Audio;
using System.IO;
using UnityEngine;

// ####################################################################
// This file is a verbatim copy of the decompiled original class.
// We only changed certain accessor types, e.g. made them public.
// Implement changes/adjustements in the specialized class.
// This should allow us to update vanilla code more easily.
// ####################################################################

public abstract class PowerSolarPanelBase : OcbPowerSource
{

    // ####################################################################
    // ####################################################################

    public ushort InputFromSun;
    protected byte sunLight;
    protected bool lastHasLight;
    protected string runningSound = "solarpanel_idle";
    public float lightUpdateTime;
    public bool isWindMill;

    // ####################################################################
    // ####################################################################

    public override void SetValuesFromBlock()
    {
        base.SetValuesFromBlock();
        var props = Block.list[this.BlockID].Properties;
        isWindMill = props.GetBool("IsWindmill");
    }

    // ####################################################################
    // ####################################################################

    bool hasLight = false;
    public bool HasLight
    {
        get {
            if (isWindMill) return true;
            return hasLight;
        }
        private set {
            hasLight = value;
        }
    }

    // ####################################################################
    // ####################################################################

    public override PowerItem.PowerItemTypes PowerItemType => PowerItem.PowerItemTypes.SolarPanel;

    public override string OnSound => "solarpanel_on";

    public override string OffSound => "solarpanel_off";


    protected override void TickPowerGeneration()
    {
        this.CurrentPower = this.MaxOutput;
    }

    public abstract void CheckLightLevel();

    // ####################################################################
    // ####################################################################

    public override void HandleSendPower()
    {
        if (!this.IsOn)
            return;
        if ((double)Time.time > (double)this.lightUpdateTime)
        {
            this.lightUpdateTime = Time.time + 2f;
            this.CheckLightLevel();
        }
        if (!this.HasLight)
            return;
        if ((int)this.CurrentPower < (int)this.MaxPower)
            this.TickPowerGeneration();
        else if ((int)this.CurrentPower > (int)this.MaxPower)
            this.CurrentPower = this.MaxPower;
        if (this.ShouldAutoTurnOff())
        {
            this.CurrentPower = (ushort)0;
            this.IsOn = false;
        }
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
        if ((int)this.LastPowerUsed >= (int)this.CurrentPower)
        {
            this.SendHasLocalChangesToRoot();
            this.CurrentPower = (ushort)0;
        }
        else
            this.CurrentPower -= (ushort)(uint)this.LastPowerUsed;
    }

    // ####################################################################
    // ####################################################################

    protected bool ShouldClearPower() => this.sunLight != (byte)15 || !GameManager.Instance.World.IsDaytime();

    // ####################################################################
    // ####################################################################

    protected override void HandleOnOffSound()
    {
        Vector3 vector3 = this.Position.ToVector3();
        Manager.BroadcastPlay(vector3, !this.isOn || !this.HasLight ? this.OffSound : this.OnSound);
        if (this.isOn && this.HasLight)
            Manager.BroadcastPlay(vector3, this.runningSound);
        else
            Manager.BroadcastStop(vector3, this.runningSound);
    }

    // ####################################################################
    // ####################################################################

    protected override void RefreshPowerStats()
    {
        base.RefreshPowerStats();
        this.MaxPower = this.MaxOutput;
    }

    // ####################################################################
    // ####################################################################

    public override void read(BinaryReader _br, byte _version)
    {
        base.read(_br, _version);
        if (OcbPowerManager.Instance.
            CurrentFileVersion < 2) return;
        this.sunLight = _br.ReadByte();
    }

    public override void write(BinaryWriter _bw)
    {
        base.write(_bw);
        _bw.Write(this.sunLight);
    }

    // ####################################################################
    // ####################################################################

}
