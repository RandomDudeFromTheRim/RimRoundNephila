using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Nephila
{
    public static class Utility
    {
        public static void DebugReport(string x)
        {
            if (Prefs.DevMode && DebugSettings.godMode)
                Log.Message("[RimRound: Nephila] " + x);
        }
    }

    /// <summary>Who counts as what, for the nanites.</summary>
    public static class NephilaUtility
    {
        // races and hediffs from other mods that mark a pawn as a machine or undead
        static readonly HashSet<string> RobotRaces = new HashSet<string> { "Android1Tier", "Android2Tier", "Android3Tier", "Android4Tier", "Android5Tier", "M7Mech", "MicroScyther", "ChjAndroid" };
        static readonly HashSet<string> UndeadRaces = new HashSet<string> { "SL_Runner", "SL_Peon", "SL_Archer", "SL_Hero", "TM_GiantSkeletonR", "TM_SkeletonR", "TM_SkeletonLichR" };
        static readonly HashSet<string> UndeadHediffs = new HashSet<string> { "TM_UndeadHD", "TM_UndeadAnimalHD", "TM_LichHD", "TM_UndeadStageHD" };

        public static Faction NephilaFaction => Find.FactionManager.FirstFactionOfDef(NephilaDefOf.NephilaNPCFaction);

        /// <summary>
        /// Grown up, by life stage rather than by years: a race that matures fast (an
        /// android that is fully adult at one year old) counts, a slow one's teenagers don't.
        /// </summary>
        public static bool IsAdult(Pawn p)
        {
            if (p?.ageTracker == null)
                return false;
            LifeStageDef stage = p.ageTracker.CurLifeStage;
            if (p.RaceProps.Humanlike)
                return p.DevelopmentalStage.Adult();
            // animals and the like: their last life stage is the grown one
            return stage != null && p.ageTracker.CurLifeStageIndex >= p.RaceProps.lifeStageAges.Count - 1;
        }

        /// <summary>Any Nephila caste (the humanlike ones).</summary>
        public static bool IsNephila(Pawn p) => p?.def?.GetModExtension<NephilaPawnTerrainHandler>()?.tags?.Contains("Nephila") == true
            || (p?.def != null && p.def.defName.StartsWith("Nephila") && p.RaceProps.Humanlike);

        public static bool HasInhibitor(Pawn p) => p?.health?.hediffSet?.HasHediff(NephilaDefOf.NephilaMechaniteInhibitor) ?? false;

        public static bool IsRobot(Pawn p)
        {
            if (p.RaceProps.IsMechanoid || !p.RaceProps.IsFlesh || RobotRaces.Contains(p.def.defName) || p.RaceProps.FleshType?.defName == "ChJDroid")
                return true;
            // Vanilla Races Expanded - Android and similar synthetic bodies
            return p.genes?.GenesListForReading.Any(g => g.Active && g.def.defName == "VREA_SyntheticBody") == true;
        }

        public static bool IsUndead(Pawn p)
        {
            if (UndeadRaces.Contains(p.def.defName) || (p.IsMutant && ModsConfig.AnomalyActive))
                return true;
            List<Hediff> hediffs = p.health?.hediffSet?.hediffs;
            if (hediffs != null)
                for (int i = 0; i < hediffs.Count; i++)
                {
                    string name = hediffs[i].def.defName;
                    if (UndeadHediffs.Contains(name) || name.Contains("ROM_Vamp"))
                        return true;
                }
            return p.story?.traits?.allTraits?.Any(t => t.def.defName == "Undead") == true;
        }

        /// <summary>
        /// Can the nanites take hold of this pawn at all: a grown, living, flesh-and-blood
        /// person, not already a Nephila, not protected by an inhibitor.
        /// </summary>
        public static bool CanBeConverted(Pawn p, bool femaleOnly = true)
        {
            if (p == null || p.Dead || !p.RaceProps.Humanlike || !IsAdult(p))
                return false;
            if (femaleOnly && p.gender != Gender.Female)
                return false;
            return !IsNephila(p) && !IsRobot(p) && !IsUndead(p) && !HasInhibitor(p);
        }
    }
}
