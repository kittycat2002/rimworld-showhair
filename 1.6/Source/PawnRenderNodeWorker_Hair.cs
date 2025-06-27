using RimWorld;
using Verse;

namespace ShowHair;

public class PawnRenderNodeWorker_Hair : PawnRenderNodeWorker_FlipWhenCrawling
{
	public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
	{
		if (!base.CanDrawNow(node, parms))
			return false;
		if (parms.pawn.apparel == null || !PawnRenderNodeWorker_Apparel_Head.HeadgearVisible(parms) ||
		    (ShowHairMod.Settings.onlyApplyToColonists && !parms.pawn.Faction.IsPlayerSafe())) return true;
		bool upperHead = false;
		foreach (Apparel apparel in parms.pawn.apparel.WornApparel)
		{
			if (!Settings.IsHeadwear(apparel.def.apparel) ||
			    !ShowHairMod.Settings.TryGetPawnHatState(parms.pawn, apparel.def, out HatEnum hatEnum)) continue;
			switch (hatEnum)
			{
				case HatEnum.HidesAllHair:
					return false;
				case HatEnum.ShowsHair:
					upperHead = true;
					break;
			}
		}

		if (upperHead)
		{
			return parms.pawn.story != null && !ShowHairMod.Settings.hiddenHairs.Contains(parms.pawn.story.hairDef);
		}

		return true;
	}
}