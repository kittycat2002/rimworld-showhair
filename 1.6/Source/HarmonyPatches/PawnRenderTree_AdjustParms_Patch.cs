﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ShowHair.HarmonyPatches;

[HarmonyPatch(typeof(PawnRenderTree), nameof(PawnRenderTree.AdjustParms))]
internal static class PawnRenderTree_AdjustParms_Patch
{
	private static void Prefix(PawnRenderTree __instance)
	{
		CacheEntry cacheEntry = Utils.pawnCache.GetOrAdd(__instance.pawn.thingIDNumber, _ => new CacheEntry());
		if (!cacheEntry.hatStateParms.HasValue)
		{
			__instance.rootNode.requestRecache = true;
			cacheEntry.hatStateParms = __instance.pawn.GetHatStateParms();
			return;
		}

		HatStateParms oldParms = cacheEntry.hatStateParms.Value;
		HatStateParms parms = __instance.pawn.GetHatStateParms();
		if (oldParms.enabled == parms.enabled && oldParms.flags == parms.flags) return;
		__instance.rootNode.requestRecache = true;
		cacheEntry.hatStateParms = parms;
	}

	private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,
		ILGenerator generator)
	{
		CodeMatcher codeMatcher = new(instructions, generator);

		codeMatcher.MatchEndForward(
			CodeMatch.Calls(AccessTools.Method(typeof(PawnRenderNodeWorker_Apparel_Head),
				nameof(PawnRenderNodeWorker_Apparel_Head.HeadgearVisible))),
			new CodeMatch(OpCodes.Brfalse)
		);
		codeMatcher.ThrowIfInvalid("Could not find HeadgearVisible");
		codeMatcher.DeclareLocal(typeof(CacheEntry), out LocalBuilder cacheEntryVariable);
		codeMatcher.DeclareLocal(typeof(HatStateParms), out LocalBuilder hatStateParmsVariable);
		codeMatcher.Advance(1); // Replace with InsertAfter, this is just for Remodder
		codeMatcher.InsertAfterAndAdvance(
			CodeInstruction.LoadField(typeof(Utils), nameof(Utils.pawnCache)),
			CodeInstruction.LoadArgument(0),
			CodeInstruction.LoadField(typeof(PawnRenderTree), nameof(PawnRenderTree.pawn)),
			CodeInstruction.LoadField(typeof(Pawn), nameof(Pawn.thingIDNumber)),
			new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(CacheEntry))),
			new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(ConcurrentDictionary<int, CacheEntry>),
				nameof(ConcurrentDictionary<,>.GetOrAdd), [typeof(int), typeof(CacheEntry)])),
			CodeInstruction.LoadField(typeof(CacheEntry), nameof(CacheEntry.hatStateParms), true),
			CodeInstruction.Call(typeof(HatStateParms?), "GetValueOrDefault"),
			CodeInstruction.StoreLocal(hatStateParmsVariable.LocalIndex)
		);
		codeMatcher.MatchStartForward(new CodeMatch(OpCodes.Stloc_1));
		codeMatcher.ThrowIfInvalid("Could not find start of loop");
		codeMatcher.DeclareLocal(typeof(HatEnum), out LocalBuilder hatEnumVariable);
		codeMatcher.DefineLabel(out Label labelEnd);
		codeMatcher.InsertAfterAndAdvance(
			new CodeInstruction(OpCodes.Call,
				AccessTools.PropertyGetter(typeof(ShowHairMod), nameof(ShowHairMod.Settings))),
			CodeInstruction.LoadLocal(hatStateParmsVariable.LocalIndex),
			CodeInstruction.LoadField(typeof(HatStateParms), nameof(HatStateParms.flags)),
			CodeInstruction.LoadLocal(1),
			CodeInstruction.LoadField(typeof(Apparel), nameof(Apparel.def)),
			CodeInstruction.Call(typeof(Settings), nameof(Settings.GetHatState)),
			CodeInstruction.StoreLocal(hatEnumVariable.LocalIndex),
			CodeInstruction.LoadLocal(hatStateParmsVariable.LocalIndex),
			CodeInstruction.LoadLocal(hatEnumVariable.LocalIndex),
			CodeInstruction.Call(typeof(PawnRenderTree_AdjustParms_Patch), nameof(ShouldDrawApparel)),
			new CodeInstruction(OpCodes.Brfalse, labelEnd)
		);

		codeMatcher.MatchStartForward(
			new CodeMatch(OpCodes.Ldloc_1),
			CodeMatch.LoadsField(AccessTools.Field(typeof(Thing), nameof(Thing.def))),
			CodeMatch.LoadsField(AccessTools.Field(typeof(ThingDef), nameof(ThingDef.apparel))),
			CodeMatch.LoadsField(AccessTools.Field(typeof(ApparelProperties),
				nameof(ApparelProperties.bodyPartGroups))),
			CodeMatch.LoadsField(AccessTools.Field(typeof(BodyPartGroupDefOf), nameof(BodyPartGroupDefOf.UpperHead)))
		);
		codeMatcher.ThrowIfInvalid("Could not find BodyPartGroupDefOf.UpperHead");
		codeMatcher.DefineLabel(out Label label);
		codeMatcher.InsertAndAdvance(
			CodeInstruction.LoadArgument(1).WithLabels(codeMatcher.Instruction.ExtractLabels()),
			CodeInstruction.LoadField(typeof(PawnDrawParms), nameof(PawnDrawParms.skipFlags)),
			CodeInstruction.LoadField(typeof(RenderSkipFlagDefOf), nameof(RenderSkipFlagDefOf.Hair)),
			CodeInstruction.Call(typeof(FlagDefUtility), nameof(FlagDefUtility.HasFlag)),
			new CodeInstruction(OpCodes.Brtrue_S, label),
			CodeInstruction.LoadLocal(hatStateParmsVariable.LocalIndex),
			CodeInstruction.LoadLocal(hatEnumVariable.LocalIndex),
			CodeInstruction.Call(typeof(PawnRenderTree_AdjustParms_Patch), nameof(ShouldDrawHair)),
			new CodeInstruction(OpCodes.Brtrue_S, label)
		);
		codeMatcher.MatchStartForward(
			new CodeMatch(OpCodes.Stind_I8)
		);
		codeMatcher.ThrowIfInvalid("Could not find end of BodyPartGroupDefOf.UpperHead");
		codeMatcher.Advance(1);
		codeMatcher.AddLabels([label]);

		codeMatcher.MatchStartForward(
			new CodeMatch(OpCodes.Ldarg_1),
			new CodeMatch(OpCodes.Ldflda,
				AccessTools.Field(typeof(PawnDrawParms), nameof(PawnDrawParms.skipFlags))),
			new CodeMatch(OpCodes.Dup),
			new CodeMatch(OpCodes.Ldind_I8),
			new CodeMatch(OpCodes.Ldsfld,
				AccessTools.Field(typeof(RenderSkipFlagDefOf), nameof(RenderSkipFlagDefOf.Hair)))
		);
		codeMatcher.ThrowIfInvalid("Could not find start of RenderSkipFlagDefOf.Hair");
		codeMatcher.DefineLabel(out label);
		codeMatcher.Insert(
			CodeInstruction.LoadArgument(1),
			CodeInstruction.LoadField(typeof(PawnDrawParms), nameof(PawnDrawParms.skipFlags)),
			CodeInstruction.LoadField(typeof(RenderSkipFlagDefOf), nameof(RenderSkipFlagDefOf.Hair)),
			CodeInstruction.Call(typeof(FlagDefUtility), nameof(FlagDefUtility.HasFlag)),
			new CodeInstruction(OpCodes.Brtrue_S, label),
			CodeInstruction.LoadLocal(hatStateParmsVariable.LocalIndex)
				.WithLabels(codeMatcher.Instruction.ExtractLabels()),
			CodeInstruction.LoadLocal(hatEnumVariable.LocalIndex),
			CodeInstruction.Call(typeof(PawnRenderTree_AdjustParms_Patch), nameof(ShouldDrawHair)),
			new CodeInstruction(OpCodes.Brtrue_S, label)
		);
		codeMatcher.MatchStartForward(new CodeMatch(OpCodes.Stind_I8));
		codeMatcher.ThrowIfInvalid("Could not find end of RenderSkipFlagDefOf.Hair");
		codeMatcher.Advance(1);
		codeMatcher.Labels.Add(label);
		codeMatcher.Advance(-1);

		codeMatcher.MatchStartForward(
			new CodeMatch(OpCodes.Ldarg_1),
			new CodeMatch(OpCodes.Ldflda,
				AccessTools.Field(typeof(PawnDrawParms), nameof(PawnDrawParms.skipFlags))),
			new CodeMatch(OpCodes.Dup),
			new CodeMatch(OpCodes.Ldind_I8),
			new CodeMatch(OpCodes.Ldsfld,
				AccessTools.Field(typeof(RenderSkipFlagDefOf), nameof(RenderSkipFlagDefOf.Beard)))
		);
		codeMatcher.ThrowIfInvalid("Could not find start of RenderSkipFlagDefOf.Beard");
		codeMatcher.DefineLabel(out label);
		codeMatcher.Insert(
			CodeInstruction.LoadArgument(1).WithLabels(codeMatcher.Instruction.ExtractLabels()),
			CodeInstruction.LoadField(typeof(PawnDrawParms), nameof(PawnDrawParms.skipFlags)),
			CodeInstruction.LoadField(typeof(RenderSkipFlagDefOf), nameof(RenderSkipFlagDefOf.Beard)),
			CodeInstruction.Call(typeof(FlagDefUtility), nameof(FlagDefUtility.HasFlag)),
			new CodeInstruction(OpCodes.Brtrue_S, label),
			CodeInstruction.LoadLocal(hatStateParmsVariable.LocalIndex)
				.WithLabels(codeMatcher.Instruction.ExtractLabels()),
			CodeInstruction.LoadLocal(hatEnumVariable.LocalIndex),
			CodeInstruction.Call(typeof(PawnRenderTree_AdjustParms_Patch), nameof(ShouldDrawBeard)),
			new CodeInstruction(OpCodes.Brtrue_S, label)
		);
		codeMatcher.MatchStartForward(new CodeMatch(OpCodes.Stind_I8));
		codeMatcher.ThrowIfInvalid("Could not find end of RenderSkipFlagDefOf.Beard");
		codeMatcher.Advance(1);
		codeMatcher.Labels.Add(label);

		codeMatcher.MatchStartForward(
			new CodeMatch(il => il.opcode == OpCodes.Ldloca_S && il.operand is LocalBuilder { LocalIndex: 0 }),
			CodeMatch.Calls(AccessTools.Method(
				typeof(List<Apparel>.Enumerator),
				nameof(List<>.Enumerator.MoveNext)))
		);
		codeMatcher.ThrowIfInvalid("Could not find end of loop");
		codeMatcher.Labels.Add(labelEnd);

		return codeMatcher.InstructionEnumeration();
	}

	private static bool ShouldDrawApparel(HatStateParms parms, HatEnum hatEnum)
	{
		return !parms.enabled ||
		       hatEnum > HatEnum.HideHat;
	}

	private static bool ShouldDrawHair(HatStateParms parms, HatEnum hatEnum)
	{
		return parms.enabled &&
		       hatEnum is HatEnum.ShowsHair or HatEnum.ShowsHairHidesBeard;
	}

	private static bool ShouldDrawBeard(HatStateParms parms, HatEnum hatEnum)
	{
		return parms.enabled &&
		       hatEnum is HatEnum.ShowsHair or HatEnum.HidesHairShowsBeard;
	}
}