﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;
using RimWorld.Planet;

namespace FactionColonies
{

    class simulateBattleFC
    {

        public static int FightBattle(militaryForce MFA, militaryForce MFB)
        {
            //Log.Message("Starting battle");
            while(MFA.forceRemaining > 0 && MFB.forceRemaining > 0)
            {
                FightRound(MFA, MFB);
            }
            if (MFA.forceRemaining <= 0)
            {
                //Log.Message("Defending Force has won.");
                //b is winner
                return 1;
            } else
            if (MFB.forceRemaining <= 0)
            {
                //Log.Message("Attacking Force has won.");
                //a is winner
                return 0;
            } else
            {
                Log.Message("FightBattle catch statement - Empire");
                //catch
                return -1;
            }
        }

        public static void FightRound(militaryForce MFA, militaryForce MFB)
        {
            int randA = Rand.Range(0, 20);
            int randB = Rand.Range(0, 20);
            //Log.Message("A Begin: " + MFA.forceRemaining + " : " + MFB.forceRemaining + " B begin");
            //Log.Message("A Rolled: " + randA.ToString() + " : " + randB.ToString() + " B rolled");
            if (randA == randB)
            {
                return;
            }
            else
            if (randA > randB)
            {
                MFB.forceRemaining -= 1;
                return;
            }
            else
            if (randB > randA)
            {
                MFA.forceRemaining -= 1;
                return;
            }
            //Log.Message("A Remain: " + MFA.forceRemaining + " : " + MFB.forceRemaining + " B remain");
        }





    }


    public class militaryForce : IExposable
    {
        public double militaryLevel;
        public double militaryEfficiency;
        public double forceRemaining;
        public int random = Rand.Range(0, 0);
        public SettlementFC homeSettlement;
        public Faction homeFaction;

        public void ExposeData()
        {
            Scribe_Values.Look<double>(ref militaryLevel, "militaryLevel");
            Scribe_Values.Look<double>(ref militaryEfficiency, "militaryEfficiency");
            Scribe_Values.Look<double>(ref forceRemaining, "forceRemaining");
            Scribe_Values.Look<int>(ref random, "random");
            Scribe_References.Look<SettlementFC>(ref homeSettlement, "homeSettlement");
            Scribe_References.Look<Faction>(ref homeFaction, "homeFaction");
        }

        public militaryForce()
        {

        }

        public militaryForce(double militaryLevel, double militaryEfficiency, SettlementFC homeSettlement, Faction homeFaction)
        {
            this.militaryLevel = militaryLevel;
            this.militaryEfficiency = militaryEfficiency;
            this.homeSettlement = homeSettlement;
            this.homeFaction = homeFaction;
            forceRemaining = Math.Round(militaryLevel*militaryEfficiency);
        }

        public static militaryForce createMilitaryForceFromSettlement(SettlementFC settlement)
        {
            //get efficiency from traits and settlement efficiency.
            double militaryLevel = settlement.settlementMilitaryLevel;
            double efficiency = traitUtilsFC.cycleTraits(new double(), "militaryMultiplierCombatEfficiency", Find.World.GetComponent<FactionFC>().traits, "multiply") * traitUtilsFC.cycleTraits(new double(), "militaryMultiplierCombatEfficiency", settlement.traits, "multiply");
            militaryForce returnForce = new militaryForce(militaryLevel, efficiency, settlement, FactionColonies.getPlayerColonyFaction());
            return returnForce;
            //create and return force.
        }

        public static militaryForce createMilitaryForceFromEnemySettlement(Settlement settlement)
        {
            double militaryLevel;
            double efficiency;

            switch (settlement.Faction.def.techLevel)
            {
                case TechLevel.Undefined:
                    militaryLevel = 1;
                    efficiency = .5;
                    break;
                case TechLevel.Animal:
                    militaryLevel = 2;
                    efficiency = .5;
                    break;
                case TechLevel.Neolithic:
                    militaryLevel = 3;
                    efficiency = 1;
                    break;
                case TechLevel.Medieval:
                    militaryLevel = 4;
                    efficiency = 1.2;
                    break;

                case TechLevel.Industrial:
                    militaryLevel = 4;
                    efficiency = 1.2;
                    break;
                case TechLevel.Spacer:
                    militaryLevel = 5;
                    efficiency = 1.3;
                    break;
                case TechLevel.Ultra:
                    militaryLevel = 6;
                    efficiency = 1.3;
                    break;
                case TechLevel.Archotech:
                    militaryLevel = 6;
                    efficiency = 1.5;
                    break;
                default:
                    militaryLevel = 1;
                    efficiency = 1;
                    Log.Message("Defaulted createMilitaryForceFromEnemyFaction switch case - Empire Mod");
                    break;
            }

            militaryForce returnForce = new militaryForce(militaryLevel, efficiency, null, settlement.Faction);
            return returnForce;
            
        }

        public static militaryForce createMilitaryForce(double strength, Faction faction)
        {
            militaryForce returnForce = new militaryForce(strength, 1, null, faction);
            return returnForce;
        }

