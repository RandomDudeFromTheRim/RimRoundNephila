using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Nephila
{
    public class WorkGiver_MilkHumanoid : WorkGiver_Scanner
    {
        public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Pawn);

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            List<Pawn> pawns = pawn.Map.mapPawns.SpawnedPawnsInFaction(pawn.Faction);
            for (int i = 0; i < pawns.Count; i++)
            {
                yield return pawns[i];
            }
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Pawn targetPawn = t as Pawn;
            if (targetPawn == null || pawn.Faction != targetPawn.Faction)
            {
                return false;
            }

            Pawn lovePartner = LovePartnerRelationUtility.ExistingLovePartner(pawn);
            if (lovePartner == null || lovePartner != targetPawn || lovePartner.Drafted || (lovePartner.CurJob?.playerForced ?? false))
            {
                return false;
            }

            if (lovePartner.CurJob != null && lovePartner.jobs.curDriver.asleep)
            {
                return false;
            }

            CompNephilaMilkableHumanoid milkComp = lovePartner.TryGetComp<CompNephilaMilkableHumanoid>();
            if (milkComp == null || !milkComp.ActiveAndCanBeMilked)
            {
                return false;
            }

            if (lovePartner.CurJob != null && milkComp.MilkProps.forbiddenJobsToInterrupt.Contains(lovePartner.CurJob.def))
            {
                return false;
            }

            if (lovePartner.Position.IsForbidden(pawn) || !pawn.CanReserve(targetPawn, 1, -1, null, forced) || !pawn.CanReach(targetPawn, PathEndMode.Touch, Danger.Deadly))
            {
                return false;
            }

            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            Pawn targetPawn = t as Pawn;
            if (targetPawn == null)
            {
                return null;
            }

            return pawn.def == NephilaDefOf.Nephila || pawn.def == NephilaDefOf.NephilaHandmaiden || pawn.def == NephilaDefOf.NephilaMatron || pawn.def == NephilaDefOf.NephilaGrandMatron
                ? new Job(NephilaDefOf.NephilaMilkyLover, targetPawn)
                : new Job(NephilaDefOf.QGMilkyLover, targetPawn);
        }
    }
}
