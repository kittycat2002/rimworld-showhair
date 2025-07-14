using HarmonyLib;
using Verse;

namespace ShowHair.HarmonyPatches;

[HarmonyPatch(typeof(Corpse), nameof(Corpse.DeSpawn))]
internal static class Corpse_DeSpawn_Patch
{
	private static void Postfix(Corpse __instance)
	{
		if (__instance.InnerPawn?.thingIDNumber != null)
			Utils.pawnCache.TryRemove(__instance.InnerPawn.thingIDNumber, out _);
	}
}