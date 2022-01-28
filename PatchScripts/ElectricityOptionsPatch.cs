using Mono.Cecil;
using Mono.Cecil.Cil;
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

    public const int BatteryPowerPerUseDefault = 25;
    public const int MinPowerForChargingDefault = 20;
    public const int FuelPowerPerUseDefault = 750;
    public const int PowerPerPanelDefault = 30;
    public const int PowerPerEngineDefault = 50;
    public const int PowerPerBatteryDefault = 50;
    public const int ChargePerBatteryDefault = 35;

    public static void PatchGamePrefsProps(ModuleDefinition module)
    {

        TypeDefinition prefs = module.Types.First(d => d.Name == "GamePrefs");
        MethodDefinition method = prefs.Methods.First(d => d.Name == "initPropertyDecl");
        // Get nested `PropertyDecl` sub-class in `GamePrefs` class
        TypeDefinition elem = prefs.NestedTypes.First(d => d.Name == "PropertyDecl");
        MethodDefinition ctor = elem.Methods.First(x => x.Name == ".ctor");

        int enums = 210; // ToDo: get this dynamically!?
        int lst = (int)method.Body.Instructions[7].Operand;
        ILProcessor worker = method.Body.GetILProcessor();
        Instruction[] instructions = new Instruction[]
        {

            worker.Create(OpCodes.Dup), // Reference array
            worker.Create(OpCodes.Ldc_I4, lst++), // index
            worker.Create(OpCodes.Ldc_I4, enums++), // EnumGamePrefs
            worker.Create(OpCodes.Ldc_I4_0), // Persistent
            worker.Create(OpCodes.Ldc_I4_3), // EnumType
            worker.Create(OpCodes.Ldc_I4_0), // Default
            worker.Create(OpCodes.Box, module.TypeSystem.Boolean), // Boxing
            worker.Create(OpCodes.Ldnull), // Unused
            worker.Create(OpCodes.Ldnull), // Unused
            worker.Create(OpCodes.Newobj, ctor),
            worker.Create(OpCodes.Stelem_Any, elem),

            worker.Create(OpCodes.Dup), // Reference array
            worker.Create(OpCodes.Ldc_I4, lst++), // index
            worker.Create(OpCodes.Ldc_I4, enums++), // EnumGamePrefs
            worker.Create(OpCodes.Ldc_I4_1), // Persistent
            worker.Create(OpCodes.Ldc_I4_0), // EnumType
            worker.Create(OpCodes.Ldc_I4, BatteryPowerPerUseDefault), // Default
            worker.Create(OpCodes.Box, module.TypeSystem.Int32), // Boxing
            worker.Create(OpCodes.Ldnull), // Unused
            worker.Create(OpCodes.Ldnull), // Unused
            worker.Create(OpCodes.Newobj, ctor),
            worker.Create(OpCodes.Stelem_Any, elem),

            worker.Create(OpCodes.Dup), // Reference array
            worker.Create(OpCodes.Ldc_I4, lst++), // index
            worker.Create(OpCodes.Ldc_I4, enums++), // EnumGamePrefs
            worker.Create(OpCodes.Ldc_I4_1), // Persistent
            worker.Create(OpCodes.Ldc_I4_0), // EnumType
            worker.Create(OpCodes.Ldc_I4, MinPowerForChargingDefault), // Default
            worker.Create(OpCodes.Box, module.TypeSystem.Int32), // Boxing
            worker.Create(OpCodes.Ldnull), // Unused
            worker.Create(OpCodes.Ldnull), // Unused
            worker.Create(OpCodes.Newobj, ctor),
            worker.Create(OpCodes.Stelem_Any, elem),

            worker.Create(OpCodes.Dup), // Reference array
            worker.Create(OpCodes.Ldc_I4, lst++), // index
            worker.Create(OpCodes.Ldc_I4, enums++), // EnumGamePrefs
            worker.Create(OpCodes.Ldc_I4_1), // Persistent
            worker.Create(OpCodes.Ldc_I4_0), // EnumType
            worker.Create(OpCodes.Ldc_I4, FuelPowerPerUseDefault), // Default
            worker.Create(OpCodes.Box, module.TypeSystem.Int32), // Boxing
            worker.Create(OpCodes.Ldnull), // Unused
            worker.Create(OpCodes.Ldnull), // Unused
            worker.Create(OpCodes.Newobj, ctor),
            worker.Create(OpCodes.Stelem_Any, elem),

            worker.Create(OpCodes.Dup), // Reference array
            worker.Create(OpCodes.Ldc_I4, lst++), // index
            worker.Create(OpCodes.Ldc_I4, enums++), // EnumGamePrefs
            worker.Create(OpCodes.Ldc_I4_1), // Persistent
            worker.Create(OpCodes.Ldc_I4_0), // EnumType
            worker.Create(OpCodes.Ldc_I4, PowerPerPanelDefault), // Default
            worker.Create(OpCodes.Box, module.TypeSystem.Int32), // Boxing
            worker.Create(OpCodes.Ldnull), // Unused
            worker.Create(OpCodes.Ldnull), // Unused
            worker.Create(OpCodes.Newobj, ctor),
            worker.Create(OpCodes.Stelem_Any, elem),

            worker.Create(OpCodes.Dup), // Reference array
            worker.Create(OpCodes.Ldc_I4, lst++), // index
            worker.Create(OpCodes.Ldc_I4, enums++), // EnumGamePrefs
            worker.Create(OpCodes.Ldc_I4_1), // Persistent
            worker.Create(OpCodes.Ldc_I4_0), // EnumType
            worker.Create(OpCodes.Ldc_I4, PowerPerEngineDefault), // Default
            worker.Create(OpCodes.Box, module.TypeSystem.Int32), // Boxing
            worker.Create(OpCodes.Ldnull), // Unused
            worker.Create(OpCodes.Ldnull), // Unused
            worker.Create(OpCodes.Newobj, ctor),
            worker.Create(OpCodes.Stelem_Any, elem),

            worker.Create(OpCodes.Dup), // Reference array
            worker.Create(OpCodes.Ldc_I4, lst++), // index
            worker.Create(OpCodes.Ldc_I4, enums++), // EnumGamePrefs
            worker.Create(OpCodes.Ldc_I4_1), // Persistent
            worker.Create(OpCodes.Ldc_I4_0), // EnumType
            worker.Create(OpCodes.Ldc_I4, PowerPerBatteryDefault), // Default
            worker.Create(OpCodes.Box, module.TypeSystem.Int32), // Boxing
            worker.Create(OpCodes.Ldnull), // Unused
            worker.Create(OpCodes.Ldnull), // Unused
            worker.Create(OpCodes.Newobj, ctor),
            worker.Create(OpCodes.Stelem_Any, elem),

            worker.Create(OpCodes.Dup), // Reference array
            worker.Create(OpCodes.Ldc_I4, lst++), // index
            worker.Create(OpCodes.Ldc_I4, enums++), // EnumGamePrefs
            worker.Create(OpCodes.Ldc_I4_1), // Persistent
            worker.Create(OpCodes.Ldc_I4_0), // EnumType
            worker.Create(OpCodes.Ldc_I4, ChargePerBatteryDefault), // Default
            worker.Create(OpCodes.Box, module.TypeSystem.Int32), // Boxing
            worker.Create(OpCodes.Ldnull), // Unused
            worker.Create(OpCodes.Ldnull), // Unused
            worker.Create(OpCodes.Newobj, ctor),
            worker.Create(OpCodes.Stelem_Any, elem),

        };

        // We are adding 8 new items to this array
        worker.Replace(method.Body.Instructions[7],
            worker.Create(OpCodes.Ldc_I4, lst));

        Instruction ins = method.Body.Instructions[
            method.Body.Instructions.Count - 2];

        foreach (var instruction in instructions)
            worker.InsertBefore(ins, instruction);

    }

    public static void PatchGameModeSurvival(ModuleDefinition module)
    {

        TypeDefinition type = module.Types.First(d => d.Name == "GameModeSurvival");
        MethodDefinition method = type.Methods.First(d => d.Name == "GetSupportedGamePrefsInfo");
        // Get nested `ModeGamePref` sub-class in `GameMode` class
        TypeDefinition elem = module.Types.First(d => d.Name == "GameMode")
            .NestedTypes.First(d => d.Name == "ModeGamePref");
        MethodDefinition ctor = elem.Methods.First(x => x.Name == ".ctor");

        int enums = 210; // ToDo: get this dynamically!?
        sbyte lst = (sbyte)method.Body.Instructions[0].Operand;
        ILProcessor worker = method.Body.GetILProcessor();
        Instruction[] instructions = new Instruction[]
        {
            worker.Create(OpCodes.Dup), // Reference array
            worker.Create(OpCodes.Ldc_I4_S, lst++), // index
            worker.Create(OpCodes.Ldc_I4, enums++), // EnumGamePrefs
            worker.Create(OpCodes.Ldc_I4_3), // EnumType
            worker.Create(OpCodes.Ldc_I4_0), // Default
            worker.Create(OpCodes.Box, module.TypeSystem.Boolean), // Boxing
            worker.Create(OpCodes.Newobj, ctor),
            worker.Create(OpCodes.Stelem_Any, elem),
            
            worker.Create(OpCodes.Dup), // Reference array
            worker.Create(OpCodes.Ldc_I4_S, lst++), // index
            worker.Create(OpCodes.Ldc_I4, enums++), // EnumGamePrefs
            worker.Create(OpCodes.Ldc_I4_0), // EnumType
            worker.Create(OpCodes.Ldc_I4, BatteryPowerPerUseDefault), // Default
            worker.Create(OpCodes.Box, module.TypeSystem.Int32), // Boxing
            worker.Create(OpCodes.Newobj, ctor),
            worker.Create(OpCodes.Stelem_Any, elem),

            worker.Create(OpCodes.Dup), // Reference array
            worker.Create(OpCodes.Ldc_I4_S, lst++), // index
            worker.Create(OpCodes.Ldc_I4, enums++), // EnumGamePrefs
            worker.Create(OpCodes.Ldc_I4_0), // EnumType
            worker.Create(OpCodes.Ldc_I4, MinPowerForChargingDefault), // Default
            worker.Create(OpCodes.Box, module.TypeSystem.Int32), // Boxing
            worker.Create(OpCodes.Newobj, ctor),
            worker.Create(OpCodes.Stelem_Any, elem),

            worker.Create(OpCodes.Dup), // Reference array
            worker.Create(OpCodes.Ldc_I4_S, lst++), // index
            worker.Create(OpCodes.Ldc_I4, enums++), // EnumGamePrefs
            worker.Create(OpCodes.Ldc_I4_0), // EnumType
            worker.Create(OpCodes.Ldc_I4, FuelPowerPerUseDefault), // Default
            worker.Create(OpCodes.Box, module.TypeSystem.Int32), // Boxing
            worker.Create(OpCodes.Newobj, ctor),
            worker.Create(OpCodes.Stelem_Any, elem),

            worker.Create(OpCodes.Dup), // Reference array
            worker.Create(OpCodes.Ldc_I4_S, lst++), // index
            worker.Create(OpCodes.Ldc_I4, enums++), // EnumGamePrefs
            worker.Create(OpCodes.Ldc_I4_0), // EnumType
            worker.Create(OpCodes.Ldc_I4, PowerPerPanelDefault), // Default
            worker.Create(OpCodes.Box, module.TypeSystem.Int32), // Boxing
            worker.Create(OpCodes.Newobj, ctor),
            worker.Create(OpCodes.Stelem_Any, elem),

            worker.Create(OpCodes.Dup), // Reference array
            worker.Create(OpCodes.Ldc_I4_S, lst++), // index
            worker.Create(OpCodes.Ldc_I4, enums++), // EnumGamePrefs
            worker.Create(OpCodes.Ldc_I4_0), // EnumType
            worker.Create(OpCodes.Ldc_I4, PowerPerEngineDefault), // Default
            worker.Create(OpCodes.Box, module.TypeSystem.Int32), // Boxing
            worker.Create(OpCodes.Newobj, ctor),
            worker.Create(OpCodes.Stelem_Any, elem),

            worker.Create(OpCodes.Dup), // Reference array
            worker.Create(OpCodes.Ldc_I4_S, lst++), // index
            worker.Create(OpCodes.Ldc_I4, enums++), // EnumGamePrefs
            worker.Create(OpCodes.Ldc_I4_0), // EnumType
            worker.Create(OpCodes.Ldc_I4, PowerPerBatteryDefault), // Default
            worker.Create(OpCodes.Box, module.TypeSystem.Int32), // Boxing
            worker.Create(OpCodes.Newobj, ctor),
            worker.Create(OpCodes.Stelem_Any, elem),

            worker.Create(OpCodes.Dup), // Reference array
            worker.Create(OpCodes.Ldc_I4_S, lst++), // index
            worker.Create(OpCodes.Ldc_I4, enums++), // EnumGamePrefs
            worker.Create(OpCodes.Ldc_I4_0), // EnumType
            worker.Create(OpCodes.Ldc_I4, ChargePerBatteryDefault), // Default
            worker.Create(OpCodes.Box, module.TypeSystem.Int32), // Boxing
            worker.Create(OpCodes.Newobj, ctor),
            worker.Create(OpCodes.Stelem_Any, elem),

        };

        // We are adding 8 new items to this array
        worker.Replace(method.Body.Instructions[0],
            worker.Create(OpCodes.Ldc_I4_S, lst));

        Instruction ins = method.Body.Instructions[
            method.Body.Instructions.Count - 1];

        foreach (var instruction in instructions)
            worker.InsertBefore(ins, instruction);

    }

    public static void PatchGamePrefPropValues(ModuleDefinition module)
    {

        TypeDefinition prefs = module.Types.First(d => d.Name == "GamePrefs");
        MethodDefinition method = prefs.Methods.First(d => d.Name == ".ctor");
        // Get nested `PropertyDecl` sub-class in `GamePrefs` class
        method.Body.Instructions[1].Operand = 8 +
            (int)method.Body.Instructions[1].Operand;

    }

    public static void Patch(AssemblyDefinition assembly)
    {
        Console.WriteLine("Applying OCB Electricity Options Patch");

        ModuleDefinition module = assembly.MainModule;

        PatchEnumGamePrefs(module);
        PatchGamePrefsProps(module);
        PatchGameModeSurvival(module);
        PatchGamePrefPropValues(module);

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