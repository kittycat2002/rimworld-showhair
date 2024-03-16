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

    /*[HarmonyPatch()]
    public static class Patch_PawnRenderer_DrawApparel
    {
        private static Type type;
        static MethodInfo TargetMethod()
        {
            var drawMethod = AccessTools.FindIncludingInnerTypes(typeof(PawnRenderer), (type) => AccessTools.FirstMethod(type, (method) => method.Name.Contains("g__DrawApparel") && method.ReturnType == typeof(void)));
            type = drawMethod.DeclaringType;
            return drawMethod;
        }
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {

            List<CodeInstruction> il = instructions.ToList();
            FieldInfo onHeadLoc = AccessTools.Field(type, "onHeadLoc");
            bool loadFound = false;
            for (int i = 0; i < il.Count; ++i)
            {
                if (!loadFound && il[i].Is(OpCodes.Ldfld, onHeadLoc))
                {
                    loadFound = true;
                    yield return il[i];
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return CodeInstruction.LoadField(typeof(ApparelGraphicRecord), "sourceApparel");
                    yield return CodeInstruction.LoadField(typeof(Thing), "def");
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch_PawnRenderer_DrawApparel), nameof(Patch_PawnRenderer_DrawApparel.GetHatRenderer)));
                    i++;
                }
                yield return il[i];
            }
        }
        public static Vector3 GetHatRenderer(Vector3 prevLayer, ThingDef hat)
        {
            if (Settings.HatDict.TryGetValue(hat, out var e) && e.hatRenderer == HatRendererEnum.ForceOverHair)
            {
                prevLayer.y += 0.002f;
            }
            return prevLayer;
        }
    }
    [HarmonyPatch(typeof(PawnRenderer), "DrawHeadHair")]
    public static class Patch_PawnRenderer_DrawHeadHair
    {
        private static bool isDrafted;
        private static Pawn pawn;
        private static List<ApparelGraphicRecord> apparelGraphics;
        private static PawnRenderFlags flags;
        private static Rot4 headFacing;
        private static bool skipDontShaveHead;

        [HarmonyPriority(Priority.First)]
        public static void Prefix(PawnRenderer __instance, Pawn ___pawn, Vector3 rootLoc, Vector3 headOffset, float angle, Rot4 bodyFacing, Rot4 headFacing, RotDrawMode bodyDrawType, PawnRenderFlags flags)
        {
            pawn = ___pawn;
            if (pawn == null || __instance == null)
                return;

            isDrafted = pawn.RaceProps.Humanlike && pawn.Drafted;

            //apparelGraphics = __instance.graphics.apparelGraphics;

            Patch_PawnRenderer_DrawHeadHair.flags = flags;
            Patch_PawnRenderer_DrawHeadHair.headFacing = headFacing;
            skipDontShaveHead = false;
        }

        [HarmonyPriority(Priority.First)]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> il = instructions.ToList();

            MethodInfo get_IdeologyActive = AccessTools.Property(typeof(ModsConfig), nameof(ModsConfig.IdeologyActive)).GetGetMethod();
            MethodInfo shouldShowHair =
                        typeof(Patch_PawnRenderer_DrawHeadHair).GetMethod(
                        nameof(Patch_PawnRenderer_DrawHeadHair.ShouldShowHair), BindingFlags.Static | BindingFlags.Public);
            MethodInfo shouldDrawHat = AccessTools.Method(
                        typeof(Patch_PawnRenderer_DrawHeadHair),
                        nameof(Patch_PawnRenderer_DrawHeadHair.ShouldDrawHat), new Type[] { typeof(ThingDef), typeof(bool) });
            MethodInfo shouldDrawHatList = AccessTools.Method(
                        typeof(Patch_PawnRenderer_DrawHeadHair),
                        nameof(Patch_PawnRenderer_DrawHeadHair.ShouldDrawHat), new Type[] { typeof(List<ApparelGraphicRecord>), typeof(bool) });
            MethodInfo drawMeshNowOrLater = AccessTools.Method(typeof(GenDraw), nameof(GenDraw.DrawMeshNowOrLater), new Type[] { typeof(Mesh), typeof(Vector3), typeof(Quaternion), typeof(Material), typeof(bool) });
            MethodInfo drawMeshNowOrLaterPatch =
                        typeof(Patch_PawnRenderer_DrawHeadHair).GetMethod(
                        nameof(Patch_PawnRenderer_DrawHeadHair.DrawMeshNowOrLaterPatch), BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo drawApparel = AccessTools.FindIncludingInnerTypes(typeof(PawnRenderer), (type) => AccessTools.FirstMethod(type, (method) => method.Name.Contains("g__DrawApparel") && method.ReturnType == typeof(void)));
            MethodInfo getApparelItem = typeof(List<ApparelGraphicRecord>).GetProperty("Item").GetGetMethod();
            bool found1 = false, found2 = false, found3 = false;
            int drawFound = 0;
            for (int i = 0; i < il.Count; i++)
            {
                // Inject after the show/hide flags are set but before they're used
                if (!found1 && il[i].Is(OpCodes.Call, get_IdeologyActive))
                {
                    found1 = true;

                    // Override this instruction as it's the goto for the end of the if clause
                    il[i].opcode = OpCodes.Ldloc_0;
                    il[i].operand = null;
                    yield return il[i];
                    yield return new CodeInstruction(OpCodes.Ldloca_S, 2);
                    yield return new CodeInstruction(OpCodes.Ldloca_S, 3);
                    yield return new CodeInstruction(OpCodes.Ldloca_S, 6);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 4);
                    yield return new CodeInstruction(OpCodes.Ldarg_S, 4);
                    yield return new CodeInstruction(OpCodes.Call, shouldShowHair);
                    // Create the overridden instruction
                    yield return new CodeInstruction(OpCodes.Call, get_IdeologyActive);
                    i++;
                }
                else if (il[i].Is(OpCodes.Call,drawMeshNowOrLater))
                {
                    ++drawFound;
                    if (drawFound == 3)
                    {
                        found2 = true;
                        il[i].operand = drawMeshNowOrLaterPatch;
                    }
                }
                else if (!found3 && il[i].opcode == OpCodes.Stloc_S && il[i].operand is LocalBuilder builder && builder.LocalIndex == 4)
                {
                    found3 = true;
                    yield return il[i];
                    yield return new CodeInstruction(OpCodes.Ldloc_1);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 4);
                    yield return new CodeInstruction(OpCodes.Call, shouldDrawHatList);
                    yield return new CodeInstruction(OpCodes.Stloc_1);
                    i++;
                }
                    yield return il[i];
            }
            if (!found1 || !found2 || !found3)
            {
                Log.Error("Show Hair or Hide All Hats could not inject itself properly. This is due to other mods modifying the same code this mod needs to modify.");
            }
        }

        private static void DrawMeshNowOrLaterPatch(Mesh mesh, Vector3 loc, Quaternion quat, Material mat, bool drawNow)
        {
            //Log.Warning($"DrawMeshNowOrLaterPatch {mat.name}");
            if (!skipDontShaveHead && Settings.UseDontShaveHead && HairUtilityFactory.GetHairUtility().TryGetCustomHairMat(pawn, headFacing, out Material m))
            {
                mat = m;
                //Log.Warning($"-UseDontShaveHead {mat.name}");
            }
            GenDraw.DrawMeshNowOrLater(mesh, loc, quat, mat, drawNow);
        }

        private static void DrawMeshNowOrLaterHat(Mesh mesh, Vector3 loc, Quaternion quat, Material mat, bool drawNow)
        {
            //Log.Warning($"DrawMeshNowOrLaterPatch {mat.name}");
            if (!skipDontShaveHead && Settings.UseDontShaveHead && HairUtilityFactory.GetHairUtility().TryGetCustomHairMat(pawn, headFacing, out Material m))
            {
                mat = m;
                //Log.Warning($"-UseDontShaveHead {mat.name}");
            }
            loc.y += 0.1f; 
            GenDraw.DrawMeshNowOrLater(mesh, loc, quat, mat, drawNow);
        }

        public static void ShouldShowHair(object locals1, ref bool showHair, ref bool showBeard, ref bool showHat, bool inBed, Rot4 bodyFacing)
        {
            Traverse locals1t = Traverse.Create(locals1);
            if (pawn.DevelopmentalStage.Baby()
                || (RotDrawMode)locals1t.Field("bodyDrawType").GetValue() == RotDrawMode.Dessicated
                || ((PawnRenderFlags)locals1t.Field("flags").GetValue()).FlagSet(PawnRenderFlags.HeadStump)
                || (flags.FlagSet(PawnRenderFlags.Portrait)
                && Prefs.HatsOnlyOnMap)
                || !showHat
                || (Settings.OnlyApplyToColonists
                && !FactionUtility.IsPlayerSafe(pawn.Faction)))
                return;

            showHair = pawn.story.hairDef != HairDefOf.Bald;
            showBeard = bodyFacing != Rot4.North && pawn.style.beardDef != null && pawn.style.beardDef != BeardDefOf.NoBeard;

            Apparel apparel;
            int hatCount = 0;
            for (int j = 0; j < apparelGraphics.Count; j++)
            {
                apparel = apparelGraphics[j].sourceApparel;
                if (Settings.IsHeadwear(apparel.def.apparel))
                {
                    HatEnum hatEnum = HatEnum.HideHat;
                    if (!Settings.HatDict.TryGetValue(apparel.def, out HatSaver hat))
                        hatEnum =HatEnum.HideHat;
                    else if (isDrafted && hat.draftedHide != HatStateEnum.Default)
                    {
                        hatEnum =(HatEnum)(hat.draftedHide - 1);
                    }
                    else if (inBed && hat.bedHide != HatStateEnum.Default)
                    {
                        hatEnum = (HatEnum)(hat.bedHide - 1);
                    }
                    else if (hat.indoorsHide != HatStateEnum.Default)
                    {
                        CompCeilingDetect comp = pawn.GetComp<CompCeilingDetect>();
                        if (comp != null && comp.IsIndoors)
                        {
                            hatEnum = (HatEnum)(hat.indoorsHide - 1);
                        }
                        else
                        {
                            hatEnum = hat.hatHide;
                        }
                    }
                    else
                    {
                        hatEnum = hat.hatHide;
                    }
                    switch (hatEnum)
                    {
                        case HatEnum.ShowsHair:
                            hatCount++;
                            break;
                        case HatEnum.HidesAllHair:
                            showHair = false;
                            showBeard = false;
                            hatCount++;
                            break;
                        case HatEnum.HidesHairShowBeard:
                            showHair = false;
                            hatCount++;
                            break;
                        case HatEnum.ShowsHairHidesBeard:
                            showBeard = false;
                            hatCount++;
                            break;
                    }
                }
            }
            skipDontShaveHead = hatCount == 0;
            if (!skipDontShaveHead && showHair && Settings.HairDict.TryGetValue(pawn.story.hairDef, out HairSaver h) && h.forceHide)
            {
                showHair = false;
            }
        }
        private static bool ShouldDrawHat(ThingDef apparel, bool inBed)
        {
            if (Settings.OnlyApplyToColonists && !FactionUtility.IsPlayerSafe(pawn.Faction))
                return true;
            HatEnum hatEnum = HatEnum.HideHat;
            if (!Settings.HatDict.TryGetValue(apparel, out HatSaver hat))
                hatEnum = HatEnum.HideHat;
            else if (isDrafted && hat.draftedHide != HatStateEnum.Default)
            {
                hatEnum = (HatEnum)(hat.draftedHide - 1);
            }
            else if (inBed && hat.bedHide != HatStateEnum.Default)
            {
                hatEnum = (HatEnum)(hat.bedHide - 1);
            }
            else if (hat.indoorsHide != HatStateEnum.Default)
            {
                CompCeilingDetect comp = pawn.GetComp<CompCeilingDetect>();
                if (comp != null && comp.IsIndoors)
                {
                    hatEnum = (HatEnum)(hat.indoorsHide - 1);
                }
                else
                {
                    hatEnum = hat.hatHide;
                }
            }
            else
            {
                hatEnum = hat.hatHide;
            }
            return hatEnum != HatEnum.HideHat;
        }
        private static bool ShouldDrawHat(ApparelGraphicRecord apparel, bool inBed)
        {
            return ShouldDrawHat(apparel.sourceApparel.def, inBed);
        }
        private static List<ApparelGraphicRecord> ShouldDrawHat(List<ApparelGraphicRecord> apparel, bool inBed)
        {
            return apparel.Where(a => ShouldDrawHat(a, inBed)).ToList();
        }
    }*/
}