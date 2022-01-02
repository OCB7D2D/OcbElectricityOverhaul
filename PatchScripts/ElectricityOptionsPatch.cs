using Mono.Cecil;
using System;
using System.Linq;
using System.Collections.Generic;

public class ElectricityOptionsPatch
{

    public static IEnumerable<string> TargetDLLs { get; } = new[] { "Assembly-CSharp.dll" };

    public static void PatchEnumGamePrefs(ModuleDefinition module)
    {

        // Add new field to EnumGamePrefs enum (not sure how `Last` enum plays here)
        var enumType = MakeTypePublic(module.Types.First(d => d.Name == "EnumGamePrefs"));
        int enumLast = enumType.Fields.Count - 2;

        enumType.Fields.Add(new FieldDefinition("LoadVanillaMap", FieldAttributes.Static | FieldAttributes.Literal
                | FieldAttributes.Public | FieldAttributes.HasDefault, enumType)
            { Constant = enumLast++ });
        enumType.Fields.Add(new FieldDefinition("BatteryPowerPerUse", FieldAttributes.Static | FieldAttributes.Literal
                | FieldAttributes.Public | FieldAttributes.HasDefault, enumType)
            { Constant = enumLast++ });
        enumType.Fields.Add(new FieldDefinition("MinPowerForCharging", FieldAttributes.Static | FieldAttributes.Literal
                | FieldAttributes.Public | FieldAttributes.HasDefault, enumType)
            { Constant = enumLast++ });
        enumType.Fields.Add(new FieldDefinition("FuelPowerPerUse", FieldAttributes.Static | FieldAttributes.Literal
                | FieldAttributes.Public | FieldAttributes.HasDefault, enumType)
            { Constant = enumLast++ });
        enumType.Fields.Add(new FieldDefinition("PowerPerPanel", FieldAttributes.Static | FieldAttributes.Literal
                | FieldAttributes.Public | FieldAttributes.HasDefault, enumType)
            { Constant = enumLast++ });
        enumType.Fields.Add(new FieldDefinition("PowerPerEngine", FieldAttributes.Static | FieldAttributes.Literal
                | FieldAttributes.Public | FieldAttributes.HasDefault, enumType)
            { Constant = enumLast++ });
        enumType.Fields.Add(new FieldDefinition("PowerPerBattery", FieldAttributes.Static | FieldAttributes.Literal
                | FieldAttributes.Public | FieldAttributes.HasDefault, enumType)
            { Constant = enumLast++ });
        enumType.Fields.Add(new FieldDefinition("ChargePerBattery", FieldAttributes.Static | FieldAttributes.Literal
                | FieldAttributes.Public | FieldAttributes.HasDefault, enumType)
            { Constant = enumLast++ });

        enumType.Fields.FirstOrDefault(item => item.Name == "Last").Constant = enumLast;

        // Add new fields to GameInfoBool enum
        var infoBoolType = MakeTypePublic(module.Types.First(d => d.Name == "GameInfoBool"));
        int infoBoolLast = infoBoolType.Fields.Count;
        infoBoolType.Fields.Add(new FieldDefinition("LoadVanillaMap", FieldAttributes.Static | FieldAttributes.Literal
                | FieldAttributes.Public | FieldAttributes.HasDefault, infoBoolType)
            { Constant = ++infoBoolLast });

        // Add new fields to GameInfoInt enum
        var infoIntType = MakeTypePublic(module.Types.First(d => d.Name == "GameInfoInt"));
        int infoIntLast = infoIntType.Fields.Count;
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

    public static void Patch(AssemblyDefinition assembly)
    {
        Console.WriteLine("Applying OCB Electricity Options Patch");

        ModuleDefinition module = assembly.MainModule;

        PatchEnumGamePrefs(module);

        MakeTypePublic(module.Types.First(d => d.Name == "GamePrefs"));

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
    private static void SetMethodToPublic(MethodDefinition field)
    {
        field.IsFamily = false;
        field.IsPrivate = false;
        field.IsPublic = true;

    }
}