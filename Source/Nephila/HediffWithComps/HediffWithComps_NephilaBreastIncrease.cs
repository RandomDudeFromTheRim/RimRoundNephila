using RimRound.Utilities;
using RimWorld;
using Verse;

namespace Nephila
{
    /// <summary>
    /// Induced lactation (the lactation psycast and similar): once the mechanites have
    /// done their work, the pawn starts lactating - vanilla lactation, which Lactation
    /// Expansion makes milkable - and her breasts swell (NephilaSwollenBreasts, which
    /// raises how much milk she makes and holds). Adults only: on anyone else the
    /// mechanites just break down.
    /// </summary>
    public class HediffWithComps_NephilaBreastIncrease : HediffWithComps
    {
        const int CheckInterval = 300;
        const float SwellPerDose = 0.35f;
        const float KilosPerDose = 6f;

        bool triggered;

        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            if (!NephilaUtility.IsAdult(pawn))
                pawn.health.RemoveHediff(this);
        }

        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (triggered || pawn == null || !pawn.Spawned || CurStageIndex < 4 || !pawn.IsHashIntervalTick(CheckInterval, delta))
                return;
            if (NephilaUtility.HasInhibitor(pawn) || !NephilaUtility.IsAdult(pawn))
                return;
            triggered = true;
            Severity = 1f;
            Swell(pawn, SwellPerDose);
            RimRound.Utilities.HediffUtility.QueueWeightGain(pawn, KilosPerDose);
            Messages.Message("Nephila_BreastsSizeIncrease".Translate(pawn.LabelShort), pawn, MessageTypeDefOf.SilentInput, historical: false);
        }

        /// <summary>Start her lactating and swell her breasts by amount (0..1 of a full swelling).</summary>
        public static void Swell(Pawn p, float amount)
        {
            NephilaLactationUtility.StartLactating(p);
            if (NephilaDefOf.NephilaSwollenBreasts == null)
                return;
            BodyPartRecord torso = p.RaceProps.body.corePart;
            Hediff breasts = p.health.hediffSet.GetFirstHediffOfDef(NephilaDefOf.NephilaSwollenBreasts);
            if (breasts == null)
            {
                breasts = HediffMaker.MakeHediff(NephilaDefOf.NephilaSwollenBreasts, p, torso);
                breasts.Severity = amount;
                p.health.AddHediff(breasts, torso);
            }
            else
            {
                breasts.Severity += amount;
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref triggered, "triggered");
        }
    }

    /// <summary>Lactation through the vanilla hediff, which Lactation Expansion builds on.</summary>
    public static class NephilaLactationUtility
    {
        public static bool IsLactating(Pawn p) => p?.health?.hediffSet?.HasHediff(HediffDefOf.Lactating) ?? false;

        public static void StartLactating(Pawn p, float severity = 0.5f)
        {
            if (p?.health == null || !p.RaceProps.Humanlike || !NephilaUtility.IsAdult(p))
                return;
            Hediff lactating = p.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Lactating);
            if (lactating == null)
            {
                lactating = HediffMaker.MakeHediff(HediffDefOf.Lactating, p);
                lactating.Severity = severity;
                p.health.AddHediff(lactating);
            }
            else if (lactating.Severity < severity)
            {
                lactating.Severity = severity;
            }
        }
    }
}
