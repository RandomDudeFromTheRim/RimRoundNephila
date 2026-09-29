using Verse;
using Verse.AI;

namespace Nephila
{
    /// <summary>A colonist Nephila gathers her own clutch once it is ready.</summary>
    public class JobGiver_NephilaMilkSelf : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.AnimalOrWildMan() || !pawn.IsColonist || pawn.Drafted || pawn.Downed)
                return null;
            CompNephilaMilkableHumanoid comp = pawn.TryGetComp<CompNephilaMilkableHumanoid>();
            if (comp == null || !comp.ActiveAndCanBeMilked || !comp.MilkProps.canMilkThemselves)
                return null;
            // the queen's guard gathers differently from the other castes
            bool broodCaste = pawn.def == NephilaDefOf.Nephila || pawn.def == NephilaDefOf.NephilaHandmaiden
                || pawn.def == NephilaDefOf.NephilaMatron || pawn.def == NephilaDefOf.NephilaGrandMatron;
            return JobMaker.MakeJob(broodCaste ? NephilaDefOf.NephilaMilkySelf : NephilaDefOf.QGMilkySelf);
        }
    }
}
