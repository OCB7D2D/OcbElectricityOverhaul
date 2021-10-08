using Mono.Cecil;
using SDX.Compiler;
using System;
using System.Linq;

// SDX "compile" time patch to alter dll (EAC incompatible)
public class ElectricityOptionsPatch : IPatcherMod
{

    public void PatchEnumGamePrefs(ModuleDefinition module)
    {

        // Add new field to EnumGamePrefs enum (not sure how `Last` enum plays here)
        var enumType = MakeTypePublic(module.Types.First(d => d.Name == "EnumGamePrefs"));
        enumType.Fields.Add(new FieldDefinition("BatterySelfCharge", FieldAttributes.Static | FieldAttributes.Literal
                | FieldAttributes.Public | FieldAttributes.HasDefault, enumType)
            { Constant = 199 });
        enumType.Fields.Add(new FieldDefinition("BatteryPowerPerUse", FieldAttributes.Static | FieldAttributes.Literal
                | FieldAttributes.Public | FieldAttributes.HasDefault, enumType)
            { Constant = 200 });
        enumType.Fields.Add(new FieldDefinition("MinPowerForCharging", FieldAttributes.Static | FieldAttributes.Literal
                | FieldAttributes.Public | FieldAttributes.HasDefault, enumType)
            { Constant = 201 });

        // Add new fields to GameInfoBool enum
        var infoBoolType = MakeTypePublic(module.Types.First(d => d.Name == "GameInfoBool"));
        infoBoolType.Fields.Add(new FieldDefinition("BatterySelfCharge", FieldAttributes.Static | FieldAttributes.Literal
                | FieldAttributes.Public | FieldAttributes.HasDefault, infoBoolType)
            { Constant = 14 });

        // Add new fields to GameInfoInt enum
        var infoIntType = MakeTypePublic(module.Types.First(d => d.Name == "GameInfoInt"));
        infoIntType.Fields.Add(new FieldDefinition("BatteryPowerPerUse", FieldAttributes.Static | FieldAttributes.Literal
                | FieldAttributes.Public | FieldAttributes.HasDefault, infoIntType)
            { Constant = 43 });
        infoIntType.Fields.Add(new FieldDefinition("MinPowerForCharging", FieldAttributes.Static | FieldAttributes.Literal
                | FieldAttributes.Public | FieldAttributes.HasDefault, infoIntType)
            { Constant = 44 });

        var type = MakeTypePublic(module.Types.First(d => d.Name == "XUiC_OptionsControls"));
        TypeReference comboBoxBoolTypeRef = module.ImportReference(typeof(XUiC_ComboBoxBool));
        type.Fields.Add(new FieldDefinition("comboBatterySelfCharge", FieldAttributes.Public, comboBoxBoolTypeRef));
        type.Fields.Add(new FieldDefinition("comboBatteryPowerPerUse", FieldAttributes.Public, comboBoxBoolTypeRef));
        type.Fields.Add(new FieldDefinition("comboMinPowerForCharging", FieldAttributes.Public, comboBoxBoolTypeRef));
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