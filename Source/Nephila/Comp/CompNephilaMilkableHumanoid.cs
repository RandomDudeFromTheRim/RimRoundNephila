using RimRound.Utilities;
using RimWorld;
using Verse;

namespace Nephila
{
    /// <summary>
    /// A Nephila's clutch: cherubim cores gestate in her and are gathered once ready (by
    /// herself or a colonist), shedding some of her weight with them. Only grown Nephila
    /// gestate, and not while starving.
    /// </summary>
    public class CompNephilaMilkableHumanoid : ThingComp
    {
        public int milkProgress;

        public CompProperties_NephilaMilkableHumanoid MilkProps => (CompProperties_NephilaMilkableHumanoid)props;

        Pawn Pawn => parent as Pawn;

        public bool Active
        {
            get
            {
                Pawn p = Pawn;
                if (p == null || p.Dead || !NephilaUtility.IsAdult(p))
                    return false;
                if (MilkProps.onlyFemales && p.gender != Gender.Female)
                    return false;
                if (MilkProps.onlyMales && p.gender != Gender.Male)
                    return false;
                return true;
            }
        }

        public bool IsOfProperAge => Pawn != null && NephilaUtility.IsAdult(Pawn) && Pawn.ageTracker.AgeBiologicalYears >= MilkProps.minimumAgeToBeMilked;
        public bool CanBeMilked => milkProgress >= MilkProps.ticksUntilMilking;
        public bool ActiveAndCanBeMilked => Active && CanBeMilked;
        public float MilkProgressPercent => (float)milkProgress / MilkProps.ticksUntilMilking;

        public override void CompTickInterval(int delta)
        {
            base.CompTickInterval(delta);
            if (milkProgress >= MilkProps.ticksUntilMilking || !Active)
                return;
            if (Pawn.needs?.food?.Starving == true)
                return;
            milkProgress += delta;
        }

        public Thing Milk(Pawn milker)
        {
            Pawn p = Pawn;
            if (p == null)
                return null;
            Thing thing = ThingMaker.MakeThing(MilkProps.milkDef);
            thing.stackCount = MilkProps.milkAmount;
            if (milker == null || milker == p)
            {
                if (MilkProps.milkThoughtMilkedSelf != null)
                    p.needs?.mood?.thoughts.memories.TryGainMemory(MilkProps.milkThoughtMilkedSelf);
            }
            else
            {
                if (MilkProps.milkThoughtMilker != null)
                    milker.needs?.mood?.thoughts.memories.TryGainMemory(MilkProps.milkThoughtMilker, p);
                if (MilkProps.milkThoughtMilked != null)
                    p.needs?.mood?.thoughts.memories.TryGainMemory(MilkProps.milkThoughtMilked, milker);
            }
            // the clutch leaves her lighter
            if (MilkProps.kilosShedPerGather > 0f)
                RimRound.Utilities.HediffUtility.QueueWeightGain(p, -MilkProps.kilosShedPerGather * MilkProps.milkAmount);
            milkProgress = 0;
            return thing;
        }

        public void GatherMilk(Pawn milker)
        {
            Thing thing = Milk(milker);
            if (thing != null)
                GenPlace.TryPlaceThing(thing, milker.Position, milker.Map, ThingPlaceMode.Near);
        }

        public void GatherMilkSelf()
        {
            Thing thing = Milk(null);
            if (thing != null)
                GenPlace.TryPlaceThing(thing, parent.Position, parent.Map, ThingPlaceMode.Near);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref milkProgress, "milkProgress");
        }

        public override string CompInspectStringExtra()
        {
            if (!Active || parent.Faction != Faction.OfPlayer)
                return null;
            string key = MilkProps.firstResourceName ? MilkProps.milkProgessKeyString : MilkProps.nephilaMilkProgessKeyString;
            return key.Translate(MilkProgressPercent.ToStringPercent());
        }
    }
}
