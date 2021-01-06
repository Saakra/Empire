using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld.Planet;
using RimWorld;
using UnityEngine;
using Verse;
using HarmonyLib;
using Verse.AI;

namespace FactionColonies
{

    public class FactionFC : WorldComponent
    {
        public int eventTimeDue = 0;
        public int taxTimeDue = Find.TickManager.TicksGame;
        public int timeStart = Find.TickManager.TicksGame;
        public int uiTimeUpdate = 0;
        public int dailyTimer = Find.TickManager.TicksGame;
        public int militaryTimeDue = 0;
        public int mercenaryTick = 0;
       

        public List<SettlementFC> settlements = new List<SettlementFC>();
        public string name = "PlayerFaction".Translate();
        public string title = "Bastion".Translate();
        public double averageHappiness = 100;
        public double averageLoyalty = 100;
        public double averageUnrest = 0;
        public double averageProsperity = 100;
        public double income = 0;
        public double upkeep = 0;
        public double profit = 0;
        public int capitalLocation = -1;
        public Map taxMap;
        public TechLevel techLevel = TechLevel.Undefined;
        private bool firstTick = true;


        //New Types of PRoductions
        public float researchPointPool = 0;
        public float powerPool = 0;
        public ThingWithComps powerOutput;

        public List<FCEvent> events = new List<FCEvent>();
        public List<String> settlementCaravansList = new List<string>(); //list of locations caravans already sent to

        public List<BillFC> OldBills = new List<BillFC>();
        public List<BillFC> Bills = new List<BillFC>();
        public bool autoResolveBills = false;
        public bool autoResolveBillsChanged = false;

        public List<PolicyFCDef> policies = new List<PolicyFCDef>() { PolicyFCDefOf.Empty, PolicyFCDefOf.Empty, PolicyFCDefOf.Empty, PolicyFCDefOf.Empty, PolicyFCDefOf.Empty, PolicyFCDefOf.Empty };
        public List<FCTraitEffectDef> traits = new List<FCTraitEffectDef>();
        public List<int> militaryTargets = new List<int>();


        //Faction resources
        public ResourceFC food = new ResourceFC("food", "Food", 1);
        public ResourceFC weapons = new ResourceFC("weapons", "Weapons", 1);
        public ResourceFC apparel = new ResourceFC("apparel", "Apparel", 1);
        public ResourceFC animals = new ResourceFC("animals", "Animals", 1);
        public ResourceFC logging = new ResourceFC("logging", "Logging", 1);
        public ResourceFC mining = new ResourceFC("mining", "Mining", 1);
        //public ResourceFC research = new ResourceFC("researching", "Researching", 1);
        public ResourceFC power = new ResourceFC("power", "Power", 1);
        public ResourceFC medicine = new ResourceFC("medicine", "Medicine", 1);
        public ResourceFC research = new ResourceFC("research", "Research", 1);

        //Faction Def
        public FactionFCDef factionDef = new FactionFCDef();

        //Update
        public double updateVersion = 0;
        public int nextSettlementFCID = 1;
        public int nextMilitaryForceID = 1;
        public int nextMercenarySquadID = 1;
        public int nextMercenaryID = 1;
        public int nextTaxID = 1;
        public int nextBillID = 1;
        public int nextEventID = 1;
        public int nextPrisonerID = 1;

        //Military 
        public int nextUnitID = 1;
        public int nextSquadID = 1;

        //Military Customization
        public MilitaryCustomizationUtil militaryCustomizationUtil = new MilitaryCustomizationUtil();



        //Call for aid
        // [HarmonyPatch(typeof(FactionDialogMaker), "CallForAid")]
        // class WorldObjectGizmos
        //{
        //    static void Prefix(Map map, Faction faction)
        //    {

        //    }
        // }

        [HarmonyPatch(typeof(FactionDialogMaker), "RequestMilitaryAidOption")]
        class disableMilitaryAid
        {
            static void Postfix(Map map, Faction faction, Pawn negotiator, ref DiaOption __result)
            {
                if (faction.def.defName == "PColony")
                {
                    __result = new DiaOption("RequestMilitaryAid".Translate(25));
                    __result.Disable("Disabled. Use the settlements military tab.");
                }
            }
        }



        [HarmonyPatch(typeof(WorldPawns), "PassToWorld")]
        class MercenaryPassToWorld
        {
            static bool Prefix(Pawn pawn, PawnDiscardDecideMode discardMode = PawnDiscardDecideMode.Decide)
            {
                if (Find.World.GetComponent<FactionFC>().militaryCustomizationUtil != null)
                {
                    if (Find.World.GetComponent<FactionFC>().militaryCustomizationUtil.AllMercenaryPawns.Contains(pawn))
                    {
                        //Don't pass
                        //Log.Message("POOf");
                        return false;
                    }
                }
                return true;
            }
        }


        [HarmonyPatch(typeof(Pawn), "GetGizmos")]
        class PawnGizmos
        {
            static void Postfix(ref Pawn __instance, ref IEnumerable<Gizmo> __result)
            {
                Pawn instance = __instance;
                if (__instance.guest != null)
                {
                    if (__instance.guest.IsPrisoner == true && __instance.guest.PrisonerIsSecure == true)
                    {
                        Pawn prisoner = __instance;


                        __result = __result.Concat(new[] { new Command_Action()
                        {
                        defaultLabel = "SendToSettlement".Translate(),
                        defaultDesc = "",
                        icon = texLoad.iconMilitary,
                        action = delegate
                        {
                            List<FloatMenuOption> settlementList = new List<FloatMenuOption>();

                            foreach (SettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
                            {
                                settlementList.Add(new FloatMenuOption(settlement.name + " - Settlement Level : " + settlement.settlementLevel + " - Prisoners: " + settlement.prisonerList.Count(), delegate
                                {
                                    //disappear colonist
                                    FactionColonies.sendPrisoner(prisoner, settlement);

                                    foreach (Map map in Find.Maps)
                                    {
                                        if (map.IsPlayerHome)
                                        {
                                            foreach (Building building in map.listerBuildings.allBuildingsColonist)
                                            {
                                                if (building is Building_Bed)
                                                {
                                                    Building_Bed bed = (Building_Bed)building;
                                                    foreach (Pawn pawn in bed.OwnersForReading)
                                                    {
                                                        if (pawn == instance)
                                                        {
                                                            bed.ForPrisoners = false;
                                                            bed.ForPrisoners = true;
                                                            break;
                                                        }
                                                    }
                                                }
                                            }

                                        }
                                    }
                                }));
                            }

                             FloatMenu floatMenu2 = new FloatMenu(settlementList);
                                        floatMenu2.vanishIfMouseDistant = true;
                                         Find.WindowStack.Add(floatMenu2);
                        }
                    }
                    });
                    }
                }
            }
        }

        //stops friendly faction from being a group source
        [HarmonyPatch(typeof(IncidentWorker_RaidFriendly), "TryResolveRaidFaction")]
        class RaidFriendlyStopSettlementFaction
        {
            static void Postfix(ref IncidentWorker_RaidFriendly __instance, ref bool __result, IncidentParms parms)
            {
                if (parms.faction == FactionColonies.getPlayerColonyFaction())
                {
                    Log.Message("Prevented");
                    parms.faction = null;
                    __result = false;
                    return;
                }
            }
        }


