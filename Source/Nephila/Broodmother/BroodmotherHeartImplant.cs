using System.Linq;
using RRHediffs = RimRound.Utilities.HediffUtility;
using RimWorld;
using UnityEngine;
using Verse;

namespace Nephila
{
    /// <summary>
    /// The broodmother's heart, pressed into someone's chest (Broodmother_Core.xml). It heals,
    /// and it rebuilds anyone who isn't already a Nephila into one (Hediff_HeartTakeover).
    /// </summary>
    public class Hediff_BroodmotherHeart : Hediff_Implant
    {
        public override void PostAdd(DamageInfo? dinfo)
        {
            base.PostAdd(dinfo);
            TryTakeOver();
        }

        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (pawn.IsHashIntervalTick(GenDate.TicksPerHour, delta))
                TryTakeOver();
        }

        /// <summary>Anyone the heart could ever rebuild; an inhibitor doesn't stop it starting, only finishing.</summary>
        public static bool CanTakeOver(Pawn p) =>
            p != null && !p.Dead && p.RaceProps.Humanlike && NephilaUtility.IsAdult(p)
            && !NephilaUtility.IsNephila(p) && !NephilaUtility.IsRobot(p) && !NephilaUtility.IsUndead(p);

        void TryTakeOver()
        {
            HediffDef takeover = BroodmotherCoreDefOf.Neph_HeartTakeover;
            if (CanTakeOver(pawn) && !pawn.health.hediffSet.HasHediff(takeover))
                pawn.health.AddHediff(takeover);
        }
    }

    /// <summary>
    /// Her heartbeat: four days of ravenous hunger and swelling, then the host becomes a Nephila
    /// handmaiden - or, if he's a man, a cherub. The heart comes through it with her.
    /// </summary>
    public class Hediff_HeartTakeover : HediffWithComps_NephilaTransformBase
    {
        const int GrowInterval = 250;

        /// <summary>Goo piled on per day at each stage.</summary>
        static readonly float[] KilosPerDay = { 6f, 10f, 16f, 24f, 30f };

        protected override bool Eligible(Pawn p) => NephilaUtility.CanBeConverted(p, femaleOnly: false);

        public override void TickInterval(int delta)
        {
            if (pawn != null && pawn.Spawned && pawn.IsHashIntervalTick(GrowInterval, delta))
                Swell(KilosPerDay[Mathf.Clamp(CurStageIndex, 0, KilosPerDay.Length - 1)] * GrowInterval / GenDate.TicksPerDay);
            base.TickInterval(delta);
        }

        void Swell(float kilos)
        {
            Hediff weight = RRHediffs.WeightHediff(pawn);
            if (weight == null)
                return;
            float now = RRHediffs.SeverityToKilosWithBaseWeight(weight.Severity);
            RRHediffs.SetHediffSeverity(RimRound.Defs.HediffDefOf.RimRound_Weight, pawn, RRHediffs.KilosToSeverityWithBaseWeight(now + kilos));
        }

        protected override void Complete()
        {
            string name = pawn.LabelShort;
            bool player = pawn.Faction == Faction.OfPlayer;
            if (pawn.gender == Gender.Female)
            {
                Pawn handmaiden = NephilaTransformUtility.Transform(pawn, DefDatabase<PawnKindDef>.GetNamedSilentFail("NephilaHandmaidenTransformation"), NephilaDefOf.NephilimVeiledOne, null, null);
                if (handmaiden == null)
                    return;
                BodyPartRecord heart = handmaiden.health.hediffSet.GetNotMissingParts().FirstOrDefault(p => p.def == BodyPartDefOf.Heart);
                if (heart != null)
                    handmaiden.health.AddHediff(BroodmotherCoreDefOf.Neph_BroodmotherHeartImplant, heart);
                if (player)
                    Find.LetterStack.ReceiveLetter($"{name} is hers",
                        $"The broodmother's heart has finished with {name}. Her own heartbeat fell into step with it hours ago; now the rest of her has followed. She sank down into a warm, heaving pool of goo - and rose from it as a Nephila handmaiden, round and soft and brimming, her brood already swelling in her.\n\nShe remembers who she was. She just doesn't much mind any more. The heart still beats in her chest.",
                        LetterDefOf.NeutralEvent, new LookTargets(handmaiden));
                return;
            }
            Pawn cherub = NephilaTransformUtility.Transform(pawn, DefDatabase<PawnKindDef>.GetNamedSilentFail("NephilaCherubim"), null, null, null);
            if (cherub != null && player)
                Find.LetterStack.ReceiveLetter($"{name} is hers",
                    $"The broodmother's heart has finished with {name} - and it had no idea what to do with a man. He sank into a heaving pool of goo, and what slithered out of it was a cherub: a fat, gooey serpent, all appetite.\n\nThe heart did not survive the change.",
                    LetterDefOf.NegativeEvent, new LookTargets(cherub));
        }
    }
}
