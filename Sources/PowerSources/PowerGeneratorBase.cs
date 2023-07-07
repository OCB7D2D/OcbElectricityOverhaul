using System.IO;

// ####################################################################
// This file is a verbatim copy of the decompiled original class.
// We only changed certain accessor types, e.g. made them public.
// Implement changes/adjustements in the specialized class.
// This should allow us to update vanilla code more easily.
// ####################################################################

public class PowerGeneratorBase : OcbPowerSource
{

    public ushort CurrentFuel;
    public ushort MaxFuel;
    public float OutputPerFuel;

    public PowerGeneratorBase() : base()
    {
    }

    public override PowerItem.PowerItemTypes PowerItemType => PowerItem.PowerItemTypes.Generator;

    public override string OnSound => "generator_start";

    public override string OffSound => "generator_stop";

    public override void read(BinaryReader _br, byte _version)
    {
        base.read(_br, _version);
        this.CurrentFuel = _br.ReadUInt16();
    }

    public override void write(BinaryWriter _bw)
    {
        base.write(_bw);
        _bw.Write(this.CurrentFuel);
    }

    protected override bool ShouldAutoTurnOff() => this.CurrentFuel <= (ushort)0;

    protected override void TickPowerGeneration()
    {
        if ((double)((int)this.MaxPower - (int)this.CurrentPower) < (double)this.OutputPerFuel || this.CurrentFuel <= (ushort)0)
            return;
        --this.CurrentFuel;
        this.CurrentPower += (ushort)(uint)(ushort)this.OutputPerFuel;
    }

    public override void SetValuesFromBlock()
    {
        base.SetValuesFromBlock();
        Block block = Block.list[(int)this.BlockID];
        if (block.Properties.Values.ContainsKey("MaxPower"))
            this.MaxPower = ushort.Parse(block.Properties.Values["MaxPower"]);
        this.MaxFuel = !block.Properties.Values.ContainsKey("MaxFuel") ? (ushort)1000 : ushort.Parse(block.Properties.Values["MaxFuel"]);
        if (block.Properties.Values.ContainsKey("OutputPerFuel"))
            this.OutputPerFuel = StringParsers.ParseFloat(block.Properties.Values["OutputPerFuel"]);
        else
            this.OutputPerFuel = 100f;
    }
}
