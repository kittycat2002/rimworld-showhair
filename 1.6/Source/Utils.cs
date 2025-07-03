using System.Linq;
using RimWorld;
using Verse;

namespace ShowHair;

public struct HatStateParms
{
	internal bool enabled;
	internal ulong flags;
}

public static class Utils
{
	public static HatStateParms GetHatStateParms(this Pawn pawn)
	{
		HatStateParms parms = new();
		if (!pawn.RaceProps.Humanlike ||
		    (ShowHairMod.Settings.onlyApplyToColonists && !pawn.Faction.IsPlayerSafe())) return parms;
		parms.enabled = true;
		parms.flags = DefDatabase<HatConditionFlagDef>.AllDefs
			.Where(def => def != HatConditionFlagDefOf.None && def.Worker.ConditionIsMet(pawn))
			.Aggregate<HatConditionFlagDef, ulong>(HatConditionFlagDefOf.None, (current, def) => current | def);

		return parms;
	}

	public static bool IsHeadwear(ApparelProperties? apparelProperties)
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
		if (!Cache.hatStateDictionary.TryGetValue(pawn.thingIDNumber, out HatStateParms hatStateParms)) return false;
		if (ShowHairMod.Settings.TryGetPawnHatState(hatStateParms.flags, apparel, out HatEnum hatEnum))
		{
			return hatEnum == HatEnum.HideHat;
		}

		return false;
	}

	public static BodyPartGroupDef? HeadCoverage(this Pawn pawn)
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