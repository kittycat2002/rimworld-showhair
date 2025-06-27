using JetBrains.Annotations;
using RimWorld;
using Verse;

namespace ShowHair;

[UsedImplicitly]
internal class CompCeilingDetect : ThingComp
{
	internal bool? isIndoors;
	
	public override void CompTickInterval(int delta)
	{
		if (!parent.IsHashIntervalTick(250, delta)) return;
		Pawn pawn = (parent as Pawn)!;
		if ((ShowHairMod.Settings.onlyApplyToColonists && !pawn.Faction.IsPlayerSafe()) || pawn.Map == null ||
		    !pawn.RaceProps.Humanlike || pawn.Dead) return;
		if (isIndoors == null)
		{
			isIndoors = DetermineIsIndoors(pawn);
			PortraitsCache.SetDirty(pawn);
			GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(pawn);
			return;
		}

		bool orig = isIndoors.Value;
		isIndoors = DetermineIsIndoors(pawn);
		if (orig == isIndoors.Value) return;
		PortraitsCache.SetDirty(pawn);
		GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(pawn);
	}

	private static bool DetermineIsIndoors(Pawn pawn)
	{
		return pawn.GetRoom().OpenRoofCountStopAt(1) == 0;
	}
}