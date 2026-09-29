using System.Collections.Generic;
using System.Linq;
using RimRound.Utilities;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Nephila
{
    /// <summary>
    /// Turns a pawn into another kind - a woman into a Nephila, a Nephila into the next
    /// caste. The nanites rebuild the body, so this makes a new pawn of the new race, but
    /// the person carries over: name, age, skills and passions, traits, backstory,
    /// ideoligion, relations, faction and guest status, work settings, looks, and her
    /// RimRound weight. Whatever the new body can't wear is dropped where she stood.
    /// </summary>
    public static class NephilaTransformUtility
    {
        public static Pawn Transform(Pawn old, PawnKindDef kind, RoyalTitleDef title, string letterLabelKey, string letterTextKey, float extraKilos = 0f)
        {
            if (old == null || kind == null || !old.Spawned)
                return null;
            Map map = old.Map;
            IntVec3 cell = old.Position;
            Faction faction = old.Faction;
            bool wasSelected = Find.Selector.IsSelected(old);
            bool humanlike = kind.RaceProps.Humanlike;

            Pawn fresh = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                kind, faction, PawnGenerationContext.NonPlayer, forceGenerateNewPawn: true,
                canGeneratePawnRelations: false, mustBeCapableOfViolence: false, colonistRelationChanceFactor: 0f,
                allowGay: true, allowPregnant: false, allowFood: false, allowAddictions: false,
                fixedBiologicalAge: old.ageTracker.AgeBiologicalYearsFloat,
                fixedChronologicalAge: old.ageTracker.AgeChronologicalYearsFloat,
                fixedGender: old.gender, forceNoGear: true));

            if (humanlike)
                CarryOverPerson(old, fresh);
            fresh.Name = old.Name;

            // what the new body can't use falls to the floor
            old.inventory?.DropAllNearPawn(cell);
            old.equipment?.DropAllEquipment(cell, forbid: false);
            old.apparel?.DropAll(cell, forbid: false);

            float weightSeverity = old.WeightHediff()?.Severity ?? -1f;

            MoveRelations(old, fresh);
            if (old.IsPrisonerOfColony)
                fresh.guest?.SetGuestStatus(Faction.OfPlayer, GuestStatus.Prisoner);
            else if (old.IsSlaveOfColony)
                fresh.guest?.SetGuestStatus(Faction.OfPlayer, GuestStatus.Slave);

            old.Destroy(DestroyMode.Vanish);
            GenSpawn.Spawn(fresh, cell, map, WipeMode.Vanish);

            if (weightSeverity >= 0f)
                RimRound.Utilities.HediffUtility.SetHediffSeverity(RimRound.Defs.HediffDefOf.RimRound_Weight, fresh, weightSeverity);
            if (extraKilos != 0f)
                RimRound.Utilities.HediffUtility.QueueWeightGain(fresh, extraKilos);

            if (title != null && ModsConfig.RoyaltyActive && fresh.royalty != null && NephilaUtility.NephilaFaction is Faction nephila)
                fresh.royalty.SetTitle(nephila, title, grantRewards: true, rewardsOnlyForNewestTitle: false, sendLetter: false);

            if (wasSelected)
                Find.Selector.Select(fresh, playSound: false);
            if (letterLabelKey != null && fresh.Faction == Faction.OfPlayer)
                Find.LetterStack.ReceiveLetter(
                    letterLabelKey.Translate(fresh.LabelShort),
                    letterTextKey.Translate(fresh.Name?.ToStringFull ?? fresh.LabelShort),
                    LetterDefOf.PositiveEvent, new LookTargets(fresh));
            return fresh;
        }

        static void CarryOverPerson(Pawn old, Pawn fresh)
        {
            if (old.story != null && fresh.story != null)
            {
                fresh.story.Childhood = old.story.Childhood;
                fresh.story.Adulthood = old.story.Adulthood;
                fresh.story.HairColor = old.story.HairColor;
                fresh.story.SkinColorBase = old.story.SkinColorBase;
                // the body type stays the new race's own: its sprites are drawn per caste,
                // and a RimRound body type would swap them for RimRound's

                if (old.story.traits != null && fresh.story.traits != null)
                    foreach (Trait trait in old.story.traits.allTraits.ToList())
                    {
                        if (trait.sourceGene != null || fresh.story.traits.HasTrait(trait.def))
                            continue;
                        if (fresh.story.traits.allTraits.Any(t => t.def.ConflictsWith(trait) || trait.def.ConflictsWith(t)))
                            continue;
                        fresh.story.traits.GainTrait(new Trait(trait.def, trait.Degree, forced: trait.ScenForced));
                    }
            }

            if (old.skills != null && fresh.skills != null)
                foreach (SkillRecord skill in old.skills.skills)
                {
                    SkillRecord to = fresh.skills.GetSkill(skill.def);
                    if (to == null)
                        continue;
                    to.Level = skill.Level;
                    to.xpSinceLastLevel = skill.xpSinceLastLevel;
                    to.passion = skill.passion;
                }

            if (ModsConfig.IdeologyActive && old.Ideo != null && fresh.ideo != null)
                fresh.ideo.SetIdeo(old.Ideo);

            if (old.workSettings != null && fresh.workSettings != null && old.workSettings.EverWork)
            {
                fresh.workSettings.EnableAndInitialize();
                foreach (WorkTypeDef work in DefDatabase<WorkTypeDef>.AllDefsListForReading)
                    if (!fresh.WorkTypeIsDisabled(work))
                        fresh.workSettings.SetPriority(work, old.workSettings.GetPriority(work));
            }

            if (old.playerSettings != null && fresh.playerSettings != null)
            {
                fresh.playerSettings.medCare = old.playerSettings.medCare;
                fresh.playerSettings.AreaRestrictionInPawnCurrentMap = old.playerSettings.AreaRestrictionInPawnCurrentMap;
                fresh.playerSettings.hostilityResponse = old.playerSettings.hostilityResponse;
            }
        }

        static void MoveRelations(Pawn old, Pawn fresh)
        {
            if (old.relations == null || fresh.relations == null)
                return;
            foreach (DirectPawnRelation relation in old.relations.DirectRelations.ToList())
            {
                if (relation.otherPawn == null || relation.otherPawn == fresh)
                    continue;
                old.relations.RemoveDirectRelation(relation);
                if (!fresh.relations.DirectRelationExists(relation.def, relation.otherPawn))
                    fresh.relations.AddDirectRelation(relation.def, relation.otherPawn);
            }
        }
    }
}
