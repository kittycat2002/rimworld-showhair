using RimWorld;
using UnityEngine;
using Verse;

namespace ShowHair;
public class Graphic_Multi_Hair : Graphic_Multi
{
	public override Material? NodeGetMat(PawnDrawParms parms)
	{
		int i = parms.facing.AsInt;
		if (i is < 0 or > 3)
		{
			return BaseContent.BadMat;
		}

		if (!ShowHairMod.Settings.useDontShaveHead || (ShowHairMod.Settings.onlyApplyToColonists && !parms.pawn.Faction.IsPlayerSafe()) ||
		    parms.pawn.apparel == null ||
		    !PawnRenderNodeWorker_Apparel_Head.HeadgearVisible(parms)) return base.NodeGetMat(parms);
		bool upperHead = false;
		foreach (Apparel apparel in parms.pawn.apparel.WornApparel)
		{
			if (!Settings.IsHeadwear(apparel.def.apparel) ||
			    !ShowHairMod.Settings.TryGetPawnHatState(parms.pawn, apparel.def, out HatEnum hatEnum) ||
			    hatEnum == HatEnum.HideHat || (apparel.def.apparel.renderSkipFlags != null &&
			                                   apparel.def.apparel.renderSkipFlags.Contains(
				                                   RenderSkipFlagDefOf.None))) continue;
			if (apparel.def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.FullHead))
			{
				return fullHeadMat[i] == null ? base.NodeGetMat(parms) : fullHeadMat[i];
			}

			if (apparel.def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.UpperHead))
			{
				upperHead = true;
			}
		}

		return !upperHead || upperHeadMat[i] == null ? base.NodeGetMat(parms) : upperHeadMat[i];
	}

	public override void Init(GraphicRequest req)
	{
		base.Init(req);
		Texture2D[] arrayFull = new Texture2D[fullHeadMat.Length];
		arrayFull[0] = ContentFinder<Texture2D>.Get(req.path + "/FullHead_north", false);
		arrayFull[1] = ContentFinder<Texture2D>.Get(req.path + "/FullHead_east", false);
		arrayFull[2] = ContentFinder<Texture2D>.Get(req.path + "/FullHead_south", false);
		arrayFull[3] = ContentFinder<Texture2D>.Get(req.path + "/FullHead_west", false);
		if (arrayFull[0] == null)
		{
			if (arrayFull[2] != null)
			{
				arrayFull[0] = arrayFull[2];
			}
			else if (arrayFull[1] != null)
			{
				arrayFull[0] = arrayFull[1];
			}
			else if (arrayFull[3] != null)
			{
				arrayFull[0] = arrayFull[3];
			}
			else
			{
				arrayFull[0] = ContentFinder<Texture2D>.Get(req.path + "/FullHead", false);
			}
		}

		if (arrayFull[2] == null)
		{
			arrayFull[2] = arrayFull[0];
		}

		if (arrayFull[1] == null)
		{
			if (arrayFull[3] != null)
			{
				arrayFull[1] = arrayFull[3];
			}
			else
			{
				arrayFull[1] = arrayFull[0];
			}
		}

		if (arrayFull[3] == null)
		{
			if (arrayFull[1] != null)
			{
				arrayFull[3] = arrayFull[1];
			}
			else
			{
				arrayFull[3] = arrayFull[0];
			}
		}

		Texture2D[] arrayUpper = new Texture2D[upperHeadMat.Length];
		arrayUpper[0] = ContentFinder<Texture2D>.Get(req.path + "/UpperHead_north", false);
		arrayUpper[1] = ContentFinder<Texture2D>.Get(req.path + "/UpperHead_east", false);
		arrayUpper[2] = ContentFinder<Texture2D>.Get(req.path + "/UpperHead_south", false);
		arrayUpper[3] = ContentFinder<Texture2D>.Get(req.path + "/UpperHead_west", false);
		if (arrayUpper[0] == null)
		{
			if (arrayUpper[2] != null)
			{
				arrayUpper[0] = arrayUpper[2];
			}
			else if (arrayUpper[1] != null)
			{
				arrayUpper[0] = arrayUpper[1];
			}
			else if (arrayUpper[3] != null)
			{
				arrayUpper[0] = arrayUpper[3];
			}
			else
			{
				arrayUpper[0] = ContentFinder<Texture2D>.Get(req.path + "/UpperHead", false);
			}
		}

		if (arrayUpper[2] == null)
		{
			arrayUpper[2] = arrayUpper[0];
		}

		if (arrayUpper[1] == null)
		{
			if (arrayUpper[3] != null)
			{
				arrayUpper[1] = arrayUpper[3];
			}
			else
			{
				arrayUpper[1] = arrayUpper[0];
			}
		}

		if (arrayUpper[3] == null)
		{
			if (arrayUpper[1] != null)
			{
				arrayUpper[3] = arrayUpper[1];
			}
			else
			{
				arrayUpper[3] = arrayUpper[0];
			}
		}

		Texture2D[] arrayFullMask = new Texture2D[fullHeadMat.Length];
		Texture2D[] arrayUpperMask = new Texture2D[upperHeadMat.Length];
		if (req.shader.SupportsMaskTex())
		{
			string text = maskPath.NullOrEmpty() ? path : maskPath;
			string text2 = maskPath.NullOrEmpty() ? "m" : string.Empty;
			arrayFullMask[0] = ContentFinder<Texture2D>.Get(text + "/FullHead_north" + text2, false);
			arrayFullMask[1] = ContentFinder<Texture2D>.Get(text + "/FullHead_east" + text2, false);
			arrayFullMask[2] = ContentFinder<Texture2D>.Get(text + "/FullHead_south" + text2, false);
			arrayFullMask[3] = ContentFinder<Texture2D>.Get(text + "/FullHead_west" + text2, false);
			if (arrayFullMask[0] == null)
			{
				if (arrayFullMask[2] != null)
				{
					arrayFullMask[0] = arrayFullMask[2];
				}
				else if (arrayFullMask[1] != null)
				{
					arrayFullMask[0] = arrayFullMask[1];
				}
				else if (arrayFullMask[3] != null)
				{
					arrayFullMask[0] = arrayFullMask[3];
				}
			}

			if (arrayFullMask[2] == null)
			{
				arrayFullMask[2] = arrayFullMask[0];
			}

			if (arrayFullMask[1] == null)
			{
				if (arrayFullMask[3] != null)
				{
					arrayFullMask[1] = arrayFullMask[3];
				}
				else
				{
					arrayFullMask[1] = arrayFullMask[0];
				}
			}

			if (arrayFullMask[3] == null)
			{
				if (arrayFullMask[1] != null)
				{
					arrayFullMask[3] = arrayFullMask[1];
				}
				else
				{
					arrayFullMask[3] = arrayFullMask[0];
				}
			}

			arrayUpperMask[0] = ContentFinder<Texture2D>.Get(text + "/UpperHead_north" + text2, false);
			arrayUpperMask[1] = ContentFinder<Texture2D>.Get(text + "/UpperHead_east" + text2, false);
			arrayUpperMask[2] = ContentFinder<Texture2D>.Get(text + "/UpperHead_south" + text2, false);
			arrayUpperMask[3] = ContentFinder<Texture2D>.Get(text + "/UpperHead_west" + text2, false);
			if (arrayUpperMask[0] == null)
			{
				if (arrayUpperMask[2] != null)
				{
					arrayUpperMask[0] = arrayUpperMask[2];
				}
				else if (arrayUpperMask[1] != null)
				{
					arrayUpperMask[0] = arrayUpperMask[1];
				}
				else if (arrayUpperMask[3] != null)
				{
					arrayUpperMask[0] = arrayUpperMask[3];
				}
			}

			if (arrayUpperMask[2] == null)
			{
				arrayUpperMask[2] = arrayUpperMask[0];
			}

			if (arrayUpperMask[1] == null)
			{
				if (arrayUpperMask[3] != null)
				{
					arrayUpperMask[1] = arrayUpperMask[3];
				}
				else
				{
					arrayUpperMask[1] = arrayUpperMask[0];
				}
			}

			if (arrayUpperMask[3] == null)
			{
				if (arrayUpperMask[1] != null)
				{
					arrayUpperMask[3] = arrayUpperMask[1];
				}
				else
				{
					arrayUpperMask[3] = arrayUpperMask[0];
				}
			}
		}

		for (int i = 0; i < arrayFull.Length; i++)
		{
			if (arrayFull[i] != null)
			{
				MaterialRequest materialRequest = default;
				materialRequest.mainTex = arrayFull[i];
				materialRequest.shader = req.shader;
				materialRequest.color = color;
				materialRequest.colorTwo = colorTwo;
				materialRequest.maskTex = arrayFullMask[i];
				materialRequest.shaderParameters = req.shaderParameters;
				materialRequest.renderQueue = req.renderQueue;
				fullHeadMat[i] = MaterialPool.MatFrom(materialRequest);
			}
			else
			{
				fullHeadMat[i] = null;
			}
		}

		for (int i = 0; i < arrayUpper.Length; i++)
		{
			if (arrayUpper[i] != null)
			{
				MaterialRequest materialRequest = default;
				materialRequest.mainTex = arrayUpper[i];
				materialRequest.shader = req.shader;
				materialRequest.color = color;
				materialRequest.colorTwo = colorTwo;
				materialRequest.maskTex = arrayUpperMask[i];
				materialRequest.shaderParameters = req.shaderParameters;
				materialRequest.renderQueue = req.renderQueue;
				upperHeadMat[i] = MaterialPool.MatFrom(materialRequest);
			}
			else
			{
				upperHeadMat[i] = null;
			}
		}
	}

	private readonly Material?[] fullHeadMat = new Material[4];
	private readonly Material?[] upperHeadMat = new Material[4];
}