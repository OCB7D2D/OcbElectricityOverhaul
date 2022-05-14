using Mono.Cecil;
using System;
using System.Linq;
using System.Collections.Generic;

public class ElectricityOverhaulPatch
{

    public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll" };

    public static void PatchPowerManager(ModuleDefinition module)
    {
        var type = MakeTypePublic(module.Types.First(d => d.Name == "PowerManager"));
        SetMethodToVirtual(type.Methods.First(d => d.Name == ".ctor"));
        SetMethodToVirtual(type.Methods.First(d => d.Name == "Update"));
        SetMethodToVirtual(type.Methods.First(d => d.Name == "LoadPowerManager"));
        SetMethodToVirtual(type.Methods.First(d => d.Name == "SavePowerManager"));
        SetMethodToVirtual(type.Methods.First(d => d.Name == "RemovePowerNode"));
        SetMethodToVirtual(type.Methods.First(d => d.Name == "AddPowerNode"));
        SetMethodToVirtual(type.Methods.First(d => d.Name == "RemoveParent"));
        SetMethodToVirtual(type.Methods.First(d => d.Name == "SetParent"));
    }

    public static void PatchPowerItem(ModuleDefinition module)
    {
        var type = MakeTypePublic(module.Types.First(d => d.Name == "PowerItem"));
        TypeReference boolTypeRef = module.ImportReference(typeof(bool));
        type.Fields.Add(new FieldDefinition("WasPowered", FieldAttributes.Public, boolTypeRef));
        SetMethodToPublic(type.Methods.First(d => d.Name == "IsPoweredChanged"), true);
    }

    public static void PatchPowerTrigger(ModuleDefinition module)
    {
        var type = MakeTypePublic(module.Types.First(d => d.Name == "PowerTrigger"));
        SetMethodToPublic(type.Methods.First(d => d.Name == "HandleSingleUseDisable"), true);
        SetMethodToPublic(type.Methods.First(d => d.Name == "CheckForActiveChange"), true);
    }

    public static void PatchPowerConsumer(ModuleDefinition module)
    {
        var type = MakeTypePublic(module.Types.First(d => d.Name == "PowerConsumer"));
    }

    public static void PatchPowerSolarPanel(ModuleDefinition module)
    {
        var manager = MakeTypePublic(module.Types.First(d => d.Name == "PowerManager"));
        var type = MakeTypePublic(module.Types.First(d => d.Name == "PowerSolarPanel"));
        TypeReference ushortTypeRef = module.ImportReference(typeof(ushort));
        type.Fields.Add(new FieldDefinition("LightLevel", FieldAttributes.Public, ushortTypeRef));
    }

