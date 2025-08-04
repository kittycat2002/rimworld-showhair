using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ShowHair.HarmonyPatches;

[HarmonyPatch(typeof(HairDef), nameof(HairDef.GraphicFor))]
internal static class HairDef_GraphicFor_Patch
{
	private static readonly Dictionary<string, (bool upper, bool full)> graphicCache = [];

	private static void Postfix(HairDef __instance, Pawn pawn, Color color)
	{
		if (__instance.noGraphic)
		{
			return;
		}

		CacheEntry cacheEntry = Utils.pawnCache.GetOrAdd(pawn.thingIDNumber, new CacheEntry());
		if (cacheEntry.hairDef == __instance.defName && cacheEntry.hairColor == color)
		{
			return;
		}

		cacheEntry.hairDef = __instance.defName;
		cacheEntry.hairColor = color;
		string texPath = __instance.texPath;
		ShaderTypeDef overrideShaderTypeDef = __instance.overrideShaderTypeDef;
		if (graphicCache.TryGetValue(__instance.defName, out (bool upper, bool full) graphics))
		{
			if (graphics.upper)
			{
				cacheEntry.upperGraphic = (Graphic_Multi)GraphicDatabase.Get<Graphic_DontShaveHair>($"{texPath}_upper",
					overrideShaderTypeDef?.Shader ?? ShaderDatabase.CutoutHair, Vector2.one, color);
			}
			if (graphics.full)
				cacheEntry.fullGraphic = (Graphic_Multi)GraphicDatabase.Get<Graphic_DontShaveHair>($"{texPath}_full",
					overrideShaderTypeDef?.Shader ?? ShaderDatabase.CutoutHair, Vector2.one, color);
		}
		else
		{
			Graphic_DontShaveHair upper = (Graphic_DontShaveHair)GraphicDatabase.Get<Graphic_DontShaveHair>($"{texPath}_upper",
				overrideShaderTypeDef?.Shader ?? ShaderDatabase.CutoutHair, Vector2.one, color);
			Graphic_DontShaveHair full = (Graphic_DontShaveHair)GraphicDatabase.Get<Graphic_DontShaveHair>($"{texPath}_full",
				overrideShaderTypeDef?.Shader ?? ShaderDatabase.CutoutHair, Vector2.one, color);
			graphicCache[__instance.defName] = (upper.isDifferentFromMulti, full.isDifferentFromMulti);
			if (upper.isDifferentFromMulti)
				cacheEntry.upperGraphic = upper;
			if (full.isDifferentFromMulti)
				cacheEntry.fullGraphic = full;
		}
	}
}