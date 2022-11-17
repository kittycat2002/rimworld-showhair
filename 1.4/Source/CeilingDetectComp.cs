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
            Pawn pawn = base.parent as Pawn;
            Map map = pawn?.Map;

            if (!Settings.CheckIndoors || Settings.OnlyApplyToColonists && pawn.Faction?.IsPlayer == false)
                return;

            if (map != null && pawn.RaceProps?.Humanlike == true && !pawn.Dead)
            {
                if (isIndoors == null)
                {
                    isIndoors = DetermineIsIndoors(pawn, map);
                    PortraitsCache.SetDirty(pawn);
                    GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(pawn);
                    return;
                }

                bool orig = isIndoors.Value;
                this.isIndoors = DetermineIsIndoors(pawn, map);
                if (orig != isIndoors.Value)
                {
                    PortraitsCache.SetDirty(pawn);
                    GlobalTextureAtlasManager.TryMarkPawnFrameSetDirty(pawn);
                }
            }
        }

        private bool DetermineIsIndoors(Pawn pawn, Map map)
        {
            var room = pawn.GetRoom();
            return room != null && room.OpenRoofCount == 0;
        }
    }
}
