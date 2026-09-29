using System.Collections.Generic;
using Verse;

namespace Nephila
{
    public class NephilaSpecialSpawnsDef : Def
    {
        public ThingDef thingDef;

        public bool allowOnWater;

        public IntRange numberToSpawn;

        public List<string> terrainValidationAllowed;

        public List<string> terrainValidationDisallowed;

        public string allowedBiome;

        public List<string> biomesWithExtraGeneration;

        public int extraGeneration = 0;

        public string disallowedBiome;

        public bool findCellsOutsideColony = false;
    }
}
