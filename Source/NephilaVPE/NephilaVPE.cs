using System.Linq;
using RimWorld;
using RimWorld.Planet;
using VanillaPsycastsExpanded;
using Verse;
using Ability = VEF.Abilities.Ability;

namespace Nephila.VPE
{
    /// <summary>
    /// The Nephilim path: the mechanites carry these powers, so only a Nephila - or someone
    /// fitted with a nephilim psy amp - can put points into it. Psytrainers still teach
    /// anyone their one psycast, as VPE's do.
    /// </summary>
    public class PsycasterPathDef_Nephilim : PsycasterPathDef
    {
        static HediffDef psyAmp;

        public override bool CanPawnUnlock(Pawn pawn)
        {
            if (!base.CanPawnUnlock(pawn))
                return false;
            if (NephilaUtility.IsNephila(pawn))
                return true;
            psyAmp ??= DefDatabase<HediffDef>.GetNamedSilentFail("Nephila_PsyAmp");
            return psyAmp != null && (pawn.health?.hediffSet?.HasHediff(psyAmp) ?? false);
        }
    }

    /// <summary>The call of lust under VPE: a lust-driven wander, so only on grown people.</summary>
    public class AbilityExtension_NephilaLust : AbilityExtension_GiveMentalState
    {
        public override bool ValidateTarget(LocalTargetInfo target, Ability ability, bool throwMessages = false)
        {
            if (target.Pawn is Pawn p && p.RaceProps.Humanlike && !NephilaUtility.IsAdult(p))
            {
                if (throwMessages)
                    Messages.Message("Nephila_AdultsOnly".Translate(p.Named("PAWN")), p, MessageTypeDefOf.RejectInput, historical: false);
                return false;
            }
            return base.ValidateTarget(target, ability, throwMessages);
        }

        public override void Cast(GlobalTargetInfo[] targets, Ability ability)
        {
            base.Cast(targets.Where(t => !(t.Thing is Pawn p) || !p.RaceProps.Humanlike || NephilaUtility.IsAdult(p)).ToArray(), ability);
        }
    }
}
