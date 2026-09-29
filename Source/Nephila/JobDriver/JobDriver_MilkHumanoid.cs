using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Nephila
{
    public class JobDriver_MilkHumanoid : JobDriver
    {
        protected float WorkTotal
        {
            get
            {
                return 400f;
            }
        }
        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return true;
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOnDowned(TargetIndex.A);
            this.FailOnNotCasualInterruptible(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            Toil wait = new Toil();
            wait.initAction = delegate ()
            {
                Pawn actor = wait.actor;
                Pawn pawn = (Pawn)wait.actor.CurJob.GetTarget(TargetIndex.A).Thing;
                actor.pather.StopDead();
                PawnUtility.ForceWait(pawn, 15000, null, true);
            };
            wait.tickAction = delegate ()
            {
                Pawn actor = wait.actor;
                actor.skills.Learn(SkillDefOf.Social, 0.142999992f, false);
                this.gatherProgress += actor.GetStatValue(StatDefOf.AnimalGatherSpeed, true);
                bool flag = this.gatherProgress >= this.WorkTotal;
                if (flag)
                {
                    Pawn thing = (Pawn)((Thing)this.job.GetTarget(TargetIndex.A));
                    thing.TryGetComp<CompNephilaMilkableHumanoid>().GatherMilk(this.pawn);
                    actor.jobs.EndCurrentJob(JobCondition.Succeeded, true);
                }
            };
            wait.AddFinishAction(delegate
            {
                Pawn pawn = (Pawn)wait.actor.CurJob.GetTarget(TargetIndex.A).Thing;
                bool flag = pawn.jobs.curJob.def == JobDefOf.Wait_MaintainPosture;
                if (flag)
                {
                    pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, true);
                }
            });
            wait.FailOnDespawnedOrNull(TargetIndex.A);
            wait.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
            wait.AddEndCondition(delegate
            {
                Pawn thing = (Pawn)((Thing)this.job.GetTarget(TargetIndex.A));
                bool flag = !thing.TryGetComp<CompNephilaMilkableHumanoid>().ActiveAndCanBeMilked;
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
