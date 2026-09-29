using RimWorld;
using Verse;

namespace Nephila
{
    [DefOf]
    public static class NephilaDefOf
    {
        static NephilaDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(NephilaDefOf));

        public static FactionDef NephilaNPCFaction;

        [MayRequireRoyalty] public static RoyalTitleDef NephilimVeiledOne;
        [MayRequireRoyalty] public static RoyalTitleDef NephilimGrandMatronTitle;
        [MayRequireRoyalty] public static RoyalTitleDef NephilimGardenTender;
        [MayRequireRoyalty] public static RoyalTitleDef NephilimGrayOne;

        public static JobDef NephilaMilkyLover;
        public static JobDef QGMilkyLover;
        public static JobDef QGMilkySelf;
        public static JobDef NephilaMilkySelf;

        public static HediffDef NephilaFecundity;
        public static HediffDef NephilaHandmaidenFecundity;
        public static HediffDef NephilaMatronFecundity;
        public static HediffDef Nephila_LustNanites;
        public static HediffDef NephilaMechaniteInhibitor;
        public static HediffDef NephilaInitialTransformationFromAnimal;
        public static HediffDef NephilaSwollenBreasts;

        public static ThingDef Nephila;
        public static ThingDef NephilaHandmaiden;
        public static ThingDef NephilaMatron;
        public static ThingDef NephilaQueensGuard;
        public static ThingDef NephilaGrandMatron;

        public static PawnKindDef Nephila_ChonkersCherub;

        public static IncidentDef Nephila_Clutchmother;
        public static GameConditionDef NephiliticFogCondition;
        public static WeatherDef NephiliticFog;

        public static DamageDef CherubSpew;
    }
}
