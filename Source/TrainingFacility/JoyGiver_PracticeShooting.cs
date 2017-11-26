﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Verse;
using Verse.AI;
using RimWorld;


namespace TrainingFacility
{

    // This is the automatic selection via joy
    public class JoyGiver_PracticeShooting : JoyGiver_WatchBuilding
    {
        private string invalidThingCategoryDef1 = "Grenades";


        public override Job TryGiveJob(Pawn pawn)
        {
            return TryGiveJob(pawn, null);
        }

        public Job TryGiveJob(Pawn pawn, Thing targetThing, bool NoJoyCheck = false)
        {
            Verb attackVerb = null;

            if (pawn != null)
            {
                attackVerb = pawn.TryGetAttackVerb(false);

                // disable job when Primary is a Grenade OR when Violent isn't available
                if (pawn.equipment == null || pawn.equipment.Primary == null || pawn.equipment.Primary.def == null || pawn.equipment.Primary.def.thingCategories == null ||
                    pawn.equipment.Primary.def.thingCategories.Contains(DefDatabase<ThingCategoryDef>.GetNamed(invalidThingCategoryDef1, false)))
                {
                    //Log.Error("Prevented Joy because of Grenade!");
                    attackVerb = null;
                }

                if (pawn.story == null ||
                    (pawn.story.CombinedDisabledWorkTags & WorkTags.Violent) == WorkTags.Violent)
                {
                    //Log.Error("Prevented Joy because of Incapable of Violent!");
                    attackVerb = null;
                }

            }

            if (attackVerb == null || attackVerb.verbProps == null || attackVerb.verbProps.MeleeRange)
                return null;

            //return base.TryGiveJob(pawn);

            // From base.TryGiveJob(pawn)
            // ==========================
            List<Thing> searchSet = pawn.Map.listerThings.ThingsOfDef(this.def.thingDefs[0]);
            Predicate<Thing> predicate = delegate (Thing t)
            {
                if (!pawn.CanReserve(t, this.def.jobDef.joyMaxParticipants))
                {
                    return false;
                }
                if (t.IsForbidden(pawn))
                {
                    return false;
                }
                if (!t.IsSociallyProper(pawn))
                {
                    return false;
                }
                CompPowerTrader compPowerTrader = t.TryGetComp<CompPowerTrader>();
                return (compPowerTrader == null || compPowerTrader.PowerOn) && (!this.def.unroofedOnly || !t.Position.Roofed(pawn.Map));
            };
            Predicate<Thing> validator = predicate;

            // Changed thing definition from base.TryGiveJob(pawn)
            // Because of selection via building
            // ---
            Thing thing = null;
            if (targetThing != null)
            {
                if (pawn.CanReach(targetThing.Position, PathEndMode.Touch, Danger.Deadly) && validator(targetThing))
                    thing = targetThing;
            }
            if (targetThing == null)
                thing = GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, searchSet, PathEndMode.OnCell, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false), 9999f, validator, null);
            // ---

            if (thing != null)
            {
                Job job = this.TryGivePlayJob(pawn, thing);
                if (job != null)
                {
                    return job;
                }
            }
            return null;
            // ==========================
        }

    }
}
