using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace ShowHair;

public static class ThingCategoryDefGenerator_Hats
{
	private static readonly Dictionary<ThingCategoryDef, ThingCategoryDef> createdDefs = [];

	public static IEnumerable<ThingCategoryDef> ImpliedThingCategoryDefs(bool hotReload = false)
	{
		createdDefs.Clear();
		foreach (ThingDef thingDef in DefDatabase<ThingDef>.AllDefs.Where(def => def.apparel.IsHeadwear()))
		{
			thingDef.thingCategories ??= [];
			if (thingDef.thingCategories.Any(def => def.Parents.Contains(RimWorld.ThingCategoryDefOf.Root)))
			{
				foreach (ThingCategoryDef thingDefCategory in thingDef.thingCategories.Where(thingCategoryDef =>
					         !thingCategoryDef.defName.StartsWith("NGXYZ_Hat_") &&
					         thingCategoryDef.Parents.Contains(RimWorld.ThingCategoryDefOf.Root)).ToList())
				{
					thingDef.thingCategories.Add(BaseThingCategoryDef(thingDefCategory, hotReload));
				}
			}
			else
			{
				thingDef.thingCategories.Add(ThingCategoryDefOf.NGXYZ_Hat_Other);
			}
		}

		foreach (ThingCategoryDef thingCategoryDef in createdDefs.Values)
		{
			yield return thingCategoryDef;
		}
	}

	private static ThingCategoryDef BaseThingCategoryDef(ThingCategoryDef baseDef, bool hotReload = false)
	{
		if (createdDefs.TryGetValue(baseDef, out ThingCategoryDef existingDef))
		{
			return existingDef;
		}

		string defName = $"NGXYZ_Hat_{baseDef.defName}";
		ThingCategoryDef thingCategoryDef = hotReload
			? DefDatabase<ThingCategoryDef>.GetNamed(defName, false) ?? new ThingCategoryDef()
			: new ThingCategoryDef();
		thingCategoryDef.defName = defName;
		thingCategoryDef.label = $"{baseDef.label}";
		thingCategoryDef.parent = baseDef.parent == RimWorld.ThingCategoryDefOf.Root || baseDef.parent is null
			? ThingCategoryDefOf.NGXYZ_HatRoot
			: BaseThingCategoryDef(baseDef.parent);
		createdDefs.Add(baseDef, thingCategoryDef);
		return thingCategoryDef;
	}
}