using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace Nephila
{
    public class PawnNephilaChonkersCherub : Pawn
    {
        public Predicate<Thing> Predicate
        {
            get
            {
                return t =>
                {
                    if (t == null || t == this || !t.Spawned)
                    {
                        return false;
                    }

                    Pawn pawn = t as Pawn;
                    if (pawn == null || pawn.Dead || pawn is PawnNephilaChonkersCherub || pawn.Faction == null)
                    {
                        return false;
                    }

                    if (Faction != null && pawn.Faction != null)
                    {
                        if (Faction == pawn.Faction || !Faction.HostileTo(pawn.Faction))
                        {
                            return false;
                        }
                    }

                    return pawn.needs?.mood?.thoughts?.memories != null;
                };
            }
        }
    }
}
