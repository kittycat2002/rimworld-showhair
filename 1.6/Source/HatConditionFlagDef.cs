using System;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace ShowHair;

public class HatConditionFlagDef : Def
{
	public override void PostSetIndices()
	{
		FlagDefUtility.SetMaskFromIndex(this, ref mask);
	}

	public static implicit operator ulong(HatConditionFlagDef? def) => def?.mask ?? 0UL;

	public override string ToString()
	{
		return $"{base.ToString()} ({mask})";
	}

	private ulong mask;

	[UsedImplicitly] public Type? workerClass;
	
	private HatConditionWorker? workerInt;
	
	public HatConditionWorker Worker => workerInt ??= (HatConditionWorker)Activator.CreateInstance(workerClass!);
}