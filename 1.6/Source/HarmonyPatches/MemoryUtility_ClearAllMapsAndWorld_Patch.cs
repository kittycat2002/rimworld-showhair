using HarmonyLib;
using Verse.Profile;

namespace ShowHair.HarmonyPatches;

[HarmonyPatch(typeof(MemoryUtility), nameof(MemoryUtility.ClearAllMapsAndWorld))]
internal static class MemoryUtility_ClearAllMapsAndWorld_Patch
{
	private static void Postfix()
	{
		Utils.pawnCache.Clear();
	}
}