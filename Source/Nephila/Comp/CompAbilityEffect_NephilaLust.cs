using RimWorld;
using Verse;

namespace Nephila
{
    /// <summary>The call of lust: a lust-driven wander, so it only takes hold of grown people.</summary>
    public class CompAbilityEffect_NephilaLust : CompAbilityEffect_GiveMentalState
    {
        public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
        {
            if (target.Pawn is Pawn p && p.RaceProps.Humanlike && !NephilaUtility.IsAdult(p))
            {
                if (throwMessages)
                    Messages.Message("Nephila_AdultsOnly".Translate(p.Named("PAWN")), p, MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }
            return base.Valid(target, throwMessages);
        }
    }
}
