using Verse;
using RimWorld;

namespace Nephila
{
    // Component that reports the work speed of a building
    public class CompReportWorkSpeed : ThingComp
    {
        public override string CompInspectStringExtra()
        {
            if (this.parent is Building_WorkTable parentBuilding)
            {
                // Return the work speed factor
                return "Work Speed: " + parentBuilding.GetStatValue(StatDefOf.WorkTableWorkSpeedFactor).ToStringPercent();
            }

            // Return a default message if the cast is invalid
            return "Parent is not a WorkTable.";
        }
    }

    // Properties for the CompReportWorkSpeed component
    public class CompProperties_ReportWorkSpeed : CompProperties
    {
        public CompProperties_ReportWorkSpeed()
        {
            this.compClass = typeof(CompReportWorkSpeed);
        }
    }
}
