﻿using HarmonyLib;
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
            if (ModLister.GetActiveModWithIdentifier("CETeam.CombatExtended") != null)
            {
                Log.Error("[Show Hair With Hats] IS NOT COMPATABLE WITH COMBAT EXTENDED.");
            }
            if (ModLister.GetActiveModWithIdentifier("velc.HatsDisplaySelection") != null)
            {
                Log.Error("[Show Hair With Hats] Consider disabling \"Hats Display Selection\" as that mod may clash with this one.");
            }

            var harmony = new Harmony("com.showhair.rimworld.mod");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
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

    /*[HarmonyPatch(typeof(Pawn), "SpawnSetup")]
    static class Patch_Pawn_TickRare
    {
        static void Postfix(Pawn __instance)
        {
            if (__instance.RaceProps.Humanlike)
            {
                if (__instance.TryGetComp<CompCeilingDetect>() == null)
                {
                    var fi = typeof(Pawn).GetField("comps", BindingFlags.NonPublic | BindingFlags.Instance);
                    List<ThingComp> comps = (List<ThingComp>)fi.GetValue(__instance);
                    var c = (ThingComp)Activator.CreateInstance(typeof(CompCeilingDetect));
                    c.parent = __instance;
                    comps.Add(c);
                    c.Initialize(new CompProperties_CeilingDetect());
                    if (comps != null)
                        comps.Add(c);
                    else
                    {
                        comps = new List<ThingComp>() { c };
                        fi.SetValue(__instance, comps);
                    }
                }
            }
        }
    }*/

    /* public class AAA
     {
         private void DrawHeadHair(Vector3 rootLoc, Vector3 headOffset, float angle, Rot4 bodyFacing, Rot4 headFacing, RotDrawMode bodyDrawType, PawnRenderFlags flags)
         {
             Vector3 onHeadLoc = rootLoc + headOffset;
             onHeadLoc.y += 0.0289575271f;
             List<ApparelGraphicRecord> apparelGraphics = null;
             Quaternion quat = Quaternion.AngleAxis(angle, Vector3.up);
             bool flag = false;
             bool flag2 = bodyFacing == Rot4.North;
             bool flag3 = flags.FlagSet(PawnRenderFlags.Headgear) && (!flags.FlagSet(PawnRenderFlags.Portrait) || !Prefs.HatsOnlyOnMap || flags.FlagSet(PawnRenderFlags.StylingStation));
             Patch_PawnRenderer_DrawHeadHair.HideHats(ref flag, ref flag2, ref flag3, bodyFacing, this);
         }
     }*/
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
            int addCount = 0;
            bool loadFound = false;
            for (int i = 0; i < il.Count; ++i)
            {
                if (!loadFound && il[i].opcode == OpCodes.Ldfld && il[i].OperandIs(onHeadLoc))
                {
                    yield return il[i];
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return CodeInstruction.LoadField(typeof(ApparelGraphicRecord), "sourceApparel");
                    yield return CodeInstruction.LoadField(typeof(Thing), "def");
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch_PawnRenderer_DrawApparel), nameof(Patch_PawnRenderer_DrawApparel.GetHatRenderer1)));
                    i++;
                }
                else if (addCount != 2 && il[i].opcode == OpCodes.Add)
                {
                    if (addCount++ == 1)
                    {
                        il[i].opcode = OpCodes.Ldarg_1;
                        il[i].operand = null;
                        yield return il[i];
                        yield return CodeInstruction.LoadField(typeof(ApparelGraphicRecord), "sourceApparel");
                        yield return CodeInstruction.LoadField(typeof(Thing), "def");
                        yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch_PawnRenderer_DrawApparel), nameof(Patch_PawnRenderer_DrawApparel.GetHatRenderer2)));
                        yield return new CodeInstruction(OpCodes.Add);
                        i++;
                    }
                }
                yield return il[i];
            }
        }
        public static Vector3 GetHatRenderer1(Vector3 prevLayer, ThingDef hat)
        {
            prevLayer.y = GetHatRenderer2(prevLayer.y, hat);
            return prevLayer;
        }
        public static float GetHatRenderer2(float prevLayer, ThingDef hat)
        {
            if (Settings.HatsRenderer.TryGetValue(hat, out var e) && e != HatRendererEnum.NormalRender)
            {
                prevLayer += 0.02f;
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
#if DEBUG && TRANSPILER
            bool firstAfterFound = true;
#endif

            MethodInfo get_IdeologyActive = AccessTools.Property(typeof(ModsConfig), nameof(ModsConfig.IdeologyActive))?.GetGetMethod();
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

        public static void HideHats(ref bool showHair, ref bool showBeard, ref bool showHat, Rot4 bodyFacing)
        {
            try
            {
                //Log.Error($"Start {pawn.Name.ToStringShort} hideHair:{hideHair}  hideBeard:{hideBeard}  showHat:{showHat}");
                // Determine if hat should be shown
                if (flags.FlagSet(PawnRenderFlags.Portrait) && Prefs.HatsOnlyOnMap)
                {
                    showHat = false;
                    showHair = true;
                    showBeard = true;
                    //Log.Error($"0 {pawn.Name.ToStringShort} hideHair:{hideHair}  hideBeard:{hideBeard}  showHat:{showHat}");
                    return;
                }

                if (!showHat ||
                    Settings.OnlyApplyToColonists && !FactionUtility.IsPlayerSafe(pawn.Faction) && !Settings.OptionsOpen)
                {
                    //Log.Error($"1 {pawn.Name.ToStringShort} hideHair:{hideHair}  hideBeard:{hideBeard}  showHat:{showHat}");
                    return;
                }

                if (showHair && !showHat)
                {
                    CheckHideHat(ref showHair, ref showBeard, ref showHat, false);
                    //Log.Error($"2 {pawn.Name.ToStringShort} hideHair:{hideHair}  hideBeard:{hideBeard}  showHat:{showHat}");
                    return;
                }

                showHair = true;
                showBeard = true;

                if (Settings.HideAllHats)
                {
                    showHat = false;
                    //Log.Error($"3 {pawn.Name.ToStringShort} hideHair:{hideHair}  hideBeard:{hideBeard}  showHat:{showHat}");
                    return;
                }

                if (Settings.ShowHatsOnlyWhenDrafted)
                {
                    showHat = isDrafted;
                    //Log.Error($"4.a {pawn.Name.ToStringShort} hideHair:{hideHair}  hideBeard:{hideBeard}  showHat:{showHat}");
                }
                else if (showHat &&
                        (Settings.Indoors == Indoors.HideHats ||
                         (Settings.Indoors == Indoors.ShowHatsWhenDrafted && !isDrafted)))
                {
                    CompCeilingDetect comp = pawn.GetComp<CompCeilingDetect>();
                    if (comp != null && comp.IsIndoors)
                    {
                        showHat = false;
                        showHair = true;
                        showBeard = true;
                        return;
                        //Log.Error($"4.b {pawn.Name.ToStringShort} hideHair:{hideHair}  hideBeard:{hideBeard}  showHat:{showHat}");
                    }
                }

                if (pawn.story?.hairDef != null && Settings.HairToHide.TryGetValue(pawn.story.hairDef, out bool hide) && hide)
                {
                    showHair = false;
                    showBeard = false;
                    showHat = true;
                    //Log.Error($"5 {pawn.Name.ToStringShort} hideHair:{hideHair}  hideBeard:{hideBeard}  showHat:{showHat}");
                    return;
                }

                CheckHideHat(ref showHair, ref showBeard, ref showHat, false);
            }
            finally
            {
                if (showBeard)
                    showBeard = bodyFacing != Rot4.North;
                skipDontShaveHead = !showHat;
            }
            //Log.Error($"Final {pawn.Name.ToStringShort} hideHair:{hideHair}  hideBeard:{hideBeard}  showHat:{showHat}");
        }

        private static void CheckHideHat(ref bool showHair, ref bool showBeard, ref bool showHat, bool draftCheckOnly)
        {
            Apparel apparel;
            for (int j = 0; j < apparelGraphics?.Count; j++)
            {
                apparel = apparelGraphics[j].sourceApparel;
                if (Settings.IsHeadwear(apparel?.def?.apparel))
                {
                    //Log.Error("Last Layer " + def.defName);
                    if (Settings.HatsThatHide.TryGetValue(apparel.def, out var e) && e != HatHideEnum.ShowsHair)
                    {
                        switch (e)
                        {
                            case HatHideEnum.HidesAllHair:
                                if (!draftCheckOnly)
                                {
                                    showHair = false;
                                    showBeard = false;
                                    showHat = true;
                                }
                                break;
                            case HatHideEnum.HidesHairShowBeard:
                                if (!draftCheckOnly)
                                {
                                    showHair = false;
                                    showBeard = true;
                                    showHat = true;
                                }
                                break;
                            case HatHideEnum.HideHat:
                                if (!draftCheckOnly)
                                {
                                    showHair = true;
                                    showBeard = true;
                                    showHat = false;
                                }
                                break;
                            default: // Drafted cases
                                if (pawn.Drafted)
                                {
                                    if (e == HatHideEnum.OnlyDraftHH)
                                    {
                                        showHair = false;
                                        showBeard = false;
                                    }
                                    else if (e == HatHideEnum.OnlyDraftHHSB)
                                    {
                                        showHair = false;
                                        showBeard = true;
                                    }
                                    else
                                    {
                                        showHair = true;
                                    }
                                    showHat = true;
                                }
                                else
                                {
                                    showHair = true;
                                    showHat = true;
                                }
                                break;
                        }
                        //Log.Error($"6 {pawn.Name.ToStringShort} hideHair:{hideHair}  hideBeard:{hideBeard}  showHat:{showHat}");
                        return;
                    }
                }
            }
        }

#if DEBUG && TRANSPILER
        static void printTranspiler(CodeInstruction i, string pre = "")
        {
            Log.Warning("CodeInstruction: " + pre + " opCode: " + i.opcode + " operand: " + i.operand + " labels: " + printLabels(i.ExtractLabels()));
        }

        static string printLabels(IEnumerable<Label> labels)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            if (labels == null)
            {
                sb.Append("<null labels>");
            }
            else
            {
                foreach (Label l in labels)
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(", ");
                    }
                    sb.Append(l);
                }
            }
            if (sb.Length == 0)
            {
                sb.Append("<empty labels>");
            }
            return sb.ToString();
        }
#endif
    }
}