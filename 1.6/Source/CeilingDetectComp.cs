using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace ShowHair;

[UsedImplicitly]
internal class CompCeilingDetect : ThingComp
{
	internal bool isIndoors;
	internal bool isInHomeArea;
	internal bool isInVacuum;
	
	public override void CompTickInterval(int delta)
	{
		if (!parent.IsHashIntervalTick(250, delta)) return;
		Pawn pawn = (Pawn)parent;
		if (!pawn.RaceProps.Humanlike || (ShowHairMod.Settings.onlyApplyToColonists && !pawn.Faction.IsPlayerSafe()) || !pawn.Spawned) return;

		isIndoors = DetermineIsIndoors(pawn);
		isInHomeArea = DetermineIsInHomeArea(pawn);
		if (ModsConfig.OdysseyActive) isInVacuum = DetermineIsInVacuum(pawn);

	}

	private static bool DetermineIsIndoors(Pawn pawn)
	{
		return pawn.GetRoom().OpenRoofCountStopAt(1) == 0;
	}
	private static bool DetermineIsInHomeArea(Pawn pawn)
	{
		return pawn.MapHeld?.areaManager.Home[pawn.PositionHeld] ?? false;
	}
	private static bool DetermineIsInVacuum(Pawn pawn)
	{
		return pawn.PositionHeld.GetVacuum(pawn.MapHeld) > 0;
	}
}