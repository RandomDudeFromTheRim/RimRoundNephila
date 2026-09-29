using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Nephila
{
    /// <summary>
    /// Nephilitic terrain (NephilaTerrainCost) is easy going for pawns that carry one of its
    /// tags - on their race, kind, faction or clothes (NephilaPawnTerrainHandler) - and
    /// heavy going for everyone else. Runs for every step pawns path through, so anything
    /// but that terrain returns at once.
    /// </summary>
    [HarmonyPatch(typeof(Pawn_PathFollower), "CostToMoveIntoCell", new Type[] { typeof(Pawn), typeof(IntVec3) })]
    internal static class NephilaCostToMove
    {
        static readonly Dictionary<TerrainDef, NephilaTerrainCost> costs = new Dictionary<TerrainDef, NephilaTerrainCost>();

        static NephilaTerrainCost CostFor(TerrainDef terrain)
        {
            if (!costs.TryGetValue(terrain, out NephilaTerrainCost cost))
                costs[terrain] = cost = terrain.GetModExtension<NephilaTerrainCost>();
            return cost;
        }

        public static void Postfix(ref float __result, Pawn pawn, IntVec3 c)
        {
            Map map = pawn?.Map;
            TerrainDef terrain = map == null ? null : c.GetTerrain(map);
            if (terrain == null)
                return;
            NephilaTerrainCost cost = CostFor(terrain);
            if (cost?.tags == null)
                return;
            // pawns with no terrain tags at all walk it like any other ground
            bool tagged = false, match = false;
            Check(pawn.def.GetModExtension<NephilaPawnTerrainHandler>(), cost.tags, ref tagged, ref match);
            Check(pawn.kindDef?.GetModExtension<NephilaPawnTerrainHandler>(), cost.tags, ref tagged, ref match);
            Check(pawn.Faction?.def?.GetModExtension<NephilaPawnTerrainHandler>(), cost.tags, ref tagged, ref match);
            List<Apparel> worn = pawn.apparel?.WornApparel;
            if (worn != null)
                for (int i = 0; i < worn.Count && !match; i++)
                    Check(worn[i].def.GetModExtension<NephilaPawnTerrainHandler>(), cost.tags, ref tagged, ref match);
            if (match)
                __result = Math.Max(1f, __result - cost.costToRefund);
            else if (tagged)
                __result += cost.costToAdd;
        }

        static void Check(NephilaPawnTerrainHandler handler, List<string> tags, ref bool tagged, ref bool match)
        {
            if (handler?.tags == null || handler.tags.Count == 0)
                return;
            tagged = true;
            for (int i = 0; i < handler.tags.Count; i++)
                if (tags.Contains(handler.tags[i]))
                    match = true;
        }
    }
}
