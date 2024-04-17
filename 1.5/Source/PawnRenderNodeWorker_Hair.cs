using RimWorld;
using Verse;

namespace ShowHair
{
    public class PawnRenderNodeWorker_Hair : PawnRenderNodeWorker_FlipWhenCrawling
    {
        public override bool CanDrawNow(PawnRenderNode node, PawnDrawParms parms)
        {
            if (!base.CanDrawNow(node, parms))
                return false;
            if (parms.pawn.apparel != null && PawnRenderNodeWorker_Apparel_Head.HeadgearVisible(parms) && (!Settings.OnlyApplyToColonists || parms.pawn.Faction.IsPlayerSafe()))
            {
                bool upperHead = false;
                foreach (Apparel apparel in parms.pawn.apparel.WornApparel)
                {
                    if (Settings.IsHeadwear(apparel.def.apparel) && Settings.TryGetPawnHatState(parms.pawn, apparel.def, out var hatEnum))
                    {
                        if (hatEnum == HatEnum.HidesAllHair)
                        {
                            return false;
                        }
                        else if (hatEnum == HatEnum.ShowsHair)
                        {
                            upperHead = true;
                        }
                    }
                    
                }
                if (upperHead)
                {
                    return parms.pawn.story != null && (!Settings.HairDict.TryGetValue(parms.pawn.story.hairDef, out HairSaver hair) || !hair.forceHide);
                }
            }
            return true;
        }
    }
}