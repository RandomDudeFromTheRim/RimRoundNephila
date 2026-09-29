using System.Collections.Generic;
using AlienRace;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace Nephila
{
    /// <summary>
    /// The castes draw at their adult life stage's draw size (matched against RimRound's bodies
    /// of the same bulk). RimRound sets every alien pawn's body draw size to its own body
    /// type's mesh size - 1 for a race it doesn't draw, like the castes - which would leave a
    /// caste's body at a person's size under a head scaled up to hers. HAR takes the sizes from
    /// the life stage just before it builds the render tree; this puts the castes' back.
    /// </summary>
    [HarmonyPatch(typeof(AlienRenderTreePatches), nameof(AlienRenderTreePatches.TrySetupGraphIfNeededPrefix))]
    public static class NephilaDrawSizePatch
    {
        static readonly Dictionary<ThingDef, bool> castes = new Dictionary<ThingDef, bool>();

        static bool IsCaste(ThingDef def)
        {
            if (!castes.TryGetValue(def, out bool caste))
                castes[def] = caste = def.HasComp(typeof(CompNephilaMass));
            return caste;
        }

        // HAR's prefix is static and takes the tree as its first argument
        public static void Postfix(PawnRenderTree __0)
        {
            Pawn pawn = __0?.pawn;
            if (pawn == null || __0.Resolved || !IsCaste(pawn.def))
                return;
            if (!(pawn.ageTracker?.CurLifeStageRace is LifeStageAgeAlien stage) || !(pawn.GetComp<AlienPartGenerator.AlienComp>() is AlienPartGenerator.AlienComp comp))
                return;
            bool female = pawn.gender == Gender.Female;
            comp.customDrawSize = female && stage.customFemaleDrawSize != Vector2.zero ? stage.customFemaleDrawSize : stage.customDrawSize;
            comp.customPortraitDrawSize = female && stage.customFemalePortraitDrawSize != Vector2.zero ? stage.customFemalePortraitDrawSize : stage.customPortraitDrawSize;
        }
    }
}
