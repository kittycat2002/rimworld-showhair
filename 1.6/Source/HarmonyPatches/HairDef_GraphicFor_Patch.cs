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
		bool upper;
		bool full;
		string texPath = __instance.texPath;
		ShaderTypeDef overrideShaderTypeDef = __instance.overrideShaderTypeDef;
		if (graphicCache.TryGetValue(__instance.defName, out (bool upper, bool full) graphics))
		{
			upper = graphics.upper;
			full = graphics.full;
		}
		else
		{
			upper = ContentFinder<Texture2D>.Get($"{texPath}/UpperHead_north", false) != null ||
			                   ContentFinder<Texture2D>.Get($"{texPath}/UpperHead_east", false) != null ||
			                   ContentFinder<Texture2D>.Get($"{texPath}/UpperHead_south", false) != null ||
			                   ContentFinder<Texture2D>.Get($"{texPath}/UpperHead_west", false) != null;
			full = ContentFinder<Texture2D>.Get($"{texPath}/FullHead_north", false) != null ||
			                  ContentFinder<Texture2D>.Get($"{texPath}/FullHead_east", false) != null ||
			                  ContentFinder<Texture2D>.Get($"{texPath}/FullHead_south", false) != null ||
			                  ContentFinder<Texture2D>.Get($"{texPath}/FullHead_west", false) != null;
			graphicCache[__instance.defName] = (upper, full);
		}

		if (upper)
			cacheEntry.upperGraphic = (Graphic_Multi)GraphicDatabase.Get<Graphic_Multi>($"{texPath}/UpperHead",
				overrideShaderTypeDef?.Shader ?? ShaderDatabase.CutoutHair, Vector2.one, color);
		if (full)
			cacheEntry.fullGraphic = (Graphic_Multi)GraphicDatabase.Get<Graphic_Multi>($"{texPath}/FullHead",
				overrideShaderTypeDef?.Shader ?? ShaderDatabase.CutoutHair, Vector2.one, color);
	}
}