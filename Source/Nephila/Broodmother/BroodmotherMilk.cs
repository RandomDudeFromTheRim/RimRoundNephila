using System.Collections.Generic;
using HarmonyLib;
using RimRound.Comps;
using RimRound.FeedingTube;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Nephila
{
    /// <summary>The overgrown broodmother's milk: terrain tagged Neph_Milk (Terrain_Broodmother.xml).</summary>
    public static class BroodmotherMilk
    {
        public const string Tag = "Neph_Milk";

        public static bool IsMilk(TerrainDef t) => t?.tags != null && t.tags.Contains(Tag);

        public static bool IsMilk(IntVec3 c, Map map) => IsMilk(c.GetTerrain(map));

        /// <summary>Flowing milk (a river) rather than a still pool.</summary>
        public static bool IsFlowing(TerrainDef t) => t?.affordances != null && t.affordances.Contains(TerrainAffordanceDefOf.MovingFluid);
    }

    /// <summary>
    /// Whether the colony has ever come across the broodmother's milk. Until it has, the milk
    /// siphoning research is hidden ("???") - there's nothing to siphon. Checked every so
    /// often over every map, and remembered once found.
    /// </summary>
    public class GameComponent_NephMilkDiscovery : GameComponent
    {
        public bool found;
        int ticks;

        public GameComponent_NephMilkDiscovery(Game game) { }

        public static bool Found => Current.Game?.GetComponent<GameComponent_NephMilkDiscovery>()?.found ?? false;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref found, "found");
        }

        public override void GameComponentTick()
        {
            if (found || ++ticks < 2000)
                return;
            ticks = 0;
            foreach (Map map in Find.Maps)
            {
                // milk only generates on her own maps: don't scan anyone else's every time
                if (map.Biome?.defName != "Neph_OvergrownBroodmother" || (!map.IsPlayerHome && !map.mapPawns.AnyColonistSpawned))
                    continue;
                foreach (IntVec3 c in map.AllCells)
                {
                    if (!BroodmotherMilk.IsMilk(c, map) || c.Fogged(map))
                        continue;
                    found = true;
                    Find.LetterStack.ReceiveLetter("Broodmother's milk",
                        "The streams and pools here aren't water: they're milk, warm and iridescent, welling up out of the broodmother's body.\n\nIt can be drunk straight from the stream - and with some research, siphoned into the feed lines.",
                        LetterDefOf.PositiveEvent, new LookTargets(c, map));
                    return;
                }
            }
        }
    }

    /// <summary>The milk siphoning research stays hidden until there's milk to siphon.</summary>
    [HarmonyPatch(typeof(ResearchProjectDef), nameof(ResearchProjectDef.IsHidden), MethodType.Getter)]
    public static class ResearchProjectDef_IsHidden_NephMilk
    {
        static ResearchProjectDef siphoning;
        static bool looked;

        public static void Postfix(ResearchProjectDef __instance, ref bool __result)
        {
            if (__result)
                return;
            if (!looked)
            {
                looked = true;
                siphoning = DefDatabase<ResearchProjectDef>.GetNamedSilentFail("Neph_MilkSiphoning");
            }
            if (__instance == siphoning && !__instance.IsFinished && !GameComponent_NephMilkDiscovery.Found)
                __result = true;
        }
    }

    // ------------------------------------------------------------------ the siphon

    public class CompProperties_NephMilkSiphon : CompProperties
    {
        /// <summary>Nutrition a day drawn from flowing milk; a still pool gives half.</summary>
        public float nutritionPerDay = 3f;
        /// <summary>Fullness per nutrition: Nephila milk is rich and thick.</summary>
        public float density = 1.3f;

        public CompProperties_NephMilkSiphon()
        {
            compClass = typeof(CompNephMilkSiphon);
        }
    }

    /// <summary>
    /// Draws the broodmother's milk up out of the stream or pool it stands in and pumps it into
    /// RimRound's feed lines (through FoodNetworkAccess, so either food network works). Nothing
    /// is bottled: with nowhere to put it, it simply stops drawing.
    /// </summary>
    public class CompNephMilkSiphon : ThingComp
    {
        float buffer;
        string status;

        CompProperties_NephMilkSiphon Props => (CompProperties_NephMilkSiphon)props;

        public override void PostExposeData()
        {
            Scribe_Values.Look(ref buffer, "buffer");
        }

        float RatePerDay
        {
            get
            {
                TerrainDef t = parent.Position.GetTerrain(parent.Map);
                if (!BroodmotherMilk.IsMilk(t))
                    return 0f;
                return BroodmotherMilk.IsFlowing(t) ? Props.nutritionPerDay : Props.nutritionPerDay * 0.5f;
            }
        }

        public override void CompTickRare()
        {
            if (!parent.Spawned || parent is not Building b)
                return;
            if (parent.GetComp<CompPowerTrader>() is CompPowerTrader power && !power.PowerOn)
            {
                status = "No power.";
                return;
            }
            float rate = RatePerDay;
            if (rate <= 0f)
            {
                status = "Not standing in milk.";
                return;
            }
            buffer = Mathf.Min(buffer + rate * GenTicks.TickRareInterval / GenDate.TicksPerDay, 2f);
            if (buffer < 0.1f)
                return;
            if (FoodNetworkAccess.Current.TryStore(b, buffer, Props.density))
            {
                buffer = 0f;
                status = null;
            }
            else
            {
                status = FoodNetworkAccess.Current.FreeCapacity(b) > 0f ? "Feed lines nearly full." : "Not connected to a feed line, or the lines are full.";
            }
        }

        public override string CompInspectStringExtra()
        {
            if (!parent.Spawned)
                return null;
            string s = $"Siphoning: {RatePerDay:0.#} nutrition a day";
            return status == null ? s : s + "\n" + status;
        }
    }

    /// <summary>The siphon has to stand in shallow milk.</summary>
    public class PlaceWorker_NephOnMilk : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            foreach (IntVec3 c in GenAdj.OccupiedRect(loc, rot, checkingDef.Size))
                if (!c.InBounds(map) || !BroodmotherMilk.IsMilk(c, map))
                    return "Must be placed in milk.";
            return true;
        }
    }

    // ------------------------------------------------------------------ drinking from the stream

    /// <summary>A pawn wades into a milk stream or pool and drinks from it, for fun - and a little food.</summary>
    public class JoyGiver_NephDrinkMilk : JoyGiver
    {
        public override Job TryGiveJob(Pawn pawn)
        {
            if (pawn.needs?.food == null)
                return null;
            bool Valid(IntVec3 c) => BroodmotherMilk.IsMilk(c, pawn.Map) && c.Standable(pawn.Map) && !c.Fogged(pawn.Map) && !c.IsForbidden(pawn) && !PawnUtility.KnownDangerAt(c, pawn.Map, pawn);
            if (!CellFinder.TryFindClosestRegionWith(pawn.GetRegion(), TraverseParms.For(pawn), r => !r.IsForbiddenEntirely(pawn) && r.TryFindRandomCellInRegionUnforbidden(pawn, Valid, out _), 80, out Region region))
                return null;
            if (!region.TryFindRandomCellInRegionUnforbidden(pawn, Valid, out IntVec3 cell))
                return null;
            return JobMaker.MakeJob(def.jobDef, cell);
        }
    }

    public class JobDriver_NephDrinkMilk : JobDriver
    {
        /// <summary>Nutrition from a long drink of milk: a sliver of a meal.</summary>
        const float Nutrition = 0.25f;
        const float Density = 1.3f;

        public override bool TryMakePreToilReservations(bool errorOnFailed) => true;

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);
            Toil drink = ToilMaker.MakeToil("DrinkMilk");
            drink.tickIntervalAction = delegate (int delta)
            {
                // a sip at a time, so an interrupted drink only gives what was drunk
                Sip(Nutrition * delta / Mathf.Max(1, job.def.joyDuration));
                JoyUtility.JoyTickCheckEnd(pawn, delta, JoyTickFullJoyAction.EndJob);
            };
            drink.defaultCompleteMode = ToilCompleteMode.Delay;
            drink.defaultDuration = job.def.joyDuration;
            drink.FailOn(() => !BroodmotherMilk.IsMilk(pawn.Position, pawn.Map));
            yield return drink;
        }

        void Sip(float nutrition)
        {
            Need_Food food = pawn.needs?.food;
            if (food == null || nutrition <= 0f)
                return;
            food.CurLevel += nutrition;
            var fnd = pawn.TryGetComp<FullnessAndDietStats_ThingComp>();
            if (fnd != null && !fnd.Disabled)
            {
                fnd.UpdateRatio(nutrition, Density);
                fnd.CurrentFullness += nutrition * Density * fnd.FullnessGainedMultiplier;
            }
        }

        public override string GetReport() => "drinking milk from the stream.";
    }
}
