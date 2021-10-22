using Mono.Cecil;
using SDX.Compiler;
using System;
using System.Linq;

// SDX "compile" time patch to alter dll (EAC incompatible)
public class ElectricityOverhaulPatch : IPatcherMod
{

    public void PatchPowerManager(ModuleDefinition module)
    {
        var type = MakeTypePublic(module.Types.First(d => d.Name == "PowerManager"));
        SetMethodToVirtual(type.Methods.First(d => d.Name == ".ctor"));
        SetMethodToVirtual(type.Methods.First(d => d.Name == "Update"));
        SetMethodToVirtual(type.Methods.First(d => d.Name == "LoadPowerManager"));
        SetMethodToVirtual(type.Methods.First(d => d.Name == "SavePowerManager"));
    }

    public void PatchPowerSource(ModuleDefinition module)
    {
        var type = MakeTypePublic(module.Types.First(d => d.Name == "PowerSource"));
        TypeReference ushortTypeRef = module.ImportReference(typeof(ushort));
        type.Fields.Add(new FieldDefinition("MaxProduction", FieldAttributes.Public, ushortTypeRef));
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
    }

    public void PatchClientPowerData(ModuleDefinition module)
    {
        TypeDefinition type = MakeTypePublic(
            module.Types.First(d => d.Name == "TileEntityPowerSource")
                .NestedTypes.First(d => d.Name == "ClientPowerData"));
        TypeReference ushortTypeRef = module.ImportReference(typeof(ushort));
        type.Fields.Add(new FieldDefinition("MaxProduction", FieldAttributes.Public, ushortTypeRef));
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
    }

    public bool Patch(ModuleDefinition module)
    {
        Console.WriteLine("Applying OCB Electricity Overhaul Patch");

        PatchPowerManager(module);
        PatchPowerSource(module);
        PatchClientPowerData(module);

        MakeTypePublic(module.Types.First(d => d.Name == "PowerItem"));
        MakeTypePublic(module.Types.First(d => d.Name == "PowerTrigger"));
        //MakeTypePublic(module.Types.First(d => d.Name == "PowerTimerRelay"));
        MakeTypePublic(module.Types.First(d => d.Name == "PowerConsumer"));
        MakeTypePublic(module.Types.First(d => d.Name == "PowerConsumerToggle"));
        MakeTypePublic(module.Types.First(d => d.Name == "PowerBatteryBank"));
        MakeTypePublic(module.Types.First(d => d.Name == "PowerSolarPanel"));
        MakeTypePublic(module.Types.First(d => d.Name == "XUiC_PowerSourceStats"));
        MakeTypePublic(module.Types.First(d => d.Name == "TileEntityPowered"));
        //MakeTypePublic(module.Types.First(d => d.Name == "TileEntityPowerSource"));

        return true;
    }

    // Called after the patching process and after scripts are compiled.
    // Used to link references between both assemblies
    // Return true if successful
    public bool Link(ModuleDefinition gameModule, ModuleDefinition modModule)
    {
        return true;
    }


    // Helper functions to allow us to access and change variables that are otherwise unavailable.
    private void SetMethodToVirtual(MethodDefinition method)
    {
        method.IsVirtual = true;
    }

    private TypeDefinition MakeTypePublic(TypeDefinition type)
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

    private void SetFieldToPublic(FieldDefinition field)
    {
        field.IsFamily = false;
        field.IsPrivate = false;
        field.IsPublic = true;

    }
    private void SetMethodToPublic(MethodDefinition field)
    {
        field.IsFamily = false;
        field.IsPrivate = false;
        field.IsPublic = true;

    }
}