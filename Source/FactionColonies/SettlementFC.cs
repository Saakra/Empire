﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld.Planet;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    public class SettlementFC : IExposable, ILoadReferenceable
    {
        public SettlementFC()
        {

        }

        public string GetUniqueLoadID()
        {
            return "SettlementFC_" + this.loadID;
        }
        public SettlementFC(string name, int location)
        {
            this.name = name;
            this.mapLocation = location;
            this.loadID = Find.World.GetComponent<FactionFC>().GetNextSettlementFCID();
            
            this.settlementLevel = 1;

            //Efficiency Multiplier
            this.productionEfficiency = 1.0;
            this.workers = 0;
            this.workersMax = this.settlementLevel * 3 + returnMaxWorkersFromPrisoners();
            this.workersUltraMax = workersMax + 5 + returnOverMaxWorkersFromPrisoners();

            // Log.Message(Find.WorldGrid.tiles[location].biome.ToString());   <= Returns biome
            //biome info
            this.biome = Find.WorldGrid.tiles[location].biome.ToString();

            this.hilliness = Find.WorldGrid.tiles[location].hilliness.ToString();
            this.biomeDef = DefDatabase<BiomeResourceDef>.GetNamed(this.biome, false);

            //modded biomes
            if (this.biomeDef == default(BiomeResourceDef))
            {
                //Log Modded Biome
                this.biomeDef = BiomeResourceDefOf.defaultBiome;
            }

            //Log.Message(hilliness);
            this.hillinessDef = DefDatabase<BiomeResourceDef>.GetNamed(this.hilliness);

            for (int i = 0; i < 8; i++)
            {
                this.buildings.Add(BuildingFCDefOf.Empty);
            }
            for (int i = 0; i < 6; i++)
            {
                this.policies.Add(PolicyFCDefOf.Empty);
            }

            initBaseProduction();
            updateProduction();

        }

        public void addPrisoner(Pawn prisoner)
        {
            prisonerList.Add(new FCPrisoner(prisoner, this));
            //Log.Message(prisoners.Count().ToString());
        }

       public int numberBuildings
        {
            get
            {
                return 3 + (int)Math.Floor(settlementLevel / 2f);
            }
        }

        public void upgradeSettlement()
        {
            settlementLevel += 1;
            updateStats();
        }

        public void delevelSettlement()
        {
            settlementLevel -= 1;
            updateStats();
        }

        public void initBaseProduction()
        {
            for (int i = 0; i < getNumberResource(); i++)
            {
                returnResourceByInt(i).baseProduction = biomeDef.BaseProductionAdditive[i] + hillinessDef.BaseProductionAdditive[i];
                returnResourceByInt(i).baseProduction = biomeDef.BaseProductionMultiplicative[i] + hillinessDef.BaseProductionMultiplicative[i];
            }
        }

        public void updateProfitAndProduction() //updates both profit and production
        {
            updateProduction();
            updateProfit();
            updateStats();
        }

        public void updateStats()
        {
            //Military Settlement Level
            settlementMilitaryLevel = (settlementLevel - 1) + Convert.ToInt32(traitUtilsFC.cycleTraits(new double(), "militaryBaseLevel", traits, "add") + traitUtilsFC.cycleTraits(new double(), "militaryBaseLevel", Find.World.GetComponent<FactionFC>().traits, "add"));

            //Worker Stats
            workersMax = (settlementLevel * 3) + (traitUtilsFC.cycleTraits(new double(), "workerBaseMax", traits, "add") + traitUtilsFC.cycleTraits(new double(), "workerBaseMax", Find.World.GetComponent<FactionFC>().traits, "add")) + returnMaxWorkersFromPrisoners();
            workersUltraMax = (workersMax + 5 + (traitUtilsFC.cycleTraits(new double(), "workerBaseOverMax", traits, "add") + traitUtilsFC.cycleTraits(new double(), "workerBaseOverMax", Find.World.GetComponent<FactionFC>().traits, "add")) + returnOverMaxWorkersFromPrisoners());
        }

        public void updateProfit() //updates profit
        {
            totalUpkeep = getTotalUpkeep();
            updateWorkerCost();
            totalIncome = getTotalIncome();
            totalProfit = Convert.ToInt32(totalIncome - totalUpkeep);
        }

        public void updateHappiness()
        {
            double happinessGainMultiplier = (traitUtilsFC.cycleTraits(new double(), "happinessGainedMultiplier", traits, "multiply") * traitUtilsFC.cycleTraits(new double(), "happinessGainedMultiplier", Find.World.GetComponent<FactionFC>().traits, "multiply"));
            double happinessLostMultiplier = (traitUtilsFC.cycleTraits(new double(), "happinessLostMultiplier", traits, "multiply") * traitUtilsFC.cycleTraits(new double(), "happinessLostMultiplier", Find.World.GetComponent<FactionFC>().traits, "multiply"));

            happiness += happinessGainMultiplier * (FactionColonies.happinessBaseGain + traitUtilsFC.cycleTraits(new double(), "happinessGainedBase", traits, "add") + traitUtilsFC.cycleTraits(new double(), "happinessGainedBase", Find.World.GetComponent<FactionFC>().traits, "add")); //Go through traits and add happiness where needed
            happiness -= happinessLostMultiplier * (FactionColonies.happinessBaseLost +  traitUtilsFC.cycleTraits(new double(), "happinessLostBase", traits, "add") + traitUtilsFC.cycleTraits(new double(), "happinessLostBase", Find.World.GetComponent<FactionFC>().traits, "add")); //Go through traits and remove happiness where needed

            if (happiness <= 0)
            {
                happiness = 1;
            }
            if (happiness > 100)
            {
                happiness = 100;
            }

        }

        public void updateLoyalty()
        {
            double loyaltyGainMultiplier = (traitUtilsFC.cycleTraits(new double(), "loyaltyGainedMultiplier", traits, "multiply") * traitUtilsFC.cycleTraits(new double(), "loyaltyGainedMultiplier", Find.World.GetComponent<FactionFC>().traits, "multiply"));
            double loyaltyLostMultiplier = (traitUtilsFC.cycleTraits(new double(), "loyaltyLostMultiplier", traits, "multiply") * traitUtilsFC.cycleTraits(new double(), "loyaltyLostMultiplier", Find.World.GetComponent<FactionFC>().traits, "multiply"));

            loyalty += loyaltyGainMultiplier * (FactionColonies.loyaltyBaseGain + traitUtilsFC.cycleTraits(new double(), "loyaltyGainedBase", traits, "add") + traitUtilsFC.cycleTraits(new double(), "loyaltyGainedBase", Find.World.GetComponent<FactionFC>().traits, "add")); //Go through traits and add loyalty where needed
            loyalty -= loyaltyLostMultiplier * (FactionColonies.loyaltyBaseLost + traitUtilsFC.cycleTraits(new double(), "loyaltyLostBase", traits, "add") + traitUtilsFC.cycleTraits(new double(), "loyaltyLostBase", Find.World.GetComponent<FactionFC>().traits, "add")); //Go through traits and remove loyalty where needed

            if( loyalty <= 0)
            {
                loyalty = 1;
            }
            if (loyalty > 100)
            {
                loyalty = 100;
            }

        }

        public void updateProsperity()
        {
            prosperity += (FactionColonies.prosperityBaseRecovery + traitUtilsFC.cycleTraits(new double(), "prosperityBaseRecovery", traits, "add") + traitUtilsFC.cycleTraits(new double(), "prosperityBaseRecovery", Find.World.GetComponent<FactionFC>().traits, "add")); //Go through traits and add prosperity where needed

            if (prosperity <= 0)
            {
                prosperity = 1;
            }
            if (prosperity > 100)
            {
                prosperity = 100;
            }
        }

        public void updateUnrest()
        {
            double unrestGainMultiplier = (traitUtilsFC.cycleTraits(new double(), "unrestGainedMultiplier", traits, "multiply") * traitUtilsFC.cycleTraits(new double(), "unrestGainedMultiplier", Find.World.GetComponent<FactionFC>().traits, "multiply"));
            double unrestLostMultiplier = (traitUtilsFC.cycleTraits(new double(), "unrestLostMultiplier", traits, "multiply") * traitUtilsFC.cycleTraits(new double(), "unrestLostMultiplier", Find.World.GetComponent<FactionFC>().traits, "multiply"));

            unrest += unrestGainMultiplier * (FactionColonies.unrestBaseGain + traitUtilsFC.cycleTraits(new double(), "unrestGainedBase", traits, "add") + traitUtilsFC.cycleTraits(new double(), "unrestGainedBase", Find.World.GetComponent<FactionFC>().traits, "add")); //Go through traits and add unrest where needed
            unrest -= unrestLostMultiplier * (FactionColonies.unrestBaseLost + traitUtilsFC.cycleTraits(new double(), "unrestLostBase", traits, "add") + traitUtilsFC.cycleTraits(new double(), "unrestLostBase", Find.World.GetComponent<FactionFC>().traits, "add")); //Go through traits and remove unrest where needed

            if (unrest < 0)
            {
                unrest = 0;
            }
            if (unrest > 100)
            {
                unrest = 100;
            }

        }


        public void updateProduction() //updates production of settlemetns
        {
            for (int i = 0; i < getNumberResource(); i++)
            {
                //Grab trait additive variables

                returnResourceByInt(i).baseProduction = biomeDef.BaseProductionAdditive[i] + hillinessDef.BaseProductionAdditive[i] + traitUtilsFC.cycleTraits(new double(), "productionBase" + returnResourceNameByInt(i), traits, "add") + traitUtilsFC.cycleTraits(new double(), "productionBase" + returnResourceNameByInt(i), Find.World.GetComponent<FactionFC>().traits, "add");
                returnResourceByInt(i).baseProductionMultiplier = biomeDef.BaseProductionMultiplicative[i] * hillinessDef.BaseProductionMultiplicative[i] * ((100 + traitUtilsFC.cycleTraits(new double(), "taxBasePercentage", traits, "add") + traitUtilsFC.cycleTraits(new double(), "taxBasePercentage", Find.World.GetComponent<FactionFC>().traits, "add"))/100);



                //add up additive variables
                double tempAdditive = 0;
                if(returnResourceByInt(i).baseProductionAdditives.Count() > 1)  //over one to skip null value (used to save in Expose
                {
                    for (int k = 1; k < returnResourceByInt(i).baseProductionAdditives.Count(); k++)
                    {
                        tempAdditive += returnResourceByInt(i).baseProductionAdditives[k].value;
                    }
                }


                //multiply multiplicative variables
                double tempMultiplier = 1;
                if (returnResourceByInt(i).baseProductionMultipliers.Count() > 1) //over one to skip null value (used to save in Expose
                {
                    for (int k = 1; k < returnResourceByInt(i).baseProductionMultipliers.Count(); k++)
                    {
                        tempMultiplier *= returnResourceByInt(i).baseProductionMultipliers[k].value;
                    }
                }




                //calculate end multiplier + endproduction and update to resource
                returnResourceByInt(i).endProductionMultiplier = (returnResourceByInt(i).baseProductionMultiplier * (prosperity/100) * tempMultiplier * traitUtilsFC.cycleTraits(new double(), "productionMultiplier" + returnResourceNameByInt(i), traits, "multiply") * traitUtilsFC.cycleTraits(new double(), "productionMultiplier" + returnResourceNameByInt(i), Find.World.GetComponent<FactionFC>().traits, "multiply"));
                returnResourceByInt(i).endProduction = returnResourceByInt(i).endProductionMultiplier * ((returnResourceByInt(i).baseProduction + tempAdditive) * returnResourceByInt(i).assignedWorkers);
            }
        }

        public double getTotalIncome() //return total income of settlements
        {
            double income = 0;
                for (int i = 0; i < getNumberResource(); i++)
                {
                if (returnResourceByInt(i) != null)
                {
                    if (returnResourceByInt(i).isTithe == false)
                    {  //if resource is not paid by tithe
                        income += (returnResourceByInt(i).endProduction * LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>().silverPerResource);
                    }
                    else
                    {  //if resource is paid by tithe  (Calculate value of tithe items

                    }
                }
                }
            //Log.Message("income " + income.ToString());
            return income;
        }

        public int getTotalWorkers()
        {
            int totalWorkers = 0;
            for (int i = 0; i < getNumberResource(); i++)
            {
                totalWorkers += returnResourceByInt(i).assignedWorkers;
            }
            if (totalWorkers > workersUltraMax)
            {
                while (totalWorkers > workersUltraMax)
                {
                    if(increaseWorkers(-1,-1) == true)
                    {
                        totalWorkers -= 1;
                    }
                    //Log.Message("Remove 1 worker");
                }
            }
            return totalWorkers;
        }

        public bool increaseWorkers(int ResourceID, int numWorkers)
        {
            if (ResourceID == -1 && numWorkers < 0 && workers > workersUltraMax)
            {
                while (workers > workersUltraMax)
                {
                    int num = Rand.RangeInclusive(0, getNumberResource()-1);
                    //Log.Message(num.ToString());
                    if (returnResourceByInt(num).assignedWorkers > 0)
                    {
                        returnResourceByInt(num).assignedWorkers -= 1;
                        return true;
                    }
                }
            } else 
            if (workers + numWorkers <= workersUltraMax && workers + numWorkers >= 0 && returnResourceByInt(ResourceID).assignedWorkers + numWorkers <= workersUltraMax && returnResourceByInt(ResourceID).assignedWorkers + numWorkers >= 0)
            {
                workers += numWorkers;
                returnResourceByInt(ResourceID).assignedWorkers += numWorkers;
                updateProfitAndProduction();
                Find.World.GetComponent<FactionFC>().updateTotalProfit();
                return true;
            }
            return false; 
        }

        public double getBaseWorkerCost()
        {
            return (LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>().workerCost + (traitUtilsFC.cycleTraits(new double(), "workerBaseCost", traits, "add") + traitUtilsFC.cycleTraits(new double(), "workerBaseCost", Find.World.GetComponent<FactionFC>().traits, "add")));
            //add building/faction modifierse
        }

        public double getTotalUpkeep() //returns total upkeep of all settlements
        {
            workers = getTotalWorkers();
            double upkeep = 0;
            double overWork;
            if (workers > workersMax)
            {
                overWork = (int)(workers - workersMax);
            }
            else
            {
                overWork = 0;
            }
            workerTotalUpkeep = (workers * getBaseWorkerCost()) + ((workers * getBaseWorkerCost()) * (overWork / 20));

            //add building upkeep

            upkeep += (workerTotalUpkeep);

            foreach (BuildingFCDef building in buildings)
            {
                upkeep += building.upkeep;
            }

            //Log.Message("upkeep " + upkeep.ToString());
            return upkeep;
        }

        public void updateWorkerCost() //runs inside updateProfit to attach during updating
        {
            workerCost = (workerTotalUpkeep/workers); 
        }

        public double getTotalProfit() //returns total profit (income - upkeep) of all settlements
        {
            return (getTotalIncome() - getTotalUpkeep());
        }

        public void ExposeData()
        {
            Scribe_Values.Look<int>(ref mapLocation, "mapLocation");
            Scribe_Values.Look<string>(ref name, "name");
            Scribe_Values.Look<int>(ref loadID, "loadID", -1);
            Scribe_Values.Look<string>(ref title, "title");
            Scribe_Values.Look<string>(ref description, "description");
            Scribe_Values.Look<double>(ref productionEfficiency, "productionEfficiency");
            Scribe_Values.Look<double>(ref workers, "workers");
            Scribe_Values.Look<double>(ref workersMax, "workersMax");
            Scribe_Values.Look<double>(ref workersUltraMax, "workersUltraMax");
            Scribe_Values.Look<int>(ref settlementLevel, "settlementLevel");
            Scribe_Values.Look<int>(ref settlementMilitaryLevel, "settlementMilitaryLevel");
            Scribe_Values.Look<double>(ref unrest, "unrest");
            Scribe_Values.Look<double>(ref loyalty, "loyalty");
            Scribe_Values.Look<double>(ref happiness, "happiness");
            Scribe_Values.Look<double>(ref prosperity, "prosperity");
            Scribe_Values.Look<double>(ref workerCost, "workerCost");
            Scribe_Values.Look<double>(ref workerTotalUpkeep, "workerTotalUpkeep");




            //save resources
            Scribe_Deep.Look<ResourceFC>(ref food, "food");
            Scribe_Deep.Look<ResourceFC>(ref weapons, "weapons");
            Scribe_Deep.Look<ResourceFC>(ref apparel, "apparel");
            Scribe_Deep.Look<ResourceFC>(ref animals, "animals");
            Scribe_Deep.Look<ResourceFC>(ref logging, "logging");
            Scribe_Deep.Look<ResourceFC>(ref mining, "mining");
            Scribe_Deep.Look<ResourceFC>(ref research, "research");
            Scribe_Deep.Look<ResourceFC>(ref power, "power");
            Scribe_Deep.Look<ResourceFC>(ref medicine, "medicine");


            //Taxes
            Scribe_Collections.Look<BuildingFCDef>(ref buildings, "buildings", LookMode.Def);
            Scribe_Collections.Look<PolicyFCDef>(ref policies, "policies", LookMode.Def);
            Scribe_Collections.Look<Thing>(ref tithe, "tithe", LookMode.Deep);
            Scribe_Values.Look<int>(ref titheEstimatedIncome, "titheEstimatedIncome");
            Scribe_Values.Look<float>(ref silverIncome, "silverIncome");


            //Traits
            Scribe_Collections.Look<FCTraitEffectDef>(ref traits, "traits", LookMode.Def);

            //Biome_info
            Scribe_Values.Look<string>(ref hilliness, "hilliness");
            Scribe_Values.Look<string>(ref biome, "biome");
            Scribe_Defs.Look<BiomeResourceDef>(ref hillinessDef, "hillinessdef");
            Scribe_Defs.Look<BiomeResourceDef>(ref biomeDef, "biomedef");

            //Military
            Scribe_Values.Look<bool>(ref militaryBusy, "militaryBusy");
            Scribe_Values.Look<int>(ref militaryLocation, "militaryLocation");
            Scribe_Values.Look<string>(ref militaryJob, "militaryJob");
            Scribe_References.Look<Faction>(ref militaryEnemy, "militaryEnemy");
            Scribe_Values.Look<bool>(ref isUnderAttack, "isUnderAttack");
            Scribe_References.Look<MercenarySquadFC>(ref militarySquad, "militarySquad");
            Scribe_Values.Look<int>(ref artilleryTimer, "artilleryTimer");

            //Prisoners
            Scribe_Collections.Look<Pawn>(ref prisoners, "prisoners", LookMode.Deep);
            Scribe_Collections.Look<FCPrisoner>(ref prisonerList, "prisonerList", LookMode.Deep);

        }

        //Settlement Base Info
        public int mapLocation;
        public string name;
        public int loadID;
        public string title = "Hamlet".Translate();
        public string description = "What are you doing here? Get out of me!";
        public double workers = 0;
        public double workersMax = 0;
        public double workersUltraMax = 0;
        public double workerCost = 0;
        public double workerTotalUpkeep = 0;
        public int settlementLevel = 1;
        public int settlementMilitaryLevel = 0;
        public double unrest = 0;
        public double loyalty = 100;
        public double happiness = 100;
        public double prosperity = 100;
        public List<BuildingFCDef> buildings = new List<BuildingFCDef>();
        public List<PolicyFCDef> policies = new List<PolicyFCDef>();
        public List<FCTraitEffectDef> traits = new List<FCTraitEffectDef>();
        public List<Pawn> prisoners = new List<Pawn>();
        public List<FCPrisoner> prisonerList = new List<FCPrisoner>();



        public float silverIncome = 0;
        public List<Thing> tithe = new List<Thing>();
        public int titheEstimatedIncome = 0;


        public string hilliness;
        public string biome;
        public BiomeResourceDef hillinessDef;
        public BiomeResourceDef biomeDef;


        //ui only
        public double totalUpkeep;
        public double totalIncome;
        public double totalProfit;


        //Military stuff
        public bool militaryBusy = false;
        public int militaryLocation = -1;
        public string militaryJob = "";
        public int militaryBusyTimer = 0;
        public Faction militaryEnemy = null;
        public bool isUnderAttack = false;
        public MercenarySquadFC militarySquad = null;
        public int artilleryTimer = 0;



        //public static Biome biome;

        //Settlement Production Information
        public double productionEfficiency; //Between 0.1 - 1

        public bool isMilitaryBusy()
        {
            if (militaryBusy) 
            { 
                Messages.Message("militaryAlreadyAssigned".Translate(), MessageTypeDefOf.RejectInput); 
            }
            return militaryBusy;
        }

        public bool isMilitarySquadValid()
        {
            if (militarySquad != null)
            {
                if (militarySquad.outfit != null)
                {
                    if (militarySquad.EquippedMercenaries.Count > 0)
                    {
                        return true;
                    }
                    else
                    {
                        Messages.Message("You can't deploy a squad with no equipped personnel!", MessageTypeDefOf.RejectInput);
                        return false;
                    }
                } else
                {
                    Messages.Message("There is no squad loadout assigned to that settlement!", MessageTypeDefOf.RejectInput);
                    return false;
                }
            }
            else
            {
                Messages.Message("There is no military squad assigned to that settlement!", MessageTypeDefOf.RejectInput);
                return false;

            }
        }

        public bool isMilitarySquadValidSilent()
        {
            if (militarySquad != null)
            {
                return true;
            } else
            {
                return false;
            }
        }

        public bool isMilitaryBusySilent()
        {
            return militaryBusy;
        }



        public bool isMilitaryValid()
        {
            if (settlementMilitaryLevel > 0)
            {
                //if settlement military is more than level 0
                return true;    
            } else
            {
                return false;
            }
        }

        public bool isTargetOccupied(int location)
        {

            if (Find.World.GetComponent<FactionFC>().militaryTargets.Contains(location))
            {
                Messages.Message("targetAlreadyBeingAttacked".Translate(), MessageTypeDefOf.RejectInput);
                return true;
            } else
            {
                return false;
            }
        }

    

        public bool sendMilitary(int location, string job, int timeToFinish, Faction enemy)
        {
            if (isMilitaryBusy() == false && isTargetOccupied(location) == false)
            {
                //if military is not busy
                militaryBusy = true;
                militaryJob = job;
                militaryLocation = location;
                if (enemy != null)
                {
                    militaryEnemy = enemy;
                }
                militaryBusyTimer = timeToFinish + Find.TickManager.TicksGame;
                if (job != "Deploy")
                {
                    Find.World.GetComponent<FactionFC>().militaryTargets.Add(location);
                    
                }


                if (militaryJob == "raidEnemySettlement")
                {
                    FCEvent tmp = FCEventMaker.MakeEvent(FCEventDefOf.raidEnemySettlement);
                    tmp.hasCustomDescription = true;
                    tmp.timeTillTrigger = Find.TickManager.TicksGame + timeToFinish;
                    tmp.location = mapLocation;
                    tmp.customDescription = TranslatorFormattedStringExtensions.Translate("settlementMilitaryForcesRaiding",  name, returnMilitaryTarget().Label);// + 
                    Find.World.GetComponent<FactionFC>().addEvent(tmp);
                }

                if (militaryJob == "captureEnemySettlement")
                {
                    FCEvent tmp = FCEventMaker.MakeEvent(FCEventDefOf.captureEnemySettlement);
                    tmp.hasCustomDescription = true;
                    tmp.timeTillTrigger = Find.TickManager.TicksGame + timeToFinish;
                    tmp.location = mapLocation;
                    tmp.customDescription = TranslatorFormattedStringExtensions.Translate("settlementMilitaryForcesCapturing", name, returnMilitaryTarget().Label);// + 
                    Find.World.GetComponent<FactionFC>().addEvent(tmp);
                }

                if (militaryJob == "defendFriendlySettlement")
                {
                    //no event needed here
                    Find.LetterStack.ReceiveLetter("militarySent".Translate(), TranslatorFormattedStringExtensions.Translate("militarySentToDefend", name, Find.World.GetComponent<FactionFC>().returnSettlementByLocation(location).name), LetterDefOf.NeutralEvent);
                }

                if (militaryJob == "Deploy")
                {
                    Find.LetterStack.ReceiveLetter("Military Deployed", "The Military forces of " + name + " have been deployed to " + Find.Maps[militaryLocation].Parent.LabelCap,  LetterDefOf.NeutralEvent);
                    militaryBusyTimer = -1;
                }

                //Find.World.GetComponent<FactionFC>().addEvent(tmp);
                return true;
            } else
            {
                return false;
            }
        }

        public Settlement returnMilitaryTarget()
        {
            if (militaryLocation == -1)
            {
                return null;
            } else
            {
                return Find.WorldObjects.SettlementAt(militaryLocation);
            }
        }


        //CURRENTLY NOT USED
        public void militaryTick()
        {
            if (militaryBusy == false)
            { //if not busy
                return;
            } else
            { //if busy
                if (militaryBusyTimer != -1 && militaryBusyTimer >= Find.TickManager.TicksGame)
                {
                  

                    returnMilitary();
                    return;
                }
            }
        }

        public void processMilitaryEvent()
        {
            //calculate success and all of that shit

            //Debug by setting faction automatically
            //returnMilitaryTarget().SetFaction(FactionColonies.getPlayerColonyFaction());
            if (Find.World.GetComponent<FactionFC>().militaryTargets.Contains(militaryLocation)){
                Find.World.GetComponent<FactionFC>().militaryTargets.Remove(militaryLocation);
            }
            //Log.Message(winner + " job = " + militaryJob);
            //Process end result here
            //attacker == 0; defender == 1;

            if (militaryJob == "raidEnemySettlement")
            {
                int winner = simulateBattleFC.FightBattle(militaryForce.createMilitaryForceFromSettlement(this), militaryForce.createMilitaryForceFromFaction(militaryEnemy, false));
                if (winner == 0)
                {
                    //if won
                    TechLevel tech = Find.WorldObjects.SettlementAt(militaryLocation).Faction.def.techLevel;
                    int lootLevel;
                    

                    switch (tech)
                    {
                        case TechLevel.Archotech:
                        case TechLevel.Ultra:
                        case TechLevel.Spacer:
                            lootLevel = 4;
                            break;
                        case TechLevel.Industrial:
                            lootLevel = 3;
                            break;
                        case TechLevel.Medieval:
                        case TechLevel.Neolithic:
                            lootLevel = 2;
                            break;
                        default:
                            lootLevel = 1;
                            break;
                    }

                    List<Thing> loot = PaymentUtil.generateRaidLoot(lootLevel, tech);

                    string text = "settlementDeliveredLoot".Translate();
                    foreach (Thing thing in loot)
                    {
                        text = text + thing.LabelCap + " x" + thing.stackCount + "\n ";
                    }

                    int num = new IntRange(0, 10).RandomInRange;
                    if (num <= 4)
                    {
                        Pawn prisoner = PaymentUtil.generatePrisoner(militaryEnemy);
                        text = text + TranslatorFormattedStringExtensions.Translate("PrisonerCaptureInfo", prisoner.Name.ToString(), this.name);
                        this.addPrisoner(prisoner);
                    }

                    Find.LetterStack.ReceiveLetter("RaidLoot".Translate(), TranslatorFormattedStringExtensions.Translate("RaidEnemySettlementSuccess", Find.WorldObjects.SettlementAt(militaryLocation).LabelCap) + "\n" + text, LetterDefOf.PositiveEvent, new LookTargets(Find.WorldObjects.SettlementAt(militaryLocation)));

                    //deliver
                    PaymentUtil.deliverThings(loot);

                } else 
                if (winner == 1)
                {
                    //if lost
                    Find.LetterStack.ReceiveLetter("RaidFailure".Translate(), TranslatorFormattedStringExtensions.Translate("RaidEnemySettlementFailure", Find.WorldObjects.SettlementAt(militaryLocation).LabelCap), LetterDefOf.NegativeEvent, new LookTargets(Find.WorldObjects.SettlementAt(militaryLocation)));
                }
            }
            else
            if (militaryJob == "captureEnemySettlement")
            {
                int winner = simulateBattleFC.FightBattle(militaryForce.createMilitaryForceFromSettlement(this), militaryForce.createMilitaryForceFromFaction(militaryEnemy, false));
                if (winner == 0)
                {
                    //Log.Message("Won");
                    //Find.WorldObjects.SettlementAt(militaryLocation).SetFaction(FactionColonies.getPlayerColonyFaction());
                    string tmpName = Find.WorldObjects.SettlementAt(militaryLocation).LabelCap;
                    TechLevel tech = Find.WorldObjects.SettlementAt(militaryLocation).Faction.def.techLevel;
                    Find.WorldObjects.SettlementAt(militaryLocation).Destroy();
                    Settlement settlement = FactionColonies.createPlayerColonySettlement(militaryLocation);
                    SettlementFC settlementfc = Find.World.GetComponent<FactionFC>().returnSettlementByLocation(militaryLocation);
                    settlement.Name = tmpName;
                    settlementfc.name = tmpName;
                    int upgradeTimes;

                    switch (tech)
                    {
                        case TechLevel.Archotech:
                        case TechLevel.Ultra:
                        case TechLevel.Spacer:
                            upgradeTimes = 2;
                            break;
                        case TechLevel.Industrial:
                            upgradeTimes = 1;
                            break;
                        default:
                            upgradeTimes = 0;
                            break;
                    }
                    for (int i = 1; i < upgradeTimes; i++)
                    {
                        settlementfc.upgradeSettlement();
                    }

                    settlementfc.loyalty = 15;
                    settlementfc.happiness = 25;
                    settlementfc.unrest = 20;
                    settlementfc.prosperity = 70;


                    Find.LetterStack.ReceiveLetter("CaptureSettlement".Translate(),  TranslatorFormattedStringExtensions.Translate("CaptureEnemySettlementSuccess" ,this.name, Find.WorldObjects.SettlementAt(militaryLocation).Name, settlementfc.settlementLevel), LetterDefOf.PositiveEvent, new LookTargets(Find.WorldObjects.SettlementAt(militaryLocation)));
                } else
                if (winner == 1)
                {

                    //Log.Message("Loss");
                    Find.LetterStack.ReceiveLetter("CaptureSettlement".Translate(), TranslatorFormattedStringExtensions.Translate("CaptureEnemySettlementFailure", this.name, Find.WorldObjects.SettlementAt(militaryLocation).Name), LetterDefOf.NegativeEvent, new LookTargets(Find.WorldObjects.SettlementAt(militaryLocation)));
                }
            }

            cooldownMilitary();
        }
        public void returnMilitary()
        {
            militaryBusy = false;
            militaryJob = "";
            militaryLocation = -1;
            militaryEnemy = null;
        }

        public void cooldownMilitary()
        {
            militaryJob = "cooldown";
            militaryBusy = true;
            militaryLocation = mapLocation;
            militaryEnemy = null;

            FCEvent tmp = FCEventMaker.MakeEvent(FCEventDefOf.cooldownMilitary);
            tmp.hasCustomDescription = true;
            tmp.timeTillTrigger = Find.TickManager.TicksGame + 300000;
            tmp.location = mapLocation;
            tmp.customDescription = TranslatorFormattedStringExtensions.Translate("MilitaryForcesReorganizing", name);// + 
            Find.World.GetComponent<FactionFC>().addEvent(tmp);
        }

        public void updateDescription()
        {
            //biome
            
            switch (biomeDef.defName)
            {
                case "BorealForest":
                    description = "FCDescBorealForest".Translate();
                    break;
                case "Tundra":
                    description = "FCDescTundra".Translate();
                    break;
                case "ColdBog":
                    description = "FCDescColdBog".Translate();
                    break;
                case "IceSheet":
                    description = "FCDescIceSheet".Translate();
                    break;
                case "SeaIce":
                    description = "FCDescIceSheet".Translate();
                    break;
                case "TemperateForest":
                    description = "FCDescTemperateForest".Translate();
                    break;
                case "TemperateSwamp":
                    description = "FCDescTemperateSwamp".Translate();
                    break;
                case "TropicalRainforest":
                    description = "FCDescTropicalRainforest".Translate();
                    break;
                case "AridShrubland":
                    description = "FCDescAridShrubland".Translate();
                    break;
                case "Desert":
                    description = "FCDescDesert".Translate();
                    break;
                case "ExtremeDesert":
                    description = "FCDescExtremeDesert".Translate();
                    break;
                default:
                    description = "FCDescUnknown".Translate();
                    break;
            }


            //town size

            switch (settlementLevel)
            {
                case 1:
                    description += "FCTownLevel1".Translate();
                    break;
                case 2:
                    description += "FCTownLevel2".Translate();
                    break;
                case 3:
                case 4:
                    description += "FCTownLevel3".Translate();
                    break;
                case 5:
                case 6:
                    description += "FCTownLevel4".Translate();
                    break;
                case 7:
                case 8:
                default:
                    description += "FCTownLevel5".Translate();
                    break;
            }
        }

        public List<FCTraitEffectDef> returnListSettlementTraits()
        {
            List<FCTraitEffectDef> tmpList = new List<FCTraitEffectDef>();
            foreach (FCTraitEffectDef trait in traits)
            {
                tmpList.Add(trait);
            }
            return tmpList;
        }

        public void deconstructBuilding(int buildingSlot)
        {
            foreach (FCTraitEffectDef trait in buildings[buildingSlot].traits) //remove traits
            {
                foreach (FCTraitEffectDef traitSettlement in traits)
                {
                    if (traitSettlement == trait)
                    {
                        traits.Remove(traitSettlement);
                        break;
                    }
                }
            }
            buildings[buildingSlot] = BuildingFCDefOf.Empty;
        }


        public void generatePrisonerTable()
        {
            if (prisonerList == null)
            {
                prisonerList = new List<FCPrisoner>();
            }
            foreach (Pawn pawn in prisoners)
            {
                prisonerList.Add(new FCPrisoner(pawn, this));
            }
            prisoners = new List<Pawn>();
            

        }

        public void repealPolicy(int policySlot)
        {
            foreach (FCTraitEffectDef trait in policies[policySlot].traits) //remove traits
            {
                foreach (FCTraitEffectDef traitSettlement in traits)
                {
                    if (traitSettlement == trait)
                    {
                        traits.Remove(traitSettlement);
                        break;
                    }
                }
            }
            policies[policySlot] = PolicyFCDefOf.Empty;
        }

        public int returnMaxWorkersFromPrisoners()
        {
            int num = 0;
            foreach (FCPrisoner prisoner in prisonerList)
            {
                if (prisoner.workload == FCWorkLoad.Medium)
                {
                    num++;
                }
                if (prisoner.workload == FCWorkLoad.Heavy)
                {
                    num += 2;
                }
            }
            return num;
        }

        public int returnOverMaxWorkersFromPrisoners()
        {
            int num = 0;
            foreach (FCPrisoner prisoner in prisonerList)
            {
                if (prisoner.workload == FCWorkLoad.Light)
                {
                    num++;
                }
            }
            Log.Message("max worker : " + num);
            return num;
        }
        public void enactPolicy(PolicyFCDef policy, int policySlot)
        {

            repealPolicy(policySlot);

            policies[policySlot] = policy;

            traits.AddRange(policy.traits); //add new traits

        }

        public bool validEnactPolicy(PolicyFCDef policy, int policySlot, bool message = true)
        {
            bool valid = true;
            foreach (PolicyFCDef slot in policies)  //check if already a building of that type constructed
            {
                if (slot == policy)
                {
                    valid = false;

                    if (message) { Messages.Message("PolicyAlreadyenacted".Translate() + "!", MessageTypeDefOf.RejectInput); }
                    break;
                }
            }

            if (PaymentUtil.getSilver() < policy.cost) //check if the player has enough money
            {
                valid = false;
                if (message) { Messages.Message("NotEnoughMoneyToEnact".Translate() + "!", MessageTypeDefOf.RejectInput); }
            }

            foreach (FCEvent event1 in Find.World.GetComponent<FactionFC>().events) //check if construction would match any already-occuring events
            {
                if (event1.source == mapLocation && event1.policy == policy && event1.def.defName == "enactPolicy")
                {
                    valid = false;
                    if (message) { Messages.Message("AlreadyPolicytypeBeingEnacted".Translate() + "!", MessageTypeDefOf.RejectInput); }
                    break;
                }

                if (event1.source == mapLocation && event1.policySlot == policySlot && event1.def.defName == "enactPolicy") //check if there is already a building being constructed in that slot
                {
                    valid = false;
                    if (message) { Messages.Message("AlreadyPolicyInSlot".Translate() + "!", MessageTypeDefOf.RejectInput); }

                    break;
                }
            }

            return valid;
        }


        public bool validConstructBuilding(BuildingFCDef building, int buildingSlot, SettlementFC settlement)
        {
            bool valid = true;
            foreach (BuildingFCDef slot in buildings)  //check if already a building of that type constructed
            {
                if (slot == building)
                {
                    valid = false;
                    Messages.Message("BuildingAlreadyType".Translate() + "!", MessageTypeDefOf.RejectInput);
                    break;
                }
            }

            if (PaymentUtil.getSilver() < building.cost) //check if the player has enough money
            {
                valid = false;
                Messages.Message("NotEnoughSilverConstructBuilding".Translate() + "!", MessageTypeDefOf.RejectInput);
            }

            foreach (FCEvent event1 in Find.World.GetComponent<FactionFC>().events) //check if construction would match any already-occuring events
            {
                if (event1.source == mapLocation && event1.building == building && event1.def.defName == "constructBuilding") 
                {
                    valid = false;
                    Messages.Message("BuildingBeingBuiltAlreadyType".Translate() + "!", MessageTypeDefOf.RejectInput);
                    break;
                }

                if (event1.source == mapLocation && event1.buildingSlot == buildingSlot && event1.def.defName == "constructBuilding") //check if there is already a building being constructed in that slot
                {
                    valid = false;
                    Messages.Message("BuildingAlreadyConstructed".Translate() + "!", MessageTypeDefOf.RejectInput);
                    break;
                }
            }

            if (building.applicableBiomes.Count() != 0)
            {
                bool match = false;
                
                if (building.applicableBiomes.Contains(settlement.biome))
                {
                    match = true;
                }
                

                //if found no matches
                if (match == false)
                {
                    valid = false;
                    Messages.Message("BuildingInvalidEnvironment".Translate(), MessageTypeDefOf.RejectInput);
                }
            }

            return valid;
        }

       

        public void constructBuilding(BuildingFCDef building, int buildingSlot)
        {

             deconstructBuilding(buildingSlot);

             buildings[buildingSlot] = building;

             traits.AddRange(building.traits); //add new traits

        }
        


        //Reference
        //0 - settlement name
        //1 - food end production
        //2 - weapon end pro
        //3 - apparel end pro
        //4 - animals end pro
        //5 - logging end pro
        //6 - mining end pro
        //7 - report button
        //8 - tithe est value
        //9 - Silver income
        //10 - location id


        public double returnTitheEstimatedValue()
        {
            double titheVal = 0;
            for (int i = 0; i < getNumberResource(); i++)
            {
                if (returnResourceByInt(i).isTithe == true)
                {
                    titheVal += returnResourceByInt(i).endProduction * LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>().silverPerResource;
                }
            }
            return titheVal;
        }

        public ResourceFC returnResource(string name) //used to return the correct resource based on string name
        {
            if (name == "food")
            {
                return food;
            }
            if (name == "weapons")
            {
                return weapons;
            }
            if (name == "apparel")
            {
                return apparel;
            }
            if (name == "animals")
            {
                return animals;
            }
            if (name == "logging")
            {
                return logging;
            }
            if (name == "mining")
            {
                return mining;
            }
            if (name == "research")
            {
                return research;
            }
            if (name == "power")
            {
                return power;
            }
            if (name == "medicine")
            {
                return medicine;
            }
            Log.Message("Unable to find resource - returnResource(string name)");
            return null;
        }
        public ResourceFC returnResourceByInt(int name) //used to return the correct resource based on string name
        {
            if (name == 0)
            {
                return food;
            }
            if (name == 1)
            {
                return weapons;
            }
            if (name == 2)
            {
                return apparel;
            }
            if (name == 3)
            {
                return animals;
            }
            if (name == 4)
            {
                return logging;
            }
            if (name == 5)
            {
                return mining;
            }
            if (name == 6)
            {
                return research;
            }
            if (name == 7)
            {
                return power;
            }
            if (name == 8)
            {
                return medicine;
            }
            Log.Message("Unable to find resource - returnResourceByInt(int name)");
            return null;
        }

        public string returnResourceNameByInt(int name) //used to return the correct resource based on string name
        {
            if (name == 0)
            {
                return "Food";
            }
            if (name == 1)
            {
                return "Weapons";
            }
            if (name == 2)
            {
                return "Apparel";
            }
            if (name == 3)
            {
                return "Animals";
            }
            if (name == 4)
            {
                return "Logging";
            }
            if (name == 5)
            {
                return "Mining";
            }
            if (name == 6)
            {
                return "Research";
            }
            if (name == 7)
            {
                return "Power";
            }
            if (name == 8)
            {
                return "Medicine";
            }
            Log.Message("Unable to find resource - returnResourceByInt(int name)");
            return null;
        }

        public int getNumberResource()
        {
            return 9;
        }

        //Settlment resources
        public ResourceFC food = new ResourceFC("food", "Food", 0);
        public ResourceFC weapons = new ResourceFC("weapons", "Weapons", 0);
        public ResourceFC apparel = new ResourceFC("apparel", "Apparel", 0);
        public ResourceFC animals = new ResourceFC("animals", "Animals", 0);
        public ResourceFC logging = new ResourceFC("logging", "Logging", 0);
        public ResourceFC mining = new ResourceFC("mining", "Mining", 0);
        public ResourceFC power = new ResourceFC("power", "Power", 0);
        public ResourceFC medicine = new ResourceFC("medicine", "Medicine", 0);
        public ResourceFC research = new ResourceFC("research", "Research", 0);


        public void taxProductionGoods() //update goods (TAX TAX TAX)   # Not used?
        {
            int silver = 0;
            for (int i = 0; i < getNumberResource(); i++)
            {
                if (returnResourceByInt(i).isTithe)
                { //if resource is paying via tithe
                    //generate the tithe things
                    //run tithe cash evaluation here
                    //ThingSetMaker gen = new ThingSetMaker();
                } else
                { //if resource is paying via silver
                    silver += (int) (returnResourceByInt(i).endProduction * LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>().silverPerResource);  //Add randomness?
                }
            }
        }

        //UNUSED FUNCTIONS
        public float getSilverIncome()
        {
            return silverIncome;
        }

        public void resetSilverIncome()
        {
            silverIncome = 0;
        }

        public void addSilverIncome(float amount)
        {
            silverIncome += amount;
        }

        public float returnSilverIncome(bool reset)
        {
            float income = silverIncome;

            if (reset)
            {
                resetSilverIncome();
            }

            return income;
        }

        public List<Thing> getTithe()
        {
            return tithe;
        }

        public void resetTithe()
        {
            tithe = new List<Thing>();
        }
        //UNUSED FUNCTIONS /END


        //returns the Settlement that the SettlementFC belongs to
        public Settlement returnFCSettlement()
        {
            return Find.WorldObjects.SettlementAt(this.mapLocation);
        }

        public void goTo()
        {
            Find.World.renderer.wantedMode = WorldRenderMode.Planet;

            //Select Settlement Tile
            //Find.WorldObjects.AnySettlementAt(settlementList[i][10])
            Find.WorldSelector.ClearSelection();
            Find.WorldSelector.Select(Find.WorldObjects.SettlementAt(mapLocation)); // = Convert.ToInt32(settlementList[i][10]); //(Find.World.GetComponent<FactionFC>().settlements[i]);
            if (Find.MainButtonsRoot.tabs.OpenTab != null)
            {
                Find.MainButtonsRoot.tabs.OpenTab.TabWindow.Close();
            }
        }

        public float createResearchPool()
        {
            double production = research.endProduction;
            return (float)Math.Round(production * FactionColonies.productionResearchBase);
        }

        public float createPowerPool()
        {
            double allotted = power.endProduction;
            return (float)Math.Round(allotted * 100);
        }

        public List<Thing> createTithe()
        {
            List<Thing> list = new List<Thing>();
            for (int i = 0; i < getNumberResource(); i++)
            {
                if (returnResourceByInt(i).isTithe == true && i != 6 && i != 7)
                {
                    List<Thing> tmpList = new List<Thing>();
                    double production = returnResourceByInt(i).endProduction;
                    production *= ((100 + traitUtilsFC.cycleTraits(new double(), "taxBasePercentage", traits, "add") + traitUtilsFC.cycleTraits(new double(), "taxBasePercentage", Find.World.GetComponent<FactionFC>().traits, "add")) /100);
                    int workers = returnResourceByInt(i).assignedWorkers;

                    tmpList = PaymentUtil.generateTithe(production, (double)LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>().productionTitheMod, workers, i, traitUtilsFC.cycleTraits(0.0, "taxBaseRandomModifier", Find.World.GetComponent<FactionFC>().traits, "add") + traitUtilsFC.cycleTraits(0.0, "taxBaseRandomModifier", this.traits, "add"));
                
                    foreach (Thing thing in tmpList)
                    {
                        list.Add(thing);
                    }
                }
            }

            return list;
        }

    }

}
