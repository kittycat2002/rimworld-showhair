using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace ShowHair.HarmonyPatches;

[HarmonyPatch]
internal static class Pawn_Removed_Patch
{
	private static IEnumerable<MethodInfo> TargetMethods()
	{
		yield return AccessTools.Method(typeof(Pawn), nameof(Pawn.DeSpawn));
		yield return AccessTools.Method(typeof(Pawn), nameof(Pawn.Destroy));
	}

	private static void Postfix(Pawn __instance)
	{
		Utils.pawnCache.TryRemove(__instance.thingIDNumber, out _);
	}
}