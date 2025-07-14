using HarmonyLib;
using Verse;

namespace ShowHair.HarmonyPatches;

[StaticConstructorOnStartup]
internal class HarmonyPatchInitializer
{
	static HarmonyPatchInitializer()
	{
		ShowHairMod.harmony!.PatchAllUncategorized();
	}
}