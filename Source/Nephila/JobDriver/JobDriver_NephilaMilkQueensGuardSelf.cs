using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Nephila
{
    public class JobDriver_NephilaMilkQueensGuardSelf : JobDriver
    {
        protected float WorkTotal
        {
            get
            {
                return 600f;
            }
        }
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            Toil wait = new Toil();
            wait.tickAction = delegate ()
            {
                Pawn actor = wait.actor;
                this.gatherProgress += actor.GetStatValue(StatDefOf.AnimalGatherSpeed, true);
                bool flag = this.gatherProgress >= this.WorkTotal;
                if (flag)
                {
                    Pawn thing = actor;
                    thing.TryGetComp<CompNephilaMilkableHumanoid>().GatherMilkSelf();
                    actor.jobs.EndCurrentJob(JobCondition.Succeeded, true);
                }
            };
            wait.AddEndCondition(delegate
            {
                Pawn actor = wait.actor;
                bool flag = !actor.TryGetComp<CompNephilaMilkableHumanoid>().ActiveAndCanBeMilked;
                JobCondition result;
                if (flag)
                {
                    result = JobCondition.Incompletable;
                }
                else
                {
                    result = JobCondition.Ongoing;
                }
                return result;
            });
            wait.defaultCompleteMode = ToilCompleteMode.Never;
            wait.WithProgressBar(TargetIndex.A, () => this.gatherProgress / this.WorkTotal, false, -0.5f);
            yield return wait;
            yield break;
        }
        private float gatherProgress;
    }
}
