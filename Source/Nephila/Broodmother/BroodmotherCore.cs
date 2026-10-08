using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using Verse.Noise;

namespace Nephila
{
    /// <summary>
    /// The broodmother's core sidequest (Broodmother_Core.xml). Live on her long enough and her
    /// navel opens onto a pocket map deep inside her: a long, winding, sealed passage past her
    /// brood (hostile queen's guards) to her heart. Commune with it and choose: tear it out, or let
    /// her embrace you. Either way her navel closes a day later, with anyone still inside.
    /// </summary>
    [DefOf]
    public static class BroodmotherCoreDefOf
    {
        public static BiomeDef Neph_OvergrownBroodmother;
        public static IncidentDef Neph_BroodmotherNavelOpens;
        public static ThingDef Neph_BroodmotherNavel;
        public static ThingDef Neph_BroodmotherHeart;
        public static ThingDef Neph_BroodmotherHeartItem;
        public static ThingDef Neph_CondensedGoo;
        public static ThingDef Neph_HeartGoo;
        public static TerrainDef Neph_NephilaSoilRich;
        public static TerrainDef Neph_MilkShallow;
        public static TerrainDef Neph_MilkDeep;
        public static HediffDef Neph_BroodmotherEmbrace;
        public static HediffDef Neph_BroodmotherHeartImplant;
        public static HediffDef Neph_HeartTakeover;
        public static JobDef Neph_CommuneWithHeart;
        public static FactionDef Neph_Brood;
        public static PawnKindDef NephilaMatronPlayer;

