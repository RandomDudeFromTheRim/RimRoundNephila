using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Nephila
{
    /// <summary>The Nephilitic Eden: rare patches on warm, wet land.</summary>
    public class BiomeWorker_NephiliticEden : BiomeWorker
    {
        const float MinRainfall = 610f;
        const float MinTemperature = -10f;
        const float Rarity = 0.007f;

        public override float GetScore(BiomeDef biome, Tile tile, PlanetTile planetTile)
        {
            if (tile.WaterCovered)
                return -100f;
            if (tile.temperature < MinTemperature || tile.rainfall < MinRainfall)
                return 0f;
            // seeded by the tile, so the same world always grows the same Edens
            if (Rand.ValueSeeded(planetTile.tileId ^ 0x4E455048) > Rarity)
                return 0f;
            return 16f + (tile.temperature - 7f) + (tile.rainfall - MinRainfall) / 180f;
        }
    }
}