        public static militaryForce createMilitaryForceFromFaction(Faction faction, bool handicap)
        {
            double militaryLevel;
            double efficiency;

            switch (faction.def.techLevel)
            {
                case TechLevel.Undefined:
                    militaryLevel = 1;
                    efficiency = .5;
                    break;
                case TechLevel.Animal:
                    militaryLevel = 2;
                    efficiency = .5;
                    break;
                case TechLevel.Neolithic:
                    militaryLevel = 3;
                    efficiency = 1;
                    break;
                case TechLevel.Medieval:
                    militaryLevel = 4;
                    efficiency = 1.2;
                    break;

                case TechLevel.Industrial:
                    militaryLevel = 4;
                    efficiency = 1.2;
                    break;
                case TechLevel.Spacer:
                    militaryLevel = 5;
                    efficiency = 1.3;
                    break;
                case TechLevel.Ultra:
                    militaryLevel = 6;
                    efficiency = 1.3;
                    break;
                case TechLevel.Archotech:
                    militaryLevel = 6;
                    efficiency = 1.5;
                    break;
                default:
                    militaryLevel = 1;
                    efficiency = 1;
                    Log.Message("Defaulted createMilitaryForceFromEnemyFaction switch case - Empire Mod");
                    break;
            }
            double value = militaryLevel + FactionColonies.randomAttackModifier();
            if (handicap)
            {
                value = Math.Min(value, (2 + Math.Ceiling((double)(Find.TickManager.TicksGame - Find.World.GetComponent<FactionFC>().timeStart) / GenDate.TicksPerSeason)));
                //Log.Message(value.ToString());
            }

            militaryForce returnForce = new militaryForce(value, efficiency, null, faction);
            return returnForce;

        }
    }

    class MilitaryUtilFC
    {
        public static void attackPlayerSettlement(militaryForce attackingForce, SettlementFC settlement, Faction enemyFaction)
        {
            FCEvent tmp = FCEventMaker.MakeEvent(FCEventDefOf.settlementBeingAttacked);
            tmp.hasCustomDescription = true;
            tmp.timeTillTrigger = Find.TickManager.TicksGame + 60000;
            tmp.location = settlement.mapLocation;
            tmp.hasDestination = true;
            tmp.customDescription = TranslatorFormattedStringExtensions.Translate("settlementAboutToBeAttacked", settlement.name, enemyFaction.Name);// + 
            tmp.militaryForceDefending = militaryForce.createMilitaryForceFromSettlement(settlement);
            tmp.militaryForceDefendingFaction = FactionColonies.getPlayerColonyFaction();
            tmp.militaryForceAttacking = attackingForce;
            tmp.militaryForceAttackingFaction = enemyFaction;
            tmp.settlementFCDefending = settlement;
            Find.World.GetComponent<FactionFC>().addEvent(tmp);

            tmp.customDescription += "\n\nThe estimated attacking force's power is: " + tmp.militaryForceAttacking.forceRemaining;
            settlement.isUnderAttack = true;

            Find.LetterStack.ReceiveLetter("settlementInDanger".Translate(), tmp.customDescription, LetterDefOf.ThreatBig, new LookTargets(Find.WorldObjects.SettlementAt(settlement.mapLocation)));
        }

        public static void changeDefendingMilitaryForce(FCEvent evt, SettlementFC settlementOfMilitaryForce)
        {
            if (settlementOfMilitaryForce == evt.militaryForceDefending.homeSettlement)
            {
                Messages.Message("militaryAlreadyDefendingSettlement".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            if(evt.militaryForceDefending.homeSettlement != Find.World.GetComponent<FactionFC>().returnSettlementByLocation(evt.location))
            {
                //if the forces defending aren't the forces belonging to the settlement
                evt.militaryForceDefending.homeSettlement.returnMilitary();
            }


            Find.World.GetComponent<FactionFC>().militaryTargets.Remove(evt.location);
            evt.militaryForceDefending = militaryForce.createMilitaryForceFromSettlement(settlementOfMilitaryForce);

            if (settlementOfMilitaryForce == Find.World.GetComponent<FactionFC>().returnSettlementByLocation(evt.location))
            {
                //if home settlement is reseting to defense
                Messages.Message("defendingMilitaryReset".Translate(), MessageTypeDefOf.NeutralEvent);

            }
            else
            {
                //if settlement is foreign
                settlementOfMilitaryForce.sendMilitary(evt.settlementFCDefending.mapLocation, "defendFriendlySettlement", -1, evt.militaryForceAttackingFaction);
            }
        }

        public static militaryForce returnDefendingMilitaryForce(FCEvent evt)
        {
            return evt.militaryForceDefending;
        }

        public static FCEvent returnMilitaryEventByLocation(int location)
        {
            foreach (FCEvent evt in Find.World.GetComponent<FactionFC>().events)
            {
                if (evt.def.isMilitaryEvent == true && evt.location == location)
                {
                    return evt;
                }
            }

            return null;
        }
    }

    class RelationsUtilFC
    {
        public static void attackFaction(Faction faction)
        {
            //Log.Message(Find.FactionManager.OfPlayer.RelationWith(faction).goodwill + " player:colony ");
            Find.FactionManager.OfPlayer.TryAffectGoodwillWith(faction, -50);
            Find.FactionManager.OfPlayer.TrySetRelationKind(faction, FactionRelationKind.Hostile);
            resetPlayerColonyRelations();
            //Log.Message(Find.FactionManager.OfPlayer.RelationWith(faction).goodwill + " player:colony ");
            //FactionColonies.getPlayerColonyFaction().TryAffectGoodwillWith(faction, -50)
        }

        public static void resetPlayerColonyRelations()
        {
            Faction PCFaction = FactionColonies.getPlayerColonyFaction();
            foreach (Faction faction in Find.FactionManager.AllFactions)
            {
                if (faction != Find.FactionManager.OfPlayer && faction != PCFaction)
                {
                    //if not player faction or player colony faction
                    PCFaction.TryAffectGoodwillWith(faction, (Find.FactionManager.OfPlayer.RelationWith(faction).goodwill - PCFaction.RelationWith(faction).goodwill));
                    PCFaction.TrySetRelationKind(faction, Find.FactionManager.OfPlayer.RelationKindWith(faction));
                    //Log.Message(Find.FactionManager.OfPlayer.RelationWith(faction).goodwill + " player:colony " + PCFaction.RelationWith(faction).goodwill);
                }
            }
        }
    }
}
