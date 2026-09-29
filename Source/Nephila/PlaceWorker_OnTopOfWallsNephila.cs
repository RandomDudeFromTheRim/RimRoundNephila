using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Nephila
{
    public class PlaceWorker_OnTopOfWallsNephila : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            Building edifice = loc.GetEdifice(map);
            if (edifice == null || edifice.def == null || edifice.def.graphicData == null)
            {
                return "Must be placed on walls.";
            }

            if ((edifice.def.graphicData.linkFlags & (LinkFlags.Rock | LinkFlags.Wall)) == LinkFlags.None)
            {
                return "Must be placed on walls.";
            }

            IntVec3 facingCell = loc + rot.FacingCell;
            if (facingCell.InBounds(map))
            {
                Building facingEdifice = facingCell.GetEdifice(map);
                if (facingEdifice != null && facingEdifice.def != null && facingEdifice.def.graphicData != null)
                {
                    return "Must have open space in front.";
                }
            }

            return AcceptanceReport.WasAccepted;
        }

        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing _thing = null)
        {
            base.DrawGhost(def, center, rot, ghostCol, null);
            bool invalidPlacement = false;
            Map currentMap = Find.CurrentMap;

            if (!(center + IntVec3.South.RotatedBy(rot)).InBounds(currentMap))
            {
                invalidPlacement = true;
            }

            Room room = (center + new IntVec3(0, 0, 1).RotatedBy(rot)).GetRoom(currentMap);
            if (room != null)
            {
                if (room != StaticWallSconceStuffNephila.lastRoom)
                {
                    StaticWallSconceStuffNephila.lastRoom = room;
                    StaticWallSconceStuffNephila.preLitCells.Clear();
                    foreach (IntVec3 intVec in room.Cells)
                    {
                        foreach (Thing thing in intVec.GetThingList(currentMap))
                        {
                            if (thing.def.defName.Contains("SconceGlower"))
                            {
                                StaticWallSconceStuffNephila.preLitCells.Add(intVec);
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                invalidPlacement = true;
            }

            if (!invalidPlacement)
            {
                StaticWallSconceStuffNephila.ClearCells();
                foreach (IntVec3 pos in StaticWallSconceStuffNephila.preLitCells)
                {
                    StaticWallSconceStuffNephila.GetLightCells(pos, currentMap);
                }
                StaticWallSconceStuffNephila.GetLightCells(center + IntVec3.South.RotatedBy(rot), currentMap);
                if (StaticWallSconceStuffNephila.totalCells.Count > 0)
                {
                    GenDraw.DrawFieldEdges(StaticWallSconceStuffNephila.totalCells);
                }
            }
        }
    }
}