            //Faction worldmap gizmos
            //Goodwill by distance to settlement
            [HarmonyPatch(typeof(WorldObject), "GetGizmos")]
        class WorldObjectGizmos
        {
            static void Postfix(ref WorldObject __instance, ref IEnumerable<Gizmo> __result)
            {
                if(__instance.def.defName == "Settlement")
                {
                    //if settlement

                    int tile = __instance.Tile;
                    if (__instance.Faction != FactionColonies.getPlayerColonyFaction() && __instance.Faction != Find.FactionManager.OfPlayer)
                    {
                        //if a valid faction to target

                        Faction faction = __instance.Faction;
                        
                        string name = __instance.LabelCap;

                        __result = __result.Concat(new[] { new Command_Action()
                        {
                            defaultLabel = "AttackSettlement".Translate(), 
                            defaultDesc = "",
                            icon = texLoad.iconMilitary,
                            action = delegate
                            {
                                List<FloatMenuOption> list = new List<FloatMenuOption>();


                                list.Add(new FloatMenuOption("CaptureSettlement".Translate(), delegate
                                {
                                    List<FloatMenuOption> settlementList = new List<FloatMenuOption>();

                                    foreach (SettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
                                    {
                                        if (settlement.isMilitaryValid() == true)
                                        {
                                            //if military is valid to use.
                                        
                                        settlementList.Add(new FloatMenuOption(settlement.name + " " + "ShortMilitary".Translate() + " " + settlement.settlementMilitaryLevel + " - " + "FCAvailable".Translate() + ": " + (!settlement.isMilitaryBusySilent()).ToString(), delegate
                                        {
                                            if(settlement.isMilitaryBusy() == true)
                                            {
                                            } else
                                            {

                                                RelationsUtilFC.attackFaction(faction);

                                                settlement.sendMilitary(tile, "captureEnemySettlement", 60000, faction);


                                                //simulateBattleFC.FightBattle(militaryForce.createMilitaryForceFromSettlement(settlement), militaryForce.createMilitaryForceFromEnemySettlement(faction));
                                            }
                                        }, MenuOptionPriority.Default, null, null, 0f, null, null
                                        ));
                                        }

                                        
                                    }

                                    if (settlementList.Count == 0)
                                    {
                                        settlementList.Add(new FloatMenuOption("NoValidMilitaries".Translate(), null));
                                    }

                                     FloatMenu floatMenu2 = new FloatMenu(settlementList);
                                        floatMenu2.vanishIfMouseDistant = true;
                                         Find.WindowStack.Add(floatMenu2);
                                }, MenuOptionPriority.Default, null, null, 0f, null, null));

                                list.Add(new FloatMenuOption("RaidSettlement".Translate(), delegate
                                {
                                    List<FloatMenuOption> settlementList = new List<FloatMenuOption>();

                                    foreach (SettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
                                    {
                                        if (settlement.isMilitaryValid() == true)
                                        {
                                            //if military is valid to use.
                                        
                                        settlementList.Add(new FloatMenuOption(settlement.name + " " + "ShortMilitary".Translate() + " " + settlement.settlementMilitaryLevel + " - " + "FCAvailable".Translate() + ": "  + (!settlement.isMilitaryBusySilent()).ToString(), delegate
                                        {
                                            if(settlement.isMilitaryBusy() == true)
                                            {
                                                //military is busy
                                            } else
                                            {

                                                RelationsUtilFC.attackFaction(faction);

                                                settlement.sendMilitary(tile, "raidEnemySettlement", 60000, faction);


                                                //simulateBattleFC.FightBattle(militaryForce.createMilitaryForceFromSettlement(settlement), militaryForce.createMilitaryForceFromEnemySettlement(faction));
                                            }
                                        }, MenuOptionPriority.Default, null, null, 0f, null, null
                                        ));
                                        }


                                    }

                                    if (settlementList.Count == 0)
                                    {
                                        settlementList.Add(new FloatMenuOption("NoValidMilitaries".Translate(), null));
                                    }

                                     FloatMenu floatMenu2 = new FloatMenu(settlementList);
                                        floatMenu2.vanishIfMouseDistant = true;
                                         Find.WindowStack.Add(floatMenu2);


                                    //set to raid settlement here
                                }, MenuOptionPriority.Default, null, null, 0f, null, null));

                                FloatMenu floatMenu = new FloatMenu(list);
                                floatMenu.vanishIfMouseDistant = true;
                                Find.WindowStack.Add(floatMenu);
                            }
                        }

                        });

                        
                    } 
                    
                    
                    else



                    {
                        if (__instance.Faction == FactionColonies.getPlayerColonyFaction())
                        {
                            //is a colony of the player faction
                            if (Find.World.GetComponent<FactionFC>().returnSettlementByLocation(__instance.Tile).isUnderAttack == true)
                            {
                                //if settlement is under attack
                                __result = __result.Concat(new[] { new Command_Action()
                                {
                                    defaultLabel = "DefendSettlement".Translate(),
                                    defaultDesc = "",
                                    icon = texLoad.iconMilitary,
                                    action = delegate
                                    {
                                        List<FloatMenuOption> list = new List<FloatMenuOption>();

                                        
                                        FCEvent evt = MilitaryUtilFC.returnMilitaryEventByLocation(tile);
                                        list.Add(new FloatMenuOption(TranslatorFormattedStringExtensions.Translate("SettlementDefendingInformation", evt.militaryForceDefending.homeSettlement.name, evt.militaryForceDefending.militaryLevel), null, MenuOptionPriority.High));
                                        list.Add(new FloatMenuOption("ChangeDefendingForce".Translate(), delegate
                                        {
                                            List<FloatMenuOption> settlementList = new List<FloatMenuOption>();
                                            SettlementFC homeSettlement = Find.World.GetComponent<FactionFC>().returnSettlementByLocation(tile);

                                            settlementList.Add(new FloatMenuOption(TranslatorFormattedStringExtensions.Translate("ResetToHomeSettlement", homeSettlement.settlementMilitaryLevel), delegate
                                            {
                                                MilitaryUtilFC.changeDefendingMilitaryForce(evt, homeSettlement);
                                            }, MenuOptionPriority.High));

                                            foreach (SettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
                                            {
                                                if (settlement.isMilitaryValid() == true && settlement != homeSettlement)
                                                {
                                                    //if military is valid to use.
                                        
                                                    settlementList.Add(new FloatMenuOption(settlement.name + " " + "ShortMilitary".Translate() + " " + settlement.settlementMilitaryLevel + " - " + "FCAvailable".Translate() + ": " + (!settlement.isMilitaryBusySilent()).ToString(), delegate
                                                    {
                                                        if(settlement.isMilitaryBusy() == true)
                                                        {
                                                            //military is busy
                                                        } else
                                                        {

                                                            MilitaryUtilFC.changeDefendingMilitaryForce(evt, settlement);

                                                        }
                                                    }, MenuOptionPriority.Default, null, null, 0f, null, null
                                                    ));
                                                }       


                                            }

                                            if (settlementList.Count == 0)
                                            {
                                                settlementList.Add(new FloatMenuOption("NoValidMilitaries".Translate(), null));
                                            }

                                            FloatMenu floatMenu2 = new FloatMenu(settlementList);
                                            floatMenu2.vanishIfMouseDistant = true;
                                            Find.WindowStack.Add(floatMenu2);


                                            //set to raid settlement here
                                        }));

                                        FloatMenu floatMenu = new FloatMenu(list);
                                        floatMenu.vanishIfMouseDistant = true;
                                        Find.WindowStack.Add(floatMenu);
                                    }



                                }});
                            }


                        }
                    }
                }
            }
        }


        [HarmonyPatch(typeof(DeathActionWorker_Simple), "PawnDied")]
        class MercenaryAnimalDied
        {
            static bool Prefix(Corpse corpse)
            {
                if (corpse.InnerPawn != null && corpse.InnerPawn.Faction != null && Find.World.GetComponent<FactionFC>().militaryCustomizationUtil.AllMercenaryPawns.Contains(corpse.InnerPawn) == true)
                {
                    corpse.Destroy();
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(JobGiver_AnimalFlee), "TryGiveJob")]
        class TryGiveJobFleeAnimal
        {
            static bool Prefix(Pawn pawn)
            {
                if (Find.World.GetComponent<FactionFC>().militaryCustomizationUtil.AllMercenaryPawns.Contains(pawn))
                {
                    return false;
                } else
                {
                    return true;
                }
            }
        }

        //Goodwill by distance to settlement
        [HarmonyPatch(typeof(SettlementProximityGoodwillUtility), "AppendProximityGoodwillOffsets")]
        class GoodwillPatch
        {
            static void Postfix(int tile, List<Pair<Settlement, int>> outOffsets, bool ignoreIfAlreadyMinGoodwill, bool ignorePermanentlyHostile)
            {
                Pair:
                foreach (Pair<Settlement, int> pair in outOffsets)
                {
                    if(pair.First.Faction.def.defName == "PColony")
                    {
                        outOffsets.Remove(pair);
                        goto Pair;
                    }
                }
            }
        }

        //CheckNaturalTendencyToReachGoodwillThreshold()
        [HarmonyPatch(typeof(Faction), "CheckNaturalTendencyToReachGoodwillThreshold")]
        class GoodwillPatchFunctionsGoodwillTendency
        {
            static bool Prefix(ref Faction __instance)
            {
                if (__instance.def.defName == "PColony")
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        //tryAffectGoodwillWith
        [HarmonyPatch(typeof(Faction), "TryAffectGoodwillWith")]
        class GoodwillPatchFunctionsGoodwillAffect
        {
            static bool Prefix(ref Faction __instance, Faction other, int goodwillChange, bool canSendMessage = true, bool canSendHostilityLetter = true, string reason = null, GlobalTargetInfo? lookTarget = null)
            {
                if (__instance.def.defName == "PColony" && other == Find.FactionManager.OfPlayer)
                {
                    if (reason == "GoodwillChangedReason_RequestedTrader".Translate())
                    {
                        return false;
                    } else
                        if (reason == "GoodwillChangedReason_ReceivedGift".Translate())
                    {
                        return false;
                    }
                    return true;
                }
                else
                {
                    return true;
                }
            }
        }


        //Notify_MemberDied(Pawn member, DamageInfo? dinfo, bool wasWorldPawn, Map map)
        [HarmonyPatch(typeof(Faction), "Notify_MemberDied")]
        class GoodwillPatchFunctionsMemberDied
        {
            static bool Prefix(ref Faction __instance, Pawn member, DamageInfo? dinfo, bool wasWorldPawn, Map map)
            {
                if (member.Faction.def.defName == "PColony" && !wasWorldPawn && !PawnGenerator.IsBeingGenerated(member) && Current.ProgramState == ProgramState.Playing && map != null && map.IsPlayerHome && !__instance.HostileTo(Faction.OfPlayer))
                {
                    if (dinfo != null && dinfo.Value.Category == DamageInfo.SourceCategory.Collapse)
                    {
                        Messages.Message("DeathOfFactionPawn".Translate(), MessageTypeDefOf.PawnDeath);
                        foreach (SettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
                        {
                            settlement.unrest += 5 * traitUtilsFC.cycleTraits(new double(), "unrestGainedMultiplier", settlement.traits, "multiply") * traitUtilsFC.cycleTraits(new double(), "unrestGainedMultiplier", Find.World.GetComponent<FactionFC>().traits, "multiply");
                            settlement.happiness -= 3 * traitUtilsFC.cycleTraits(new double(), "happinessLostMultiplier", settlement.traits, "multiply") * traitUtilsFC.cycleTraits(new double(), "happinessLostMultiplier", Find.World.GetComponent<FactionFC>().traits, "multiply");
                        }
                            
                    }
                    else if (dinfo != null && (dinfo.Value.Instigator == null || dinfo.Value.Instigator.Faction == null))
                    {
                        Pawn pawn = dinfo.Value.Instigator as Pawn;
                        if (pawn == null || !pawn.RaceProps.Animal || pawn.mindState.mentalStateHandler.CurStateDef != MentalStateDefOf.ManhunterPermanent)
                        {
                            Messages.Message("DeathOfFactionPawn".Translate(), MessageTypeDefOf.PawnDeath);
                            foreach (SettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
                            {
                                settlement.unrest += 5 * traitUtilsFC.cycleTraits(new double(), "unrestGainedMultiplier", settlement.traits, "multiply") * traitUtilsFC.cycleTraits(new double(), "unrestGainedMultiplier", Find.World.GetComponent<FactionFC>().traits, "multiply");
                                settlement.happiness -= 3 * traitUtilsFC.cycleTraits(new double(), "happinessLostMultiplier", settlement.traits, "multiply") * traitUtilsFC.cycleTraits(new double(), "happinessLostMultiplier", Find.World.GetComponent<FactionFC>().traits, "multiply");
                            }
                        }
                    }

                    //return false to stop from continuing method
                    return false;
                }
               
                else
                {
                    return true;
                }
            }
        }


        //member exit map
        [HarmonyPatch(typeof(Faction), "Notify_MemberExitedMap")]
        class GoodwillPatchFunctionsExitedMap
        {
            static bool Prefix(ref Faction __instance, Pawn member, bool free)
            {
                if (__instance.def.defName == "PColony")
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        //member took damage
        [HarmonyPatch(typeof(Faction), "Notify_MemberTookDamage")]
        class GoodwillPatchFunctionsTookDamage
        {
            static bool Prefix(ref Faction __instance, Pawn member, DamageInfo dinfo)
            {
                if (__instance.def.defName == "PColony")
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        //Player traded
        [HarmonyPatch(typeof(Faction), "Notify_PlayerTraded")]
        class GoodwillPatchFunctionsPlayerTraded
        {
            static bool Prefix(ref Faction __instance, float marketValueSentByPlayer, Pawn playerNegotiator)
            {
                if (__instance.def.defName == "PColony")
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        //Player traded
        [HarmonyPatch(typeof(Faction), "Notify_MemberCaptured")]
        class GoodwillPatchFunctionsCapturedPawn
        {
            static bool Prefix(ref Faction __instance, Pawn member, Faction violator)
            {
                if (__instance.def.defName == "PColony" && violator == Find.FactionManager.OfPlayer)
                {
                    Messages.Message("CaptureOfFactionPawn".Translate(), MessageTypeDefOf.NegativeEvent);
                    foreach (SettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
                    {
                        settlement.unrest += 15 * traitUtilsFC.cycleTraits(new double(), "unrestGainedMultiplier", settlement.traits, "multiply") * traitUtilsFC.cycleTraits(new double(), "unrestGainedMultiplier", Find.World.GetComponent<FactionFC>().traits, "multiply");
                        settlement.happiness -= 10 * traitUtilsFC.cycleTraits(new double(), "happinessLostMultiplier", settlement.traits, "multiply") * traitUtilsFC.cycleTraits(new double(), "happinessLostMultiplier", Find.World.GetComponent<FactionFC>().traits, "multiply");

                        settlement.unrest = Math.Min(settlement.unrest, 100);
                        settlement.happiness = Math.Max(settlement.happiness, 0);
                    }

                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        //CallForAid
        //Remove ability to attack colony.




        public FactionFC(World world) : base(world)
        {
            var harmony = new Harmony("com.Saakra.Empire");

            harmony.PatchAll();

            power.isTithe = true;
            power.isTitheBool = true;
            research.isTithe = true;
            research.isTitheBool = true;

        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (firstTick == true)
            {
                //Log.Message("First Tick");
                FactionColonies.updateChanges();
                firstTick = false;
                Faction FCf = FactionColonies.getPlayerColonyFaction();
                if (FCf != null)
                    FCf.def.techLevel = TechLevel.Undefined;

            }
            FCEventMaker.ProcessEvents(in events);
            billUtility.processBills();

            TickMecernaries();

            



            //If Player Colony Faction does exists
            Faction faction = FactionColonies.getPlayerColonyFaction();
            if (faction != null)
            {
                TaxTick();
                UITick();
                StatTick();
                MilitaryTick();
            }

        }


        public void TickMecernaries()
        {
            mercenaryTick++;
            if (mercenaryTick > 120)
            {


                foreach (MercenarySquadFC squad in militaryCustomizationUtil.mercenarySquads)
                {
                    if (squad.isDeployed)
                    {
                        if (Find.WindowStack.IsOpen(typeof(EmpireUIMercenaryCommandMenu)) == false)
                        {
                            //Log.Message("Opening Window");
                            // menu.focusWhenOpened = false;
                            
                            Find.WindowStack.Add(new EmpireUIMercenaryCommandMenu());
                           
                        }
                        bool deployed = false;

                    
                        foreach (Mercenary merc in squad.DeployedMercenaries)
                        {

                                //If pawn is up and moving, not downed.
                                if (merc.pawn.Map != null && merc.pawn.health.State == PawnHealthState.Mobile)
                                {
                                    //set hitmap if not already
                                    if (!(squad.hitMap))
                                    {
                                        squad.hitMap = true;
                                    }


                                //Log.Message(merc.pawn.CurJob.ToString());

                                //If in combat
                                //Log.Message("Start - Fight");
                                    JobGiver_AIFightEnemies jobGiver = new JobGiver_AIFightEnemies();
                                    ThinkResult result = jobGiver.TryIssueJobPackage(merc.pawn, new JobIssueParams());
                                    bool isValid = result.IsValid;
                                    if (isValid)
                                    {
                                        //Log.Message("Success");
                                        if ((merc.pawn.jobs.curJob.def == JobDefOf.Goto || merc.pawn.jobs.curJob.def != result.Job.def) && merc.pawn.jobs.curJob.def.defName != "ReloadWeapon" && merc.pawn.jobs.curJob.def.defName != "ReloadTurret")
                                        {
                                            merc.pawn.jobs.StartJob(result.Job, JobCondition.Ongoing);
                                            //Log.Message(result.Job.ToString());
                                        }
                                    }
                                    else
                                    {
                                        //Log.Message("Fail");
                                        if (squad.timeDeployed + 30000 >= Find.TickManager.TicksGame)
                                        {
                                            if (squad.order == MilitaryOrders.Standby)
                                            {
                                                //Log.Message("Standby");
                                                merc.pawn.mindState.forcedGotoPosition = squad.orderLocation;
                                                JobGiver_ForcedGoto jobGiver_Standby = new JobGiver_ForcedGoto();
                                                ThinkResult resultStandby = jobGiver_Standby.TryIssueJobPackage(merc.pawn, new JobIssueParams());
                                                bool isValidStandby = resultStandby.IsValid;
                                                if (isValidStandby)
                                                {
                                                    //Log.Message("valid");
                                                    merc.pawn.jobs.StartJob(resultStandby.Job, JobCondition.InterruptForced);
                                                }
                                            }
                                            else
                                            if (squad.order == MilitaryOrders.Attack)
                                            {
                                                //Log.Message("Attack");
                                                //If time is up, leave, else go home
                                                JobGiver_AIGotoNearestHostile jobGiver_Move = new JobGiver_AIGotoNearestHostile();
                                                ThinkResult resultMove = jobGiver_Move.TryIssueJobPackage(merc.pawn, new JobIssueParams());
                                                bool isValidMove = resultMove.IsValid;
                                                //Log.Message(resultMove.ToString());
                                                if (isValidMove)
                                                {
                                                    merc.pawn.jobs.StartJob(resultMove.Job, JobCondition.InterruptForced);
                                                }
                                                else
                                                {

                                                }
                                            }
                                            else
                                            if (squad.order == MilitaryOrders.RecoverWounded)
                                            {
                                                JobGiver_RescueNearby jobGiver_Rescue = new JobGiver_RescueNearby();
                                                ThinkResult resultRescue = jobGiver_Rescue.TryIssueJobPackage(merc.pawn, new JobIssueParams());
                                                bool isValidRescue = resultRescue.IsValid;

                                                if (isValidRescue)
                                                {
                                                    merc.pawn.jobs.StartJob(resultRescue.Job, JobCondition.InterruptForced);
                                                }
                                            }
                                        }
                                    }



                                    //end of if pawn is mobile
                                }
                                else
                                {
                                    //if pawn is down,dead, or gone

                                    //Log.Message("Not Deployed");
                                    //not deployed
                                }


                            if (merc.pawn.health.Dead)
                            {

                                squad.removeDroppedEquipment();

                                //Log.Message("Passing to dead Pawns");
                                squad.PassPawnToDeadMercenaries(merc);

                                squad.hasDead = true;
                            }


                            if (merc.pawn.Map != null && !(merc.pawn.health.Dead))
                            {
                                deployed = true;
                            }
                        }

                        foreach (Mercenary animal in squad.DeployedMercenaryAnimals)
                        {
                            if (animal.pawn.Map != null && animal.pawn.health.State == PawnHealthState.Mobile)
                            {
                                animal.pawn.mindState.duty = new PawnDuty();
                                animal.pawn.mindState.duty.def = DutyDefOf.Defend;
                                animal.pawn.mindState.duty.attackDownedIfStarving = false;
                                //animal.pawn.mindState.duty.radius = 2;
                                animal.pawn.mindState.duty.focus = animal.handler.pawn;
                                //If master is not dead
                                JobGiver_AIFightEnemies jobGiver = new JobGiver_AIFightEnemies();
                                   ThinkResult result = jobGiver.TryIssueJobPackage(animal.pawn, new JobIssueParams());
                                    bool isValid = result.IsValid;
                                    if (isValid)
                                    {
                                        //Log.Message("att");
                                        if (animal.pawn.jobs.curJob.def != result.Job.def)
                                        {
                                        animal.pawn.jobs.StartJob(result.Job, JobCondition.InterruptForced);
                                        }
                                    }
                                    else
                                    {
                                        animal.pawn.mindState.duty.def = DutyDefOf.Defend;
                                        animal.pawn.mindState.duty.radius = 2;
                                        animal.pawn.mindState.duty.focus = animal.handler.pawn;
                                        //if defend master not valid, follow master
                                        JobGiver_AIFollowEscortee jobGiverFollow = new JobGiver_AIFollowEscortee();
                                        ThinkResult resultFollow = jobGiverFollow.TryIssueJobPackage(animal.pawn, new JobIssueParams());
                                        bool isValidFollow = resultFollow.IsValid;
                                        if (isValidFollow)
                                        {
                                            //Log.Message("foloor");
                                            if (animal.pawn.jobs.curJob.def != resultFollow.Job.def)
                                            {
                                                animal.pawn.jobs.StartJob(resultFollow.Job, JobCondition.Ongoing);
                                            }
                                        }
                                    }



                            }
                            if (animal.pawn.health.Dead || animal.pawn.health.Downed)
                            {
                                //Log.Message("Despawning dead");
                                //animal.pawn.DeSpawn();
                                
                            }

                            if (animal.pawn.Map != null && !(animal.pawn.health.Dead))
                            {
                                deployed = true;
                            }
                        }



                        if (!(deployed) && squad.hitMap)
                        {
                            squad.isDeployed = false;
                            squad.removeDroppedEquipment();
                            squad.getSettlement.cooldownMilitary();
                            //Log.Message("Reseting Squad");
                            militaryCustomizationUtil.checkMilitaryUtilForErrors();
                            squad.OutfitSquad(squad.outfit);
                            squad.hitMap = false;

                        }
                    } else
                    {

                        //If squads not deployed

                    }
                }

                mercenaryTick = 0;
            }if (militaryCustomizationUtil.fireSupport == null)
            {
                militaryCustomizationUtil.fireSupport = new List<MilitaryFireSupport>();
            }
            
            //Other functions
            ResetFireSupport:
            foreach (MilitaryFireSupport fireSupport in militaryCustomizationUtil.fireSupport)
            {
                if (fireSupport.ticksTillEnd <= Find.TickManager.TicksGame)
                {
                    militaryCustomizationUtil.fireSupport.Remove(fireSupport);
                    goto ResetFireSupport;

                } else
                {
                    //process firesupport
                    if (fireSupport.fireSupportType == "lightArtillery")
                    {
                        if ((fireSupport.timeRunning % 15) == 0 && fireSupport.timeRunning > fireSupport.startupTime) 
                        {
                            //Log.Message("Boom");
                            IntVec3 spawnCenter = (from x in GenRadial.RadialCellsAround(fireSupport.location, fireSupport.accuracy, true)
                                              where x.InBounds(fireSupport.map)
                                              select x).RandomElementByWeight((IntVec3 x) => new SimpleCurve { { new CurvePoint(0f, 1f), true }, { new CurvePoint(fireSupport.accuracy, 0.1f), true } }.Evaluate(x.DistanceTo(fireSupport.location)));

                            Map map = fireSupport.map;
                            float radius = (float)Rand.Range(3,6);
                            DamageDef damage = DamageDefOf.Bomb;
                            Thing instigator = new OrbitalStrike();
                            int damAmount = -1;
                            float armorPenetration = -1f;

                            GenExplosion.DoExplosion(spawnCenter, map, radius, damage, instigator, damAmount, armorPenetration);
                        }
                    }


                }
               // Log.Message("tick - " + fireSupport.timeRunning);
                fireSupport.timeRunning++;
            }




        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<string>(ref name, "name");
            Scribe_Values.Look<string>(ref title, "title");
            Scribe_Values.Look<int>(ref capitalLocation, "capitalLocation");
            Scribe_References.Look<Map>(ref taxMap, "taxMap");

            Scribe_Values.Look<double>(ref averageHappiness, "averageHappiness");
            Scribe_Values.Look<double>(ref averageLoyalty, "averageLoyalty");
            Scribe_Values.Look<double>(ref averageUnrest, "averageUnrest");
            Scribe_Values.Look<double>(ref averageProsperity, "averageProsperity");

            Scribe_Values.Look<double>(ref income, "income");
            Scribe_Values.Look<double>(ref upkeep, "upkeep");
            Scribe_Values.Look<double>(ref profit, "profit");

            Scribe_Values.Look<int>(ref eventTimeDue, "eventTimeDue");
            Scribe_Values.Look<int>(ref taxTimeDue, "taxTimeDue");
            Scribe_Values.Look<int>(ref timeStart, "timeStart", -1);
            Scribe_Values.Look<int>(ref uiTimeUpdate, "uiTimeUpdate");
            Scribe_Values.Look<int>(ref militaryTimeDue, "militaryTimeDue", -1);
            Scribe_Values.Look<int>(ref dailyTimer, "dailyTimer");
            Scribe_Values.Look<TechLevel>(ref techLevel, "techLevel");

            Scribe_Collections.Look<SettlementFC>(ref settlements, "settlements", LookMode.Deep);
            Scribe_Collections.Look<PolicyFCDef>(ref policies, "policies", LookMode.Def);
            Scribe_Collections.Look<FCEvent>(ref events, "events", LookMode.Deep);
            Scribe_Collections.Look<string>(ref settlementCaravansList, "settlementCaravansList", LookMode.Value);
            Scribe_Collections.Look<FCTraitEffectDef>(ref traits, "traits", LookMode.Def);
            Scribe_Collections.Look<int>(ref militaryTargets, "militaryTargets", LookMode.Value);

            //New Producitons types
            Scribe_Values.Look<float>(ref researchPointPool, "researchPointPool", 0);
            Scribe_Values.Look<float>(ref powerPool, "powerPool", 0);
            Scribe_References.Look<ThingWithComps>(ref powerOutput, "powerOutput");
            


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

            //Faction Def
            Scribe_Deep.Look<FactionFCDef>(ref factionDef, "factionDef");

            //Update
            Scribe_Values.Look<double>(ref updateVersion, "updateVersion");
            Scribe_Values.Look<int>(ref nextSettlementFCID, "nextSettlementFCID", 0);


            //Military Customization Util
            Scribe_Deep.Look<MilitaryCustomizationUtil>(ref militaryCustomizationUtil, "militaryCustomizationUtil");
            Scribe_Values.Look<int>(ref nextSquadID, "nextSquadID", 1);
            Scribe_Values.Look<int>(ref nextUnitID, "nextUnitID", 1);
            Scribe_Values.Look<int>(ref nextMercenaryID, "nextMercenaryID", 1);
            Scribe_Values.Look<int>(ref nextMercenarySquadID, "nextMercenarySquadID", 1);
            Scribe_Values.Look<int>(ref mercenaryTick, "mercenaryTick", -1);
            Scribe_Values.Look<int>(ref nextPrisonerID, "nextPrisonerID", 1);

            //New Tax Stuff
            Scribe_Values.Look<int>(ref nextTaxID, "nextTaxID", 1);
            Scribe_Values.Look<int>(ref nextBillID, "nextBillID", 1);
            Scribe_Values.Look<int>(ref nextEventID, "nextEventID", 1);

            
            Scribe_Collections.Look<BillFC>(ref Bills, "Bills", LookMode.Deep);
            Scribe_Collections.Look<BillFC>(ref OldBills, "OldBills", LookMode.Deep);
            Scribe_Values.Look<bool>(ref autoResolveBills, "autoResolveBills", false);

        }


        public int GetNextSettlementFCID()
        {
            this.nextSettlementFCID++;
            //Log.Message("Returning next settlement FC ID " + nextSettlementFCID);

            return nextSettlementFCID;
        }

        public int GetNextMercenaryID()
        {
            this.nextMercenaryID++;
            //Log.Message("Returning next mercenary ID " + nextMercenaryID);
            return nextMercenaryID;
        }

        public int GetNextUnitID()
        {
            this.nextUnitID++;
            //Log.Message("Returning next Unit ID " + nextUnitID);

            return nextUnitID;
        }

        public int GetNextSquadID()
        {
            this.nextSquadID++;
            //Log.Message("Returning next SquadID " + nextSquadID);

            return nextSquadID;
        }

        public int GetNextMercenarySquadID()
        {
            this.nextMercenarySquadID++;
            //Log.Message("Returning next MercenarySquadID " + nextMercenarySquadID);

            return nextMercenarySquadID;
        }

        public int GetNextTaxID()
        {
            this.nextTaxID++;
            return nextTaxID;
        }

        public int GetNextEventID()
        {
            this.nextEventID++;
            return nextEventID;
        }

        public int GetNextBillID()
        {
            this.nextBillID++;
            return nextBillID;
        }

        public int GetNextPrisonerID()
        {
            this.nextPrisonerID++;
            return nextPrisonerID;
        }

        public List<FCTraitEffectDef> returnListFactionTraits()
        {
            List<FCTraitEffectDef> tmpList = new List<FCTraitEffectDef>();
            foreach (FCTraitEffectDef trait in traits)
            {
                tmpList.Add(trait);
            }
            return tmpList;
        }


        public void setStartTime()
        {
            taxTimeDue = Find.TickManager.TicksGame + LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>().timeBetweenTaxes;
            dailyTimer = Find.TickManager.TicksGame + 2000;
        }

        //0 defname
        //1 desc
        //2 location
        //3 time till trigger


        public void enactPolicy(PolicyFCDef policy, int policySlot)
        {

            repealPolicy(policySlot);

            policies[policySlot] = policy;

            traits.AddRange(policy.traits); //add new traits

        }

       

        public void updateFaction()
        {
            if (Find.World.GetComponent<FactionFC>().factionDef != null)
            {

            }
            else
            {
                Find.World.GetComponent<FactionFC>().factionDef = new FactionFCDef();
            }

            //load factionfcvalues
            //FactionColonies.getPlayerColonyFaction().def.techLevel = factionDef.techLevel;
            //FactionColonies.getPlayerColonyFaction().def.apparelStuffFilter = factionDef.apparelStuffFilter;
        }
        public void updateTechLevel(ResearchManager researchManager)
        {




            if (DefDatabase<ResearchProjectDef>.GetNamed("ShipBasics", false) != null && researchManager.GetProgress(DefDatabase<ResearchProjectDef>.GetNamed("ShipBasics", false)) == DefDatabase<ResearchProjectDef>.GetNamed("ShipBasics", false).baseCost && techLevel < TechLevel.Ultra){
                techLevel = TechLevel.Ultra;
                factionDef.techLevel = TechLevel.Ultra;
                Log.Message("Ultra");
            }
            else if (DefDatabase<ResearchProjectDef>.GetNamed("Fabrication", false) != null && researchManager.GetProgress(DefDatabase<ResearchProjectDef>.GetNamed("Fabrication", false)) == DefDatabase<ResearchProjectDef>.GetNamed("Fabrication", false).baseCost && techLevel < TechLevel.Spacer)
            {
                techLevel = TechLevel.Spacer;
                factionDef.techLevel = TechLevel.Spacer;
                Log.Message("Spacer");
            }
            else if (DefDatabase<ResearchProjectDef>.GetNamed("Electricity", false) != null && researchManager.GetProgress(DefDatabase<ResearchProjectDef>.GetNamed("Electricity", false)) == DefDatabase<ResearchProjectDef>.GetNamed("Electricity", false).baseCost && techLevel < TechLevel.Industrial)
            {
                techLevel = TechLevel.Industrial;
                factionDef.techLevel = TechLevel.Industrial;
                Log.Message("Industrial");
            }
            else if (DefDatabase<ResearchProjectDef>.GetNamed("Smithing", false) != null && researchManager.GetProgress(DefDatabase<ResearchProjectDef>.GetNamed("Smithing", false)) == DefDatabase<ResearchProjectDef>.GetNamed("Smithing", false).baseCost && techLevel < TechLevel.Medieval)
            {
                techLevel = TechLevel.Medieval;
                factionDef.techLevel = TechLevel.Medieval;
                //Log.Message("Medieval");
                Log.Message("Medieval");
            }
            else
            {
                //Log.Message("Neolithic");
                if (techLevel < TechLevel.Neolithic)
                {
                    Log.Message("Neolithic");
                    techLevel = TechLevel.Neolithic;
                }
            }
            //update to player colony faction
            updateFaction();

            Faction playerColonyfaction = FactionColonies.getPlayerColonyFaction();
            if (playerColonyfaction.def.techLevel < techLevel)
            {
                Log.Message("Updating Tech Level");
                if (playerColonyfaction != null)
                {
                    updateFactionDef(techLevel, ref playerColonyfaction);
                }
            }
        }

        public void updateFactionDef(TechLevel tech, ref Faction faction)
        {
            FactionDef replacingDef;
            ThingFilter apparelStuffFilter = new ThingFilter();
            FactionDef def = faction.def;

            switch (tech)
            {
                case TechLevel.Archotech:
                case TechLevel.Ultra:
                case TechLevel.Spacer:
                    replacingDef = DefDatabase<FactionDef>.GetNamedSilentFail("OutlanderCivil");
      
                    break;
                case TechLevel.Industrial:
                    replacingDef = DefDatabase<FactionDef>.GetNamedSilentFail("OutlanderCivil");
                    break;
                case TechLevel.Medieval:
                    if (FactionColonies.checkForMod("OskarPotocki.VanillaFactionsExpanded.MedievalModule"))
                    {
                        replacingDef = DefDatabase<FactionDef>.GetNamedSilentFail("VFEM_KingdomCivil");
                    } else
                    {
                        replacingDef = DefDatabase<FactionDef>.GetNamedSilentFail("TribeCivil");
                    }
                    break;
                case TechLevel.Neolithic:
                case TechLevel.Animal:
                case TechLevel.Undefined:
                default:
                    replacingDef = DefDatabase<FactionDef>.GetNamedSilentFail("TribeCivil");
                    break;

            }
            //Log.Message("FactionFC.updateFactionDef - switch(tech) passed");

            //Log.Message("1");
            def.pawnGroupMakers = replacingDef.pawnGroupMakers;
            //Log.Message("2");
            def.caravanTraderKinds = replacingDef.caravanTraderKinds;
            //Log.Message("3");
            if (replacingDef.backstoryFilters != null && replacingDef.backstoryFilters.Count != 0)
                def.backstoryFilters = replacingDef.backstoryFilters;
            //Log.Message("4");
            def.techLevel = tech;
            //Log.Message("5");
            def.hairTags = replacingDef.hairTags;
            //Log.Message("6");
            def.visitorTraderKinds = replacingDef.visitorTraderKinds;
            //Log.Message("7");
            def.baseTraderKinds = replacingDef.baseTraderKinds;
            //Log.Message("8");
            if (replacingDef.apparelStuffFilter != null)
            def.apparelStuffFilter = replacingDef.apparelStuffFilter;


            if (tech >= TechLevel.Spacer && def.apparelStuffFilter != null)
            {
                def.apparelStuffFilter.SetAllow(DefDatabase<StuffCategoryDef>.GetNamedSilentFail("Synthread"), true);
                def.apparelStuffFilter.SetAllow(DefDatabase<StuffCategoryDef>.GetNamedSilentFail("Hyperweave"), true);
                def.apparelStuffFilter.SetAllow(DefDatabase<StuffCategoryDef>.GetNamedSilentFail("Plasteel"), true);
            }

            Log.Message("FactionFC.updateFactionDef - Completed tech update");

        }

        public bool validEnactPolicy(PolicyFCDef policy, int policySlot, bool message = true)
        {
            bool valid = true;
            foreach (PolicyFCDef slot in policies)  //check if already a building of that type constructed
            {
                if (slot == policy)
                {
                    valid = false;

                    if (message) { Messages.Message("PolicyAlreadyEnacted".Translate() + "!", MessageTypeDefOf.RejectInput); }

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
                if (event1.policy == policy && (event1.def.defName == "enactFactionPolicy" || event1.def.defName == "enactSettlementPolicy"))
                {
                    valid = false;
                    if (message) { Messages.Message("AlreadyPolicyTypeBeingEnacted" + "!", MessageTypeDefOf.RejectInput); }
                    break;
                }

                if (event1.policySlot == policySlot && (event1.def.defName == "enactFactionPolicy" || event1.def.defName == "enactSettlementPolicy")) //check if there is already a building being constructed in that slot
                {
                    valid = false;
                    if (message) { Messages.Message("AlreadyPolicyInSlot".Translate() + "!", MessageTypeDefOf.RejectInput); }

                    break;
                }
            }

            return valid;
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

        public void updateAverages()
        {
            int averageHappinessTmp = 0;
            int averageLoyaltyTmp = 0;
            int averageUnrestTmp = 0;
            int averageProsperityTmp = 0;

            if (settlements.Count() > 0)
            {

                foreach (SettlementFC settlement in settlements)
                {
                    averageHappinessTmp += Convert.ToInt32(settlement.happiness);
                    averageLoyaltyTmp += Convert.ToInt32(settlement.loyalty);
                    averageUnrestTmp += Convert.ToInt32(settlement.unrest);
                    averageProsperityTmp += Convert.ToInt32(settlement.prosperity);
                }
                averageHappinessTmp /= settlements.Count();
                averageLoyaltyTmp /= settlements.Count();
                averageUnrestTmp /= settlements.Count();
                averageProsperityTmp /= settlements.Count();

            }

            averageHappiness = averageHappinessTmp;
            averageLoyalty = averageLoyaltyTmp;
            averageUnrest = averageUnrestTmp;
            averageProsperity = averageProsperityTmp;

            if (settlements.Count() > 0)
            {
                FactionColonies.getPlayerColonyFaction().TryAffectGoodwillWith(Find.FactionManager.OfPlayer, (Convert.ToInt32(averageHappiness) - FactionColonies.getPlayerColonyFaction().PlayerGoodwill));
            }
        }

        public void setName(string name)
        {
            this.name = name;
        }

        public void addSettlement(SettlementFC settlement)
        {
            settlements.Add(settlement);
            uiUpdate();
        }

        public void uiUpdate()
        {
            //Pop UI updates
            updateTotalResources();
            updateTotalProfit();
            updateTechLevel(Find.ResearchManager);
        }

        public double getTotalIncome() //return total income of settlements       ####MAKE UPDATE PER HOUR TICK
        {
            double income = 0;
            for (int i = 0; i < settlements.Count(); i++)
            {
                income += settlements[i].getTotalIncome();
            }
            return income;
        }


        public double getTotalUpkeep() //returns total upkeep of all settlements
        {
            double upkeep = 0;
            for (int i = 0; i < settlements.Count(); i++)
            {
                upkeep += settlements[i].getTotalUpkeep();
            }
            return upkeep;
        }

        public double getTotalProfit() //returns total profit (income - upkeep) of all settlements
        {
            return (getTotalIncome() - getTotalUpkeep());
        }

        public void updateTotalProfit()
        {
            income = getTotalIncome();
            upkeep = getTotalUpkeep();
            profit = income - upkeep;
        }

        public void updateTotalResources()
        {
            for (int i = 0; i < returnNumberResource(); i++)
            {
                int resource = 0;

                for (int k = 0; k < settlements.Count(); k++)
                {
                    resource += (int)settlements[k].returnResourceByInt(i).endProduction;
                }
                returnResourceByInt(i).amount = resource;
                //Log.Message(i + " " + returnResourceByInt(i).amount);  //display total resources by type
            }
        }

        public void updateDailyResearch()
        {
            //Research adding
            if (Find.ResearchManager.currentProj == null && researchPointPool != 0)
            {
                Messages.Message(TranslatorFormattedStringExtensions.Translate("NoResearchExpended", Math.Round(researchPointPool)), MessageTypeDefOf.NeutralEvent);
            }
            else if (researchPointPool != 0 && Find.ResearchManager.currentProj != null)
            {
                //Log.Message(researchTotal.ToString());
                float neededPoints;
                neededPoints = (float)Math.Ceiling(Find.ResearchManager.currentProj.CostApparent - Find.ResearchManager.currentProj.ProgressReal);
                Log.Message(neededPoints.ToString());

                float expendedPoints;
                if (researchPointPool >= neededPoints)
                {
                    researchPointPool -= neededPoints;
                    expendedPoints = neededPoints;
                }
                else
                {
                    expendedPoints = researchPointPool;
                    researchPointPool = 0;
                }

                Find.LetterStack.ReceiveLetter("ResearchPointsExpended".Translate(), TranslatorFormattedStringExtensions.Translate("ResearchExpended", Math.Round(expendedPoints), Find.ResearchManager.currentProj.LabelCap, Math.Round(researchPointPool)), LetterDefOf.PositiveEvent);
                Find.ResearchManager.ResearchPerformed((float)Math.Ceiling((1 / 0.00825) * expendedPoints), null);

            }
        }



        public void addTax(bool isUpdating)
        {
            //if (capitalLocation == -1)
            //{
            //    setCapital();
            //}
            powerPool = 0;

            if (settlements.Count != 0) //if settlements is not zero
            {
                
                foreach(SettlementFC settlement in settlements)
                {
                    List<Thing> list = new List<Thing>();
                    settlement.updateProfitAndProduction();
                    list = settlement.createTithe();
                    float researchPool = settlement.createResearchPool();
                    float electricityAllotted = settlement.createPowerPool();

                    BillFC bill = new BillFC(settlement);  //Create new bill connected to settlement
                    bill.taxes.electricityAllotted = electricityAllotted;
                    bill.taxes.researchCompleted = researchPool;
                    bill.taxes.itemTithes.AddRange(list); //Add tithe to bill's tithes
                    bill.taxes.silverAmount = (float)(settlement.totalProfit) + settlement.returnSilverIncome(true);
                    Bills.Add(bill);

                    TaxTickPrisoner(settlement);
                }


                if (!isUpdating) //if done updating (timeskip) then send goods/silver etc
                {
                    //Messages.Message("TaxesBilled".Translate() + "!", MessageTypeDefOf.PositiveEvent);
                    Find.LetterStack.ReceiveLetter("Taxes Billed", "Taxes from your settlements have been billed", LetterDefOf.PositiveEvent);
                    uiUpdate();
                    
                    //Messages.Message(Find.TickManager.TicksGame.ToString(), MessageTypeDefOf.PositiveEvent);
                }

            }
            else
            {

                Messages.Message("NoSettlementsToTax".Translate(), MessageTypeDefOf.NeutralEvent);
            }


        }


        public void addEvent(FCEvent fcevent)
        {   //Add event to events
            events.Add(fcevent);

            //check if event has a location, if does, add traits to that specific location;
            if (fcevent.settlementTraitLocations.Count() > 0) //if has specific locations
            {
                foreach(SettlementFC location in fcevent.settlementTraitLocations)
                {
                    location.traits.AddRange(fcevent.def.traits);
                    foreach( FCTraitEffectDef trait in fcevent.def.traits)
                    {
                        //Log.Message(trait.label);
                    }
                    
                }
            } else 
            { //if no specific location then faction wide
                traits.AddRange(fcevent.traits);
            }
        }

        public bool checkSettlementCaravansList(string location) //list of destinations caravans gone to
        {
            for (int i = 0; i < settlementCaravansList.Count(); i++)
            {
                if(location == settlementCaravansList[i] || Find.WorldGrid.IsNeighbor(Convert.ToInt32(location), Convert.ToInt32(settlementCaravansList[i])))
                {
                    return true; // is on list
                }
            }
            return false; //is not on list
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


        public int returnNumberResource()
        {
            return 9;
        }

        public void setCapital(){
            if (Find.CurrentMap != null && Find.CurrentMap.IsPlayerHome == true && Find.CurrentMap.Parent is Settlement)
			{
                capitalLocation = Find.CurrentMap.Parent.Tile;
                Messages.Message(TranslatorFormattedStringExtensions.Translate("SetAsFactionCapital", Find.CurrentMap.Parent.LabelCap), MessageTypeDefOf.NeutralEvent);
            } else
            {
                Messages.Message("Unable to set faction capital on this map. Please go to your capital map and use the Set Capital button or else you may have some bugs soon.", MessageTypeDefOf.NegativeEvent);
            }
        }

        public int returnCapitalMapId()
        {
            for (int i = 0; i < Find.Maps.Count(); i++)
            {
                if (Find.Maps[i].Tile == capitalLocation)
                {
                    return i;
                }
            }
            Log.Message("CouldNotFindMapOfCapital".Translate());
            return -1;
        }

        public int returnSettlementFCIDByLocation(int location)
        {
            for (int i = 0; i < settlements.Count(); i++)
            {
                if(settlements[i].mapLocation == location)
                {
                    return i;
                }
            }
            return -1;
        }

        public SettlementFC returnSettlementByLocation(int location)
        {
            for (int i = 0; i < settlements.Count(); i++)
            {
                if (settlements[i].mapLocation == location)
                {
                    return settlements[i];
                }
            }
            return null;
        }

        public string getSettlementName(int location)
        {
            int i = returnSettlementFCIDByLocation(location);
            switch (i)
            {
                case -1:
                    return "Null";

                default:
                    return settlements[returnSettlementFCIDByLocation(location)].name;
            }
        }


        public void updateSettlementStats()
        {
            foreach (SettlementFC settlement in settlements)
            {
                settlement.updateHappiness();
                settlement.updateLoyalty();
                settlement.updateUnrest();
                settlement.updateProsperity();
            }
        }

        public void TaxTick()
        {
            if (Find.TickManager.TicksGame >= taxTimeDue) // taxTimeDue being used as set interval when skipping time
            {
                while (Find.TickManager.TicksGame >= taxTimeDue)   //while updating events
                {//update events in this order: regular events: tax events.


                    if (Find.TickManager.TicksGame > taxTimeDue)
                    {

                        addTax(true);
                    }
                    else
                    {
                        Log.Message("Empire Mod - TaxTick - Catching Up - Did you skip time? Report this if you did not");
                        addTax(false);
                        //NOT WHERE FINAL UPDATE IS. Go to addTax Function

                    }
                    taxTimeDue += LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>().timeBetweenTaxes;
                    //Log.Message(Find.TickManager.TicksGame + " vs " + taxTimeDue + " - Taxing");
                }

                //if Autoresolve bills on, attempt to autoresolve
                switch (autoResolveBills)
                {
                    case true:
                        PaymentUtil.autoresolveBills(Bills);
                        break;
                    case false:
                        break;
                }
            }
        }

        public void TaxTickPrisoner(SettlementFC settlement)
        {
            Reset:
            foreach (FCPrisoner prisoner in settlement.prisonerList)
            {
                switch (prisoner.workload)
                {
                    case FCWorkLoad.Heavy:
                        if (prisoner.AdjustHealth(-20))
                            goto Reset;
                        break;
                    case FCWorkLoad.Medium:
                        if(prisoner.AdjustHealth(-10))
                            goto Reset;
                        break;
                    case FCWorkLoad.Light:
                        if(prisoner.AdjustHealth(4))
                            goto Reset;
                        break;
                }
            }
        }



        public void StatTick()
        {
            if (Find.TickManager.TicksGame >= dailyTimer) // taxTimeDue being used as set interval when skipping time
            {
                while (Find.TickManager.TicksGame >= dailyTimer)   //while updating events
                {//update events in this order: regular events: tax events.

                   // Log.Message("Tick");
                    updateSettlementStats();
                    updateAverages();
                    RelationsUtilFC.resetPlayerColonyRelations();


                    updateDailyResearch();


                    //Random event creation
                    int tmpNum = Rand.Range(1, 100);
                    //Log.Message(tmpNum.ToString());
                    if (tmpNum <= FactionColonies.randomEventChance)
                    {
                        FCEvent tmpEvt = FCEventMaker.MakeRandomEvent(FCEventMaker.returnRandomEvent(), null);
                        //Log.Message(tmpEvt.def.label);
                        if (tmpEvt != null)
                        {
                            Find.World.GetComponent<FactionFC>().addEvent(tmpEvt);


                            //letter code
                            string settlementString = "";
                            foreach (SettlementFC loc in tmpEvt.settlementTraitLocations)
                            {
                                if (settlementString == "")
                                {
                                    settlementString = settlementString + loc.name;
                                }
                                else
                                {
                                    settlementString = settlementString + ", " + loc.name;
                                }
                            }
                            if (settlementString != "")
                            {
                                Find.LetterStack.ReceiveLetter("Random Event", tmpEvt.def.desc + "\n This event is affecting the following settlements: " + settlementString, LetterDefOf.NeutralEvent);
                            }
                            else
                            {
                                Find.LetterStack.ReceiveLetter("Random Event", tmpEvt.def.desc, LetterDefOf.NeutralEvent);
                            }
                        }
                    }


                    dailyTimer += GenDate.TicksPerDay;
                    //Log.Message(Find.TickManager.TicksGame + " vs " + taxTimeDue + " - Taxing");
                }
            }
        }

        public void MilitaryTick()
        {
            if (Find.TickManager.TicksGame >= militaryTimeDue)
            {
                if (LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>().disableHostileMilitaryActions == false & Find.TickManager.TicksGame > (timeStart + 900000))
                {
                    //if military actions not disabled or game has not passed through the first season
                    //Log.Message("Mil Action debug");




                //if settlements exist

                // get list of settlements

                //if not underattack, add to list

                //create weight list by settlement military level

                //choose random

                    if (settlements.Count() > 0)
                    {
                        //if settlements exist
                        List<SettlementFC> targets = new List<SettlementFC>();
                        foreach (SettlementFC settlement in settlements)
                        {
                            //create weight list of settlements
                            if (settlement.isUnderAttack == false)
                            {
                                //if not underattack, add to list
                                //get weightvalue of target
                                int weightValue;
                                switch (settlement.settlementMilitaryLevel)
                                {
                                    case 0:
                                    case 1:
                                        weightValue = 10;
                                        break;
                                    case 2:
                                    case 3:
                                        weightValue = 7;
                                        break;
                                    case 4:
                                    case 5:
                                        weightValue = 3;
                                        break;
                                    default:
                                        weightValue = 1;
                                        break;
                                }

                                for (int k = 0; k < weightValue; k++)
                                {
                                    targets.Add(settlement);
                                }

                            }
                        }

                        //List created, pick from list
                        Faction enemy = Find.FactionManager.RandomEnemyFaction();
                        MilitaryUtilFC.attackPlayerSettlement(militaryForce.createMilitaryForceFromFaction(enemy, true), targets.RandomElement(), enemy);
                    }




                } else
                {
                    //Log.Message("Empire - Debug - Military Actions Disabled. Not processing.");
                }
                militaryTimeDue = Find.TickManager.TicksGame + (60000 * LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>().minMaxDaysTillMilitaryAction.RandomInRange);
                //Log.Message(militaryTimeDue + " - " + Find.TickManager.TicksGame);
//Log.Message((militaryTimeDue - Find.TickManager.TicksGame) / 60000 + " days till next military action");
                //militaryTimeDue =
            }
        }


        public void UITick()
        {
            if (uiTimeUpdate <= 0) //update per time?
            {
                uiTimeUpdate = FactionColonies.updateUiTimer;

                //already built in ui update -.-
                Find.WindowStack.WindowsUpdate();

                //Pop UI updates
                uiUpdate();

            }
            else
            {
                uiTimeUpdate -= 1;
            }
        }



    }
}