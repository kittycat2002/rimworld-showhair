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

internal static class Utils
{
	internal static readonly ConcurrentDictionary<int, CacheEntry> pawnCache = [];

	internal static HatStateParms GetHatStateParms(this Pawn pawn)
	{
		if (!pawn.RaceProps.Humanlike ||
		    (ShowHairMod.Settings.onlyApplyToColonists && !pawn.Faction.IsPlayerSafe())) return new HatStateParms();
		return new HatStateParms(
			true,
			DefDatabase<HatConditionFlagDef>.AllDefs
				.Where(def => def != HatConditionFlagDefOf.None && def.Worker.ConditionIsMet(pawn))
				.Aggregate<HatConditionFlagDef, ulong>(HatConditionFlagDefOf.None, (current, def) => current | def)
		);
	}

	internal static bool IsHeadwear(ApparelProperties? apparelProperties)
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

	internal static BodyPartGroupDef? HeadCoverage(this Pawn pawn)
	{
		bool upperHead = false;
		foreach (Apparel apparel in pawn.apparel.WornApparel.Where(apparel =>
			         IsHeadwear(apparel.def.apparel) && !pawn.HeadwearHidden(apparel.def)))
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