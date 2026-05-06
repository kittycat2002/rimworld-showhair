using System;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace ShowHair;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
public class HatConditionFlagDef : Def
{
	[UsedImplicitly(ImplicitUseKindFlags.Assign)] public string checkedDescription = "";
	[UsedImplicitly(ImplicitUseKindFlags.Assign)] public string uncheckedDescription = "";
	[UsedImplicitly(ImplicitUseKindFlags.Assign)] public Type workerClass = null!;
	
	[Unsaved] private ulong mask;
	
	public override void PostSetIndices()
	{
		FlagDefUtility.SetMaskFromIndex(this, ref mask);
	}

	public static implicit operator ulong(HatConditionFlagDef? def) => def?.mask ?? 0UL;

	public override string ToString()
	{
		return $"{base.ToString()} ({mask})";
	}

	public HatConditionWorker Worker => field ??= (HatConditionWorker)Activator.CreateInstance(workerClass ?? throw new NullReferenceException($"Null workerClass in HatConditionFlagDef: {defName}"));
}