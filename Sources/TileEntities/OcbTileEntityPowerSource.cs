public class OcbTileEntityPowerSource : TileEntityPowerSource
{

    // ####################################################################
    // ####################################################################

    public class OcbClientPowerData : ClientPowerData
    {
        public ushort MaxProduction;
        public ushort MaxGridProduction;
        public ushort LentConsumed;
        public ushort LentCharging;
        public ushort ChargingUsed;
        public ushort ChargingDemand;
        public ushort ConsumerUsed;
        public ushort ConsumerDemand;
        public ushort GridConsumerDemand;
        public ushort GridChargingDemand;
        public ushort GridConsumerUsed;
        public ushort GridChargingUsed;
        public ushort LentConsumerUsed;
        public ushort LentChargingUsed;
        public bool ChargeFromSolar;
        public bool ChargeFromGenerator;
        public bool ChargeFromBattery;
        public ushort LightLevel;
    }

    // ####################################################################
    // ####################################################################

    public OcbTileEntityPowerSource(Chunk _chunk) : base(_chunk) {}

    // ####################################################################
    // ####################################################################

    public new OcbClientPowerData ClientData => base.ClientData as OcbClientPowerData;

    // ####################################################################
    // ####################################################################

    private OcbTileEntityPowerSource(OcbTileEntityPowerSource _other, Chunk _chunk) : base(_chunk)
    {
        SetOwner(_other.GetOwner());
        PowerItem = _other.PowerItem;
    }

    // ####################################################################
    // ####################################################################

    public override TileEntity Clone() => new OcbTileEntityPowerSource(this, chunk);

    // ####################################################################
    // ####################################################################

    public override bool CanHaveParent(IPowered powered)
    {
        return PowerItemType == PowerItem.PowerItemTypes.BatteryBank ||
               PowerItemType == PowerItem.PowerItemTypes.SolarPanel ||
               PowerItemType == PowerItem.PowerItemTypes.Generator;
    }

    // ####################################################################
    // ####################################################################

    public override void write(PooledBinaryWriter _bw, StreamModeWrite _eStreamMode)
    {
        base.write(_bw, _eStreamMode);
        // Remove checks once everything works
        if (!(base.ClientData is OcbClientPowerData))
            Log.Error("Not of type ClientPowerDataOcb");
        // We assume this is valid if everything is ok
        var data = base.ClientData as OcbClientPowerData;
        switch (_eStreamMode)
        {
            case StreamModeWrite.Persistency:
                break;
            case StreamModeWrite.ToServer:
                _bw.Write(data.ChargeFromSolar);
                _bw.Write(data.ChargeFromGenerator);
                _bw.Write(data.ChargeFromBattery);
                break;
            default: // ToClient
                if (!(base.PowerItem is OcbPowerSource))
                    Log.Error("Not of type PowerSourceOcb");
                var source = base.PowerItem as OcbPowerSource;
                _bw.Write(source != null);
                if (source == null) break;
                // ToDo: check if we need em all (now 180 bytes)
                _bw.Write(source.MaxProduction);
                _bw.Write(source.MaxGridProduction);
                _bw.Write(source.ChargingUsed);
                _bw.Write(source.ChargingDemand);
                _bw.Write(source.ConsumerUsed);
                _bw.Write(source.ConsumerDemand);
                _bw.Write(source.LentConsumed);
                _bw.Write(source.LentCharging);
                _bw.Write(source.GridConsumerDemand);
                _bw.Write(source.GridChargingDemand);
                _bw.Write(source.GridConsumerUsed);
                _bw.Write(source.GridChargingUsed);
                _bw.Write(source.LentConsumerUsed);
                _bw.Write(source.LentChargingUsed);
                _bw.Write(source.ChargeFromSolar);
                _bw.Write(source.ChargeFromGenerator);
                _bw.Write(source.ChargeFromBattery);
                if (source is OcbPowerSolarPanel panel)
                    _bw.Write(panel.LightLevel);
                else _bw.Write((ushort)0);
                break;
        }

    }

    // ####################################################################
    // ####################################################################

    public override void read(PooledBinaryReader _br, StreamModeRead _eStreamMode)
    {
        base.read(_br, _eStreamMode);
        // Remove checks once everything works
        if (!(base.ClientData is OcbClientPowerData))
            Log.Error("Not of type ClientPowerDataOcb");
        // We assume this is valid if everything is ok
        var data = base.ClientData as OcbClientPowerData;
        switch (_eStreamMode)
        {
            case StreamModeRead.Persistency:
                break;
            case StreamModeRead.FromClient:
                data.ChargeFromSolar = _br.ReadBoolean();
                data.ChargeFromGenerator = _br.ReadBoolean();
                data.ChargeFromBattery = _br.ReadBoolean();
                if (!(base.PowerItem is OcbPowerSource))
                    Log.Error("Not of type PowerSourceOcb");
                var source = base.PowerItem as OcbPowerSource;
                source.ChargeFromSolar = data.ChargeFromSolar;
                source.ChargeFromGenerator = data.ChargeFromGenerator;
                source.ChargeFromBattery = data.ChargeFromBattery;
                break;
            default: // FromServer
                if (!_br.ReadBoolean()) break;
                // ToDo: check if we need em all (now 180 bytes)
                data.MaxProduction = _br.ReadUInt16();
                data.MaxGridProduction = _br.ReadUInt16();
                data.ChargingUsed = _br.ReadUInt16();
                data.ChargingDemand = _br.ReadUInt16();
                data.ConsumerUsed = _br.ReadUInt16();
                data.ConsumerDemand = _br.ReadUInt16();
                data.LentConsumed = _br.ReadUInt16();
                data.LentCharging = _br.ReadUInt16();
                data.GridConsumerDemand = _br.ReadUInt16();
                data.GridChargingDemand = _br.ReadUInt16();
                data.GridConsumerUsed = _br.ReadUInt16();
                data.GridChargingUsed = _br.ReadUInt16();
                data.LentConsumerUsed = _br.ReadUInt16();
                data.LentChargingUsed = _br.ReadUInt16();
                data.ChargeFromSolar = _br.ReadBoolean();
                data.ChargeFromGenerator = _br.ReadBoolean();
                data.ChargeFromBattery = _br.ReadBoolean();
                data.LightLevel = _br.ReadUInt16();
                break;
        }
    }

    // ####################################################################
    // ####################################################################

}

