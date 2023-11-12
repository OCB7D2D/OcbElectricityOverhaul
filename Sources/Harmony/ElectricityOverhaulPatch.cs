using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

public class ElectricityOverhaulPatch : IModApi
{

    // ####################################################################
    // ####################################################################

    public void InitMod(Mod mod)
    {
        Log.Out("OCB Harmony Patch: " + GetType().ToString());
        Harmony harmony = new Harmony(GetType().ToString());
        harmony.PatchAll(Assembly.GetExecutingAssembly());
        // Extend static data for new custom game prefs
        CustomGamePrefsPatch.InitCustomGamePrefs();
    }

    // ####################################################################
    // Below is a very generic and powerfull patching function that will
    // replace anything referencing old power classes with new ones.
    // ####################################################################

    static readonly Tuple<Type, Type>[] Conversions = new Tuple<Type, Type>[]
    {
            new Tuple<Type, Type>(typeof(PowerManager), typeof(OcbPowerManager)),
            new Tuple<Type, Type>(typeof(PowerSource), typeof(OcbPowerSource)),
            new Tuple<Type, Type>(typeof(PowerGenerator), typeof(OcbPowerGenerator)),
            new Tuple<Type, Type>(typeof(PowerSolarPanel), typeof(OcbPowerSolarPanel)),
            new Tuple<Type, Type>(typeof(PowerBatteryBank), typeof(OcbPowerBatteryBank)),
            new Tuple<Type, Type>(typeof(TileEntityPowerSource), typeof(OcbTileEntityPowerSource)),
            new Tuple<Type, Type>(typeof(TileEntityPowerSource.ClientPowerData), typeof(OcbTileEntityPowerSource.OcbClientPowerData)),
    };

    [HarmonyPatch]
    class PatchPowerManager
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(GameManager), "gmUpdate");
            yield return AccessTools.Method(typeof(GameManager), "SaveAndCleanupWorld");
            yield return AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(GameManager), "StartAsServer"));
            yield return AccessTools.Method(typeof(BlockLauncher), "updateState");
            yield return AccessTools.Method(typeof(BlockLauncher), "OnBlockLoaded");
            yield return AccessTools.Method(typeof(BlockRanged), "updateState");
            yield return AccessTools.Method(typeof(BlockRanged), "OnBlockLoaded");
            yield return AccessTools.Method(typeof(BlockPowerSource), "OnBlockRemoved");
            yield return AccessTools.Method(typeof(BlockPowerSource), "OnBlockValueChanged");
            yield return AccessTools.Method(typeof(BlockPowered), "OnBlockRemoved");
            yield return AccessTools.Method(typeof(NetPackageWireActions), "ProcessPackage");
            yield return AccessTools.Method(typeof(PowerItem), "read");
            // yield return AccessTools.Method(typeof(PowerItem), "write");
            yield return AccessTools.Method(typeof(PowerItem), "CreateItem");
            yield return AccessTools.Method(typeof(PowerItem), "RemoveSelfFromParent");
            yield return AccessTools.Method(typeof(PowerItem), "ClearChildren");
            yield return AccessTools.Method(typeof(PowerSolarPanel), "read");
            yield return AccessTools.Method(typeof(PowerSolarPanel), "write");
            yield return AccessTools.Method(typeof(TileEntityPowered), "CheckForNewWires");
            yield return AccessTools.Method(typeof(TileEntityPowered), "InitializePowerData");
            yield return AccessTools.Method(typeof(TileEntityPowered), "CreatePowerItemForTileEntity");
            yield return AccessTools.Method(typeof(TileEntityPowered), "SetParentWithWireTool");
            yield return AccessTools.Method(typeof(TileEntityPowerSource), "OnDestroy");
            yield return AccessTools.Method(typeof(TileEntityPoweredBlock), "OnRemove");
            yield return AccessTools.Method(typeof(TileEntityPoweredBlock), "OnUnload");
            yield return AccessTools.Constructor(typeof(TileEntityPowerSource), new Type[] { typeof(Chunk) });
            yield return AccessTools.Method(typeof(TileEntityPowerSource), "read");
            yield return AccessTools.Method(typeof(TileEntityPowerSource), "write");
            yield return AccessTools.PropertyGetter(typeof(TileEntityPowerSource), "CurrentFuel");
            yield return AccessTools.PropertyGetter(typeof(TileEntityPowerSource), "MaxFuel");
            yield return AccessTools.Method(typeof(BlockBatteryBank), "CreateTileEntity");
            yield return AccessTools.Method(typeof(BlockGenerator), "CreateTileEntity");
            yield return AccessTools.Method(typeof(BlockSolarPanel), "CreateTileEntity");
            yield return AccessTools.Method(typeof(TileEntity), "Instantiate");
            yield return AccessTools.Method(typeof(XUiC_PowerSourceStats), "BtnRefuel_OnPress");
        }

        static IEnumerable<CodeInstruction> Transpiler(
            IEnumerable<CodeInstruction> instructions,
            ILGenerator generator, MethodBase method)
        {
            var codes = new List<CodeInstruction>(instructions);
            bool done = false;
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].operand is Type type)
                {
                    foreach (var convert in Conversions)
                    {
                        if (type == convert.Item1)
                        {
                            codes[i].operand = convert.Item2;
                            done = true;
                        }
                    }
                }
                else if (codes[i].operand is ConstructorInfo ctor)
                {
                    foreach (var convert in Conversions)
                    {
                        if (ctor.DeclaringType == convert.Item1)
                        {
                            codes[i].operand = AccessTools.Constructor(convert.Item2,
                                Array.ConvertAll(ctor.GetParameters(), x => x.ParameterType));
                            done = true;
                        }
                    }
                }
                else if (codes[i].operand is MethodInfo fn)
                {
                    foreach (var convert in Conversions)
                    {
                        if (fn.DeclaringType == convert.Item1)
                        {
                            codes[i].operand = fn = AccessTools.Method(convert.Item2, fn.Name,
                                Array.ConvertAll(fn.GetParameters(), x => x.ParameterType));
                            if (codes[i].opcode == OpCodes.Callvirt && !fn.IsVirtual)
                                codes[i].opcode = OpCodes.Call;
                            else if (codes[i].opcode == OpCodes.Call && fn.IsVirtual)
                                codes[i].opcode = OpCodes.Callvirt;
                            done = true;
                        }
                    }
                }
                else if (codes[i].operand is FieldInfo field)
                {
                    foreach (var convert in Conversions)
                    {
                        if (field.DeclaringType == convert.Item1)
                        {
                            codes[i].operand = AccessTools.Field(convert.Item2, field.Name);
                        }
                    }
                }
            }
            // Give a debug message for now if a function does not require patching
            if (!done) Log.Warning("DID NOT PATCH {1} {0}", method.DeclaringType, method.Name);
            // Return the result
            return codes;
        }

    }

    // ####################################################################
    // ####################################################################

    [HarmonyPatch(typeof(PowerManager), MethodType.Constructor)]
    class PowerManagerPatchToDenyIt
    {
        static void Postfix(PowerManager __instance)
        {
            Log.Error("Something is trying to create old power manager!");
            Log.Error("This means there is a fatal error bound to happen.");
            Log.Error("Please report this with a full log to the developers.");
            throw new Exception("Suspicious old PowerManager instantiation");
        }
    }

    // ####################################################################
    // ####################################################################

}