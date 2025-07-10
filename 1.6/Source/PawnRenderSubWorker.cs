using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace ShowHair;

[UsedImplicitly]
internal class PawnRenderSubWorkerHair : PawnRenderSubWorker
{
	public override void EditMaterial(PawnRenderNode node, PawnDrawParms parms, ref Material material)
	{
		if (!ShowHairMod.Settings.useDontShaveHead) return;
		if (!Utils.pawnCache.TryGetValue(parms.pawn.thingIDNumber, out CacheEntry cacheEntry) ||
		    !cacheEntry.hatStateParms.HasValue ||
		    (cacheEntry.fullGraphic == null && cacheEntry.upperGraphic == null)) return;
		HatStateParms hatStateParms = cacheEntry.hatStateParms.Value;
		if (!hatStateParms.enabled) return;
		BodyPartGroupDef? coverage = parms.pawn.HeadCoverage(true);
		if (coverage == BodyPartGroupDefOf.UpperHead)
		{
			Material? newMaterial = cacheEntry.upperGraphic?.NodeGetMat(parms);
			if (newMaterial != null)
				material = newMaterial;
		}
		else if (coverage == BodyPartGroupDefOf.FullHead)
		{
			Material? newMaterial = cacheEntry.fullGraphic?.NodeGetMat(parms);
			if (newMaterial != null)
				material = newMaterial;
		}
	}

	public override bool CanDrawNowSub(PawnRenderNode node, PawnDrawParms parms)
	{
		if (parms.pawn.apparel == null || !PawnRenderNodeWorker_Apparel_Head.HeadgearVisible(parms)) return true;
		if (!Utils.pawnCache.TryGetValue(parms.pawn.thingIDNumber, out CacheEntry cacheEntry) ||
		    !cacheEntry.hatStateParms.HasValue) return true;
		HatStateParms hatStateParms = cacheEntry.hatStateParms.Value;
		if (!hatStateParms.enabled) return true;
		bool hasHat = false;
		foreach (Apparel apparel in parms.pawn.apparel.WornApparel.Where(apparel =>
			         Utils.IsHeadwear(apparel.def.apparel)))
		{
			switch (ShowHairMod.Settings.GetHatState(hatStateParms.flags, apparel.def))
			{
				case HatEnum.HidesAllHair:
					return false;
				case HatEnum.ShowsHair:
					hasHat = true;
					break;
			}
		}

		if (hasHat)
		{
			return parms.pawn.story != null &&
			       !ShowHairMod.Settings.HairSelectorUI.enabledDefs.Contains(parms.pawn.story.hairDef);
		}

		return true;
	}
}

internal class PawnRenderSubWorkerHat : PawnRenderSubWorker
{
	public override bool CanDrawNowSub(PawnRenderNode node, PawnDrawParms parms)
	{
		if (!Utils.pawnCache.TryGetValue(parms.pawn.thingIDNumber, out CacheEntry cacheEntry) ||
		    !cacheEntry.hatStateParms.HasValue) return true;
		HatStateParms hatStateParms = cacheEntry.hatStateParms.Value;
		if (!hatStateParms.enabled || node.apparel == null) return true;
		return ShowHairMod.Settings.GetHatState(hatStateParms.flags, node.apparel.def) != HatEnum.HideHat;
	}
}