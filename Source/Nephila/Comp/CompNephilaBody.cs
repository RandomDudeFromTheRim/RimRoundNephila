using RimWorld;
using Verse;

namespace Nephila
{
    public class CompProperties_NephilaBody : CompProperties
    {
        /// <summary>The caste's breasts (a hediff on the torso).</summary>
        public HediffDef breasts;
        /// <summary>Grown ones lactate all the time.</summary>
        public bool lactates = true;

        public CompProperties_NephilaBody() => compClass = typeof(CompNephilaBody);
    }

    /// <summary>
    /// A Nephila's body, now that RimJobWorld is gone: her breasts and constant lactation
    /// (vanilla lactation, which Lactation Expansion makes milkable). Her womb comes from
    /// Intimacy - Gender Works itself, through the race's GenderWorksModExtension
    /// (Mods/GenderWorks). Set up once she is grown, then kept up.
    /// </summary>
    public class CompNephilaBody : ThingComp
    {
        const int CheckInterval = 2500;

        bool organsSet;

        CompProperties_NephilaBody Props => (CompProperties_NephilaBody)props;
        Pawn Pawn => parent as Pawn;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            Refresh();
        }

        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            if (parent.IsHashIntervalTick(CheckInterval, delta))
                Refresh();
        }

        void Refresh()
        {
            Pawn p = Pawn;
            if (p == null || p.Dead || p.health == null || !NephilaUtility.IsAdult(p))
                return;
            if (!organsSet)
            {
                organsSet = true;
                if (Props.breasts != null && !p.health.hediffSet.HasHediff(Props.breasts))
                    p.health.AddHediff(Props.breasts, p.RaceProps.body.corePart);
            }
            if (Props.lactates && p.gender == Gender.Female)
                NephilaLactationUtility.StartLactating(p);
        }


        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref organsSet, "organsSet");
        }
    }
}
