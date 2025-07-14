using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace ShowHair;

[DefOf]
public static class HatConditionFlagDefOf
{
	[UsedImplicitly] public static HatConditionFlagDef? None;
	[UsedImplicitly] public static HatConditionFlagDef? InHomeArea;

	static HatConditionFlagDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(HatConditionFlagDefOf));
	}
}

[DefOf]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class ThingCategoryDefOf
{
	[UsedImplicitly] public static ThingCategoryDef? NGXYZ_HatRoot;
	[UsedImplicitly] public static ThingCategoryDef? NGXYZ_Hat_Other;

	static ThingCategoryDefOf()
	{
		DefOfHelper.EnsureInitializedInCtor(typeof(ThingCategoryDefOf));
	}
}