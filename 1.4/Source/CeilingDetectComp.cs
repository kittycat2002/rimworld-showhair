using RimWorld;
using Verse;

namespace ShowHair
{
    class CompCeilingDetect : ThingComp
    {
        public bool? isIndoors = null;

        public bool IsIndoors
        {
            get
            {
                if (isIndoors != null)
                    return isIndoors.Value;
                return false;
            }
        }

        public override void CompTickRare()
        {
            Pawn pawn = parent as Pawn;

            if (Settings.CheckIndoors && !(Settings.OnlyApplyToColonists && pawn.Faction.IsPlayerSafe()) && pawn?.Map != null && pawn.RaceProps?.Humanlike == true && !pawn.Dead)
            {
                if (isIndoors == null)
                {
                    isIndoors = DetermineIsIndoors(pawn);
                    PortraitsCache.SetDirty(pawn);
                    GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(pawn);
                    return;
                }

                bool orig = isIndoors.Value;
                isIndoors = DetermineIsIndoors(pawn);
                if (orig != isIndoors.Value)
                {
                    PortraitsCache.SetDirty(pawn);
                    GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(pawn);
                }
            }
        }

        private bool DetermineIsIndoors(Pawn pawn)
        {
            var room = pawn.GetRoom();
            return room != null && room.OpenRoofCount == 0;
        }
    }
}
