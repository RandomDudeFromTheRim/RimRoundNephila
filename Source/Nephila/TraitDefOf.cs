using RimWorld;

namespace Nephila
{
    [DefOf]
    public static class TraitDefOf
    {
        static TraitDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(TraitDefOf));
        }

        public static TraitDef ViolenceInhibition;
    }
}
