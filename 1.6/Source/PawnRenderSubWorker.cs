using System.Collections.Generic;
using JetBrains.Annotations;
using RimWorld;
using UnityEngine;
using Verse;

namespace ShowHair;

[UsedImplicitly]
public class PawnRenderSubWorkerHair : PawnRenderSubWorker
{
	public override void EditMaterial(PawnRenderNode node, PawnDrawParms parms, ref Material material)
	{
		if (!Cache.hatStateDictionary.TryGetValue(parms.pawn.thingIDNumber, out HatStateParms hatStateParms)) return;
		if (!hatStateParms.enabled) return;
		BodyPartGroupDef? coverage = parms.pawn.HeadCoverage();
		if (coverage == null) return;
		if (!Cache.extraHairGraphicsDictionary.TryGetValue(parms.pawn.thingIDNumber, out (Graphic_Multi?, Graphic_Multi?) graphics)) return;
		
		if (coverage == BodyPartGroupDefOf.UpperHead)
		{
			Material? newMaterial = graphics.Item1?.NodeGetMat(parms);
			if (newMaterial != null)
				material = newMaterial;
		}
		else if (coverage == BodyPartGroupDefOf.FullHead)
		{
			Material? newMaterial = graphics.Item2?.NodeGetMat(parms);
			if (newMaterial != null)
				material = newMaterial;
		}
	}

	public override bool CanDrawNowSub(PawnRenderNode node, PawnDrawParms parms)
	{
		if (parms.pawn.apparel == null || !PawnRenderNodeWorker_Apparel_Head.HeadgearVisible(parms)) return true;
		if (!Cache.hatStateDictionary.TryGetValue(parms.pawn.thingIDNumber, out HatStateParms hatStateParms)) return true;
		if (!hatStateParms.enabled) return true;
		bool upperHead = false;
		foreach (Apparel apparel in parms.pawn.apparel.WornApparel)
		{
			if (!Utils.IsHeadwear(apparel.def.apparel) ||
			    !ShowHairMod.Settings.TryGetPawnHatState(hatStateParms.flags, apparel.def, out HatEnum hatEnum))
				continue;
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
			return parms.pawn.story != null && !ShowHairMod.Settings.HairSelectorUI.enabledDefs.Contains(parms.pawn.story.hairDef);
		}

		return true;
	}
}

public class PawnRenderSubWorkerHat : PawnRenderSubWorker
{
	public override bool CanDrawNowSub(PawnRenderNode node, PawnDrawParms parms)
	{
		if (!Cache.hatStateDictionary.TryGetValue(parms.pawn.thingIDNumber, out HatStateParms hatStateParms)) return true;
		if (hatStateParms.enabled && node.apparel != null &&
		    ShowHairMod.Settings.TryGetPawnHatState(hatStateParms.flags, node.apparel.def, out HatEnum hatEnum))
			return hatEnum != HatEnum.HideHat;
		return true;
	}
}