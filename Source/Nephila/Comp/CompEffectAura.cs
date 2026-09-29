using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Nephila
{
    public class CompEffectAura : ThingComp
    {
        private CompProperties_EffectAura Props => (CompProperties_EffectAura)props;
        private int _internalTicks = 0;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            if (parent != null && parent.Map != null)
            {
                _internalTicks = Random.Range(0, Props.Frequency);
            }
        }

        public override void CompTick()
        {
            base.CompTick();

            if (parent == null || parent.Map == null)
            {
                return;
            }

            _internalTicks++;
            if (_internalTicks >= Props.Frequency)
            {
                _internalTicks = 0;
                ApplyAuraEffect();
            }
        }

        public override void PostDeSpawn(Map map, DestroyMode mode = DestroyMode.Vanish)
        {
            base.PostDeSpawn(map, mode);
            // Any necessary cleanup on despawn
            _internalTicks = 0; // Resetting internal state
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            base.PostDestroy(mode, previousMap);
            // Any necessary cleanup on destroy
            _internalTicks = 0; // Resetting internal state
        }

        private void ApplyAuraEffect()
        {
            if (parent == null || parent.Map == null)
            {
                return;
            }

            var position = parent.Position;
            var squaredRadius = Props.Radius * Props.Radius;

            if (Props.Effects == null)
            {
                return;
            }

            List<Pawn> targets = parent.Map.mapPawns.AllPawnsSpawned
                .Where(pawn => (pawn.Position - position).LengthHorizontalSquared < squaredRadius)
                .Where(pawn => pawn != parent && pawn.Map == parent.Map)
                // every Nephila aura is a lust haze: only ever grown people
                .Where(pawn => pawn.RaceProps.Humanlike && NephilaUtility.IsAdult(pawn))
                .ToList();

            foreach (var target in targets)
            {
                foreach (var effect in Props.Effects)
                {
                    if (effect.Filter == null)
                    {
                        continue;
                    }

                    if (effect.Filter.Eval(parent, target))
                    {
                        ApplyEffectOnPawn(target, effect);
                    }
                }
            }
        }

        private void ApplyEffectOnPawn(Pawn pawn, FilteredEffect effect)
        {
            if (Rand.Range(0f, 1f) <= effect.Chance)
            {
                var existingHediff = pawn.health.hediffSet.GetFirstHediffOfDef(effect.Effect);

                if (existingHediff == null)
                {
                    var newHediff = HediffMaker.MakeHediff(effect.Effect, pawn);
                    newHediff.Severity = effect.InitialSeverity;
                    pawn.health.AddHediff(newHediff);
                }
                else
                {
                    existingHediff.Severity += effect.SeverityGrowth;
                    pawn.health.Notify_HediffChanged(existingHediff);
                }
            }
        }
    }

    public class FilteredEffect
    {
        public float Chance = 1.0f;
        public HediffDef Effect;
        public float InitialSeverity = 0.5f;
        public float SeverityGrowth = 0.1f;
        public PawnFilter Filter = new PawnFilter_Dummy();

        public void ApplyOnPawn(Pawn pawn)
        {
            if (Rand.Range(0f, 1f) <= Chance)
            {
                var firstTime = !pawn.health.hediffSet.HasHediff(Effect);
                if (firstTime)
                {
                    pawn.health.AddHediff(Effect);
                }
                var targetHediff = pawn.health.hediffSet.GetFirstHediffOfDef(Effect);
                targetHediff.Severity = firstTime ? InitialSeverity : targetHediff.Severity + SeverityGrowth;
                pawn.health.Notify_HediffChanged(targetHediff);
            }
        }
    }

    public abstract class PawnFilter
    {
        public abstract bool Eval(ThingWithComps source, Pawn target);
    }

    public class PawnFilter_Dummy : PawnFilter
    {
        public override bool Eval(ThingWithComps source, Pawn target)
        {
            return true;
        }
    }

    public class PawnFilter_Range : PawnFilter
    {
        public int SquaredRadius;

        public override bool Eval(ThingWithComps source, Pawn target)
        {
            return (target.Position - source.Position).LengthHorizontalSquared < SquaredRadius;
        }
    }

    public class PawnFilter_Age : PawnFilter
    {
        public int MaxAge;
        public int MinAge;

        public override bool Eval(ThingWithComps source, Pawn target)
        {
            return MinAge <= target.ageTracker.AgeBiologicalYears && MaxAge >= target.ageTracker.AgeBiologicalYears;
        }
    }

    public class PawnFilter_Gender : PawnFilter
    {
        public List<Gender> Genders;

        public override bool Eval(ThingWithComps source, Pawn target)
        {
            return Genders.Contains(target.gender);
        }
    }

    public class PawnFilter_Or : PawnFilter
    {
        public List<PawnFilter> Or;

        public override bool Eval(ThingWithComps source, Pawn target)
        {
            return Or.Any(filter => filter.Eval(source, target));
        }
    }

    public class PawnFilter_And : PawnFilter
    {
        public List<PawnFilter> And;

        public override bool Eval(ThingWithComps source, Pawn target)
        {
            return And.All(filter => filter.Eval(source, target));
        }
    }

    public class PawnFilter_Not : PawnFilter
    {
        public PawnFilter Not;

        public override bool Eval(ThingWithComps source, Pawn target)
        {
            return !Not.Eval(source, target);
        }
    }

    public class PawnFilter_Faction : PawnFilter
    {
        public bool AllowAlly;
        public bool AllowNeutral;
        public bool AllowEnemy;

        public override bool Eval(ThingWithComps source, Pawn target)
        {
            if (AllowAlly && source.Faction == target.Faction) { return true; }
            var relationship = source.Faction.RelationKindWith(target.Faction);
            return (AllowAlly && relationship == FactionRelationKind.Ally) ||
                   (AllowNeutral && relationship == FactionRelationKind.Neutral) ||
                   (AllowEnemy && relationship == FactionRelationKind.Hostile);
        }
    }

    public class PawnFilter_Humanlike : PawnFilter
    {
        public override bool Eval(ThingWithComps source, Pawn target)
        {
            return target.RaceProps.Humanlike;
        }
    }

    public class PawnFilter_Flesh : PawnFilter
    {
        public override bool Eval(ThingWithComps source, Pawn target)
        {
            return target.RaceProps.IsFlesh;
        }
    }

    public class CompProperties_EffectAura : CompProperties
    {
        public List<FilteredEffect> Effects;
        public int Radius = 10;
        public int Frequency = 500;

        public CompProperties_EffectAura()
        {
            compClass = typeof(CompEffectAura);
        }
    }
}
