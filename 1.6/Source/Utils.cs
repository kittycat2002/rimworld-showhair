using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
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
	internal string hairDef = "";
	internal Color? hairColor;
	internal readonly Dictionary<Type, (int, bool)> conditionWorkers = [];
}

[StaticConstructorOnStartup]
internal static class Utils
{
	internal static readonly ConcurrentDictionary<int, CacheEntry> pawnCache = [];

	extension(string str)
	{
		internal string TrimMultiline()
		{
			return string.Join("\n", str.Trim().Split("\n").Select(str2 => str2.Trim()));
		}
	}

	extension(TaggedString str)
	{
		internal string TrimMultiline()
		{
			return new TaggedString(str.RawText.TrimMultiline());
		}
	}

	extension(ApparelProperties? apparelProperties)
	{
		internal bool IsHeadwear()
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
	}

	extension(Pawn pawn)
	{
		private bool HeadwearHidden(ThingDef apparel)
		{
			if (!pawnCache.TryGetValue(pawn.thingIDNumber, out CacheEntry cacheEntry) ||
			    !cacheEntry.hatStateParms.HasValue) return false;
			HatStateParms parms = cacheEntry.hatStateParms.Value;
			return ShowHairMod.Settings.GetHatState(parms.flags, apparel) == HatEnum.HideHat;
		}

		private bool UseDontShaveHead(ThingDef apparel)
		{
			if (!pawnCache.TryGetValue(pawn.thingIDNumber, out CacheEntry cacheEntry) ||
			    !cacheEntry.hatStateParms.HasValue) return false;
			HatStateParms parms = cacheEntry.hatStateParms.Value;
			return ShowHairMod.Settings.GetHatDontShaveHead(parms.flags, apparel);
		}

		internal BodyPartGroupDef? HeadCoverage(bool onlyIncludeDontShaveHead = false)
		{
			bool upperHead = false;
			foreach (Apparel apparel in pawn.apparel.WornApparel)
			{
				if (!apparel.def.apparel.IsHeadwear() || pawn.HeadwearHidden(apparel.def) ||
				    (onlyIncludeDontShaveHead && !pawn.UseDontShaveHead(apparel.def))) continue;
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

		internal HatStateParms GetHatStateParms()
		{
			if (ShowHairMod.Settings.onlyApplyToColonists && !pawn.Faction.IsPlayerSafe()) return new HatStateParms();
			ulong result = HatConditionFlagDefOf.None;
			foreach (HatConditionFlagDef def in ShowHairMod.Settings.EnabledConditions)
			{
				if (def != HatConditionFlagDefOf.None && def.Worker.ConditionIsMet(pawn)) result |= def;
			}

			return new HatStateParms(
				true,
				result
			);
		}
	}
}