    public static void PatchPowerSource(ModuleDefinition module)
    {
        var manager = MakeTypePublic(module.Types.First(d => d.Name == "PowerManager"));
        var type = MakeTypePublic(module.Types.First(d => d.Name == "PowerSource"));
        TypeReference ushortTypeRef = module.ImportReference(typeof(ushort));
        TypeReference ulongTypeRef = module.ImportReference(typeof(ulong));
        TypeReference boolTypeRef = module.ImportReference(typeof(bool));
        TypeReference floatTypeRef = module.ImportReference(typeof(float));
        FieldReference fieldPowerSources = manager.Fields.First(d => d.Name == "PowerSources");
		TypeReference powerSourceListTypeRef = module.ImportReference(fieldPowerSources.FieldType);
        FieldReference fieldPowerTriggers = manager.Fields.First(d => d.Name == "PowerTriggers");
		TypeReference powerTriggerListTypeRef = module.ImportReference(fieldPowerTriggers.FieldType);
        type.Fields.Add(new FieldDefinition("StackPower", FieldAttributes.Public, ushortTypeRef));
        type.Fields.Add(new FieldDefinition("MaxProduction", FieldAttributes.Public, ushortTypeRef));
        type.Fields.Add(new FieldDefinition("MaxGridProduction", FieldAttributes.Public, ushortTypeRef));
        type.Fields.Add(new FieldDefinition("LentConsumed", FieldAttributes.Public, ushortTypeRef));
        type.Fields.Add(new FieldDefinition("LentCharging", FieldAttributes.Public, ushortTypeRef));
        type.Fields.Add(new FieldDefinition("ChargingUsed", FieldAttributes.Public, ushortTypeRef));
        type.Fields.Add(new FieldDefinition("ChargingDemand", FieldAttributes.Public, ushortTypeRef));
        type.Fields.Add(new FieldDefinition("ConsumerUsed", FieldAttributes.Public, ushortTypeRef));
        type.Fields.Add(new FieldDefinition("ConsumerDemand", FieldAttributes.Public, ushortTypeRef));
        type.Fields.Add(new FieldDefinition("GridConsumerDemand", FieldAttributes.Public, ushortTypeRef));
        type.Fields.Add(new FieldDefinition("GridChargingDemand", FieldAttributes.Public, ushortTypeRef));
        type.Fields.Add(new FieldDefinition("GridConsumerUsed", FieldAttributes.Public, ushortTypeRef));
        type.Fields.Add(new FieldDefinition("GridChargingUsed", FieldAttributes.Public, ushortTypeRef));
        type.Fields.Add(new FieldDefinition("LentConsumerUsed", FieldAttributes.Public, ushortTypeRef));
        type.Fields.Add(new FieldDefinition("LentChargingUsed", FieldAttributes.Public, ushortTypeRef));
        type.Fields.Add(new FieldDefinition("ChargeFromSolar", FieldAttributes.Public, boolTypeRef));
        type.Fields.Add(new FieldDefinition("ChargeFromGenerator", FieldAttributes.Public, boolTypeRef));
        type.Fields.Add(new FieldDefinition("ChargeFromBattery", FieldAttributes.Public, boolTypeRef));
        type.Fields.Add(new FieldDefinition("PowerSources", FieldAttributes.Public, powerSourceListTypeRef));
        type.Fields.Add(new FieldDefinition("PowerTriggers", FieldAttributes.Public, powerTriggerListTypeRef));
        type.Fields.Add(new FieldDefinition("UpdateTime", FieldAttributes.Public, floatTypeRef));
        type.Fields.Add(new FieldDefinition("AvgTime", FieldAttributes.Public, floatTypeRef));
        type.Fields.Add(new FieldDefinition("LastTick", FieldAttributes.Public, ulongTypeRef));
        SetMethodToPublic(type.Methods.First(d => d.Name == "TickPowerGeneration"), true);
        SetMethodToPublic(type.Methods.First(d => d.Name == "ShouldAutoTurnOff"), true);
    }

