using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld.Planet;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace FactionColonies
{

	public class FactionColonies : ModSettings
	{
		public FactionColonies()
		{
		}

		public static void updateChanges()
		{
			FactionFC factionFC = Find.World.GetComponent<FactionFC>();

			MilitaryCustomizationUtil util = factionFC.militaryCustomizationUtil;
			
			if (factionFC.updateVersion < 0.311)
			{
				factionFC.Bills = new List<BillFC>();
				factionFC.events = new List<FCEvent>();
				verifyTraits();
			}

			if (factionFC.updateVersion < 0.305)
			{
				foreach (MercenarySquadFC squad in util.mercenarySquads)
				{
					squad.animals = new List<Mercenary>();
				}

				util.deadPawns = new List<Mercenary>();
			}

			if (factionFC.updateVersion < 0.304)
			{
				factionFC.nextMercenaryID = 1;
				foreach (MercenarySquadFC squad in util.mercenarySquads)
				{
					squad.initiateSquad();
				}

				util.deadPawns = new List<Mercenary>();
			}


			if (factionFC.updateVersion < 0.302)
			{
				util.fireSupport = new List<MilitaryFireSupport>();

				foreach (SettlementFC settlement in factionFC.settlements)
				{
					settlement.artilleryTimer = 0;
				}
			}

			if (factionFC.updateVersion < 0.300)
			{
				factionFC.militaryCustomizationUtil = new MilitaryCustomizationUtil();

			}

			if (factionFC.updateVersion < 0.301)
			{
				foreach (MilUnitFC unit in util.units)
				{
					unit.pawnKind = PawnKindDefOf.Colonist;
				}

				foreach (MercenarySquadFC squad in util.mercenarySquads)
				{
					if (squad.outfit != null && squad.isDeployed == false)
					{
						squad.OutfitSquad(squad.outfit);
					} else
					{
						squad.UsedApparelList = new List<Apparel>();
					}
				}

			}
			
			//NEW PLACE FOR UPDATE VERSIONS

			if (factionFC.updateVersion < 0.312)
			{
				foreach (SettlementFC settlement in factionFC.settlements)
				{
					settlement.returnMilitary();
					factionFC.updateVersion = 0.312;
				}
			}

			if (factionFC.updateVersion < 0.314)
			{

				LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>().settlementMaxLevel = 10;

				foreach (SettlementFC settlement in factionFC.settlements)
				{
					settlement.power = new ResourceFC("power", "Power", 0);
					settlement.medicine = new ResourceFC("medicine", "Medicine", 0);
					settlement.research = new ResourceFC("research", "Research", 0);
					settlement.power.isTithe = true;
					settlement.power.isTitheBool = true;
					settlement.research.isTithe = true;
					settlement.research.isTitheBool = true;

					for (int i = 0; i < 4; i++)
					{
						settlement.buildings.Add(BuildingFCDefOf.Empty);
					}
				}

				factionFC.power = new ResourceFC("power", "Power", 0);
				factionFC.medicine = new ResourceFC("medicine", "Medicine", 0);
				factionFC.research = new ResourceFC("research", "Research", 0);
				factionFC.power.isTithe = true;
				factionFC.power.isTitheBool = true;
				factionFC.research.isTithe = true;
				factionFC.research.isTitheBool = true;
				factionFC.researchPointPool = 0;

				
			}

			if (factionFC.updateVersion < 0.323)
			{
				foreach (SettlementFC settlement in factionFC.settlements)
				{
					settlement.power.isTithe = true;
					settlement.power.isTitheBool = true;
					settlement.research.isTithe = true;
					settlement.research.isTitheBool = true;
				}
				factionFC.power.isTithe = true;
				factionFC.power.isTitheBool = true;
				factionFC.research.isTithe = true;
				factionFC.research.isTitheBool = true;

				factionFC.updateVersion = 0.323;
			}

			if (factionFC.updateVersion < 0.324)
			{
				factionFC.medicine.label = "Medicine";

				factionFC.updateVersion = 0.323;
			}


			if (factionFC.updateVersion < 0.328)
			{
				foreach (SettlementFC settlement in factionFC.settlements)
				{
					settlement.generatePrisonerTable();
				}
			}

			if (factionFC.updateVersion < 0.329)
			{
				foreach (SettlementFC settlement in factionFC.settlements)
				{
					//reset prisoner hp
					foreach (FCPrisoner prisoner in settlement.prisonerList)
					{
						prisoner.healthTracker = new Pawn_HealthTracker(prisoner.prisoner);
						prisoner.healthTracker = prisoner.prisoner.health;
						HealthUtility.HealNonPermanentInjuriesAndRestoreLegs(prisoner.prisoner);
					}
				}
			}


			//CHECK SAVE DATA



			bool broken = false;
			Log.Message("Empire - Test Settlements");
			foreach (WorldObject obj in Find.WorldObjects.AllWorldObjects)
			{
				if (obj.def.defName == "Settlement")
				{
					//Log.Message(obj.Faction.ToString());

					if (obj.Faction.def == null)
					{
						//Log.Message(obj.Label);
						Log.Message("Detected broken save");
						broken = true;
					}
				}
			}

			Log.Message("Empire - Test Settlement Save");
			if ((factionFC.name != "PlayerFaction".Translate() || factionFC.settlements.Count() > 0) && getPlayerColonyFaction() == null || broken == true)
			{
				Log.Message("Old save detected - Adjusting factions to fix possible issues. Note: You will see some until the next load.");
				Faction newFaction = createPlayerColonyFaction();
				foreach (WorldObject obj in Find.WorldObjects.AllWorldObjects)
				{
					if (obj.def.defName == "Settlement")
					{
						//Log.Message(obj.Faction.ToString());

						if (obj.Faction.ToString() == factionFC.name)
						{
							if (obj.Faction == null || obj.Faction.def == null)
							{
								//Log.Message(obj.Label);
								obj.SetFaction(newFaction);
								Log.Message("Reseting Faction of Settlement " + obj.LabelCap + ". If this is in error, please report it on the Empire Discord");

							}


						}
					}
				}
			}

			Log.Message("Empire - Test FactionDef Change");

			foreach (Faction faction in Find.FactionManager.AllFactions)
			{
				if (faction.Name == factionFC.name && faction.leader != null || faction.def.defName == "PColonySpacer" || faction.def.defName == "PColonyTribal" || faction.def.defName == "PColonyIndustrial")
				{
					//Log.Message("Found Faction");
					if (faction.def.defName != "PColony" || faction.def == null)
					{
						//Log.Message("Setting new factiondef");
						faction.def = DefDatabase<FactionDef>.GetNamed("PColony");
						factionFC.updateFaction();
					}
				}
			}
			Log.Message("Empire - Setting Null Variables");

			if (factionFC.militaryTargets == null)
			{
				Log.Message("Empire - militaryTargets was Null");
				factionFC.militaryTargets = new List<int>();
			}

			if (factionFC.factionDef == null)
			{
				Log.Message("Empire - factionDef was Null");
				factionFC.updateFaction();
			}


			if (factionFC.militaryTimeDue == -1)
			{
				Log.Message("Empire - militaryTimeDue was Null");
				factionFC.militaryTimeDue = Find.TickManager.TicksGame + 30000;
			}

			if (factionFC.timeStart == -1)
			{
				Log.Message("Empire - timeStart was Null");
				factionFC.timeStart = Find.TickManager.TicksGame - 600000;
			}

			if (factionFC.militaryCustomizationUtil == null)
			{
				Log.Message("Empire - militaryCustomizationUtil was Null");
				factionFC.militaryCustomizationUtil = new MilitaryCustomizationUtil();
			}

			Log.Message("Empire - Testing Settlements for null variables");
			foreach (SettlementFC settlement in factionFC.settlements)
			{
				if (settlement.loadID == -1)
				{
					settlement.loadID = factionFC.GetNextSettlementFCID();
					Log.Message("Settlement: " + settlement.name + " - Reseting load ID");
				}

				if (settlement.prisoners == null)
				{
					settlement.prisoners = new List<Pawn>();
				}

				foreach (SettlementFC settlement2 in factionFC.settlements)
				{
					if (settlement != settlement2)
					{
						//if not same
						if (settlement.loadID == settlement2.loadID)
						{
							Log.Message("Fixing LoadID of settlement");
							settlement2.loadID = factionFC.GetNextSettlementFCID(); ;
						}
					}
				}
			}

			Log.Message("Empire - Testing for traits with no tie");
			//check for dull traits
			verifyTraits();


			Log.Message("Empire - Testing for invalid capital map");
			//Check for an invalid capital map
			if (Find.WorldObjects.SettlementAt(factionFC.capitalLocation) == null)
			{
				Messages.Message("Please reset your capital location. If you continue to see this after reseting, please report it.", MessageTypeDefOf.NegativeEvent);
			}
			else
			{
				
				//if (Find.WorldObjects.SettlementAt(Find.World.GetComponent<FactionFC>().capitalLocation).Map != null)
				//{
				//	if (Find.CurrentMap.IsPlayerHome == true)
				//	{
						//	Find.World.GetComponent<FactionFC>().capitalLocation = Find.CurrentMap.Tile;
				//	}
				//}
			}

			if (factionFC.taxMap == null)
			{
				Messages.Message("Your tax map has not been set. Please set it using the faction menu.", MessageTypeDefOf.CautionInput);
			}

			Log.Message("Empire - Testing for update change");



			//Add update letter/checker here!!
			if (factionFC.updateVersion < 0.333)
			{
				string str;
				str = "A new update for Empire has been released!  v.0.333\n The following abbreviated changes have occurred:";
				str += "\n\n- Empire is now Open-Source. Visit the discord for more information on how you can contribute to development!";
				//str += "\n- View Prisoners per settlement in the settlement menu";
				//str += "\n- Sell prisoners and have the amount earned be returned in the next bill";
				//str += "\n- Send and retrieve prisoners back from the settlement";
				//str += "\n- Adjust the workload of prisoners";
				//str += "\n- Upon reaching 0 health, prisoners will die";
				//str += "\n- Fixed specific animals from the Witcher Mod, Alpha Animals, Genetic Rim animals from being animal tithes";

				//str += "\n- A bit more randomness added to enemy factions attacking. It is now possible for their attacking force to be 2 below or 2 above their standard attack force size. (This is before combat modifiers)";
				//str += "\n- Bug Fixes";
				//str += "\n";
				str += "\n\n Also, if you need help, go check out this video I made with this url: https://youtu.be/FrVFMjC2RJc";


				str += "\n\nWant to see the full patch notes? Join us on Discord! https://discord.gg/f3zFQqA";

				factionFC.updateVersion = 0.333;
				Find.LetterStack.ReceiveLetter("Empire Mod Update!", str, LetterDefOf.NewQuest);


			}

			
		}

		public static void testLogFunction()
		{
			Log.Message("Test Successful");
		}

		public static void verifyTraits()
		{
			//make new list for factionfc traits
			//loop through events and add traits
			//loop through
			List<FCTraitEffectDef> factionTraits = new List<FCTraitEffectDef>();

			foreach (FCEvent evt in Find.World.GetComponent<FactionFC>().events)
			{
				if (evt.settlementTraitLocations.Count() > 0)
				{
					//ignore
				} else
				{
					factionTraits.AddRange(evt.traits);
				}
			}

			foreach (PolicyFCDef policy in Find.World.GetComponent<FactionFC>().policies)
			{
				factionTraits.AddRange(policy.traits);
			}

			Find.World.GetComponent<FactionFC>().traits = factionTraits;

			//go through each settlement and make new list for each settlement
			//loop through each active event and add settlement traits
			//loop through buildings and add traits

			foreach (SettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
			{
				List<FCTraitEffectDef> settlementsTraits = new List<FCTraitEffectDef>();

				foreach (FCEvent evt in Find.World.GetComponent<FactionFC>().events)
				{
					if (evt.settlementTraitLocations.Count() > 0)
					{
						//ignore
						if (evt.settlementTraitLocations.Contains(settlement) == true)
						{
							settlementsTraits.AddRange(evt.traits);
						}
					}
					else
					{
						//factionTraits.AddRange(evt.traits);
					}
				}

				foreach (BuildingFCDef building in settlement.buildings)
				{
					settlementsTraits.AddRange(building.traits);
				}

				settlement.traits = settlementsTraits;
			}



		}

		public static bool checkForMod(string packageID)
		{
			foreach (ModContentPack mod in LoadedModManager.RunningModsListForReading)
			{
				//Log.Message(mod.PackageIdPlayerFacing);
				if (mod.PackageIdPlayerFacing == packageID)
				{
					return true;
				}
			}

			return false;
		}

		public static Type returnUnknownTypeFromName(string name)
		{
			foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
			{
				var type = a.GetType(name);
				if (type != null)
					return type;
			}
			return null;
		}

		public static double calculateMilitaryLevelPoints(int MilitaryLevel)
		{
			double points = 500; //starting points at mil level 0
			for (int i = 1; i <= MilitaryLevel; i++)
			{
				points += (500 * MilitaryLevel);
			}
			return points;
		}

		public static bool canCraftItem(ThingDef thing)
		{
			bool canCraft = true;
			if (thing.recipeMaker != null)
			{
				if (thing.recipeMaker.researchPrerequisites != null)
				{
					foreach (ResearchProjectDef research in thing.recipeMaker.researchPrerequisites)
					{
						if (!(Find.ResearchManager.GetProgress(research) >= research.baseCost))
						{
							//research is not good
							canCraft = false;
						}
					}
				}
				if (thing.recipeMaker.researchPrerequisite != null)
				{
					if (!(Find.ResearchManager.GetProgress(thing.recipeMaker.researchPrerequisite) >= thing.recipeMaker.researchPrerequisite.baseCost))
					{
						//research is not good
						canCraft = false;
					}
				}
			} else
			{
				if (Find.World.GetComponent<FactionFC>().techLevel < thing.techLevel)
				{
					canCraft = false;
				}
			}

			if (thing.thingSetMakerTags != null && thing.thingSetMakerTags.Contains("SingleUseWeapon"))
			{
				canCraft = false;
			}


			return canCraft;
		}
		public static Faction getPlayerColonyFaction()
		{
			return Find.FactionManager.FirstFactionOfDef(DefDatabase<FactionDef>.GetNamed("PColony"));
		}


		//<DevAdd>   Create new seperate function to create a faction
		public static Settlement createPlayerColonySettlement(int tile)
		{
			//Log.Message("boop");
			StringBuilder reason = new StringBuilder();
			if (!TileFinder.IsValidTileForNewSettlement(tile, reason))
			{
				//Log.Message("Invalid Tile");
				//Alert Error to User
				Messages.Message(reason.ToString(), MessageTypeDefOf.NegativeEvent);


				return null;
				//create alert with reason
				//AlertsReadout alert = new AlertsReadout()
			}

			//Log.Message("Colony is being created");
			Faction faction = getPlayerColonyFaction();


			//Log.Message(faction.Name);
			Settlement settlement = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
			settlement.SetFaction(faction);
			settlement.Tile = tile; TileFinder.RandomSettlementTileFor(faction, false, null);
			settlement.Name = SettlementNameGenerator.GenerateSettlementName(settlement, null);
			Find.WorldObjects.Add(settlement);

			//create settlement data for world object
			SettlementFC settlementfc = new SettlementFC(settlement.Name, settlement.Tile);
			settlementfc.power.isTithe = true;
			settlementfc.power.isTitheBool = true;
			settlementfc.research.isTithe = true;
			settlementfc.research.isTitheBool = true;


			Find.World.GetComponent<FactionFC>().addSettlement(settlementfc);
			Messages.Message("TheSettlement".Translate() + " " + settlement.Name + "HasBeenFormed".Translate() + "!", MessageTypeDefOf.PositiveEvent);

			//Example to grab settlement data from FC
			//Log.Message(settlementfc.ReturnFCSettlement().Name.ToString());



			return settlement;
		}

		[DebugAction("Empire", "Increment Time 5 Days", allowedGameStates = AllowedGameStates.Playing)]
		private static void incrementTimeFiveDays()
		{
			//Log.Message("Debug - Increment Time 5 Days");
			Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + 300000);
		}



		[DebugAction("Empire", "Make Random Event", allowedGameStates = AllowedGameStates.Playing)]
		private static void makeRandomEvent()
		{
			List<DebugMenuOption> list = new List<DebugMenuOption>();
			foreach (FCEventDef evtDef in DefDatabase<FCEventDef>.AllDefsListForReading)
			{
				if (evtDef.isRandomEvent == true)
					list.Add(new DebugMenuOption(evtDef.label, DebugMenuOptionMode.Action, delegate ()
					{
						FCEvent evt;

						Log.Message("Debug - Make Random Event - " + evtDef.label);
						evt = FCEventMaker.MakeRandomEvent(evtDef, null);
						if (evtDef.activateAtStart == false)
						{
							FCEventMaker.MakeRandomEvent(evtDef, null); Find.World.GetComponent<FactionFC>().addEvent(evt);
						}

						//letter code
						string settlementString = "";
						foreach (SettlementFC loc in evt.settlementTraitLocations)
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
							Find.LetterStack.ReceiveLetter("Random Event", evtDef.desc + "\n This event is affecting the following settlements: " + settlementString, LetterDefOf.NeutralEvent);
						}
						else
						{

						}
					}
	
					));
			}
			Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));




		}

		[DebugAction("Empire", "Proc MilitaryTimeDue", allowedGameStates = AllowedGameStates.Playing)]
		private static void procMilitaryTimeDue()
		{
			Log.Message("Debug - Proc MilitaryTimeDue");
			Find.World.GetComponent<FactionFC>().militaryTimeDue = Find.TickManager.TicksGame + 1;
		}

		[DebugAction("Empire", "Attack Player Settlement", allowedGameStates = AllowedGameStates.Playing)]
		private static void attackPlayerSettlement()
		{
			List<DebugMenuOption> list = new List<DebugMenuOption>();
			foreach (SettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
			{
				list.Add(new DebugMenuOption(settlement.name, DebugMenuOptionMode.Action, delegate ()
				{
					Log.Message("Debug - Attack Player Settlement - " + settlement.name);
					Faction enemyFaction = Find.FactionManager.RandomEnemyFaction();
					MilitaryUtilFC.attackPlayerSettlement(militaryForce.createMilitaryForceFromFaction(enemyFaction, true), settlement, enemyFaction);
				}
				));
			}
			Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
		}



		[DebugAction("Empire", "Change Settlement Defending Force", allowedGameStates = AllowedGameStates.Playing)]
		private static void ChangeAttackPlayerSettlementMilitaryForce()
		{

			List<DebugMenuOption> list = new List<DebugMenuOption>();
			foreach (FCEvent evt in Find.World.GetComponent<FactionFC>().events)
			{


				if (evt.def == FCEventDefOf.settlementBeingAttacked)
				{

					list.Add(new DebugMenuOption(Find.World.GetComponent<FactionFC>().returnSettlementByLocation(evt.location).name, DebugMenuOptionMode.Action, delegate ()
					{
						//when event is selected, select defending force to replace it with

						List<DebugMenuOption> list2 = new List<DebugMenuOption>();
						foreach (SettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
						{
							if (settlement.isMilitaryValid() == true && settlement.name != evt.settlementFCDefending.name)
							{

								list2.Add(new DebugMenuOption(settlement.name + " - " + settlement.settlementMilitaryLevel + " - Busy: " + settlement.isMilitaryBusySilent(), DebugMenuOptionMode.Action, delegate ()
								{
									if (settlement.isMilitaryBusy() == false)
									{
										Log.Message("Debug - Change Player Settlement - " + evt.militaryForceDefending.homeSettlement.name + " to " + settlement.name);
										MilitaryUtilFC.changeDefendingMilitaryForce(evt, settlement);
									}
								}
								));
							}
						}
						Find.WindowStack.Add(new Dialog_DebugOptionListLister(list2));

					}
					));
					Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
				}
			}

		}

		[DebugAction("Empire", "Upgrade Player Settlement", allowedGameStates = AllowedGameStates.Playing)]
		private static void UpgradePlayerSettlement()
		{
			List<DebugMenuOption> list = new List<DebugMenuOption>();
			foreach (SettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
			{
				list.Add(new DebugMenuOption(settlement.name, DebugMenuOptionMode.Action, delegate ()
				{
					Log.Message("Debug - Upgrade Player Settlement - " + settlement.name);
					settlement.upgradeSettlement();
				}
				));
			}
			Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
		}

		[DebugAction("Empire", "Upgrade Player Settlement x5", allowedGameStates = AllowedGameStates.Playing)]
		private static void UpgradePlayerSettlementx5()
		{
			List<DebugMenuOption> list = new List<DebugMenuOption>();
			foreach (SettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
			{
				list.Add(new DebugMenuOption(settlement.name, DebugMenuOptionMode.Action, delegate ()
				{
					Log.Message("Debug - Upgrade Player Settlement x5- " + settlement.name);
					settlement.upgradeSettlement();
					settlement.upgradeSettlement();
					settlement.upgradeSettlement();
					settlement.upgradeSettlement();
					settlement.upgradeSettlement();
				}
				));
			}
			Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
		}


		[DebugAction("Empire", "Test Function", allowedGameStates = AllowedGameStates.Playing)]
		private static void testVariable()
		{
			Log.Message("Debug - Test Function - ");
			for (int i = 0; i < 100; i++)
			{
				//Log.Message(FactionColonies.randomAttackModifier().ToString());
			}
		}

		[DebugAction("Empire", "De-Level Player Settlement", allowedGameStates = AllowedGameStates.Playing)]
		private static void DelevelPlayerSettlement()
		{
			List<DebugMenuOption> list = new List<DebugMenuOption>();
			foreach (SettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
			{
				list.Add(new DebugMenuOption(settlement.name, DebugMenuOptionMode.Action, delegate ()
				{
					Log.Message("Debug - Delevel Player Settlement - " + settlement.name);
					settlement.delevelSettlement();
				}
				));
			}
			Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
		}

		[DebugAction("Empire", "Reset Military Squads", allowedGameStates = AllowedGameStates.Playing)]
		private static void ResetMilitarySquads()
		{
			Find.World.GetComponent<FactionFC>().militaryCustomizationUtil.mercenarySquads = new List<MercenarySquadFC>();

			foreach (SettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
			{
				settlement.returnMilitary();
			}
		}

		[DebugAction("Empire", "Clear Old Bills", allowedGameStates = AllowedGameStates.Playing)]
		private static void clearOldBills()
		{
			Find.World.GetComponent<FactionFC>().OldBills = new List<BillFC>();
		}

		[DebugAction("Empire", "Place 500 Silver", allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void placeSilverFC()
		{


			DebugTool tool = null;
			IntVec3 DropPosition;
			Map map;
			tool = new DebugTool("Select Drop Position", delegate ()
			{
				DropPosition = UI.MouseCell();
				map = Find.CurrentMap;


				Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
				silver.stackCount = 500;
				GenPlace.TryPlaceThing(silver, DropPosition, map, ThingPlaceMode.Near);
			});
			DebugTools.curTool = tool;
		}


		public static void CallinAlliedForces(SettlementFC settlement)
		{
			IncidentParms parms = new IncidentParms();
			parms.target = Find.CurrentMap;
			parms.faction = FactionColonies.getPlayerColonyFaction();
			parms.podOpenDelay = 140;
			parms.points = 999;
			parms.raidArrivalModeForQuickMilitaryAid = true;
			parms.raidNeverFleeIndividual = true;
			parms.raidForceOneIncap = true;
			parms.raidArrivalMode = PawnsArrivalModeDefOf.CenterDrop;
			parms.raidStrategy = RaidStrategyDefOf.ImmediateAttackFriendly;
			parms.raidArrivalModeForQuickMilitaryAid = true;

			settlement.militarySquad.updateSquadStats(settlement.settlementMilitaryLevel);


			DebugTool tool = null;
			IntVec3 DropPosition;
			tool = new DebugTool("Select Drop Position", delegate ()
			{
				DropPosition = UI.MouseCell();
				parms.spawnCenter = DropPosition;

				//List<Pawn> list2 = parms.raidStrategy.Worker.SpawnThreats(parms);
				//parms.raidArrivalMode.Worker.Arrive(list2, parms);


				PawnsArrivalModeWorkerUtility.DropInDropPodsNearSpawnCenter(parms, settlement.militarySquad.AllEquippedMercenaryPawns);
				settlement.militarySquad.isDeployed = true;
				settlement.militarySquad.order = MilitaryOrders.Standby;
				settlement.militarySquad.orderLocation = DropPosition;
				settlement.militarySquad.timeDeployed = Find.TickManager.TicksGame;

				DebugTools.curTool = null;
				settlement.sendMilitary(Find.CurrentMap.Index, "Deploy", 1, null);
			});
			DebugTools.curTool = tool;

			//UI.UIToMapPosition(UI.MousePositionOnUI).ToIntVec3();
		}

		public static void LightArtilleryStrike(SettlementFC settlement)
		{
			DebugTool tool = null;
			IntVec3 DropPosition;
			tool = new DebugTool("Select Artillery Position", delegate ()
			{
				if (PaymentUtil.getSilver() > 3000)
				{
					PaymentUtil.paySilver(3000);
					DropPosition = UI.MouseCell();
					IntVec3 spawnCenter = DropPosition;
					Map map = Find.CurrentMap;
					MilitaryFireSupport fireSupport = new MilitaryFireSupport("lightArtillery", map, spawnCenter, 600, 600, 20);
					Find.World.GetComponent<FactionFC>().militaryCustomizationUtil.fireSupport.Add(fireSupport);

					Messages.Message("An artillery strike will be occuring shortly on the marked position!", MessageTypeDefOf.ThreatSmall);
					settlement.artilleryTimer = Find.TickManager.TicksGame + 60000;

				} else
				{
					Messages.Message("You do not have enough silver to pay for the strike!", MessageTypeDefOf.RejectInput);
				}


				DebugTools.curTool = null;
			}, delegate
			{
				GenDraw.DrawCircleOutline(UI.MouseCell().ToVector3(), 20);
				GenDraw.DrawCircleOutline(UI.MouseCell().ToVector3(), 26);
			});
			DebugTools.curTool = tool;
		}


		[DebugAction("Empire", "Call In Artillery", allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void ArtilleryStrike()
		{
			DebugTool tool = null;
			IntVec3 DropPosition;
			tool = new DebugTool("Select Artillery Position", delegate ()
			{
				DropPosition = UI.MouseCell();
				IntVec3 spawnCenter = DropPosition;
				Map map = Find.CurrentMap;
				MilitaryFireSupport fireSupport = new MilitaryFireSupport("lightArtillery", map, spawnCenter, 600, 600, 20);
				Find.World.GetComponent<FactionFC>().militaryCustomizationUtil.fireSupport.Add(fireSupport);

				Messages.Message("An artillery strike will be occuring shortly on the marked position!", MessageTypeDefOf.ThreatSmall);




				DebugTools.curTool = null;
			}, onGUIAction: delegate
			{
				//GenUI.RenderMouseoverBracket();
				//GenDraw.DrawRadiusRing(UI.MouseCell(), 26, Color.yellow, null);
				//GenDraw.DrawRadiusRing(UI.MouseCell(), 20, Color.red, null);
				GenDraw.DrawRadiusRing(UI.MouseCell(), 26, Color.yellow, null);
				GenDraw.DrawRadiusRing(UI.MouseCell(), 20, Color.red, null);
			});
			DebugTools.curTool = tool;
		}

		public static void TurretDrop()
		{

		}

		private static void CallInAlliedForcesSelect()
		{
			List<FloatMenuOption> list = new List<FloatMenuOption>();
			foreach (SettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
			{
				if (settlement.militarySquad != null)
				{
					list.Add(new FloatMenuOption(settlement.name, delegate ()
					{

						IncidentParms parms = new IncidentParms();
						parms.target = Find.CurrentMap;
						parms.faction = FactionColonies.getPlayerColonyFaction();
						parms.podOpenDelay = 140;
						parms.points = 999;
						parms.raidArrivalModeForQuickMilitaryAid = true;
						parms.raidNeverFleeIndividual = true;
						parms.raidForceOneIncap = true;
						parms.raidArrivalMode = PawnsArrivalModeDefOf.CenterDrop;
						parms.raidStrategy = RaidStrategyDefOf.ImmediateAttackFriendly;
						parms.raidArrivalModeForQuickMilitaryAid = true;

						settlement.militarySquad.updateSquadStats(settlement.settlementMilitaryLevel);


						DebugTool tool = null;
						IntVec3 DropPosition;
						tool = new DebugTool("Select Drop Position", delegate ()
						{
							DropPosition = UI.MouseCell();
							parms.spawnCenter = DropPosition;

							//List<Pawn> list2 = parms.raidStrategy.Worker.SpawnThreats(parms);
							//parms.raidArrivalMode.Worker.Arrive(list2, parms);
							settlement.militarySquad.isDeployed = true;
							settlement.militarySquad.order = MilitaryOrders.Standby;
							settlement.militarySquad.orderLocation = DropPosition;
							settlement.militarySquad.timeDeployed = Find.TickManager.TicksGame;



							PawnsArrivalModeWorkerUtility.DropInDropPodsNearSpawnCenter(parms, settlement.militarySquad.AllEquippedMercenaryPawns);
							settlement.militarySquad.isDeployed = true;
							DebugTools.curTool = null;
						});
						DebugTools.curTool = tool;

						//UI.UIToMapPosition(UI.MousePositionOnUI).ToIntVec3();

					}
					));
				}
			}
			Find.WindowStack.Add(new FloatMenu(list));
		}


		[DebugAction("Empire", "Call In Allied Forces", allowedGameStates = AllowedGameStates.PlayingOnMap)]
		private static void CallInAlliedForcesDebug()
		{
			CallInAlliedForcesSelect();
		}

		public static void removePlayerSettlement(SettlementFC settlement)
		{
			FactionFC faction = Find.World.GetComponent<FactionFC>();
			faction.settlements.Remove(settlement);
			Messages.Message(TranslatorFormattedStringExtensions.Translate("SettlementRemoved", settlement.name), MessageTypeDefOf.NegativeEvent);
			Find.WorldObjects.Remove(Find.World.worldObjects.SettlementAt(settlement.mapLocation));

			//clear military events
			settlement.returnMilitary();
			Reset:
			foreach (FCEvent evt in faction.events)
			{
				//military event removal
				if (evt.def == FCEventDefOf.captureEnemySettlement || evt.def == FCEventDefOf.raidEnemySettlement)
				{
					if(evt.militaryForceAttacking.homeSettlement == settlement)
					{
						faction.events.Remove(evt);
						goto Reset;
					}
				}
				if (evt.def == FCEventDefOf.settlementBeingAttacked)
				{
					if (evt.militaryForceDefending.homeSettlement == settlement)
					{
						if (evt.settlementFCDefending == settlement)
						{
							faction.events.Remove(evt);
							goto Reset;
						} else
						{
							//if not defending settlement
							MilitaryUtilFC.changeDefendingMilitaryForce(evt, evt.settlementFCDefending);
						}
					} else
					{
						//if force belongs to other settlement
						evt.militaryForceDefending.homeSettlement.cooldownMilitary();

						faction.events.Remove(evt);
						goto Reset;
					}
				}


				//settlement event removal
				if (evt.def == FCEventDefOf.constructBuilding || evt.def == FCEventDefOf.enactSettlementPolicy || evt.def == FCEventDefOf.upgradeSettlement || evt.def == FCEventDefOf.cooldownMilitary)
				{
					if (evt.source == settlement.mapLocation)
					{
						faction.events.Remove(evt);
						goto Reset;
					}
				}

				if (evt.def.isRandomEvent == true && evt.settlementTraitLocations.Count() > 0)
				{
					if (evt.settlementTraitLocations.Contains(settlement))
					{
						evt.settlementTraitLocations.Remove(settlement);
						if (evt.settlementTraitLocations.Count() == 0)
						{
							faction.events.Remove(evt);
							goto Reset;
						}
					}
				}
			}
		}

		public static int CompareFloatMenuOption(FloatMenuOption x, FloatMenuOption y)
		{
			return String.Compare(x.Label, y.Label);
		}

		public static int CompareBuildingDef(BuildingFCDef x, BuildingFCDef y)
		{
			return string.Compare(x.label, y.label);
		}

		public static int ReturnTicksToArrive(int currentTile, int destinationTile)
		{
			int ticksToArrive = -1;

			if (currentTile == -1 || destinationTile == -1)
			{
				if (Find.ResearchManager.GetProgress(DefDatabase<ResearchProjectDef>.GetNamed("TransportPod", false)) == DefDatabase<ResearchProjectDef>.GetNamed("TransportPod", false).baseCost)
				{
					//if have research pod tech
					return 30000;
				}

				return 600000;
			}

			using (WorldPath tempPath = Find.WorldPathFinder.FindPath(currentTile, destinationTile, null, null))
			{
				if (tempPath == WorldPath.NotFound)
				{
					ticksToArrive = 600000;
				}
				else
				{
					ticksToArrive = CaravanArrivalTimeEstimator.EstimatedTicksToArrive(currentTile, destinationTile, tempPath, 0f, CaravanTicksPerMoveUtility.GetTicksPerMove(null, null), Find.TickManager.TicksAbs);
				}
			}


			if (Find.ResearchManager.GetProgress(DefDatabase<ResearchProjectDef>.GetNamed("TransportPod", false)) == DefDatabase<ResearchProjectDef>.GetNamed("TransportPod", false).baseCost && ticksToArrive > 30000)
			{
				//if have research pod tech
				return 30000;
			}
			return ticksToArrive;
		}

		public static void sendPrisoner(Pawn prisoner, SettlementFC settlement)
		{
			settlement.addPrisoner(prisoner);
			prisoner.DeSpawn();
		}
		public static Faction createPlayerColonyFaction()
		{
			//Log.Message("Creating new faction");
			//Set start time for world component to start tracking your faction;
			Find.World.GetComponent<FactionFC>().setStartTime();
			Find.World.GetComponent<FactionFC>().setCapital();
			Find.World.GetComponent<FactionFC>().timeStart = Find.TickManager.TicksGame;

			//Log.Message("Faction is being created");
			FactionDef facDef = new FactionDef();


			facDef = DefDatabase<FactionDef>.GetNamed("PColony");

			Faction faction = new Faction();
			faction.def = facDef;
			faction.def.techLevel = TechLevel.Undefined;
			faction.loadID = Find.UniqueIDsManager.GetNextFactionID();
			faction.colorFromSpectrum = FactionGenerator.NewRandomColorFromSpectrum(faction);
			faction.Name = "PlayerColony".Translate();
			faction.centralMelanin = Rand.Value;
			//<DevAdd> Copy player faction relationships  
			foreach (Faction other in Find.FactionManager.AllFactionsListForReading)
			{
				faction.TryMakeInitialRelationsWith(other);
			}

			faction.GenerateNewLeader();
			Find.FactionManager.Add(faction);

			return faction;
		}

		public static void changePlayerColonyFaction(Faction faction)
		{
			faction = createPlayerColonyFaction();
			Log.Message("Faction was updated - " + faction.Name);
		}


		private static List<float> getAttackPoints()
		{
			List<float> list = new List<float>();
			for (int i = -Convert.ToInt32(plusOrMinusRandomAttackValue * 10); i < plusOrMinusRandomAttackValue * 10; i++)
			{
				list.Add((i / 10));
			}
			return list;

		}

		public static float randomAttackModifier()
		{
			float y = (from x in getAttackPoints()
					   select x).RandomElementByWeight((float x) => new SimpleCurve { { new CurvePoint(0f, 1f), true }, { new CurvePoint(plusOrMinusRandomAttackValue, .1f), true } }.Evaluate(Math.Abs(x) - 2));
			return y;

		}


		public static string FloorStat(double stat)
		{
			return Convert.ToString(Math.Floor((stat * 100)) / 100);
		}






		public int silverPerResource = 100;
		public static double silverToCreateSettlement = 1000;
		public int timeBetweenTaxes = GenDate.TicksPerTwelfth;
		public static int updateUiTimer = 150;
		public int productionTitheMod = 25;
		public static int productionResearchBase = 100;
		public static int storeReportCount = 4;
		public int workerCost = 100;

		public static double unrestBaseGain = 0;
		public static double unrestBaseLost = 1;

		public static double loyaltyBaseGain = 1;
		public static double loyaltyBaseLost = 0;

		public static double happinessBaseGain = 1;
		public static double happinessBaseLost = 0;

		public static double prosperityBaseRecovery = 1;

		public double settlementBaseUpgradeCost = 1000;
		public int settlementMaxLevel = 10;

		public static int randomEventChance = 25;

		public bool disableHostileMilitaryActions = false;

		public int minDaysTillMilitaryAction = 4;
		public int maxDaysTillMilitaryAction = 10;
		public IntRange minMaxDaysTillMilitaryAction = new IntRange(4, 10);

		private static float plusOrMinusRandomAttackValue = 2;
		public static double militaryAnimalCostMultiplier = 1.5;
		public static double militaryRaceCostMultiplier = .15;




		public override void ExposeData()
		{

			base.ExposeData();
			Scribe_Values.Look(ref silverPerResource, "silverPerResource");
			Scribe_Values.Look(ref timeBetweenTaxes, "timeBetweenTaxes");
			Scribe_Values.Look(ref productionTitheMod, "productionTitheMod");
			Scribe_Values.Look(ref workerCost, "workerCost");
			Scribe_Values.Look(ref settlementMaxLevel, "settlementMaxLevel");

			Scribe_Values.Look(ref disableHostileMilitaryActions, "disableHostileMilitaryActions");
			Scribe_Values.Look(ref minDaysTillMilitaryAction, "minDaysTillMilitaryAction");
			Scribe_Values.Look(ref maxDaysTillMilitaryAction, "maxDaysTillMilitaryAction");
			//Log.Message("load");
			//Log.Message(silverPerResource.ToString());

		}


	}

	public class FactionColoniesMod : Mod
	{
		public FactionColonies settings = new FactionColonies();

		public FactionColoniesMod(ModContentPack content) : base(content)
		{
			settings = GetSettings<FactionColonies>();
		}

		string silverPerResource;
		string timeBetweenTaxes;
		string productionTitheMod;
		string workerCost;
		string settlementMaxLevel;
		int daysBetweenTaxes;
		bool disableHostileMilitaryActions;
		int minDaysTillMilitaryAction;
		int maxDaysTillMilitaryAction;
		IntRange minMaxDaysTillMilitaryAction;


		public override void DoSettingsWindowContents(Rect inRect)
		{
			silverPerResource = settings.silverPerResource.ToString();
			timeBetweenTaxes = (settings.timeBetweenTaxes / 60000).ToString();
			productionTitheMod = settings.productionTitheMod.ToString();
			workerCost = settings.workerCost.ToString();
			settlementMaxLevel = settings.settlementMaxLevel.ToString();
			daysBetweenTaxes = (settings.timeBetweenTaxes / 60000);
			disableHostileMilitaryActions = settings.disableHostileMilitaryActions;
			minDaysTillMilitaryAction = settings.minDaysTillMilitaryAction;
			maxDaysTillMilitaryAction = settings.maxDaysTillMilitaryAction;
			minMaxDaysTillMilitaryAction = new IntRange(minDaysTillMilitaryAction, maxDaysTillMilitaryAction);

			settings.minMaxDaysTillMilitaryAction = new IntRange(minDaysTillMilitaryAction,maxDaysTillMilitaryAction);

			Listing_Standard listingStandard = new Listing_Standard();
			listingStandard.Begin(inRect);
			listingStandard.Label("Silver amount gained per resource");
			listingStandard.IntEntry(ref settings.silverPerResource, ref silverPerResource);
			listingStandard.Label("Days between tax time");
			listingStandard.IntEntry(ref daysBetweenTaxes, ref timeBetweenTaxes, 1);
			settings.timeBetweenTaxes = daysBetweenTaxes * 60000;
			listingStandard.Label("Production Tithe Modifier");
			listingStandard.IntEntry(ref settings.productionTitheMod, ref productionTitheMod);
			listingStandard.Label("Cost Per Worker");
			listingStandard.IntEntry(ref settings.workerCost, ref workerCost);
			listingStandard.Label("Max Settlement Level");
			listingStandard.IntEntry(ref settings.settlementMaxLevel, ref settlementMaxLevel);
			listingStandard.CheckboxLabeled("Disable Hostile Military Actions", ref settings.disableHostileMilitaryActions);
			listingStandard.Label("Min/Max Days Until Military Action (ex. Settlements being attacked)");
			listingStandard.IntRange(ref minMaxDaysTillMilitaryAction, 1, 20);
			settings.minDaysTillMilitaryAction = minMaxDaysTillMilitaryAction.min;
			settings.maxDaysTillMilitaryAction = minMaxDaysTillMilitaryAction.max;

			if(listingStandard.ButtonText("Reset Settings"))
			{
				FactionColonies blank = new FactionColonies();
				settings.silverPerResource = blank.silverPerResource;
				settings.timeBetweenTaxes = blank.timeBetweenTaxes;
				settings.productionTitheMod = blank.productionTitheMod;
				settings.workerCost = blank.workerCost;
				settings.settlementMaxLevel = blank.settlementMaxLevel;
				settings.minMaxDaysTillMilitaryAction = blank.minMaxDaysTillMilitaryAction;
			}

			listingStandard.End();
			base.DoSettingsWindowContents(inRect);
		}

		public override string SettingsCategory()
		{
			return "Empire";
		}

		public override void WriteSettings()
		{
			LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>().timeBetweenTaxes = daysBetweenTaxes * 60000;
			LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>().minDaysTillMilitaryAction = LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>().minMaxDaysTillMilitaryAction.min;
			LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>().maxDaysTillMilitaryAction = LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>().minMaxDaysTillMilitaryAction.max;
			base.WriteSettings();

		}

		
	}



}

