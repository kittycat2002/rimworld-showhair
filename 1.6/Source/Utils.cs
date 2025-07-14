using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace ShowHair;

internal struct HatStateParms(bool enabled, ulong flags)
{
	internal readonly bool enabled = enabled;
	internal readonly ulong flags = flags;
}

internal class CacheEntry
{
	internal HatStateParms? hatStateParms;
	internal Graphic_Multi? upperGraphic;
	internal Graphic_Multi? fullGraphic;
	internal readonly Dictionary<Type, (int, bool)> conditionWorkers = [];
}

[StaticConstructorOnStartup]
internal static class Utils
{
	internal static readonly ConcurrentDictionary<int, CacheEntry> pawnCache = [];

	internal static IEnumerable<HatConditionFlagDef> ConditionFlagDefsEnabled =
		ShowHairMod.Settings.GetEnabledConditions();

	internal static string TrimMultiline(this string str)
	{
		return string.Join("\n", str.Trim().Split("\n").Select(str2 => str2.Trim()));
	}

	internal static string TrimMultiline(this TaggedString str)
	{
		return new TaggedString(str.RawText.TrimMultiline());
	}

	internal static HatStateParms GetHatStateParms(this Pawn pawn)
	{
		if (ShowHairMod.Settings.onlyApplyToColonists && !pawn.Faction.IsPlayerSafe()) return new HatStateParms();
		return new HatStateParms(
			true,
			ConditionFlagDefsEnabled
				.Where(def => def != HatConditionFlagDefOf.None && def.Worker.ConditionIsMet(pawn))
				.Aggregate<HatConditionFlagDef, ulong>(HatConditionFlagDefOf.None, (current, def) => current | def)
		);
	}

	internal static bool IsHeadwear(this ApparelProperties? apparelProperties)
	{
		if (apparelProperties is null) return false;
		ApparelLayerDef lastLayer = apparelProperties.LastLayer;

		bool flag = lastLayer == ApparelLayerDefOf.Overhead || lastLayer == ApparelLayerDefOf.EyeCover;
		if (apparelProperties.parentTagDef == null) return flag;
		if (apparelProperties.parentTagDef == PawnRenderNodeTagDefOf.ApparelHead)
		{
			return true;
		}

		return apparelProperties.parentTagDef != PawnRenderNodeTagDefOf.ApparelBody && flag;
	}

	private static bool HeadwearHidden(this Pawn pawn, ThingDef apparel)
	{
		if (!pawnCache.TryGetValue(pawn.thingIDNumber, out CacheEntry cacheEntry) ||
		    !cacheEntry.hatStateParms.HasValue) return false;
		HatStateParms parms = cacheEntry.hatStateParms.Value;
		return ShowHairMod.Settings.GetHatState(parms.flags, apparel) == HatEnum.HideHat;
	}

	private static bool UseDontShaveHead(this Pawn pawn, ThingDef apparel)
	{
		if (!pawnCache.TryGetValue(pawn.thingIDNumber, out CacheEntry cacheEntry) ||
		    !cacheEntry.hatStateParms.HasValue) return false;
		HatStateParms parms = cacheEntry.hatStateParms.Value;
		return ShowHairMod.Settings.GetHatDontShaveHead(parms.flags, apparel);
	}

	internal static BodyPartGroupDef? HeadCoverage(this Pawn pawn, bool onlyIncludeDontShaveHead = false)
	{
		bool upperHead = false;
		foreach (Apparel apparel in pawn.apparel.WornApparel.Where(apparel =>
			         apparel.def.apparel.IsHeadwear() && !pawn.HeadwearHidden(apparel.def) &&
			         (!onlyIncludeDontShaveHead || pawn.UseDontShaveHead(apparel.def))))
		{
			if (apparel.def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.FullHead))
			{
				return BodyPartGroupDefOf.FullHead;
			}

			if (!upperHead && apparel.def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.UpperHead))
			{
				upperHead = true;
			}
		}

		return upperHead ? BodyPartGroupDefOf.UpperHead : null;
	}
}