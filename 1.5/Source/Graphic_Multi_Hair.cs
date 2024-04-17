using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace ShowHair
{
    public class Graphic_Multi_Hair : Graphic_Multi
    {
        public override Material NodeGetMat(PawnDrawParms parms)
        {
            int i = parms.facing.AsInt;
            if (i < 0 || i > 3)
            {
                return BaseContent.BadMat;
            }
            if (Settings.UseDontShaveHead && (!Settings.OnlyApplyToColonists || parms.pawn.Faction.IsPlayerSafe()) && parms.pawn.apparel != null && PawnRenderNodeWorker_Apparel_Head.HeadgearVisible(parms))
            {
                bool upperHead = false;
                foreach (Apparel apparel in parms.pawn.apparel.WornApparel)
                {
                    if (Settings.IsHeadwear(apparel.def.apparel) && Settings.TryGetPawnHatState(parms.pawn, apparel.def, out var hatEnum) && hatEnum != HatEnum.HideHat && (apparel.def.apparel.renderSkipFlags == null || !apparel.def.apparel.renderSkipFlags.Contains(RenderSkipFlagDefOf.None)))
                    {
                        if (apparel.def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.FullHead))
                        {
                            if (fullHeadMat[i] == null)
                            {
                                return base.NodeGetMat(parms);
                            }
                            return fullHeadMat[i];
                        }
                        else if (apparel.def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.UpperHead))
                        {
                            upperHead = true;
                        }
                    }
                }
                if (upperHead)
                {
                    if (upperHeadMat[i] == null)
                    {
                        return base.NodeGetMat(parms);
                    }
                    return upperHeadMat[i];
                }
            }
            return base.NodeGetMat(parms);
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
        readonly Material[] fullHeadMat = new Material[4];
        readonly Material[] upperHeadMat = new Material[4];
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