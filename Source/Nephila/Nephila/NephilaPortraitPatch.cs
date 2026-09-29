using AlienRace;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace Nephila
{
    /// <summary>
    /// The castes draw bigger than a person (their adult draw size, set against RimRound's
    /// bodies of the same bulk), so their portraits zoom out by as much and still show the
    /// whole woman, as they would at a person's size.
    /// </summary>
    [HarmonyPatch(typeof(PortraitsCache), nameof(PortraitsCache.Get))]
    public static class NephilaPortraitPatch
    {
        public static void Prefix(Pawn pawn, ref float cameraZoom)
        {
            if (pawn?.ageTracker?.CurLifeStageRace is LifeStageAgeAlien stage && pawn.TryGetComp<CompNephilaMass>() != null)
            {
                float size = stage.customPortraitDrawSize.x;
                if (size > 1.01f)
                    cameraZoom /= size;
            }
        }
    }
}
