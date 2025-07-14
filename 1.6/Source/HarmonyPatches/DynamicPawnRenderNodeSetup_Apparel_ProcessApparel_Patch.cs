using System;
using System.Collections.Generic;
using HarmonyLib;
using Verse;

namespace ShowHair.HarmonyPatches;

[HarmonyPatch(typeof(DynamicPawnRenderNodeSetup_Apparel), nameof(DynamicPawnRenderNodeSetup_Apparel.ProcessApparel))]
internal static class DynamicPawnRenderNodeSetup_Apparel_ProcessApparel_Patch
{
	private static IEnumerable<ValueTuple<PawnRenderNode?, PawnRenderNode>> Postfix(
		IEnumerable<ValueTuple<PawnRenderNode?, PawnRenderNode>> nodes)
	{
		foreach ((PawnRenderNode?, PawnRenderNode) node in nodes)
		{
			if (node.Item1?.Worker is PawnRenderNodeWorker_Apparel_Head)
			{
				node.Item1.Props.subworkerClasses ??= [];
				node.Item1.Props.subworkerClasses.Add(typeof(PawnRenderSubWorkerHat));
			}

			yield return node;
		}
	}
}