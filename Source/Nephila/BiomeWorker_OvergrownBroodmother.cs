using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Nephila
{
    /// <summary>
    /// Where an overgrown broodmother lies: warm, wet land, and rarer than a Nephilitic Eden.
    /// (On a planet that wants more of her - an Alien Worlds planet type - its biome config
    /// raises her score instead.)
    /// </summary>
    public class BiomeWorker_OvergrownBroodmother : BiomeWorker
    {
        const float MinRainfall = 900f;
        const float MinTemperature = 4f;
        const float Rarity = 0.0035f;

        public override float GetScore(BiomeDef biome, Tile tile, PlanetTile planetTile)
        {
            if (tile.WaterCovered)
                return -100f;
            // she is a body to live in, not a wall: never on impassable ground (the
            // world step makes all her tiles mountainous - WorldGenStep_BroodmotherShape)
            if (tile.hilliness == Hilliness.Impassable)
                return 0f;
            if (tile.temperature < MinTemperature || tile.rainfall < MinRainfall)
                return 0f;
            // seeded by the tile, so the same world always grows her in the same places
            if (Rand.ValueSeeded(planetTile.tileId ^ 0x42524F44) > Rarity)
                return 0f;
            return 18f + (tile.temperature - 4f) + (tile.rainfall - MinRainfall) / 160f;
        }
    }
}