        static BroodmotherCoreDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(BroodmotherCoreDefOf));
    }

    public static class BroodmotherCore
    {
        /// <summary>How long it takes to dig through, or how long her navel stays open afterwards.</summary>
        public const int CloseDelayTicks = GenDate.TicksPerDay;

        /// <summary>Her brood: a hidden, permanently hostile faction. Made on demand for saves from before it existed.</summary>
        public static Faction Brood
        {
            get
            {
                Faction f = Find.FactionManager.FirstFactionOfDef(BroodmotherCoreDefOf.Neph_Brood);
                if (f != null)
                    return f;
                f = FactionGenerator.NewGeneratedFaction(new FactionGeneratorParms(BroodmotherCoreDefOf.Neph_Brood, default, true));
                Find.FactionManager.Add(f);
                return f;
            }
        }

        public static bool IsBroodmother(Map map) => map?.Biome == BroodmotherCoreDefOf.Neph_OvergrownBroodmother;

        public static BroodmotherNavel NavelInto(Map pocketMap)
        {
            Map surface = (pocketMap?.Parent as PocketMapParent)?.sourceMap;
            if (surface == null)
                return null;
            foreach (Thing t in surface.listerThings.ThingsOfDef(BroodmotherCoreDefOf.Neph_BroodmotherNavel))
                if (t is BroodmotherNavel n && n.PocketMap == pocketMap)
                    return n;
            return null;
        }

        public static bool AnyNavel => Find.Maps.Any(m => m.listerThings.ThingsOfDef(BroodmotherCoreDefOf.Neph_BroodmotherNavel).Any());
    }

    // ------------------------------------------------------------------ the trigger

    /// <summary>
    /// Counts how long the colony has lived on the broodmother. Once it has been long enough
    /// (20-35 days, rolled afresh each time), her navel opens. After an embrace she may open
    /// up again, onto another of her hearts; once one has been torn out, never again.
    /// </summary>
    public class GameComponent_BroodmotherCore : GameComponent
    {
        public bool opened;
        public bool heartTorn;
        public float daysOnHer;
        public float daysNeeded = -1f;

        public GameComponent_BroodmotherCore(Game game) { }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref opened, "opened");
            Scribe_Values.Look(ref heartTorn, "heartTorn");
            Scribe_Values.Look(ref daysOnHer, "daysOnHer");
            Scribe_Values.Look(ref daysNeeded, "daysNeeded", -1f);
        }

        public override void GameComponentTick()
        {
            if (heartTorn || Find.TickManager.TicksGame % GenDate.TicksPerHour != 0)
                return;
            if (opened)
            {
                // her navel has closed again (or its map was abandoned): start counting afresh
                if (!BroodmotherCore.AnyNavel)
                {
                    opened = false;
                    daysOnHer = 0f;
                    daysNeeded = -1f;
                }
                return;
            }
            Map home = Find.Maps.FirstOrDefault(m => m.IsPlayerHome && BroodmotherCore.IsBroodmother(m));
            if (home == null)
                return;
            if (daysNeeded < 0f)
                daysNeeded = Rand.Range(20f, 35f);
            daysOnHer += 1f / GenDate.HoursPerDay;
            if (daysOnHer < daysNeeded)
                return;
            IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.Misc, home);
            if (BroodmotherCoreDefOf.Neph_BroodmotherNavelOpens.Worker.TryExecute(parms))
                opened = true;
        }
    }

    public class IncidentWorker_BroodmotherNavel : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms) =>
            parms.target is Map map && BroodmotherCore.IsBroodmother(map) && !BroodmotherCore.AnyNavel
            && Current.Game.GetComponent<GameComponent_BroodmotherCore>()?.heartTorn != true && TryFindSpot(map, out _);

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            if (parms.target is not Map map || !TryFindSpot(map, out IntVec3 spot))
                return false;
            BroodmotherNavel navel = (BroodmotherNavel)GenSpawn.Spawn(BroodmotherCoreDefOf.Neph_BroodmotherNavel, spot, map, WipeMode.FullRefund);
            SendStandardLetter(parms, navel);
            if (Find.CurrentMap == map)
                Find.CameraDriver.shaker.DoShake(0.1f);
            var comp = Current.Game.GetComponent<GameComponent_BroodmotherCore>();
            if (comp != null)
                comp.opened = true;
            return true;
        }

        /// <summary>Open ground, away from the edge - outside the colony if there's room, anywhere if not.</summary>
        static bool TryFindSpot(Map map, out IntVec3 spot)
        {
            bool Fits(IntVec3 c, bool avoidHome)
            {
                if (c.DistanceToEdge(map) < 8)
                    return false;
                foreach (IntVec3 x in GenAdj.OccupiedRect(c, Rot4.North, new IntVec2(3, 3)))
                {
                    if (!x.InBounds(map) || !x.Standable(map) || x.GetEdifice(map) != null || x.GetTerrain(map).IsWater)
                        return false;
                    if (avoidHome && map.areaManager.Home[x])
                        return false;
                }
                return true;
            }
            return CellFinder.TryFindRandomCell(map, c => Fits(c, true), out spot)
                || CellFinder.TryFindRandomCell(map, c => Fits(c, false), out spot);
        }
    }

    // ------------------------------------------------------------------ her navel

    /// <summary>The way down into her. Closes a day after her heart has been dealt with, crushing anyone still inside.</summary>
    public class BroodmotherNavel : MapPortal
    {
        int closeTick = -1;

        public bool Closing => closeTick >= 0;

        public override bool AutoDraftOnEnter => true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref closeTick, "closeTick", -1);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (!respawningAfterLoad)
                foreach (IntVec3 c in this.OccupiedRect().ExpandedBy(1))
                    if (c.InBounds(map))
                        FleckMaker.ThrowDustPuffThick(c.ToVector3Shifted(), map, 2f, new Color(0.95f, 0.9f, 0.93f));
        }

        public void BeginClosing()
        {
            if (Closing)
                return;
            closeTick = Find.TickManager.TicksGame + BroodmotherCore.CloseDelayTicks;
            Find.LetterStack.ReceiveLetter("Her navel is closing",
                "With her heart dealt with, the broodmother is drawing her navel shut. In a day it will be closed for good - and anyone still inside her will be crushed in her folds.\n\nGet everyone out.",
                LetterDefOf.ThreatBig, this);
        }

        protected override void Tick()
        {
            base.Tick();
            if (Closing && Find.TickManager.TicksGame >= closeTick)
                Close();
        }

        void Close()
        {
            Map map = Map;
            IntVec3 pos = Position;
            if (PocketMapExists)
            {
                var crush = new DamageInfo(DamageDefOf.Crush, 99999f, 999f);
                for (int i = pocketMap.mapPawns.AllPawns.Count - 1; i >= 0; i--)
                {
                    Pawn p = pocketMap.mapPawns.AllPawns[i];
                    p.TakeDamage(crush);
                    if (!p.Dead)
                        p.Kill(crush);
                }
                PocketMapUtility.DestroyPocketMap(pocketMap);
            }
            Thing.allowDestroyNonDestroyable = true;
            Destroy(DestroyMode.Vanish);
            Thing.allowDestroyNonDestroyable = false;
            Messages.Message("The broodmother's navel has closed.", new TargetInfo(pos, map), MessageTypeDefOf.NeutralEvent);
        }

        public override bool IsEnterable(out string reason)
        {
            if (Closing && !beenEntered)
            {
                reason = "Her navel is closing.";
                return false;
            }
            return base.IsEnterable(out reason);
        }

        public override string GetInspectString()
        {
            string s = base.GetInspectString();
            if (!Closing)
                return s;
            string closing = "Closing in " + (closeTick - Find.TickManager.TicksGame).ToStringTicksToPeriod();
            return s.NullOrEmpty() ? closing : s + "\n" + closing;
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (Gizmo g in base.GetGizmos())
                yield return g;
            if (DebugSettings.ShowDevGizmos && !Closing)
                yield return new Command_Action { defaultLabel = "DEV: Begin closing", action = BeginClosing };
        }
    }

    // ------------------------------------------------------------------ inside her

    /// <summary>
    /// Inside the broodmother: solid goo under an overhead mountain (no drop pods, no shuttles)
    /// with one long passage winding round the map from the way in, through three of her brood's
    /// chambers, to her heart at the centre. The passage is clenched shut between chambers by
    /// sphincters of heart-goo, and the heart's chamber sits inside a shell of it. In there, a
    /// moat of deep milk rings the heart, crossed by a single shallow causeway.
    /// </summary>
    public class GenStep_BroodmotherDepths : GenStep
    {
        const float ChamberRadius = 4.6f;
        const float HeartChamberRadius = 10.5f;
        const float ShellThickness = 3.2f;
        const float MoatInner = 4.2f, MoatOuter = 6.8f;

        public override int SeedPart => 0x4EA270;

        public override void Generate(Map map, GenStepParams parms)
        {
            var d = BroodmotherCoreDefOf.Neph_CondensedGoo;
            var open = new HashSet<IntVec3>();
            IntVec3 centre = map.Center;
            float reach = Mathf.Min(map.Size.x, map.Size.z) / 2f;

            // the way: in from near the edge, round past three chambers, then in to her heart
            float a0 = Rand.Range(0f, 360f);
            float turn = Rand.Bool ? 1f : -1f;
            IntVec3 At(float angle, float radius) =>
                (centre.ToVector3Shifted() + Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward * radius).ToIntVec3().ClampInsideMap(map);
            IntVec3 entry = At(a0, reach - 9f);
            var chambers = new List<IntVec3>
            {
                At(a0 + turn * Rand.Range(80f, 100f), reach * Rand.Range(0.58f, 0.68f)),
                At(a0 + turn * Rand.Range(170f, 200f), reach * Rand.Range(0.58f, 0.68f)),
                At(a0 + turn * Rand.Range(260f, 290f), reach * Rand.Range(0.5f, 0.6f)),
            };
            var waypoints = new List<IntVec3> { entry };
            waypoints.AddRange(chambers);
            waypoints.Add(centre);

            var noise = new Perlin(0.05, 2.0, 0.5, 3, Rand.Int, QualityMode.Medium);
            var sphincters = new List<IntVec3>();
            for (int i = 0; i < waypoints.Count - 1; i++)
            {
                List<IntVec3> path = Carve(map, waypoints[i], waypoints[i + 1], Rand.Range(1.3f, 1.9f), noise, open);
                // between chambers (not before the first, not into the heart: the shell does that)
                if (i >= 1 && i < waypoints.Count - 2 && path.Count > 6)
                    sphincters.Add(path[path.Count / 2]);
            }
            foreach (IntVec3 c in chambers)
                Dig(map, c, ChamberRadius + Rand.Range(-0.5f, 0.8f), open);
            Dig(map, entry, 4.5f, open);
            Dig(map, centre, HeartChamberRadius, open);

            // the last approach to the causeway comes in from the last chamber
            Vector3 toHeart = (chambers[chambers.Count - 1].ToVector3Shifted() - centre.ToVector3Shifted()).normalized;

            foreach (IntVec3 c in map.AllCells)
            {
                map.roofGrid.SetRoof(c, RoofDefOf.RoofRockThick);
                float r = c.DistanceTo(centre);
                TerrainDef floor = BroodmotherCoreDefOf.Neph_NephilaSoilRich;
                ThingDef wall = open.Contains(c) ? null : d;
                if (r > HeartChamberRadius && r <= HeartChamberRadius + ShellThickness)
                    wall = BroodmotherCoreDefOf.Neph_HeartGoo;
                else if (r <= HeartChamberRadius && r >= MoatInner && r <= MoatOuter)
                {
                    // the moat, and its one causeway
                    Vector3 v = c.ToVector3Shifted() - centre.ToVector3Shifted();
                    float off = Vector3.Cross(toHeart, v).magnitude;
                    bool causeway = Vector3.Dot(toHeart, v) > 0f && off < 1.6f;
                    floor = causeway ? BroodmotherCoreDefOf.Neph_MilkShallow : BroodmotherCoreDefOf.Neph_MilkDeep;
                }
                map.terrainGrid.SetTerrain(c, floor);
                if (wall != null)
                    GenSpawn.Spawn(wall, c, map);
            }
            foreach (IntVec3 s in sphincters)
                foreach (IntVec3 c in GenRadial.RadialCellsAround(s, 2.6f, true))
                    if (c.InBounds(map) && open.Contains(c))
                        GenSpawn.Spawn(BroodmotherCoreDefOf.Neph_HeartGoo, c, map);

            // a little milk pooled in each chamber
            foreach (IntVec3 c in chambers)
                foreach (IntVec3 x in GenRadial.RadialCellsAround(c + IntVec3.North * Rand.RangeInclusive(-1, 1), Rand.Range(1.5f, 2.3f), true))
                    if (x.InBounds(map) && x.GetEdifice(map) == null)
                        map.terrainGrid.SetTerrain(x, BroodmotherCoreDefOf.Neph_MilkShallow);

            GenSpawn.Spawn(BroodmotherCoreDefOf.Neph_BroodmotherHeart, centre, map);

            // the way back up
            GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.CaveExit), entry, map);
            MapGenerator.PlayerStartSpot = entry;

            SpawnBrood(map, chambers, centre);
        }

        /// <summary>A noisy, wandering tunnel from a to b. Returns the cells along its spine.</summary>
        static List<IntVec3> Carve(Map map, IntVec3 a, IntVec3 b, float width, Perlin noise, HashSet<IntVec3> open)
        {
            var spine = new List<IntVec3>();
            Vector3 pos = a.ToVector3Shifted();
            Vector3 end = b.ToVector3Shifted();
            for (int step = 0; step < 600 && (pos - end).MagnitudeHorizontal() > 1.5f; step++)
            {
                Vector3 dir = (end - pos).normalized;
                float wander = (float)noise.GetValue(pos.x, 0, pos.z) * 70f;
                pos += Quaternion.AngleAxis(wander, Vector3.up) * dir * 0.8f;
                pos.x = Mathf.Clamp(pos.x, 4f, map.Size.x - 5f);
                pos.z = Mathf.Clamp(pos.z, 4f, map.Size.z - 5f);
                IntVec3 c = pos.ToIntVec3();
                if (spine.Count == 0 || spine[spine.Count - 1] != c)
                    spine.Add(c);
                Dig(map, c, width + (float)noise.GetValue(pos.z, 0, pos.x) * 0.5f, open);
            }
            return spine;
        }

        static void Dig(Map map, IntVec3 at, float radius, HashSet<IntVec3> open)
        {
            foreach (IntVec3 c in GenRadial.RadialCellsAround(at, Mathf.Max(radius, 1f), true))
                if (c.InBounds(map) && c.DistanceToEdge(map) > 1)
                    open.Add(c);
        }

        /// <summary>
        /// Her brood: half again the colony's usual threat, a third spread over the three chambers,
        /// the rest at her heart. Each group holds its own chamber.
        /// </summary>
        static void SpawnBrood(Map map, List<IntVec3> chambers, IntVec3 heart)
        {
            Map surface = (map.Parent as PocketMapParent)?.sourceMap;
            float points = Mathf.Max(600f, (surface != null ? StorytellerUtility.DefaultThreatPointsNow(surface) : 600f) * 1.5f);
            Faction brood = BroodmotherCore.Brood;
            foreach (IntVec3 c in chambers)
                SpawnGroup(map, brood, c, points * 0.35f / chambers.Count, ChamberRadius);
            SpawnGroup(map, brood, heart, points * 0.65f, HeartChamberRadius - 1f);
        }

        static void SpawnGroup(Map map, Faction brood, IntVec3 at, float points, float radius)
        {
            var kinds = new List<(string kind, float weight)>
            {
                ("Neph_BroodGuard", 10f), ("Neph_BroodGuardEnticer", 4f), ("Neph_BroodGuardSniper", 3f), ("Neph_BroodGuardHeavy", 2f),
            };
            var pawns = new List<Pawn>();
            while (points > 0f || pawns.Count == 0)
            {
                PawnKindDef kind = DefDatabase<PawnKindDef>.GetNamed(kinds.RandomElementByWeight(k => k.weight).kind);
                Pawn p = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind, brood, mustBeCapableOfViolence: true));
                if (!CellFinder.TryFindRandomCellNear(at, map, Mathf.CeilToInt(radius), c => c.Standable(map) && !c.GetTerrain(map).IsWater && c.GetEdifice(map) == null, out IntVec3 cell))
                    cell = at;
                GenSpawn.Spawn(p, cell, map);
                pawns.Add(p);
                points -= kind.combatPower;
                if (pawns.Count >= 12)
                    break;
            }
            LordMaker.MakeNewLord(brood, new LordJob_DefendPoint(at, radius * 0.6f, radius + 4f, false, false), map, pawns);
        }
    }

    // ------------------------------------------------------------------ her heart

    public class CompProperties_BroodmotherHeart : CompProperties
    {
        public CompProperties_BroodmotherHeart() => compClass = typeof(CompBroodmotherHeart);
    }

    /// <summary>Communing with her heart: only once her brood are no longer guarding it.</summary>
    public class CompBroodmotherHeart : ThingComp
    {
        public bool resolved;

        public override void PostExposeData() => Scribe_Values.Look(ref resolved, "resolved");

        bool Guarded(out int guards)
        {
            guards = parent.Map.mapPawns.AllPawnsSpawned.Count(p => p.Faction == BroodmotherCore.Brood && !p.Downed && !p.Dead);
            return guards > 0;
        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            if (resolved)
                yield break;
            if (Guarded(out int guards))
            {
                yield return new FloatMenuOption($"Can't commune with her heart: her brood still guard it ({guards} left)", null);
                yield break;
            }
            if (!selPawn.CanReach(parent, PathEndMode.Touch, Danger.Deadly))
            {
                yield return new FloatMenuOption("Can't commune with her heart: no path", null);
                yield break;
            }
            yield return new FloatMenuOption("Commune with the broodmother's heart", () =>
                selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(BroodmotherCoreDefOf.Neph_CommuneWithHeart, parent), JobTag.Misc));
        }

        public override string CompInspectStringExtra()
        {
            if (resolved)
                return "Her heart is quiet now.";
            return Guarded(out int guards) ? $"Guarded by her brood ({guards} left)." : "Unguarded. Someone could commune with it.";
        }

        /// <summary>The choice, once someone has knelt at her heart.</summary>
        public void Commune(Pawn pawn)
        {
            if (resolved)
                return;
            var node = new DiaNode(
                $"{pawn.LabelShort} lays a hand on the broodmother's heart. It is warm, and it beats - slow and heavy - against the palm. All around, the walls tighten very slightly, as though she is holding her breath.\n\n" +
                "She will let one thing happen here, and only one.");
            node.options.Add(new DiaOption("Tear out her heart")
            {
                action = () => TearOut(pawn),
                resolveTree = true,
            });
            node.options.Add(new DiaOption($"Let her embrace {pawn.LabelShort}")
            {
                action = () => Embrace(pawn),
                resolveTree = true,
            });
            node.options.Add(new DiaOption("Leave it be, for now") { resolveTree = true });
            Find.WindowStack.Add(new Dialog_NodeTree(node, true, false, "The broodmother's heart"));
        }

        void TearOut(Pawn pawn)
        {
            resolved = true;
            Map map = parent.Map;
            IntVec3 pos = parent.Position;
            Thing heart = ThingMaker.MakeThing(BroodmotherCoreDefOf.Neph_BroodmotherHeartItem);
            Thing.allowDestroyNonDestroyable = true;
            parent.Destroy(DestroyMode.Vanish);
            Thing.allowDestroyNonDestroyable = false;
            GenPlace.TryPlaceThing(heart, pos, map, ThingPlaceMode.Near);
            foreach (IntVec3 c in GenRadial.RadialCellsAround(pos, 2.5f, true))
                if (c.InBounds(map) && c.Standable(map))
                    FilthMaker.TryMakeFilth(c, map, ThingDefOf.Filth_Slime);
            Find.CameraDriver.shaker.DoShake(0.3f);
            if (Current.Game.GetComponent<GameComponent_BroodmotherCore>() is GameComponent_BroodmotherCore core)
                core.heartTorn = true;
            Find.LetterStack.ReceiveLetter("Her heart torn out",
                $"{pawn.LabelShort} has torn the broodmother's heart out of her. It still beats in {pawn.gender.GetPossessive()} hands.\n\nAll around, her body convulses. She will live - a body this vast has many hearts - but she will never let any of you near another one. Her navel will not open for you again.\n\nShe is drawing it shut now, and anyone left inside her when it closes will be crushed.\n\nThe heart itself is made of her nanites. Whoever takes it into their chest will be hers before long.",
                LetterDefOf.PositiveEvent, new LookTargets(heart));
            BroodmotherCore.NavelInto(map)?.BeginClosing();
        }

        void Embrace(Pawn pawn)
        {
            resolved = true;
            Map map = parent.Map;
            if (!pawn.health.hediffSet.HasHediff(BroodmotherCoreDefOf.Neph_BroodmotherEmbrace))
                pawn.health.AddHediff(BroodmotherCoreDefOf.Neph_BroodmotherEmbrace);
            Pawn daughter = PawnGenerator.GeneratePawn(new PawnGenerationRequest(BroodmotherCoreDefOf.NephilaMatronPlayer, Faction.OfPlayer, PawnGenerationContext.NonPlayer, forceGenerateNewPawn: true));
            if (!CellFinder.TryFindRandomCellNear(parent.Position, map, 3, c => c.Standable(map) && !c.GetTerrain(map).IsWater, out IntVec3 at))
                at = pawn.Position;
            GenSpawn.Spawn(daughter, at, map);
            Find.LetterStack.ReceiveLetter("Embraced by her",
                $"{pawn.LabelShort} let the broodmother embrace {pawn.gender.GetObjective()}. The walls closed softly around {pawn.gender.GetObjective()}, warm and close, and held {pawn.gender.GetObjective()} for a long time.\n\nWhen they let go, {daughter.LabelShort} came out of them: one of her daughters, sent up with {pawn.LabelShort} to the surface. She has joined you.\n\nNow the broodmother is drawing her navel shut. Anyone left inside her when it closes will be crushed. She has other hearts, though - and if you stay, one day she may open up to you again.",
                LetterDefOf.PositiveEvent, new LookTargets(pawn, daughter));
            BroodmotherCore.NavelInto(map)?.BeginClosing();
        }
    }

    public class JobDriver_CommuneWithHeart : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_General.WaitWith(TargetIndex.A, 600, true, false, false, TargetIndex.A, PathEndMode.Touch);
            Toil commune = ToilMaker.MakeToil("Commune");
            commune.initAction = () => job.targetA.Thing?.TryGetComp<CompBroodmotherHeart>()?.Commune(pawn);
            commune.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return commune;
        }
    }
}
