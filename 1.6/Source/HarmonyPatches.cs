using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ShowHair;

[StaticConstructorOnStartup]
internal class HarmonyPatches
{
	static HarmonyPatches()
	{
		Harmony harmony = new("cat2002.showhair");
		harmony.PatchAll();
	}
}

[HarmonyPatch(typeof(Pawn_DraftController), "set_Drafted")]
internal static class Patch_Pawn_DraftController
{
	internal static void Postfix(Pawn_DraftController __instance)
	{
		Pawn p = __instance.pawn;
		if (!p.IsColonist || p.Dead || !p.def.race.Humanlike) return;
		PortraitsCache.SetDirty(p);
		GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(p);
	}
}

[HarmonyPatch(typeof(PawnRenderTree), "AdjustParms")]
internal static class Patch_PawnRenderTree_AdjustParms
{
	internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
		ILGenerator generator)
	{
		CodeMatcher codeMatcher = new(instructions, generator);

		codeMatcher.MatchStartForward(new CodeMatch(OpCodes.Stloc_1));
		codeMatcher.ThrowIfInvalid("Invalid #1");
		codeMatcher.Advance(1);
		codeMatcher.DefineLabel(out Label labelEnd);
		codeMatcher.Insert(
			CodeInstruction.LoadArgument(0),
			CodeInstruction.LoadLocal(1),
			CodeInstruction.Call(typeof(Patch_PawnRenderTree_AdjustParms), nameof(ShouldDrawApparel)),
			new CodeInstruction(OpCodes.Brfalse, labelEnd)
		);

		codeMatcher.MatchStartForward(
			new CodeMatch(OpCodes.Ldarg_1),
			new CodeMatch(OpCodes.Ldflda,
				AccessTools.Field(typeof(PawnDrawParms), nameof(PawnDrawParms.skipFlags))),
			new CodeMatch(OpCodes.Dup),
			new CodeMatch(OpCodes.Ldind_I8),
			new CodeMatch(OpCodes.Ldsfld,
				AccessTools.Field(typeof(RenderSkipFlagDefOf), nameof(RenderSkipFlagDefOf.Hair)))
		);
		codeMatcher.ThrowIfInvalid("Invalid #2");
		codeMatcher.DefineLabel(out Label label);
		codeMatcher.Insert(
			CodeInstruction.LoadArgument(0),
			CodeInstruction.LoadLocal(1),
			CodeInstruction.Call(typeof(Patch_PawnRenderTree_AdjustParms), nameof(ShouldDrawHair)),
			new CodeInstruction(OpCodes.Brtrue_S, label)
		);
		codeMatcher.MatchStartForward(new CodeMatch(OpCodes.Stind_I8));
		codeMatcher.ThrowIfInvalid("Invalid #3");
		codeMatcher.Advance(1);
		codeMatcher.Labels.Add(label);

		codeMatcher.MatchStartForward(
			new CodeMatch(OpCodes.Ldarg_1),
			new CodeMatch(OpCodes.Ldflda,
				AccessTools.Field(typeof(PawnDrawParms), nameof(PawnDrawParms.skipFlags))),
			new CodeMatch(OpCodes.Dup),
			new CodeMatch(OpCodes.Ldind_I8),
			new CodeMatch(OpCodes.Ldsfld,
				AccessTools.Field(typeof(RenderSkipFlagDefOf), nameof(RenderSkipFlagDefOf.Hair)))
		);
		codeMatcher.ThrowIfInvalid("Invalid #4");
		codeMatcher.DefineLabel(out label);
		codeMatcher.Insert(
			CodeInstruction.LoadArgument(0),
			CodeInstruction.LoadLocal(1),
			CodeInstruction.Call(typeof(Patch_PawnRenderTree_AdjustParms), nameof(ShouldDrawHair)),
			new CodeInstruction(OpCodes.Brtrue_S, label)
		);
		codeMatcher.MatchStartForward(new CodeMatch(OpCodes.Stind_I8));
		codeMatcher.ThrowIfInvalid("Invalid #5");
		codeMatcher.Advance(1);
		codeMatcher.Labels.Add(label);

		codeMatcher.MatchStartForward(
			new CodeMatch(OpCodes.Ldarg_1),
			new CodeMatch(OpCodes.Ldflda,
				AccessTools.Field(typeof(PawnDrawParms), nameof(PawnDrawParms.skipFlags))),
			new CodeMatch(OpCodes.Dup),
			new CodeMatch(OpCodes.Ldind_I8),
			new CodeMatch(OpCodes.Ldsfld,
				AccessTools.Field(typeof(RenderSkipFlagDefOf), nameof(RenderSkipFlagDefOf.Beard)))
		);
		codeMatcher.ThrowIfInvalid("Invalid #6");
		codeMatcher.DefineLabel(out label);
		codeMatcher.Insert(
			CodeInstruction.LoadArgument(0),
			CodeInstruction.LoadLocal(1),
			CodeInstruction.Call(typeof(Patch_PawnRenderTree_AdjustParms), nameof(ShouldDrawBeard)),
			new CodeInstruction(OpCodes.Brtrue_S, label)
		);
		codeMatcher.MatchStartForward(new CodeMatch(OpCodes.Stind_I8));
		codeMatcher.Advance(1);
		codeMatcher.Labels.Add(label);

		codeMatcher.MatchStartForward(
			new CodeMatch(il => il.opcode == OpCodes.Ldloca_S && il.operand is LocalBuilder { LocalIndex: 0 }),
			CodeMatch.Calls(AccessTools.Method(
				typeof(List<Apparel>.Enumerator),
				nameof(List<>.Enumerator.MoveNext)))
		);
		codeMatcher.ThrowIfInvalid("Invalid #6");
		codeMatcher.Labels.Add(labelEnd);
		return codeMatcher.InstructionEnumeration();
	}

	private static bool ShouldDrawApparel(PawnRenderTree tree, Apparel apparel)
	{
		return ShowHairMod.Settings.onlyApplyToColonists && !tree.pawn.Faction.IsPlayerSafe() ||
		       !ShowHairMod.Settings.TryGetPawnHatState(tree.pawn, apparel.def, out HatEnum hatEnum) || hatEnum != HatEnum.HideHat;
	}

	private static bool ShouldDrawHair(PawnRenderTree tree, Apparel apparel)
	{
		return (!ShowHairMod.Settings.onlyApplyToColonists || tree.pawn.Faction.IsPlayerSafe()) &&
		       ShowHairMod.Settings.TryGetPawnHatState(tree.pawn, apparel.def, out HatEnum hatEnum) &&
		       hatEnum is HatEnum.ShowsHair or HatEnum.ShowsHairHidesBeard;
	}

	private static bool ShouldDrawBeard(PawnRenderTree tree, Apparel apparel)
	{
		return (!ShowHairMod.Settings.onlyApplyToColonists || tree.pawn.Faction.IsPlayerSafe()) &&
		       ShowHairMod.Settings.TryGetPawnHatState(tree.pawn, apparel.def, out HatEnum hatEnum) &&
		       hatEnum is HatEnum.ShowsHair or HatEnum.HidesHairShowBeard;
	}
}

