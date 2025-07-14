using HarmonyLib;
using RimWorld;
using Verse;

namespace ShowHair.HarmonyPatches;

[HarmonyPatch(typeof(DefGenerator), nameof(DefGenerator.GenerateImpliedDefs_PreResolve))]
[HarmonyPatchCategory("ModInitialization")]
internal static class DefGenerator_GenerateImpliedDefs_PreResolve_Patch
{
	internal static void Postfix(bool hotReload)
	{
		foreach (ThingCategoryDef thingCategoryDef in
		         ThingCategoryDefGenerator_Hats.ImpliedThingCategoryDefs(hotReload))
		{
			DefGenerator.AddImpliedDef(thingCategoryDef, hotReload);
		}
	}
}