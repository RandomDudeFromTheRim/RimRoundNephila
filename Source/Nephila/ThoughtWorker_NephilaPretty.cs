using RimWorld;
using Verse;

namespace Nephila
{
    public class ThoughtWorker_NephilaPretty : ThoughtWorker
    {
        protected override ThoughtState CurrentSocialStateInternal(Pawn pawn, Pawn other)
        {
            if (!other.RaceProps.Humanlike || !RelationsUtility.PawnsKnowEachOther(pawn, other))
            {
                return false;
            }
            if (RelationsUtility.IsDisfigured(other))
            {
                return false;
            }
            if (!pawn.health.capacities.CapableOf(PawnCapacityDefOf.Sight))
            {
                return false;
            }
            int num = other.story.traits.DegreeOfTrait(TraitDefOf.ViolenceInhibition);
            if (num == 1)
            {
                return ThoughtState.ActiveAtStage(0);
            }
            if (num == 2)
            {
                return ThoughtState.ActiveAtStage(1);
            }
            if (num == 3)
            {
                return ThoughtState.ActiveAtStage(2);
            }
            return false;
        }
    }

}
