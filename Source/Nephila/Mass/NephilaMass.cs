using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Nephila
{
    /// <summary>
    /// A caste's mass. The castes don't use RimRound's weight or fullness - they eat like
    /// anyone else - and their size is the caste's own sprite, so their weight is a scale of
    /// its own: what a grown woman of the caste settles at (matched against RimRound's bodies
    /// of the same bulk), plus what she carries.
    /// </summary>
    public class CompProperties_NephilaMass : CompProperties
    {
        /// <summary>What a grown woman of this caste settles at.</summary>
        public float kilos = 140f;
        /// <summary>How fast her gel settles towards that, as a share of it per day.</summary>
        public float settlePerDay = 0.06f;
        /// <summary>A full clutch (ready to gather) adds this share of her mass.</summary>
        public float clutchShare = 0.08f;
        /// <summary>At the worst of starvation she wastes to this share of it.</summary>
        public float starvedShare = 0.55f;

        public CompProperties_NephilaMass() => compClass = typeof(CompNephilaMass);
    }

    /// <summary>
    /// Mass on a hediff: a brood adds kilos as it swells (by its progress), anything else
    /// adds them by severity, and massShare adds that share of the caste's mass instead.
    /// </summary>
    public class NephilaMassExtension : DefModExtension
    {
        public float kilos;
        public float massShare;
    }

    /// <summary>
    /// Keeps a caste's NephilaMass hediff: her body ("flesh") drifts over the days towards
    /// the caste's settled mass - less while she starves - and her broods, swollen breasts
    /// and clutch ride on top. A woman who just changed caste keeps the mass she had and
    /// grows (or wastes) into the new one.
    /// </summary>
    public class CompNephilaMass : ThingComp
    {
        const int Interval = 2500;

        float flesh = -1f;

        public CompProperties_NephilaMass Props => (CompProperties_NephilaMass)props;
        Pawn Pawn => parent as Pawn;

        /// <summary>What she settles at when fed: the caste's mass, scaled for a child.</summary>
        public float SettledKilos
        {
            get
            {
                Pawn p = Pawn;
                float factor = p?.ageTracker?.CurLifeStage?.bodySizeFactor ?? 1f;
                return Props.kilos * Mathf.Clamp(factor, 0.1f, 1f);
            }
        }

        public float Kilos => Mathf.Max(1f, flesh) + CarriedKilos;

        public float CarriedKilos
        {
            get
            {
                Pawn p = Pawn;
                if (p?.health?.hediffSet == null)
                    return 0f;
                float kg = 0f;
                List<Hediff> hediffs = p.health.hediffSet.hediffs;
                for (int i = 0; i < hediffs.Count; i++)
                {
                    NephilaMassExtension ext = hediffs[i].def.GetModExtension<NephilaMassExtension>();
                    if (ext == null)
                        continue;
                    float amount = hediffs[i] is HediffWithComps_NephilaBrood brood ? brood.Progress : hediffs[i].Severity;
                    kg += amount * (ext.kilos + ext.massShare * SettledKilos);
                }
                if (parent.TryGetComp<CompNephilaMilkableHumanoid>() is CompNephilaMilkableHumanoid clutch && clutch.Active)
                    kg += Mathf.Clamp01(clutch.MilkProgressPercent) * Props.clutchShare * SettledKilos;
                return kg;
            }
        }

        /// <summary>Set her body's own mass, e.g. what she weighed before she changed.</summary>
        public void SetFlesh(float kilos)
        {
            flesh = Mathf.Max(1f, kilos);
            Refresh(0);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            Refresh(0);
        }

        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            if (parent.IsHashIntervalTick(Interval, delta))
                Refresh(Interval);
        }

        void Refresh(int ticks)
        {
            Pawn p = Pawn;
            if (p == null || p.Dead || p.health == null || NephilaDefOf.NephilaMass == null)
                return;
            float settled = SettledKilos;
            if (flesh < 0f)
                flesh = settled;
            if (ticks > 0)
            {
                float target = settled;
                if (p.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Malnutrition) is Hediff starving)
                    target = Mathf.Lerp(settled, settled * Props.starvedShare, Mathf.Clamp01(starving.Severity));
                float step = Props.settlePerDay * settled * ticks / GenDate.TicksPerDay;
                flesh = Mathf.MoveTowards(flesh, target, step);
            }
            Hediff mass = p.health.hediffSet.GetFirstHediffOfDef(NephilaDefOf.NephilaMass);
            if (mass == null)
            {
                mass = HediffMaker.MakeHediff(NephilaDefOf.NephilaMass, p);
                mass.Severity = Kilos;
                p.health.AddHediff(mass);
            }
            else
            {
                mass.Severity = Kilos;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref flesh, "nephilaFlesh", -1f);
        }
    }

    /// <summary>
    /// A caste's mass in kilos (its severity). Its stages go by mass against what the caste
    /// settles at: the stages' minSeverity is that share, not kilos.
    /// </summary>
    public class Hediff_NephilaMass : Hediff
    {
        public CompNephilaMass Comp => pawn?.TryGetComp<CompNephilaMass>();

        public float Share
        {
            get
            {
                float settled = Comp?.SettledKilos ?? 0f;
                return settled > 0f ? Severity / settled : 1f;
            }
        }

        public override int CurStageIndex
        {
            get
            {
                if (def.stages == null)
                    return 0;
                float share = Share;
                for (int i = def.stages.Count - 1; i >= 0; i--)
                    if (share >= def.stages[i].minSeverity)
                        return i;
                return 0;
            }
        }

        public override string SeverityLabel => RimRound.Utilities.GlobalSettings.usePoundsWherePossible
            ? $"{Severity * 2.20462f:F0} lbs"
            : $"{Severity:F0} kg";

        public override string TipStringExtra
        {
            get
            {
                string tip = base.TipStringExtra;
                CompNephilaMass comp = Comp;
                if (comp == null)
                    return tip;
                string line = "Nephila_MassTip".Translate(comp.SettledKilos.ToString("F0"), comp.CarriedKilos.ToString("F0"));
                return tip.NullOrEmpty() ? line : tip + "\n" + line;
            }
        }
    }
}
