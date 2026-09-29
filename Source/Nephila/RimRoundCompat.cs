using System.Collections.Generic;
using System.Reflection;
using RimRound.Comps;
using RimRound.Utilities;
using RimWorld;
using Verse;

namespace Nephila
{
    /// <summary>
    /// Tells RimRound how to draw the races it should draw. The Nephila castes are left out
    /// on purpose - each caste is its own sprite, and RimRound's bodies would replace them -
    /// so they keep their art and just carry weight. The human-equivalent race is drawn like
    /// a human, so it gets RimRound's human bodies.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class RimRoundCompat
    {
        static readonly string[] HumanlikeRaces = { "NephilaHumanEquiv" };

        static RimRoundCompat()
        {
            var table = RacialBodyTypeInfoUtility.raceToProperDictDictionary;
            FieldInfo field = typeof(RacialBodyTypeInfoUtility).GetField("defaultSet", BindingFlags.NonPublic | BindingFlags.Static);
            if (!(field?.GetValue(null) is Dictionary<Gender, Dictionary<BodyArchetype, Dictionary<BodyTypeDef, BodyTypeInfo>>> defaultSet))
            {
                Log.Warning("[RimRound: Nephila] Couldn't find RimRound's default body set; human-equivalent Nephila pawns won't get RimRound bodies.");
                return;
            }
            foreach (string race in HumanlikeRaces)
                if (!table.ContainsKey(race) && DefDatabase<ThingDef>.GetNamedSilentFail(race) != null)
                    table.Add(race, new Dictionary<Gender, Dictionary<BodyArchetype, Dictionary<BodyTypeDef, BodyTypeInfo>>>(defaultSet));
        }
    }
}
