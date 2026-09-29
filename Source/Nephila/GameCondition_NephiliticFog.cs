using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Nephila
{
    /// <summary>
    /// The Nephilitic fog a clutch mother brings: lilac skies, a chonkers cherub roaming the
    /// map, and grown colonists caught outdoors slowly taking in the Nephila sickness.
    /// </summary>
    public class GameCondition_NephiliticFog : GameCondition
    {
        const float MaxSkyLerpFactor = 0.5f;
        const float SkyGlow = 0.85f;
        const int ExposureInterval = 3451;
        const int WeatherInterval = 1000;
        const float ExposurePerInterval = 0.028758334f;

        static readonly SkyColorSet FogColors = new SkyColorSet(
            new ColorInt(216, 180, 216).ToColor,
            new ColorInt(216, 180, 216).ToColor,
            new Color(0.8f, 0.7f, 0.8f),
            0.9f);

        readonly List<SkyOverlay> overlays = new List<SkyOverlay> { new WeatherOverlay_Fallout() };
        int ticksInFog;

        static StatDef toxicSensitivity;
        static StatDef ToxicSensitivity => toxicSensitivity ?? (toxicSensitivity = DefDatabase<StatDef>.GetNamedSilentFail("ToxicEnvironmentResistance"));

        public override void Init()
        {
            base.Init();
            LessonAutoActivator.TeachOpportunity(ConceptDefOf.ForbiddingDoors, OpportunityType.Critical);
            LessonAutoActivator.TeachOpportunity(ConceptDefOf.AllowedAreas, OpportunityType.Critical);
        }

        public override void End()
        {
            foreach (Map map in AffectedMaps)
                map?.weatherManager.TransitionTo(WeatherDefOf.Clear);
            base.End();
        }

        public override void GameConditionTick()
        {
            ticksInFog++;
            if (Find.TickManager.TicksGame % ExposureInterval == 0)
                foreach (Map map in AffectedMaps)
                    foreach (Pawn pawn in map.mapPawns.PawnsInFaction(Faction.OfPlayer))
                        DoPawnNephilaToxicDamage(pawn);

            if (ticksInFog % WeatherInterval == 0)
                foreach (Map map in AffectedMaps)
                {
                    if (NephilaDefOf.NephiliticFog != null && map.weatherManager.curWeather != NephilaDefOf.NephiliticFog)
                        map.weatherManager.TransitionTo(NephilaDefOf.NephiliticFog);
                    EnsureCherub(map);
                }
        }

        /// <summary>One chonkers cherub roams the fog; a few tries to place her, then give up until next time.</summary>
        static void EnsureCherub(Map map)
        {
            if (NephilaDefOf.Nephila_ChonkersCherub == null)
                return;
            foreach (Pawn p in map.mapPawns.AllPawnsSpawned)
                if (p is PawnNephilaChonkersCherub)
                    return;
            if (CellFinder.TryFindRandomCellNear(map.Center, map, 60,
                    c => c.Standable(map) && !c.Roofed(map) && map.reachability.CanReachColony(c), out IntVec3 cell, 100))
                GenSpawn.Spawn(PawnGenerator.GeneratePawn(NephilaDefOf.Nephila_ChonkersCherub), cell, map);
        }

        /// <summary>A grown, flesh-and-blood pawn out under the fog takes in some of the sickness.</summary>
        public static void DoPawnNephilaToxicDamage(Pawn p)
        {
            if (!p.Spawned || p.Position.Roofed(p.Map) || !p.RaceProps.IsFlesh || !NephilaUtility.CanBeConverted(p))
                return;
            float amount = ExposurePerInterval;
            if (ToxicSensitivity != null)
                amount *= Mathf.Max(0f, 1f - p.GetStatValue(ToxicSensitivity));
            if (amount <= 0f)
                return;
            amount *= Mathf.Lerp(0.85f, 1.15f, Rand.ValueSeeded(p.thingIDNumber ^ 74374237));
            HealthUtility.AdjustSeverity(p, NephilaDefOf.NephilaInitialTransformationFromAnimal, amount);
        }

        public override void GameConditionDraw(Map map)
        {
            foreach (SkyOverlay overlay in overlays)
                overlay.DrawOverlay(map);
        }

        public override float SkyTargetLerpFactor(Map map) => GameConditionUtility.LerpInOutValue(this, TransitionTicks, MaxSkyLerpFactor);
        public override SkyTarget? SkyTarget(Map map) => new SkyTarget(SkyGlow, FogColors, 1f, 1f);
        public override float AnimalDensityFactor(Map map) => 0f;
        public override float PlantDensityFactor(Map map) => 0f;
        public override List<SkyOverlay> SkyOverlays(Map map) => overlays;
        public override bool AllowEnjoyableOutsideNow(Map map) => false;
    }
}
