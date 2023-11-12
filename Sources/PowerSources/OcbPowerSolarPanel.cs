using UnityEngine;

public class OcbPowerSolarPanel : PowerSolarPanelBase
{

    // ####################################################################
    // ####################################################################

    // Last calculated light level
    public ushort LightLevel;

    // Wear factor set by Block XML
    public float WearFactor = 1f;

    // Last time wear was calculated
    public float wearUpdateTime;

    // ####################################################################
    // ####################################################################

    public bool IsWindMill => Block.list[BlockID].Properties.Values.ContainsKey("IsWindmill");

    // ####################################################################
    // ####################################################################

    // private static read-only float PerlinTimeFactor = 0.9f / 45f;
    public static readonly float PerlinRotateFactor = 0.00005f;
    public static readonly float PerlinSpeedFactor = 0.01005f;

    // Scale world coordinates to Perlin coordinates
    // Wind should be similar for field close together
    public static readonly float WorldPerlinScale = 0.001f;

    private static float TimeWindFactor(World world, Vector3i position)
    {
        // Sample in y direction
        float speed = Mathf.PerlinNoise(
            position.z * WorldPerlinScale,
            position.x * WorldPerlinScale +
            world.worldTime * PerlinSpeedFactor);
        // Smooth the edges a little bit
        speed = Mathf.SmoothStep(0, 1, speed);
        // Get the height of the regular terrain at position
        var height = world.GetTerrainHeight(position.x, position.z);
        // if y is 10 below world height, factor will be zero
        speed *= Mathf.Lerp(0, 1, Mathf.InverseLerp(height - 10, height, position.y));
        // if y is above 50 up to 125 we gain a bonus up to 25%
        speed *= Mathf.Lerp(1, 1.25f, Mathf.InverseLerp(50, 125, position.y));
        // Make sure to return a minimum and in defined quantums
        return Mathf.Max((int)(speed * 12f) / 12f, 0.05f);
    }

    // ####################################################################
    // ####################################################################

    private static float TimeLightFactor(World world, int x)
    {
        // add small static world position offset to offset big updates
        float time = ((world.worldTime + x * 0.1f) % 24000UL) / 1000f;
        float noon = (world.DawnHour + world.DuskHour) * 0.5f;
        float range = (world.DuskHour - world.DawnHour) * 0.5f;
        float factor = 1 - (range - Mathf.Abs(noon - time)) / range;
        factor = Mathf.Clamp01((1f - factor * factor) * 2f);
        return Mathf.Max((int)(factor * 12f) / 12f, 0.05f);
    }

    // ####################################################################
    // ####################################################################

    public override void CheckLightLevel()
    {

        // Wait a bit longer to update wind/sun
        // We may send changes often over the wire
        // Add a very slight variation to ensure we don't all
        // get called again on the same frame, distribute the
        // load across different frame updates (just to be sure).
       // lightUpdateTime = Time.time + Random.Range(14.5f, 15.5f);

        // Get sun light value from block
        // Determines general sun availability
        if (TileEntity != null)
        {
            Chunk chunk = TileEntity.GetChunk();
            Vector3i localChunkPos = TileEntity.localChunkPos;
            sunLight = chunk.GetLight(
                localChunkPos.x,
                localChunkPos.y,
                localChunkPos.z,
                Chunk.LIGHT_TYPE.SUN);
        }

        if (sunLight > 15) Log.Warning("Sun light is too bright!?");

        this.lastHasLight = this.HasLight;

        // this.HasLight = this.sunLight == (byte)15 && GameManager.Instance.World.IsDaytime();

        // Get dynamic factor for wind (perlin-noise) or sun-light (from chunk light)
        float factor = isWindMill ? TimeWindFactor(GameManager.Instance.World, Position)
            : TimeLightFactor(GameManager.Instance.World, Position.x) * sunLight / 15f;

        // Update the light level for this panel
        LightLevel = (ushort)(factor * ushort.MaxValue);

        // We keep the power generation to a minimum of 1 watts
        // This will make sure we can always switch it off
        ushort power = (ushort)(Mathf.Max(MaxPower * factor, 1f));

        // if (isWindMill) Log.Out("Wind max power of {0} at {1}", power, factor);
        // else Log.Out("Solar max power of {0} at {1} X {2}", power, factor, sunLight);

        // Update power output if required
        if (MaxOutput != power)
        {
            MaxOutput = power;
            TileEntity?.MarkChanged();
            // __instance.SendHasLocalChangesToRoot()
            hasChangesLocal |= true;
        }

    }

    // ####################################################################
    // ####################################################################

    public override void SetValuesFromBlock()
    {
        base.SetValuesFromBlock();
        var props = Block.list[BlockID].Properties;
        if (props.Values.ContainsKey("WearFactor"))
            WearFactor = float.Parse(props.Values["WearFactor"]);
    }

    // ####################################################################
    // ####################################################################

}
