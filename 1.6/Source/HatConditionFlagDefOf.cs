using JetBrains.Annotations;
using RimWorld;

namespace ShowHair;

[DefOf]
public static class HatConditionFlagDefOf
{
	[UsedImplicitly] public static HatConditionFlagDef? None;
	
	static HatConditionFlagDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(HatConditionFlagDefOf));
	}
}