[HarmonyPatch(typeof(PawnRenderNodeWorker_Apparel_Head), "CanDrawNow")]
internal static class Patch_PawnRenderNodeWorker_Apparel_Head_CanDrawNow
{
	internal static void Postfix(ref bool __result, PawnRenderNode n)
	{
		if ((!ShowHairMod.Settings.onlyApplyToColonists || n.tree.pawn.Faction.IsPlayerSafe()) && n.apparel != null &&
		    ShowHairMod.Settings.TryGetPawnHatState(n.tree.pawn, n.apparel.def, out HatEnum hatEnum))
			__result = __result && hatEnum != HatEnum.HideHat;
	}
}

[HarmonyPatch(typeof(HairDef), "GraphicFor")]
internal static class Patch_HairDef_GraphicFor
{
	internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		CodeMatcher codeMatcher = new(instructions);
		while (true)
		{
			codeMatcher.MatchStartForward(CodeMatch.Calls(AccessTools.Method(
				typeof(GraphicDatabase),
				nameof(GraphicDatabase.Get),
				[typeof(string), typeof(Shader), typeof(Vector2), typeof(Color)],
				[typeof(Graphic_Multi)])));
			if (!codeMatcher.IsValid) break;
			codeMatcher.RemoveInstruction();
			codeMatcher.Insert(CodeInstruction.Call(typeof(GraphicDatabase),
				nameof(GraphicDatabase.Get),
				[typeof(string), typeof(Shader), typeof(Vector2), typeof(Color)],
				[typeof(Graphic_Multi_Hair)]));
		}

		return codeMatcher.InstructionEnumeration();
	}
}