using System;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace Nephila
{
    public class CompNephilaHatcher : ThingComp
    {
        private CompTemperatureRuinable FreezerComp
        {
            get
            {
                return this.parent.GetComp<CompTemperatureRuinable>();
            }
        }

        public bool TemperatureDamaged
        {
            get
            {
                return this.FreezerComp != null && this.FreezerComp.Ruined;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref this.gestateProgress, "gestateProgress", 0f);
            Scribe_References.Look(ref this.hatcheeParent, "hatcheeParent");
            Scribe_References.Look(ref this.otherParent, "otherParent");
            Scribe_References.Look(ref this.hatcheeFaction, "hatcheeFaction");
        }

        public override void CompTick()
        {
            if (!this.TemperatureDamaged)
            {
                float num = 1f / (this.Props.hatcherDaystoHatch * 60000f);
                this.gestateProgress += num;
                if (this.gestateProgress > 1f)
                {
                    this.NephilaHatch();
                }
            }
        }

        public override void PreAbsorbStack(Thing otherStack, int count)
        {
            float t = (float)count / (float)(this.parent.stackCount + count);
            float b = ((ThingWithComps)otherStack).GetComp<CompNephilaHatcher>().gestateProgress;
            this.gestateProgress = Mathf.Lerp(this.gestateProgress, b, t);
        }

        public override void PostSplitOff(Thing piece)
        {
            CompNephilaHatcher comp = ((ThingWithComps)piece).GetComp<CompNephilaHatcher>();
            comp.gestateProgress = this.gestateProgress;
            comp.hatcheeParent = this.hatcheeParent;
            comp.otherParent = this.otherParent;
            comp.hatcheeFaction = Faction.OfPlayer;
        }

        public override void PrePreTraded(TradeAction action, Pawn playerNegotiator, ITrader trader)
        {
            base.PrePreTraded(action, playerNegotiator, trader);
            if (action == TradeAction.PlayerBuys)
            {
                this.hatcheeFaction = Faction.OfPlayer;
                return;
            }
            if (action == TradeAction.PlayerSells)
            {
                this.hatcheeFaction = trader.Faction;
            }
        }

        public override void PostPostGeneratedForTrader(TraderKindDef trader, PlanetTile forTile, Faction forFaction)
        {
            base.PostPostGeneratedForTrader(trader, forTile, forFaction);
            this.hatcheeFaction = forFaction;
        }

        public override string CompInspectStringExtra()
        {
            if (!this.TemperatureDamaged)
            {
                return "EggProgress".Translate() + ": " + this.gestateProgress.ToStringPercent();
            }
            return null;
        }

        public CompProperties_NephilaHatcher Props
        {
            get
            {
                return (CompProperties_NephilaHatcher)this.props;
            }
        }

        public static Faction Nephila
        {
            get
            {
                return Find.FactionManager.FirstFactionOfDef(NephilaDefOf.NephilaNPCFaction);
            }
        }

        public void NephilaHatch()
        {
            Pawn firstBorn = null;
            try
            {
                PawnGenerationRequest request = new PawnGenerationRequest(
                    this.Props.hatcherPawn,
                    Faction.OfPlayer,
                    PawnGenerationContext.NonPlayer,
                    -1,
                    forceGenerateNewPawn: true,
                    allowDead: false,
                    allowDowned: false,
                    canGeneratePawnRelations: false,
                    mustBeCapableOfViolence: false,
                    colonistRelationChanceFactor: 1f,
                    forceAddFreeWarmLayerIfNeeded: false,
                    allowGay: true,
                    allowPregnant: false,
                    allowFood: true,
                    allowAddictions: true,
                    inhabitant: false,
                    certainlyBeenInCryptosleep: false,
                    forceRedressWorldPawnIfFormerColonist: false,
                    worldPawnFactionDoesntMatter: false,
                    biocodeWeaponChance: 0f,
                    biocodeApparelChance: 0f,
                    extraPawnForExtraRelationChance: null,
                    relationWithExtraPawnChanceFactor: 1f,
                    validatorPreGear: null,
                    validatorPostGear: null,
                    forcedTraits: null,
                    prohibitedTraits: null,
                    minChanceToRedressWorldPawn: null,
                    fixedBiologicalAge: null,
                    fixedChronologicalAge: null,
                    fixedGender: null,
                    fixedLastName: null,
                    fixedBirthName: null,
                    fixedTitle: null,
                    fixedIdeo: null,
                    forceNoIdeo: false,
                    forceNoBackstory: false,
                    forbidAnyTitle: false,
                    forceDead: false,
                    forcedXenogenes: null,
                    forcedEndogenes: null,
                    forcedXenotype: null,
                    forcedCustomXenotype: null,
                    allowedXenotypes: null,
                    forceBaselinerChance: 0f,
                    developmentalStages: DevelopmentalStage.Adult,
                    pawnKindDefGetter: null,
                    excludeBiologicalAgeRange: null,
                    biologicalAgeRange: null,
                    forceRecruitable: false,
                    dontGiveWeapon: false,
                    onlyUseForcedBackstories: false,
                    maximumAgeTraits: -1,
                    minimumAgeTraits: 0,
                    forceNoGear: false
                );

                for (int i = 0; i < this.parent.stackCount; i++)
                {
                    Pawn pawn = PawnGenerator.GeneratePawn(request);
                    if (PawnUtility.TrySpawnHatchedOrBornPawn(pawn, this.parent))
                    {
                        if (pawn != null)
                        {
                            if (this.hatcheeParent != null)
                            {
                                if (pawn.playerSettings != null && this.hatcheeParent.playerSettings != null && this.hatcheeParent.Faction == this.hatcheeFaction)
                                {
                                    pawn.playerSettings.AreaRestrictionInPawnCurrentMap = this.hatcheeParent.playerSettings.AreaRestrictionInPawnCurrentMap; // Updated to use AreaRestriction
                                }
                                if (pawn.RaceProps.IsFlesh)
                                {
                                    pawn.relations.AddDirectRelation(PawnRelationDefOf.Parent, this.hatcheeParent);
                                }
                            }
                            if (this.otherParent != null && (this.hatcheeParent == null || this.hatcheeParent.gender != this.otherParent.gender) && pawn.RaceProps.IsFlesh)
                            {
                                pawn.relations.AddDirectRelation(PawnRelationDefOf.Parent, this.otherParent);
                            }
                            if (pawn.def == NephilaDefOf.NephilaQueensGuard && ModsConfig.RoyaltyActive && pawn.royalty != null && Nephila != null && NephilaDefOf.NephilimGrayOne != null)
                            {
                                pawn.royalty.SetTitle(Nephila, NephilaDefOf.NephilimGrayOne, true, false, false);
                            }
                            if (firstBorn == null)
                                firstBorn = pawn;
                        }
                        if (this.parent.Spawned)
                        {
                            FilthMaker.TryMakeFilth(this.parent.Position, this.parent.Map, ThingDefOf.Filth_AmnioticFluid, 1);
                        }
                    }
                    else
                    {
                        Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);
                    }
                }
            }
            finally
            {
                if (firstBorn != null && firstBorn.Faction == Faction.OfPlayer && firstBorn.RaceProps.Humanlike)
                    Find.LetterStack.ReceiveLetter("Nephila_NephilimBornLabel".Translate(), "Nephila_NephilimBornText".Translate(firstBorn.LabelShort),
                        LetterDefOf.PositiveEvent, firstBorn);
                this.parent.Destroy(DestroyMode.Vanish);
            }
        }

        private float gestateProgress;

        public Pawn hatcheeParent;

        public Pawn otherParent;

        public Faction hatcheeFaction;
    }
}
