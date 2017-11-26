﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using RimWorld;
using RimWorld.Planet;
using Verse; 
using Verse.AI; 
using Verse.Sound;

//using CommonMisc; // Helper classes


namespace Incidents
{
    public class IncidentWorker_CrashedCryptoSleepCasket : IncidentWorker
    {

        protected override bool CanFireNowSub(IIncidentTarget target)
        {
            return base.CanFireNowSub(target);
        }

        public override bool TryExecute(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            ThingDef thingDef = DefDatabase<ThingDef>.GetNamed("CrashedCryptoSleepCasket");
            
            CellRect mapRect;
            int numXZ = thingDef.Size.x > thingDef.Size.z ?
                            thingDef.Size.x : thingDef.Size.z;
            //numXZ +=1;
            int count = 0;
            // find valid place
            while (true)
            {
                IntVec3 pos = CellFinder.RandomNotEdgeCell(20, map);
                mapRect = new CellRect(pos.x, pos.z, numXZ, numXZ);

                // Valid position
                if (IsMapRectClear(mapRect, map))
                    break;

                count++;
                if (count > 100)
                    return false;
            }


            // Create casket
            Building_CryptosleepCasket casket = TryMakeCasket(mapRect, map, thingDef);
            if (casket == null)
                return false;

            // Do bomb and flame explosion at place of impact
            GenExplosion.DoExplosion(casket.PositionHeld, map, 7, DamageDefOf.Bomb, casket);
            GenExplosion.DoExplosion(casket.PositionHeld, map, 4, DamageDefOf.Flame, casket);

            // Passenger count: 1 to 5 rnd
            int ccount = Rand.RangeInclusive(1, 5);
            for (int i = 0; i < ccount; i++)
            {
                MakeCasketContents(casket);
            }

            GenSpawn.Spawn(casket, casket.Position, map);

            Letter letter = LetterMaker.MakeLetter("Letter_Label_CrashedCasket".Translate(), "Letter_Text_CrashedCasket".Translate(), LetterDefOf.BadNonUrgent, casket);
            Find.LetterStack.ReceiveLetter(letter);

            return true;
        }

        
        
        private static List<string> PodContentAnimalDefs = new List<string>() { "YorkshireTerrier", "Husky", "LabradorRetriever", "Cat"};

        // Extracted and modified from GenStep_ScatterShrines:
        private static bool IsMapRectClear(CellRect mapRect, Map map)
        {
            foreach (IntVec3 cell in mapRect)
            {
                List<Thing> thingList = cell.GetThingList(map);
                for (int i = 0; i < thingList.Count; i++)
                {
                    if (thingList[i].def.category == ThingCategory.Item || thingList[i].def.category == ThingCategory.Building || thingList[i].def.category == ThingCategory.Pawn)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private static Building_CryptosleepCasket TryMakeCasket(CellRect mapRect, Map map, ThingDef thingDef)
        {
            mapRect.ClipInsideMap(map);
            CellRect cellRect = new CellRect(mapRect.BottomLeft.x + 1, mapRect.BottomLeft.z + 1, 2, 1);
            cellRect.ClipInsideMap(map);
            foreach (IntVec3 current in cellRect)
            {
                List<Thing> thingList = current.GetThingList(map);
                for (int i = 0; i < thingList.Count; i++)
                {
                    if (!thingList[i].def.destroyable)
                    {
                        return null;
                    }
                }
            }

            Building_CryptosleepCasket casket = (Building_CryptosleepCasket)ThingMaker.MakeThing(thingDef, null);
            casket.SetPositionDirect(cellRect.BottomLeft);

            if (Rand.Value < 0.5f)
                casket.Rotation = Rot4.East;
            else
                casket.Rotation = Rot4.North;
            
            return casket;
        }

        private static void MakeCasketContents(Building_CryptosleepCasket casket)
        {
            //Source from http://akshaya-m.blogspot.de/2015/03/elegant-way-to-switch-if-else.html
            // Definition:
            var newSwitch = new Dictionary<Func<int, bool>, Action>
            {
             { x => x < 10  ,   () =>  GenerateFriendlyAnimal(casket)   },  
             { x => x < 20  ,   () =>  GenerateFriendlySpacer(casket)   },
             { x => x < 30  ,   () =>  GenerateIncappedSpacer(casket)   },
             { x => x < 45  ,   () =>  GenerateSlave(casket)            },
             { x => x < 50  ,   () =>  GenerateHalfEatenSpacer(casket)  },
             { x => x >= 50 ,   () =>  GenerateAngrySpacer(casket)      } 
            };
            // Call:
            newSwitch.First(sw => sw.Key( Rand.RangeInclusive(0, 100) )).Value();
            
        }

        private static void GenerateFriendlyAnimal(Building_CryptosleepCasket pod)
        {
            Faction faction = Find.FactionManager.FirstFactionOfDef(FactionDefOf.PlayerColony);
            Pawn pawn = PawnGenerator.GeneratePawn(PawnKindDef.Named( PodContentAnimalDefs.RandomElement() ), faction);
            if (!pod.TryAcceptThing(pawn, false))
            {
                Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);
                return;
            }
            //GiveRandomLootInventoryForTombPawn(pawn);
        }

        private static void GenerateFriendlySpacer(Building_CryptosleepCasket pod)
        {
            Faction faction = Find.FactionManager.FirstFactionOfDef(FactionDefOf.Spacer);
            Pawn pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.SpaceSoldier, faction);
            if (!pod.TryAcceptThing(pawn, false))
            {
                Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);
                return;
            }
            GiveRandomLootInventoryForTombPawn(pawn);
        }

