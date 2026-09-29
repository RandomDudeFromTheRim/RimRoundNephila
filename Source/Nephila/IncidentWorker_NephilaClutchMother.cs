using RimWorld;
using Verse;

namespace Nephila
{
    public class IncidentWorker_NephilaClutchMother : IncidentWorker_MakeGameCondition
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            return map.mapTemperature.SeasonAndOutdoorTemperatureAcceptableFor(ThingDef.Named("Nephila_ChonkersCherub"));
        }

        private bool TryFindEntryCell(Map map, out IntVec3 cell)
        {
            return RCellFinder.TryFindRandomPawnEntryCell(out cell, map, CellFinder.EdgeRoadChance_Animal + 0.2f, false, null);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            PawnKindDef kindDef = PawnKindDef.Named("Nephila_ChonkersCherub");
            if (!TryFindEntryCell(map, out IntVec3 entryCell))
            {
                return false;
            }

            IntVec3 loc = CellFinder.RandomClosewalkCellNear(entryCell, map, 1, null);
            Pawn pawn = PawnGenerator.GeneratePawn(kindDef, null);
            pawn.gender = Gender.Female;
            GenSpawn.Spawn(pawn, loc, map, WipeMode.Vanish);

            Find.LetterStack.ReceiveLetter(
                "LetterLabelNephila_ChonkersCherub".Translate(),
                "Nephila_ChonkersCherub".Translate(),
                LetterDefOf.ThreatBig,
                pawn,
                null,
                null,
                null,
                null
            );
            return true;
        }
    }
}
