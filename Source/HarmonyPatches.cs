using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;

namespace ShowHair
{
    [StaticConstructorOnStartup]
    class HarmonyPatches
    {
        static HarmonyPatches()
        {

            var harmony = new Harmony("cat2002.showhair");
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(Game), "InitNewGame")]
    static class Patch_Game_InitNewGame
    {
        static void Postfix()
        {
            Settings.Initialize();
            //Patch_PawnRenderer_DrawHeadHair.Initialize();
        }
    }

    [HarmonyPatch(typeof(SavedGameLoaderNow), "LoadGameFromSaveFileNow")]
    static class Patch_SavedGameLoader_LoadGameFromSaveFileNow
    {
        [HarmonyPriority(Priority.Last)]
        static void Postfix()
        {
            Settings.Initialize();
            //Patch_PawnRenderer_DrawHeadHair.Initialize();

        }
    }

    [HarmonyPatch(typeof(Pawn_DraftController), "set_Drafted")]
    static class Patch_Pawn_DraftController
    {
        static void Postfix(Pawn_DraftController __instance)
        {
            var p = __instance.pawn;
            if (p.IsColonist && !p.Dead && p.def.race.Humanlike)
            {
                PortraitsCache.SetDirty(p);
                GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(p);
            }
        }
    }
    [HarmonyPatch()]
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
                if (!loadFound && il[i].opcode == OpCodes.Ldfld && il[i].OperandIs(onHeadLoc))
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

            apparelGraphics = __instance.graphics.apparelGraphics;

            Patch_PawnRenderer_DrawHeadHair.flags = flags;
            Patch_PawnRenderer_DrawHeadHair.headFacing = headFacing;
            skipDontShaveHead = false;
        }

        [HarmonyPriority(Priority.First)]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> il = instructions.ToList();

            MethodInfo get_IdeologyActive = AccessTools.Property(typeof(ModsConfig), nameof(ModsConfig.IdeologyActive)).GetGetMethod();
            MethodInfo hideHats =
                        typeof(Patch_PawnRenderer_DrawHeadHair).GetMethod(
                        nameof(Patch_PawnRenderer_DrawHeadHair.HideHats), BindingFlags.Static | BindingFlags.Public);
            MethodInfo drawMeshNowOrLater = AccessTools.Method(typeof(GenDraw), nameof(GenDraw.DrawMeshNowOrLater), new Type[] { typeof(Mesh), typeof(Vector3), typeof(Quaternion), typeof(Material), typeof(bool) });
            MethodInfo drawMeshNowOrLaterPatch =
                        typeof(Patch_PawnRenderer_DrawHeadHair).GetMethod(
                        nameof(Patch_PawnRenderer_DrawHeadHair.DrawMeshNowOrLaterPatch), BindingFlags.Static | BindingFlags.NonPublic);

            bool found1 = false, found2 = false;
            int drawFound = 0;
            for (int i = 0; i < il.Count; ++i)
            {
                // Inject after the show/hide flags are set but before they're used
                if (!found1 && il[i].opcode == OpCodes.Call && il[i].OperandIs(get_IdeologyActive))
                {
                    found1 = true;

                    // Override this instruction as it's the goto for the end of the if clause
                    il[i].opcode = OpCodes.Ldloca_S;
                    il[i].operand = 2;
                    yield return il[i];
                    yield return new CodeInstruction(OpCodes.Ldloca_S, 3);
                    yield return new CodeInstruction(OpCodes.Ldloca_S, 6);
                    yield return new CodeInstruction(OpCodes.Ldloc_S, 4);
                    yield return new CodeInstruction(OpCodes.Ldarg_S, 4);
                    yield return new CodeInstruction(OpCodes.Call, hideHats);
                    // Create the overridden instruction
                    yield return new CodeInstruction(OpCodes.Call, get_IdeologyActive);
                    ++i;
                }
                if (il[i].opcode == OpCodes.Call && il[i].OperandIs(drawMeshNowOrLater))
                {
                    ++drawFound;
                    if (drawFound == 3)
                    {
                        found2 = true;
                        il[i].operand = drawMeshNowOrLaterPatch;
                    }
                }
                yield return il[i];
            }
            if (!found1 && !found2)
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

        public static void HideHats(ref bool showHair, ref bool showBeard, ref bool showHat, bool inBed, Rot4 bodyFacing)
        {
            if (flags.FlagSet(PawnRenderFlags.Portrait) && Prefs.HatsOnlyOnMap)
            {
                showHat = false;
                showHair = pawn.story.hairDef != HairDefOf.Bald;
                showBeard = bodyFacing != Rot4.North && pawn.style.beardDef != BeardDefOf.NoBeard;
                return;
            }

            if (!showHat || Settings.OnlyApplyToColonists && !FactionUtility.IsPlayerSafe(pawn.Faction))
            {
                return;
            }

            showHair = true;
            showBeard = bodyFacing != Rot4.North && pawn.style.beardDef != BeardDefOf.NoBeard;

            HatEnum hatEnum = GetHideEnum(inBed);
            switch (hatEnum)
            {
                case HatEnum.ShowsHair:
                    showHair = ShowHair();
                    showHat = true;
                    break;
                case HatEnum.HidesAllHair:
                    showHair = false;
                    showBeard = false;
                    showHat = true;
                    break;
                case HatEnum.HidesHairShowBeard:
                    showHair = false;
                    break;
                case HatEnum.HideHat:
                    showHair = ShowHair();
                    showHat = false;
                    break;
                case HatEnum.ShowsHairHidesBeard:
                    showHair = ShowHair();
                    showBeard = false;
                    showHat = true;
                    break;
            }
            skipDontShaveHead = !showHat;
        }

        private static bool ShowHair() => !(pawn.story.hairDef == HairDefOf.Bald || Settings.HairDict.TryGetValue(pawn.story.hairDef, out HairSaver h) && h.forceHide);
        private static HatEnum GetHideEnum(bool inBed)
        {
            Apparel apparel;
            for (int j = 0; j < apparelGraphics.Count; j++)
            {
                apparel = apparelGraphics[j].sourceApparel;
                if (Settings.IsHeadwear(apparel.def.apparel))
                {
                    if (!Settings.HatDict.TryGetValue(apparel.def,out HatSaver hat))
                        return HatEnum.HideHat;
                    Log.Message($"hatDef: {hat.defName}, hatHide: {hat.hatHide}, draftedHide: {hat.draftedHide}, indoorsHide: {hat.indoorsHide}, bedHide: {hat.bedHide}, hatRenderer: {hat.hatRenderer}");
                    if (isDrafted && hat.draftedHide != HatStateEnum.Default)
                    {
                        return (HatEnum)(hat.draftedHide - 1);
                    }
                    else if (inBed && hat.bedHide != HatStateEnum.Default)
                    {
                        return (HatEnum)(hat.bedHide - 1);
                    }
                    else if (Settings.CheckIndoors && hat.indoorsHide != HatStateEnum.Default)
                    {
                        CompCeilingDetect comp = pawn.GetComp<CompCeilingDetect>();
                        if (comp != null && comp.IsIndoors)
                        {
                            return (HatEnum)(hat.indoorsHide - 1);
                        }
                    }
                    return hat.hatHide;
                }
            }
            return HatEnum.HideHat;
        }
    }
}