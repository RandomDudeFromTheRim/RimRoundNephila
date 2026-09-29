using System;
using RimWorld;
using Verse;

namespace Nephila
{
    public class CompProperties_NephilaHatcher : CompProperties
    {
        public CompProperties_NephilaHatcher()
        {
            this.compClass = typeof(CompNephilaHatcher);
        }

        public float hatcherDaystoHatch = 1f;

        public PawnKindDef hatcherPawn;
    }
}
