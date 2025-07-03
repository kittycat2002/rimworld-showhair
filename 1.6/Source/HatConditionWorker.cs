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

public class HatConditionWorkerIndoors : HatConditionWorker
{
	public override bool ConditionIsMet(Pawn pawn) => pawn.TryGetComp(out CompCeilingDetect comp) && comp.isIndoors;
}

public class HatConditionWorkerInHomeArea : HatConditionWorker
{
	public override bool ConditionIsMet(Pawn pawn) => pawn.TryGetComp(out CompCeilingDetect comp) && comp.isInHomeArea;
}

public class HatConditionWorkerMeditating : HatConditionWorker
{
	public override bool ConditionIsMet(Pawn pawn) => pawn.psychicEntropy.IsCurrentlyMeditating;
}

public class HatConditionWorkerIsInVacuum : HatConditionWorker
{
	public override bool ConditionIsMet(Pawn pawn) => pawn.TryGetComp(out CompCeilingDetect comp) && comp.isInVacuum;
}