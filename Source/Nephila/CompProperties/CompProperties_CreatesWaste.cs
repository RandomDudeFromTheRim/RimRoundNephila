using Verse;

namespace Nephila
{
    public class CompCreatesWaste : ThingComp
    {
        public CompProperties_CreatesWaste Props => (CompProperties_CreatesWaste) props;

        public override void ReceiveCompSignal(string signal)
        {
            base.ReceiveCompSignal(signal);
            if (signal == "RanOutOfFuel")
            {
                var product = ThingMaker.MakeThing(Props.wasteProduct);
                product.stackCount = Props.wasteAmount;
                GenSpawn.Spawn(product, parent.Position, parent.Map);
            }
        }
    }
    
    public class CompProperties_CreatesWaste : CompProperties
    {
        public ThingDef wasteProduct;
        public int wasteAmount;
        
        public CompProperties_CreatesWaste()
        {
            compClass = typeof(CompCreatesWaste);
        }
    }
}
