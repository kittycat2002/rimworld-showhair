using RimWorld;
using Verse;

namespace ShowHair;

public abstract class HatConditionWorker
{
	public abstract bool ConditionIsMet(Pawn pawn);
}

public class HatConditionWorkerInBed : HatConditionWorker
{
	public override bool ConditionIsMet(Pawn pawn) => pawn.InBed();
}

public class HatConditionWorkerDrafted : HatConditionWorker
{
	public override bool ConditionIsMet(Pawn pawn) => pawn.Drafted;
}

public abstract class HatConditionWorkerCached : HatConditionWorker
{
	protected virtual int CacheExpirationTime => 250;

	public sealed override bool ConditionIsMet(Pawn pawn)
	{
		if (!pawn.SpawnedOrAnyParentSpawned || (ShowHairMod.Settings.onlyApplyToColonists && !pawn.Faction.IsPlayerSafe())) return false;
		CacheEntry cacheEntry = Utils.pawnCache.GetOrAdd(pawn.thingIDNumber, new CacheEntry());
		if (cacheEntry.conditionWorkers.TryGetValue(GetType(), out (int, bool) condition))
		{
			if (condition.Item1 > GenTicks.TicksGame)
			{
				return condition.Item2;
			}
		}
		bool conditionIsMet = ConditionIsMetCached(pawn);
		cacheEntry.conditionWorkers[GetType()] = (GenTicks.TicksGame + CacheExpirationTime, conditionIsMet);
		return conditionIsMet;
	}

	protected abstract bool ConditionIsMetCached(Pawn pawn);
}

public class HatConditionWorkerIndoors : HatConditionWorkerCached
{
	protected override bool ConditionIsMetCached(Pawn pawn) => pawn.GetRoom()?.OpenRoofCountStopAt(1) == 0;
}

public class HatConditionWorkerInHomeArea : HatConditionWorkerCached
{
	protected override bool ConditionIsMetCached(Pawn pawn) => pawn.MapHeld?.areaManager.Home[pawn.PositionHeld] ?? false;
}

public class HatConditionWorkerMeditating : HatConditionWorkerCached
{
	protected override bool ConditionIsMetCached(Pawn pawn) => pawn.psychicEntropy?.IsCurrentlyMeditating ?? false;
}

public class HatConditionWorkerIsInVacuum : HatConditionWorkerCached
{
	protected override bool ConditionIsMetCached(Pawn pawn) => pawn.GetRoom()?.Vacuum > 0f;
}