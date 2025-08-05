using LudeonTK;
using RimWorld;
using UnityEngine;
using Verse;

namespace ShowHair;

public static class DebugActions
{
	[DebugOutput("Show Hair With Hats")]
	private static void DontShaveHair()
	{
		TableDataGetter<HairDef>[] table = new TableDataGetter<HairDef>[11];
		table[0] = new TableDataGetter<HairDef>("defName", def => def.defName);
		table[1] = new TableDataGetter<HairDef>("label", def => def.label);
		table[2] = new TableDataGetter<HairDef>("texPath", def => def.texPath);
		table[3] = new TableDataGetter<HairDef>("hasUpperNorth",
			def => (ContentFinder<Texture2D>.Get($"{def.texPath}_upper_north", false) != null).ToStringCheckBlank());
		table[4] = new TableDataGetter<HairDef>("hasUpperEast",
			def => (ContentFinder<Texture2D>.Get($"{def.texPath}_upper_east", false) != null).ToStringCheckBlank());
		table[5] = new TableDataGetter<HairDef>("hasUpperSouth",
			def => (ContentFinder<Texture2D>.Get($"{def.texPath}_upper_south", false) != null).ToStringCheckBlank());
		table[6] = new TableDataGetter<HairDef>("hasUpperWest",
			def => (ContentFinder<Texture2D>.Get($"{def.texPath}_upper_west", false) != null).ToStringCheckBlank());
		table[7] = new TableDataGetter<HairDef>("hasFullNorth",
				def => (ContentFinder<Texture2D>.Get($"{def.texPath}_upper_north", false) != null).ToStringCheckBlank());
		table[8] = new TableDataGetter<HairDef>("hasFullEast",
			def => (ContentFinder<Texture2D>.Get($"{def.texPath}_upper_north", false) != null).ToStringCheckBlank());
		table[9] = new TableDataGetter<HairDef>("hasFullSouth",
			def => (ContentFinder<Texture2D>.Get($"{def.texPath}_upper_south", false) != null).ToStringCheckBlank());
		table[10] = new TableDataGetter<HairDef>("hasFullWest",
			def => (ContentFinder<Texture2D>.Get($"{def.texPath}_upper_west", false) != null).ToStringCheckBlank());
		DebugTables.MakeTablesDialog(DefDatabase<HairDef>.AllDefs, table);
	}
}