using Mono.Cecil;
using SDX.Compiler;
using System;
using System.Linq;

// SDX "compile" time patch to alter dll (EAC incompatible)
public class ElectricityOptionsPatch : IPatcherMod
{

    public void PatchEnumGamePrefs(ModuleDefinition module)
    {

        int enumLast = (int)EnumGamePrefs.Last - 1;

        // Add new field to EnumGamePrefs enum (not sure how `Last` enum plays here)
        var enumType = MakeTypePublic(module.Types.First(d => d.Name == "EnumGamePrefs"));
        enumType.Fields.Add(new FieldDefinition("LoadVanillaMap", FieldAttributes.Static | FieldAttributes.Literal
                | FieldAttributes.Public | FieldAttributes.HasDefault, enumType)
            { Constant = ++enumLast });
        enumType.Fields.Add(new FieldDefinition("BatteryPowerPerUse", FieldAttributes.Static | FieldAttributes.Literal
                | FieldAttributes.Public | FieldAttributes.HasDefault, enumType)
            { Constant = ++enumLast });
        enumType.Fields.Add(new FieldDefinition("MinPowerForCharging", FieldAttributes.Static | FieldAttributes.Literal
                | FieldAttributes.Public | FieldAttributes.HasDefault, enumType)
            { Constant = ++enumLast });
        enumType.Fields.Add(new FieldDefinition("FuelPowerPerUse", FieldAttributes.Static | FieldAttributes.Literal
                | FieldAttributes.Public | FieldAttributes.HasDefault, enumType)
            { Constant = ++enumLast });
        enumType.Fields.Add(new FieldDefinition("PowerPerPanel", FieldAttributes.Static | FieldAttributes.Literal
                | FieldAttributes.Public | FieldAttributes.HasDefault, enumType)
            { Constant = ++enumLast });
        enumType.Fields.Add(new FieldDefinition("PowerPerEngine", FieldAttributes.Static | FieldAttributes.Literal
                | FieldAttributes.Public | FieldAttributes.HasDefault, enumType)
            { Constant = ++enumLast });
        enumType.Fields.Add(new FieldDefinition("PowerPerBattery", FieldAttributes.Static | FieldAttributes.Literal
                | FieldAttributes.Public | FieldAttributes.HasDefault, enumType)
            { Constant = ++enumLast });
        enumType.Fields.Add(new FieldDefinition("ChargePerBattery", FieldAttributes.Static | FieldAttributes.Literal
                | FieldAttributes.Public | FieldAttributes.HasDefault, enumType)
            { Constant = ++enumLast });

        int infoBoolLast = (int)GameInfoBool.TwitchBloodMoonAllowed;

        // Add new fields to GameInfoBool enum
        var infoBoolType = MakeTypePublic(module.Types.First(d => d.Name == "GameInfoBool"));
        infoBoolType.Fields.Add(new FieldDefinition("LoadVanillaMap", FieldAttributes.Static | FieldAttributes.Literal
                | FieldAttributes.Public | FieldAttributes.HasDefault, infoBoolType)
            { Constant = ++infoBoolLast });

        int infoIntLast = (int)GameInfoInt.BedrollExpiryTime;

        // Add new fields to GameInfoInt enum
        var infoIntType = MakeTypePublic(module.Types.First(d => d.Name == "GameInfoInt"));
        infoIntType.Fields.Add(new FieldDefinition("BatteryPowerPerUse", FieldAttributes.Static | FieldAttributes.Literal
                | FieldAttributes.Public | FieldAttributes.HasDefault, infoIntType)
            { Constant = ++infoIntLast });
        infoIntType.Fields.Add(new FieldDefinition("MinPowerForCharging", FieldAttributes.Static | FieldAttributes.Literal
                | FieldAttributes.Public | FieldAttributes.HasDefault, infoIntType)
            { Constant = ++infoIntLast });
        infoIntType.Fields.Add(new FieldDefinition("FuelPowerPerUse", FieldAttributes.Static | FieldAttributes.Literal
                | FieldAttributes.Public | FieldAttributes.HasDefault, infoIntType)
            { Constant = ++infoIntLast });
        infoIntType.Fields.Add(new FieldDefinition("PowerPerPanel", FieldAttributes.Static | FieldAttributes.Literal
                | FieldAttributes.Public | FieldAttributes.HasDefault, infoIntType)
            { Constant = ++infoIntLast });
        infoIntType.Fields.Add(new FieldDefinition("PowerPerEngine", FieldAttributes.Static | FieldAttributes.Literal
                | FieldAttributes.Public | FieldAttributes.HasDefault, infoIntType)
            { Constant = ++infoIntLast });
        infoIntType.Fields.Add(new FieldDefinition("PowerPerBattery", FieldAttributes.Static | FieldAttributes.Literal
                | FieldAttributes.Public | FieldAttributes.HasDefault, infoIntType)
            { Constant = ++infoIntLast });
        infoIntType.Fields.Add(new FieldDefinition("ChargePerBattery", FieldAttributes.Static | FieldAttributes.Literal
                | FieldAttributes.Public | FieldAttributes.HasDefault, infoIntType)
            { Constant = ++infoIntLast });
    }

    public bool Patch(ModuleDefinition module)
    {
        Console.WriteLine("Applying OCB Electricity Options Patch");

        PatchEnumGamePrefs(module);

        MakeTypePublic(module.Types.First(d => d.Name == "GamePrefs"));

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