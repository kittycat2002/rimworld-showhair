using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ShowHair.HarmonyPatches;

[HarmonyPatch(typeof(HairDef), nameof(HairDef.GraphicFor))]
internal static class HairDef_GraphicFor_Patch
{
	private static void Postfix(HairDef __instance, Pawn pawn, Color color)
	{
		if (__instance.noGraphic)
		{
			return;
		}

		string texPath = __instance.texPath;
		ShaderTypeDef overrideShaderTypeDef = __instance.overrideShaderTypeDef;
		bool upperExists = ContentFinder<Texture2D>.Get($"{texPath}/UpperHead_north", false) != null ||
		                   ContentFinder<Texture2D>.Get($"{texPath}/UpperHead_east", false) != null ||
		                   ContentFinder<Texture2D>.Get($"{texPath}/UpperHead_south", false) != null ||
		                   ContentFinder<Texture2D>.Get($"{texPath}/UpperHead_west", false) != null;
		bool fullExists = ContentFinder<Texture2D>.Get($"{texPath}/FullHead_north", false) != null ||
		                  ContentFinder<Texture2D>.Get($"{texPath}/FullHead_east", false) != null ||
		                  ContentFinder<Texture2D>.Get($"{texPath}/FullHead_south", false) != null ||
		                  ContentFinder<Texture2D>.Get($"{texPath}/FullHead_west", false) != null;
		CacheEntry cacheEntry = Utils.pawnCache.GetOrAdd(pawn.thingIDNumber, new CacheEntry());
		if (upperExists)
			cacheEntry.upperGraphic = (Graphic_Multi)GraphicDatabase.Get<Graphic_Multi>($"{texPath}/UpperHead",
				overrideShaderTypeDef?.Shader ?? ShaderDatabase.CutoutHair, Vector2.one, color);
		if (fullExists)
			cacheEntry.fullGraphic = (Graphic_Multi)GraphicDatabase.Get<Graphic_Multi>($"{texPath}/FullHead",
				overrideShaderTypeDef?.Shader ?? ShaderDatabase.CutoutHair, Vector2.one, color);
	}
}