        private static void GenerateIncappedSpacer(Building_CryptosleepCasket pod)
        {
            Faction faction = Find.FactionManager.FirstFactionOfDef(FactionDefOf.Spacer);
            Pawn pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.SpaceSoldier, faction);
            if (!pod.TryAcceptThing(pawn, false))
            {
                Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);
                return;
            }
            HealthUtility.DamageUntilDowned(pawn);
            GiveRandomLootInventoryForTombPawn(pawn);
        }

        private static void GenerateSlave(Building_CryptosleepCasket pod)
        {
            Faction faction = Find.FactionManager.FirstFactionOfDef(FactionDefOf.Spacer);
            Pawn pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Slave, faction);
            if (!pod.TryAcceptThing(pawn, false))
            {
                Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);
                return;
            }
            HealthUtility.DamageUntilDowned(pawn);
            GiveRandomLootInventoryForTombPawn(pawn);
            if (Rand.Value < 0.5f)
            {
                HealthUtility.DamageUntilDead(pawn);
            }
        }

        private static void GenerateAngrySpacer(Building_CryptosleepCasket pod)
        {
            Faction faction = Find.FactionManager.FirstFactionOfDef(FactionDefOf.SpacerHostile);
            Pawn pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.SpaceSoldier, faction);
            if (!pod.TryAcceptThing(pawn, false))
            {
                Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);
                return;
            }
            GiveRandomLootInventoryForTombPawn(pawn);
        }

        private static void GenerateHalfEatenSpacer(Building_CryptosleepCasket pod)
        {
            Faction faction = Find.FactionManager.FirstFactionOfDef(FactionDefOf.Spacer);
            Pawn pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.SpaceSoldier, faction);
            if (!pod.TryAcceptThing(pawn, false))
            {
                Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.Discard);
                return;
            }
            int num = Rand.Range(6, 10);
            for (int i = 0; i < num; i++)
            {
                pawn.TakeDamage(new DamageInfo(DamageDefOf.Bite, Rand.Range(3, 8), -1, pawn, null, null));
            }
            GiveRandomLootInventoryForTombPawn(pawn);

            Pawn pawn2;
            int pawnCount;
            float rnd2 = Rand.Value;
            if (rnd2 < 0.05)
            {
                pawn2 = PawnGenerator.GeneratePawn(PawnKindDefOf.Spelopede, null);
                pawnCount = 1;
            }
            else if (rnd2 < 0.15) 
            {
                pawn2 = PawnGenerator.GeneratePawn(PawnKindDefOf.Megaspider, null);
                pawnCount = 1;
            }
            else
            {
                pawn2 = PawnGenerator.GeneratePawn(PawnKindDefOf.Megascarab, null);
                pawnCount = Rand.Range(3, 6);
            }

            for (int j = 0; j < pawnCount; j++)
            {
                if (!pod.TryAcceptThing(pawn2, false))
                {
                    Find.WorldPawns.PassToWorld(pawn2, PawnDiscardDecideMode.Discard);
                    return;
                }
                pawn2.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Manhunter, null, false);
            }
        }

        // from RimWorld.ItemCollectionGenerator_AncientPodContents
        private static void GiveRandomLootInventoryForTombPawn(Pawn p)
        {
            if ((double)Rand.Value < 0.65)
            {
                MakeIntoContainer(p.inventory.innerContainer, ThingDefOf.Gold, Rand.Range(10, 50));
            }
            else
            {
                MakeIntoContainer(p.inventory.innerContainer, ThingDefOf.Plasteel, Rand.Range(10, 50));
            }
            MakeIntoContainer(p.inventory.innerContainer, ThingDefOf.Component, Rand.Range(-2, 4));
        }
        private static void MakeIntoContainer(ThingOwner container, ThingDef def, int count)
        {
            if (count <= 0)
            {
                return;
            }
            Thing thing = ThingMaker.MakeThing(def, null);
            thing.stackCount = count;
            container.TryAdd(thing, true);
        }



    }
}