    public static void PatchClientPowerData(ModuleDefinition module)
    {
        TypeDefinition type = MakeTypePublic(
            module.Types.First(d => d.Name == "TileEntityPowerSource")
                .NestedTypes.First(d => d.Name == "ClientPowerData"));
        TypeReference ushortTypeRef = module.ImportReference(typeof(ushort));
        TypeReference boolTypeRef = module.ImportReference(typeof(bool));
        type.Fields.Add(new FieldDefinition("MaxProduction", FieldAttributes.Public, ushortTypeRef));
        type.Fields.Add(new FieldDefinition("MaxGridProduction", FieldAttributes.Public, ushortTypeRef));
        type.Fields.Add(new FieldDefinition("LentConsumed", FieldAttributes.Public, ushortTypeRef));
        type.Fields.Add(new FieldDefinition("LentCharging", FieldAttributes.Public, ushortTypeRef));
        type.Fields.Add(new FieldDefinition("ChargingUsed", FieldAttributes.Public, ushortTypeRef));
        type.Fields.Add(new FieldDefinition("ChargingDemand", FieldAttributes.Public, ushortTypeRef));
        type.Fields.Add(new FieldDefinition("ConsumerUsed", FieldAttributes.Public, ushortTypeRef));
        type.Fields.Add(new FieldDefinition("ConsumerDemand", FieldAttributes.Public, ushortTypeRef));
        type.Fields.Add(new FieldDefinition("GridConsumerDemand", FieldAttributes.Public, ushortTypeRef));
        type.Fields.Add(new FieldDefinition("GridChargingDemand", FieldAttributes.Public, ushortTypeRef));
        type.Fields.Add(new FieldDefinition("GridConsumerUsed", FieldAttributes.Public, ushortTypeRef));
        type.Fields.Add(new FieldDefinition("GridChargingUsed", FieldAttributes.Public, ushortTypeRef));
        type.Fields.Add(new FieldDefinition("LentConsumerUsed", FieldAttributes.Public, ushortTypeRef));
        type.Fields.Add(new FieldDefinition("LentChargingUsed", FieldAttributes.Public, ushortTypeRef));
        type.Fields.Add(new FieldDefinition("ChargeFromSolar", FieldAttributes.Public, boolTypeRef));
        type.Fields.Add(new FieldDefinition("ChargeFromGenerator", FieldAttributes.Public, boolTypeRef));
        type.Fields.Add(new FieldDefinition("ChargeFromBattery", FieldAttributes.Public, boolTypeRef));
        type.Fields.Add(new FieldDefinition("LightLevel", FieldAttributes.Public, ushortTypeRef));
    }

    public static void Patch(AssemblyDefinition assembly)
    {
        Console.WriteLine("Applying OCB Electricity Overhaul Patch");

        ModuleDefinition module = assembly.MainModule;

        PatchPowerItem(module);
        PatchPowerTrigger(module);
        PatchPowerConsumer(module);
        PatchPowerSource(module);
        PatchPowerSolarPanel(module);
        PatchPowerManager(module);
        PatchClientPowerData(module);

        MakeTypePublic(module.Types.First(d => d.Name == "PowerConsumerToggle"));
        MakeTypePublic(module.Types.First(d => d.Name == "PowerBatteryBank"));
        MakeTypePublic(module.Types.First(d => d.Name == "PowerSolarPanel"));
        MakeTypePublic(module.Types.First(d => d.Name == "XUiC_PowerSourceStats"));
        MakeTypePublic(module.Types.First(d => d.Name == "TileEntityPowered"));
        //MakeTypePublic(module.Types.First(d => d.Name == "TileEntityPowerSource"));

    }

    // Called after the patching process and after scripts are compiled.
    // Used to link references between both assemblies
    // Return true if successful
    public static bool Link(ModuleDefinition gameModule, ModuleDefinition modModule)
    {
        return true;
    }


    // Helper functions to allow us to access and change variables that are otherwise unavailable.
    private static void SetMethodToVirtual(MethodDefinition method)
    {
        method.IsVirtual = true;
    }

    private static TypeDefinition MakeTypePublic(TypeDefinition type)
    {
        foreach (var myField in type.Fields)
        {
            SetFieldToPublic(myField);
        }
        foreach (var myMethod in type.Methods)
        {
            SetMethodToPublic(myMethod);
        }

        return type;
    }

    private static void SetFieldToPublic(FieldDefinition field)
    {
        field.IsFamily = false;
        field.IsPrivate = false;
        field.IsPublic = true;

    }
    private static void SetMethodToPublic(MethodDefinition field, bool force = false)
    {
        // Leave protected virtual methods alone to avoid
        // issues with others inheriting from it, as it gives
        // a compile error when protection level mismatches.
        // Unsure if this changes anything on runtime though?
        if (!field.IsFamily || !field.IsVirtual || force) {
            field.IsFamily = false;
            field.IsPrivate = false;
            field.IsPublic = true;
        }
    }

}
