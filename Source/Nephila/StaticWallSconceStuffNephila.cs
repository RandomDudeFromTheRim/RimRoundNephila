using System.Collections.Generic;
using Verse;

namespace Nephila
{
    public static class StaticWallSconceStuffNephila
    {
        public static void GetLightCells(IntVec3 pos, Map map)
        {
            if (!pos.InBounds(map))
            {
                return;
            }

            Region region = pos.GetRegion(map, RegionType.Set_Passable);
            if (region == null)
            {
                return;
            }

            RegionTraverser.BreadthFirstTraverse(region, (Region from, Region r) => r.door == null, delegate (Region r)
            {
                foreach (IntVec3 item in r.Cells)
                {
                    if (item.InHorDistOf(pos, StaticWallSconceStuffNephila.displayRad) && !StaticWallSconceStuffNephila.totalCells.Contains(item))
                    {
                        StaticWallSconceStuffNephila.totalCells.Add(item);
                    }
                }
                return false;
            }, 13, RegionType.Set_Passable);
        }

        public static void ClearCells()
        {
            StaticWallSconceStuffNephila.totalCells.Clear();
        }

        public static int lastRad = 8;

        public static int lastPow = 10;

        public static int lastCost = 5;

        public static float displayRad = 5.2f;

        public static List<IntVec3> preLitCells = new List<IntVec3>();

        public static List<IntVec3> totalCells = new List<IntVec3>();

        public static Room lastRoom;
    }
}
