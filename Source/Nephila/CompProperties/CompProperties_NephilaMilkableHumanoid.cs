using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Nephila
{
    public class CompProperties_NephilaMilkableHumanoid : CompProperties
    {
        public CompProperties_NephilaMilkableHumanoid()
        {
            this.compClass = typeof(CompNephilaMilkableHumanoid);
        }
        public ThingDef milkDef;

        public int milkAmount = 15;

        public ThoughtDef milkThoughtMilker;

        public ThoughtDef milkThoughtMilked;

        public ThoughtDef milkThoughtMilkedSelf;

        public int ticksUntilMilking = 60000;

        public bool onlyFemales = true;

        public bool onlyMales;

        public bool canMilkThemselves = true;

        public List<JobDef> forbiddenJobsToInterrupt = new List<JobDef>();

        public int minimumAgeToBeMilked;

        public string milkProgessKeyString = "NephilaMilkProgress";

        public bool firstResourceName;

        public string nephilaMilkProgessKeyString = "NephilaResourceProgress";

        /// <summary>RimRound weight each gathered item takes off her.</summary>
        public float kilosShedPerGather = 3f;
    }
}
