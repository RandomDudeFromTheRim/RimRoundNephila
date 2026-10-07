using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Noise;

namespace Nephila
{
    /// <summary>
    /// Shapes the world around an overgrown broodmother (after rivers, before mutators and
    /// factions): her tiles are all mountainous - she is a body, not a plain - and milk runs
    /// through every one of them. Each of her tiles gets a river link, threaded through her
    /// region from wherever it touches an existing river or the sea, so her rivers show on
    /// the world map and every map of her has a milk river in it.
    /// </summary>
    public class WorldGenStep_BroodmotherShape : WorldGenStep
    {
        public override int SeedPart => 0x4E42524F;

        public override void GenerateFresh(string seed, PlanetLayer layer)
        {
            BiomeDef biome = DefDatabase<BiomeDef>.GetNamedSilentFail("Neph_OvergrownBroodmother");
            if (biome == null || layer?.Def != PlanetLayerDefOf.Surface)
                return;
            RiverDef river = DefDatabase<RiverDef>.GetNamedSilentFail("River") ?? DefDatabase<RiverDef>.AllDefsListForReading.FirstOrDefault();
            RiverDef creek = DefDatabase<RiverDef>.GetNamedSilentFail("Creek") ?? river;

            var hers = new HashSet<PlanetTile>();
            foreach (Tile t in layer.Tiles)
                if (t.PrimaryBiome == biome)
                    hers.Add(t.tile);
            if (hers.Count == 0)
                return;

            foreach (PlanetTile pt in hers)
            {
                Tile t = layer[pt];
                if (t.hilliness != Hilliness.Impassable)
                    t.hilliness = Hilliness.Mountainous;
            }

            // milk: grow outward through her region from tiles that already touch water or a
            // river, linking each newly reached tile back to the one it was reached from
            var neighbours = new List<PlanetTile>();
            var linked = new HashSet<PlanetTile>();
            var frontier = new Queue<PlanetTile>();
            foreach (PlanetTile pt in hers)
            {
                if (HasRiver(layer[pt]))
                {
                    linked.Add(pt);
                    frontier.Enqueue(pt);
                    continue;
                }
                layer.GetTileNeighbors(pt, neighbours);
                List<PlanetTile> outlets = neighbours.Where(n => layer[n].WaterCovered || (!hers.Contains(n) && HasRiver(layer[n]))).ToList();
                if (outlets.Count > 0)
                {
                    Find.WorldGrid.OverlayRiver(pt, outlets[0], river);
                    linked.Add(pt);
                    frontier.Enqueue(pt);
                }
            }
            while (true)
            {
                while (frontier.Count > 0)
                {
                    PlanetTile cur = frontier.Dequeue();
                    layer.GetTileNeighbors(cur, neighbours);
                    foreach (PlanetTile n in neighbours)
                    {
                        if (!hers.Contains(n) || linked.Contains(n))
                            continue;
                        Find.WorldGrid.OverlayRiver(n, cur, linked.Count % 3 == 0 ? creek : river);
                        linked.Add(n);
                        frontier.Enqueue(n);
                    }
                }
                // a patch of her with no water anywhere near: it springs from her lowest point
                List<PlanetTile> unlinked = hers.Where(p => !linked.Contains(p)).OrderBy(p => layer[p].elevation).ToList();
                if (unlinked.Count == 0)
                    break;
                PlanetTile source = unlinked[0];
                layer.GetTileNeighbors(source, neighbours);
                List<PlanetTile> down = neighbours.Where(n => !layer[n].WaterCovered).OrderBy(n => layer[n].elevation).ToList();
                if (down.Count > 0)
                    Find.WorldGrid.OverlayRiver(source, down[0], creek);
                linked.Add(source);
                frontier.Enqueue(source);
            }
        }

        static bool HasRiver(Tile t) => t is SurfaceTile s && !s.Rivers.NullOrEmpty();
    }

    /// <summary>
    /// Her folds (every overgrown broodmother map that isn't a landmark): the land rises into
    /// condensed goo almost everywhere, leaving only the narrow creases between her folds,
    /// the odd soft chamber, and a hollow in the middle to settle in. Runs before caves, rivers
    /// and lakes, so they still cut through her.
    /// </summary>
    public class TileMutatorWorker_BroodmotherFolds : TileMutatorWorker
    {
        /// <summary>Half-width of a crease in noise units: wider means roomier passages.</summary>
        const float CreaseWidth = 0.09f;
        const float ChamberThreshold = 0.62f;
        const float CenterHollowRadius = 13f;

        public TileMutatorWorker_BroodmotherFolds(TileMutatorDef def) : base(def) { }

        public override void GeneratePostElevationFertility(Map map)
        {
            int seed = map.Tile.tileId;
            ModuleBase folds = new Perlin(0.028, 2.0, 0.5, 3, seed, QualityMode.Medium);
            ModuleBase chambers = new Perlin(0.016, 2.0, 0.5, 2, seed ^ 0x5EED, QualityMode.Medium);
            MapGenFloatGrid elevation = MapGenerator.Elevation;
            IntVec3 center = map.Center;
            foreach (IntVec3 c in map.AllCells)
            {
                float f = Mathf.Abs((float)folds.GetValue(c.x, 0.0, c.z));
                float d = c.DistanceTo(center);
                if (f < CreaseWidth)
                    elevation[c] = 0.45f * f / CreaseWidth;         // a crease between folds
                else if ((float)chambers.GetValue(c.x, 0.0, c.z) > ChamberThreshold)
                    elevation[c] = 0.5f;                            // a soft chamber
                else if (d < CenterHollowRadius)
                    elevation[c] = 0.55f * d / CenterHollowRadius;  // the hollow to settle in
                else
                    elevation[c] = Mathf.Max(elevation[c], 0.74f + 0.25f * Mathf.Clamp01(f));  // her flesh, set hard
            }
        }
    }
}
