using RimRound.Utilities;
using RimWorld;
using Verse;

namespace Nephila
{
    /// <summary>
    /// Brood weight: while a Nephila brood swells it adds to RimRound's weight, and a hediff
    /// with this extension adds kilosPerDay at full severity (less while it is young).
    /// </summary>
    public class NephilaBroodExtension : DefModExtension
    {
        public float kilosPerDay;
        /// <summary>Kilos added all at once when the brood completes and she changes caste.</summary>
        public float kilosOnCompletion;
    }

    /// <summary>
    /// A swelling brood: adds RimRound weight as it grows (NephilaBroodExtension), scaled by
    /// how far along it is. The matron's own brood is just this; the others change caste.
    /// </summary>
    public class HediffWithComps_NephilaBrood : HediffWithComps
    {
        const int WeightInterval = 2500;

        protected NephilaBroodExtension Brood => def.GetModExtension<NephilaBroodExtension>();

        /// <summary>0 when it starts, 1 at its last stage.</summary>
        public float Progress
        {
            get
            {
                float full = def.stages != null && def.stages.Count > 1 ? def.stages[def.stages.Count - 1].minSeverity : def.maxSeverity;
                return full <= 0f || full > 1e6f ? 0f : UnityEngine.Mathf.Clamp01(Severity / full);
            }
        }

        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (pawn != null && pawn.Spawned && Brood is NephilaBroodExtension brood && brood.kilosPerDay > 0f && pawn.IsHashIntervalTick(WeightInterval, delta))
                RimRound.Utilities.HediffUtility.QueueWeightGain(pawn, brood.kilosPerDay * Progress * WeightInterval / GenDate.TicksPerDay);
        }
    }

    /// <summary>
    /// A condition that, once it reaches its last stage, rebuilds the pawn as something
    /// else (NephilaTransformUtility). Checked a few times a second, not every tick.
    /// </summary>
    public abstract class HediffWithComps_NephilaTransformBase : HediffWithComps_NephilaBrood
    {
        const int CheckInterval = 300;

        protected virtual int TriggerStage => 4;
        protected abstract bool Eligible(Pawn p);
        protected abstract void Complete();

        public override void TickInterval(int delta)
        {
            base.TickInterval(delta);
            if (pawn == null || !pawn.Spawned)
                return;
            if (CurStageIndex >= TriggerStage && pawn.IsHashIntervalTick(CheckInterval, delta) && Eligible(pawn))
            {
                Utility.DebugReport($"{pawn} completes {def.defName}");
                pawn.health.RemoveHediff(this);
                Complete();
            }
        }

        protected float CompletionKilos => Brood?.kilosOnCompletion ?? 0f;

        protected Pawn Become(string kindDefName, RoyalTitleDef title, string letterKey) =>
            NephilaTransformUtility.Transform(pawn, DefDatabase<PawnKindDef>.GetNamedSilentFail(kindDefName), title,
                letterKey + "Label", letterKey + "Description", CompletionKilos);
    }

    /// <summary>Nephila sickness: a woman's body is taken over and rebuilt as a Nephila.</summary>
    public class HediffWithComps_NephilaSicknessTransformation : HediffWithComps_NephilaTransformBase
    {
        protected override bool Eligible(Pawn p) => NephilaUtility.CanBeConverted(p);
        protected override void Complete() => Become("NephilaVillager", NephilaDefOf.NephilimVeiledOne, "Nephila_NephilaInitialTransformation");
    }

    /// <summary>The same sickness, caught from a cherub's tentacles or the fog.</summary>
    public class HediffWithComps_NephilaSicknessTransformationFromAnimal : HediffWithComps_NephilaSicknessTransformation
    {
    }

    /// <summary>Serpent sickness from a grand matron's spew: the victim becomes a cherubim.</summary>
    public class HediffWithComps_NephilaGrandMatronSpewTransformer : HediffWithComps_NephilaTransformBase
    {
        protected override int TriggerStage => 3;
        protected override bool Eligible(Pawn p) => NephilaUtility.CanBeConverted(p, femaleOnly: false);

        protected override void Complete()
        {
            string name = pawn.LabelShort;
            Pawn cherub = NephilaTransformUtility.Transform(pawn, DefDatabase<PawnKindDef>.GetNamedSilentFail("NephilaCherubim"), null, null, null);
            if (cherub != null)
                Messages.Message("Nephila_SerpentSicknessComplete".Translate(name), cherub, MessageTypeDefOf.NegativeEvent);
        }
    }

    /// <summary>A maiden's first brood: she swells into a handmaiden.</summary>
    public class HediffWithComps_NephilaMaidenFullyPreggers : HediffWithComps_NephilaTransformBase
    {
        protected override bool Eligible(Pawn p) => NephilaUtility.IsAdult(p) && !NephilaUtility.HasInhibitor(p);
        protected override void Complete() => Become("NephilaHandmaidenTransformation", NephilaDefOf.NephilimVeiledOne, "Nephila_NephilaTransformation");
    }

    /// <summary>A handmaiden's brood: she swells into a matron.</summary>
    public class HediffWithComps_NephilaHandMaidenFullyPreggers : HediffWithComps_NephilaTransformBase
    {
        protected override bool Eligible(Pawn p) => NephilaUtility.IsAdult(p) && !NephilaUtility.HasInhibitor(p);
        protected override void Complete() => Become("NephilaMatronVillager", NephilaDefOf.NephilimGardenTender, "Nephila_NephilaHandmaidenTransformation");
    }

    /// <summary>
    /// A matron's brood: she swells into a grand matron. Forced on anyone who isn't a matron
    /// (the ascension drug), it is more than the body can take.
    /// </summary>
    public class HediffWithComps_NephilaMatronFullyPreggers : HediffWithComps_NephilaTransformBase
    {
        protected override bool Eligible(Pawn p) => p.def != NephilaDefOf.NephilaGrandMatron && NephilaUtility.IsAdult(p) && !NephilaUtility.HasInhibitor(p);

        protected override void Complete()
        {
            if (pawn.def == NephilaDefOf.NephilaMatron)
            {
                Become("NephilaGrandMatronVillager", NephilaDefOf.NephilimGrandMatronTitle, "Nephila_NephilaMatronTransformation");
                return;
            }
            if (pawn.Faction == Faction.OfPlayer)
                Find.LetterStack.ReceiveLetter("Nephila_AscensionAddictDied".Translate(pawn.LabelShort),
                    "Nephila_AscensionAddictDiedDescription".Translate(pawn.Name?.ToStringFull ?? pawn.LabelShort),
                    LetterDefOf.NegativeEvent, new LookTargets(pawn.Position, pawn.Map));
            pawn.Kill(null);
        }
    }
}
