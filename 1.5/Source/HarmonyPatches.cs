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
        }
    }

    [HarmonyPatch(typeof(SavedGameLoaderNow), "LoadGameFromSaveFileNow")]
    static class Patch_SavedGameLoader_LoadGameFromSaveFileNow
    {
        static void Postfix()
        {
            Settings.Initialize();

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
    [HarmonyPatch(typeof(PawnRenderTree), "AdjustParms")]
    static class Patch_PawnRenderTree_AdjustParms
    {
        private static readonly MethodInfo ApparelMoveNext = AccessTools.Method(
            typeof(List<Apparel>.Enumerator),
            nameof(List<Apparel>.Enumerator.MoveNext));
        private static readonly MethodInfo HairMethod = AccessTools.Method(
            typeof(Patch_PawnRenderTree_AdjustParms),
            nameof(ShouldDrawHair));
        private static readonly MethodInfo BeardMethod = AccessTools.Method(
            typeof(Patch_PawnRenderTree_AdjustParms),
            nameof(ShouldDrawBeard));
        private static readonly FieldInfo HairField = AccessTools.Field(
            typeof(RenderSkipFlagDefOf),
            nameof(RenderSkipFlagDefOf.Hair));
        private static readonly FieldInfo BeardField = AccessTools.Field(
            typeof(RenderSkipFlagDefOf),
            nameof(RenderSkipFlagDefOf.Beard));
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            Label? labelEnd = null;
            Label? label = null;
            List<CodeInstruction> il = instructions.ToList();
            Dictionary<int, (Label, MethodInfo)> branches = new Dictionary<int, (Label, MethodInfo)>();
            int lastLoad = 0;
            for (int i = 0; i < il.Count; ++i)
            {
                if (labelEnd is null)
                {
                    if (il[i].opcode == OpCodes.Ldarg_1)
                    {
                        lastLoad = i;
                    }
                    if (il[i].Is(OpCodes.Call, ApparelMoveNext))
                    {
                        labelEnd = il[i - 1].labels[0];
                        break;
                    }
                    else if (il[i].Is(OpCodes.Ldsfld, HairField))
                    {
                        label = generator.DefineLabel();
                        branches.Add(lastLoad, (label.Value, HairMethod));
                    }
                    else if (il[i].Is(OpCodes.Ldsfld, BeardField))
                    {
                        label = generator.DefineLabel();
                        branches.Add(lastLoad, (label.Value, BeardMethod));
                    }
                    else if (label != null && il[i - 1].opcode == OpCodes.Stind_I8)
                    {
                        il[i].labels.Add(label.Value);
                        label = null;
                    }
                }
            }
            if (labelEnd is null)
            {
                Log.Error("ShowHair: Could not find end of foreach loop in Patch_PawnRenderTree_AdjustParms, please submit a log to the developer.");
                yield break;
            }
            for (int i = 0; i < il.Count; ++i)
            {
                if (il[i].opcode == OpCodes.Stloc_1)
                {
                    yield return il[i];
                    yield return CodeInstruction.LoadArgument(0);
                    yield return CodeInstruction.LoadLocal(1);
                    yield return CodeInstruction.Call(typeof(Patch_PawnRenderTree_AdjustParms), nameof(ShouldDrawApparel));
                    yield return new CodeInstruction(OpCodes.Brfalse, labelEnd);
                    i++;
                }
                else if (branches.TryGetValue(i, out var tuple))
                {
                    yield return CodeInstruction.LoadArgument(0);
                    yield return CodeInstruction.LoadLocal(1);
                    yield return new CodeInstruction(OpCodes.Call, tuple.Item2);
                    yield return new CodeInstruction(OpCodes.Brtrue, tuple.Item1);
                }
                yield return il[i];
            }
        }

        private static bool ShouldDrawApparel(PawnRenderTree tree, Apparel apparel)
        {
            return Settings.OnlyApplyToColonists && !tree.pawn.Faction.IsPlayerSafe() || !Settings.TryGetPawnHatState(tree.pawn, apparel.def, out var hatEnum) || hatEnum != HatEnum.HideHat;
        }
        private static bool ShouldDrawHair(PawnRenderTree tree, Apparel apparel)
        {
            return (!Settings.OnlyApplyToColonists || tree.pawn.Faction.IsPlayerSafe()) && Settings.TryGetPawnHatState(tree.pawn, apparel.def, out var hatEnum) && (hatEnum == HatEnum.ShowsHair || hatEnum == HatEnum.ShowsHairHidesBeard);
        }
        private static bool ShouldDrawBeard(PawnRenderTree tree, Apparel apparel)
        {
            return (!Settings.OnlyApplyToColonists || tree.pawn.Faction.IsPlayerSafe()) && Settings.TryGetPawnHatState(tree.pawn, apparel.def, out var hatEnum) && (hatEnum == HatEnum.ShowsHair || hatEnum == HatEnum.HidesHairShowBeard);
        }
    }
    [HarmonyPatch(typeof(PawnRenderNodeWorker_Apparel_Head), "CanDrawNow")]
    static class Patch_PawnRenderNodeWorker_Apparel_Head_CanDrawNow
    {
        static void Postfix(ref bool __result, PawnRenderNode n)
        {
            if (!Settings.OnlyApplyToColonists || n.tree.pawn.Faction.IsPlayerSafe())
                __result = __result && Settings.TryGetPawnHatState(n.tree.pawn, n.apparel.def, out var hatEnum) && hatEnum != HatEnum.HideHat;
        }
    }

    [HarmonyPatch(typeof(HairDef), "GraphicFor")]
    static class Patch_HairDef_GraphicFor
    {
        private static readonly MethodInfo GraphicMultiMethod = AccessTools.Method(
            typeof(GraphicDatabase),
            nameof(GraphicDatabase.Get),
            new Type[] { typeof(string), typeof(Shader), typeof(Vector2), typeof(Color) },
            new Type[] { typeof(Graphic_Multi) });
        private static readonly MethodInfo GraphicMultiHairMethod = AccessTools.Method(
            typeof(GraphicDatabase),
            nameof(GraphicDatabase.Get),
            new Type[] { typeof(string), typeof(Shader), typeof(Vector2), typeof(Color) },
            new Type[] { typeof(Graphic_Multi_Hair) });
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.Is(OpCodes.Call, GraphicMultiMethod))
                {
                    yield return new CodeInstruction(OpCodes.Call, GraphicMultiHairMethod);
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }
}