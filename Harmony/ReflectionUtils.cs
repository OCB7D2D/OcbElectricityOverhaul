using System;
using System.Linq;
using HarmonyLib;

static class ReflectionUtils
{
	public static Type TypeByName(string name)
	{
		var type = Type.GetType(name, false);
		var assemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName.StartsWith("Microsoft.VisualStudio") is false);
		if (type is null)
			type = assemblies
				.SelectMany(a => AccessTools.GetTypesFromAssembly(a))
				.FirstOrDefault(t => t.FullName == name);
		if (type is null)
			type = assemblies
				.SelectMany(a => AccessTools.GetTypesFromAssembly(a))
				.FirstOrDefault(t => t.Name == name);
		return type;
	}
}
