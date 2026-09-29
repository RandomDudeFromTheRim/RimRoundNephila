using HarmonyLib;
using Verse;

namespace Nephila
{
    public class NephilaMod : Mod
    {
        public NephilaMod(ModContentPack content) : base(content)
        {
            new Harmony("RandomDudeFromTheRim.RimRound.Nephila").PatchAll();
        }
    }
}
