using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace FactionColonies
{
	public class PaymentUtil
	{

		public static (List<BillFC>, List<BillFC>) returnBillTypes(List<BillFC> bills)
			{

			List<BillFC> positiveBills = new List<BillFC>();
			List<BillFC> negativeBills = new List<BillFC>();

			foreach (BillFC bill in bills)
			{
				if (bill.taxes.silverAmount >= 0)
				{

					positiveBills.Add(bill);
				}
				else
				{
					negativeBills.Add(bill);
				}
			}
			return (negativeBills, positiveBills);
			}

		public static void autoresolveBills(List<BillFC> bills)
		{
			int resolvedBills = 0;

			(List<BillFC> negativeBills, List<BillFC> positiveBills) = returnBillTypes(bills);

			
		//Go through each negative bill
		//cycle through each positive bill
		//subtract silver from positive bill until negative bill = 0
		//if negative bill equals zero, move to next.

		//if make it to the end of the positive bills, goto function to check if there's enough silver. If so, pay, if not, return the function
		Reset:
			foreach (BillFC negativeBill in negativeBills)
			{
				ResetInner:
				foreach (BillFC positiveBill in positiveBills)
				{
					float result = positiveBill.taxes.silverAmount + negativeBill.taxes.silverAmount;
					if (result == 0)
					{
						//Log.Message("Equal");
						//if bills cancel eachother out
						//resolve positive bill and negative bill
						positiveBill.taxes.silverAmount = 0;
						negativeBill.taxes.silverAmount = 0;
						positiveBill.resolve();
						negativeBill.resolve();
						resolvedBills += 2;
						(negativeBills, positiveBills) = returnBillTypes(bills);
						goto Reset;

					} else if (result > 0)
					{
						//Log.Message("More");
						//if positive bill greater than negative bill
						positiveBill.taxes.silverAmount = result;
						negativeBill.taxes.silverAmount = 0;
						negativeBill.resolve();
						resolvedBills++;
						(negativeBills, positiveBills) = returnBillTypes(bills);
						goto Reset;
					} else if (result < 0)
					{
						//Log.Message("Less");
						//if negative bill is greater (technically lesser) than positive bill
						positiveBill.taxes.silverAmount = 0;
						negativeBill.taxes.silverAmount = result;
						positiveBill.resolve();
						resolvedBills++;
						(negativeBills, positiveBills) = returnBillTypes(bills);
						goto ResetInner;
					}
				}

				//if looped through all positive bills, attempt to resolve

				if (negativeBill.attemptResolve())
				{
					(negativeBills, positiveBills) = returnBillTypes(bills);
					resolvedBills++;
				}

			}

			ResetOuter:
			foreach (BillFC positiveBill in positiveBills)
			{
				positiveBill.resolve();
				resolvedBills++;
				(negativeBills, positiveBills) = returnBillTypes(bills);
				goto ResetOuter;
			}

			Messages.Message(TranslatorFormattedStringExtensions.Translate("NumberTaxesHasBeenSolved", resolvedBills), MessageTypeDefOf.NeutralEvent);
			



		}
		public static void placeThing(Thing thing)
		{
			Map map;
			Map taxMap = Find.World.GetComponent<FactionFC>().taxMap;
			if (taxMap == null)
			{
				if (Find.WorldObjects.SettlementAt(Find.World.GetComponent<FactionFC>().capitalLocation) == null) 
				{
					if (Find.WorldObjects.SettlementAt(Find.World.GetComponent<FactionFC>().capitalLocation).Map == null)
					{

						//if no tax map or no capital map is valid
						if (Find.CurrentMap.IsPlayerHome == true)
						{
							map = Find.CurrentMap;
						}
						else
						{
							map = Find.AnyPlayerHomeMap;
						}
						Log.Message("Unable to find a player-set tax map or a valid location for the capital. Please open the faction main menu tab and set the capital and tax map. Taxes were sent to the following random PlayerHomeMap " + map.Parent.LabelCap);

					}
					else
					{
						map = Find.Maps.ElementAt(Find.World.GetComponent<FactionFC>().capitalLocation);
					}
				}
				else
				{
					//if no tax map or no capital map is valid
					if (Find.CurrentMap.IsPlayerHome == true)
					{
						map = Find.CurrentMap;
					}
					else
					{
						map = Find.AnyPlayerHomeMap;
					}
					Log.Message("Unable to find a player-set tax map or a valid location for the capital. Please open the faction main menu tab and set the capital and tax map. Taxes were sent to the following random PlayerHomeMap " + map.Parent.LabelCap);

				}
			} else
			{
				map = taxMap;
			}

			IntVec3 intvec;
			if (checkForTaxSpot(map, out intvec) != true)
			{
				intvec = DropCellFinder.TradeDropSpot(map);
			}

			GenPlace.TryPlaceThing(thing, intvec, map, ThingPlaceMode.Near);
		}
        public static void deliverThings(FCEvent events)
        {
            foreach (Thing thing in events.goods)
            {
				placeThing(thing);
            }
        }




		public static void deliverThings(List<Thing> things)
		{
			foreach (Thing thing in things)
			{
				placeThing(thing);
			}
		}

		public static bool paySilver(int amount)
		{
			Paid:
			while (amount > 0)
			{
				foreach (Map map in Find.Maps)
				{
					if (map.IsPlayerHome)
					{
					List:
						foreach (Thing item in map.listerThings.AllThings) //loop through each item in cell
						{
							if (item.def == ThingDefOf.Silver && item.IsInAnyStorage() == true)
							{ //if silver, add to count
								if (amount - item.stackCount < 0) //if removing silver would pay too much
								{
									int overdraw = -1 * (amount - item.stackCount);
									amount -= (item.stackCount - overdraw);
									item.SplitOff(item.stackCount - overdraw).Destroy(DestroyMode.Vanish);
									goto Paid;
								}
								else if (amount - item.stackCount > 0) //if removing silver would leave some
								{
									amount -= item.stackCount;
									item.Destroy(DestroyMode.Vanish);
									goto List;
								}
								else if (amount - item.stackCount == 0) //if removing silver will make amount = 0
								{
									amount -= item.stackCount;
									item.Destroy(DestroyMode.Vanish);
									goto Paid;
								}
							}
						}
					}
				}
			}
			return true;
		}

		public static bool checkForTaxSpot(Map map, out IntVec3 dropSpot)
		{
			foreach (IntVec3 cell in map.AllCells) //loop through all zones
			{
				foreach (Thing item in map.thingGrid.ThingsAt(cell)) //loop through each item in cell
				{
					if (item.def.defName == "TaxSpot")
					{ //if silver, add to count
						dropSpot = cell;
						//Log.Message("Found thing!");
						return true;
					}
				}
			}
			dropSpot = new IntVec3();
			return false;
		}

		public static int getSilver()
		{
			int silver = 0;

			foreach (Map map in Find.Maps)
			{
				if (map.IsPlayerHome)
				{
					foreach (Thing thing in map.listerThings.AllThings)
					{
						if (thing.def == ThingDefOf.Silver && thing.IsInAnyStorage() == true)
						{
							//Log.Message(thing.LabelCap + " inStorage: " + thing.IsInAnyStorage());
							silver += thing.stackCount;
						}
					}
				}
			}

				//Log.Message("Silver: " + silver);
			return silver;
		}

		public static ThingSetMakerParams returnThingSetMakerParams(int baseValue, int rangeMod)
		{
			ThingSetMakerParams parms = new ThingSetMakerParams();
			parms.techLevel = Find.FactionManager.OfPlayer.def.techLevel;
			parms.totalMarketValueRange = new FloatRange(baseValue - rangeMod, baseValue + rangeMod);
			return parms;
		}

		public static List<Thing> generateRaidLoot(int lootLevel, TechLevel techLevel)
		{
			List<Thing> things = new List<Thing>();
			ThingSetMaker thingSetMaker = new ThingSetMaker_MarketValue();
			ThingSetMakerParams param = new ThingSetMakerParams();
			param.totalMarketValueRange = new FloatRange( 300 + (lootLevel*100), 100 + (lootLevel*500));
			param.filter = new ThingFilter();
			param.techLevel = techLevel;
			param.countRange = new IntRange(3, 20);

			//set allow
			param.filter.SetAllow(ThingCategoryDefOf.FoodMeals, true);
			param.filter.SetAllow(ThingCategoryDefOf.Weapons, true);
			param.filter.SetAllow(ThingCategoryDefOf.Apparel, true);
			param.filter.SetAllow(ThingCategoryDefOf.BuildingsArt, true);
			param.filter.SetAllow(ThingCategoryDefOf.Drugs, true);
			param.filter.SetAllow(ThingCategoryDefOf.Items, true);
			param.filter.SetAllow(ThingCategoryDefOf.Medicine, true);
			param.filter.SetAllow(ThingCategoryDefOf.ResourcesRaw, true);
			param.filter.SetAllow(ThingCategoryDefOf.Techprints, true);
			param.filter.SetAllow(ThingCategoryDefOf.Buildings, true);
			param.filter.SetAllow(StuffCategoryDefOf.Metallic, true);

			//set disallow
			param.filter.SetAllow(DefDatabase<ThingDef>.GetNamedSilentFail("Teachmat"), false);

			things = thingSetMaker.Generate(param);
			return things;

		}

		public static List<Thing> generateTithe(double valueBase, double valueDiff, int multiplier, int resourceID, double traitValueMod )
		{
			FactionFC faction = Find.World.GetComponent<FactionFC>();
			List <Thing> things = new List<Thing>();
			ThingSetMaker thingSetMaker = new ThingSetMaker_MarketValue();
			ThingSetMakerParams param = new ThingSetMakerParams();
			param.totalMarketValueRange = new FloatRange((float)((valueBase* LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>().silverPerResource )- ((valueDiff + traitValueMod) *multiplier)), (float)((valueBase * LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>().silverPerResource) + ((valueDiff + traitValueMod) * multiplier)));
			param.filter = new ThingFilter();
			param.techLevel = FactionColonies.getPlayerColonyFaction().def.techLevel;

			switch (resourceID)
			{
				case 0: //food
					param.filter.SetAllow(ThingCategoryDefOf.FoodMeals, true);
					param.countRange = new IntRange(1, 5+multiplier);
					break;
				case 1: //weapons
					param.filter.SetAllow(ThingCategoryDefOf.Weapons, true);
					param.countRange = new IntRange(1, 4+(2*multiplier));
					break;
				case 2: //apparel
					param.filter.SetAllow(ThingCategoryDefOf.Apparel, true);
					param.countRange = new IntRange(1, 4+ (3*multiplier));
					break;
				case 3: //animals
					thingSetMaker = new ThingSetMaker_Animals();
					param.techLevel = TechLevel.Undefined;
					//param.countRange = new IntRange(1,4);
					break;
				case 4: //Logging
					param.filter.SetAllow(ThingDefOf.WoodLog, true);
					param.countRange = new IntRange(1, 5*multiplier);
					break;
				case 5: //Mining
					param.filter.SetAllow(StuffCategoryDefOf.Metallic, true);
					param.filter.SetAllow(ThingDefOf.Silver, false);
					//Android shit?
					param.filter.SetAllow(DefDatabase<ThingDef>.GetNamedSilentFail("Teachmat"), false);
					//Remove RimBees Beeswax
					param.filter.SetAllow(DefDatabase<StuffCategoryDef>.GetNamedSilentFail("RB_Waxy"), false);
					//Remove Alpha Animals skysteel
					param.filter.SetAllow(DefDatabase<ThingDef>.GetNamedSilentFail("AA_SkySteel"), false);
					param.countRange = new IntRange(1, 4*multiplier);
					break;
				case 6: //research
					Log.Message("generateTithe - Research Tithe - How did you get here?");

					break;
				case 7: //Power
					Log.Message("generateTithe - Power Tithe - How did you get here?");
					break;
				case 8: //Medicine/Bionics
					param.filter.SetAllow(ThingCategoryDefOf.Medicine, true);
					param.filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("BodyPartsNatural"), true);
					switch (faction.techLevel)
					{
						case TechLevel.Archotech:
						case TechLevel.Ultra:
							param.filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("BodyPartsUltra"), true);
							param.filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("BodyPartsBionic"), true);
							param.filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("BodyPartsProsthetic"), true);
							param.filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("BodyPartsNatural"), true);

							if (DefDatabase<ThingCategoryDef>.GetNamedSilentFail("BionicProstheses") != null)
							param.filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("BionicProstheses"), true);
							if (DefDatabase<ThingCategoryDef>.GetNamedSilentFail("AdvancedProstheses") != null)
								param.filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("AdvancedProstheses"), true);
							if (DefDatabase<ThingCategoryDef>.GetNamedSilentFail("SyntheticOrgans") != null)
								param.filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("SyntheticOrgans"), true);
							if (DefDatabase<ThingCategoryDef>.GetNamedSilentFail("Neurotrainers") != null)
								param.filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("Neurotrainers"), true);
							break;
						case TechLevel.Spacer:
							param.filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("BodyPartsBionic"), true);
							param.filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("BodyPartsProsthetic"), true);
							param.filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("BodyPartsNatural"), true);
							if (DefDatabase<ThingCategoryDef>.GetNamedSilentFail("BionicProstheses") != null)
								param.filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("BionicProstheses"), true);
							if (DefDatabase<ThingCategoryDef>.GetNamedSilentFail("SyntheticOrgans") != null)
								param.filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("SyntheticOrgans"), true);
							break;
						case TechLevel.Industrial:
							param.filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("BodyPartsProsthetic"), true);
							param.filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("BodyPartsNatural"), true);
							break;
						default:
							break;
					}

					

					param.countRange = new IntRange(1, 2 * multiplier);
					break;
			}

			//Log.Message(resourceID.ToString());


			//thingSetMaker.root
			things = thingSetMaker.Generate(param);
			return things;
		}

		public static Pawn generatePrisoner(Faction faction)
		{
			Pawn pawn;

			PawnKindDef raceChoice;
			raceChoice = faction.RandomPawnKind();

			pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(raceChoice, FactionColonies.getPlayerColonyFaction(), PawnGenerationContext.NonPlayer, -1, false, false, false, false, false, true, 0, false, false, false, false, false, false, false, false, 0, null, 0, null, null, null, null, null, null, null, null, null, null, null, null));
			pawn.guest.isPrisonerInt = true;



			return pawn;
		}

		public static List<Thing> generateThing(double valueBase, string resourceOfThing)
		{
			regen:
			List<Thing> things = new List<Thing>();
			ThingSetMaker thingSetMaker = new ThingSetMaker_MarketValue();
			ThingSetMakerParams param = new ThingSetMakerParams();
			param.totalMarketValueRange = new FloatRange((float)(valueBase-300),(float)(valueBase+300));
			param.filter = new ThingFilter();
			param.techLevel = FactionColonies.getPlayerColonyFaction().def.techLevel;

			switch (resourceOfThing)
			{
				case "food": //food
					param.filter.SetAllow(ThingCategoryDefOf.FoodMeals, true);
					param.countRange = new IntRange(1, 1);
					break;
				case "weapons": //weapons
					param.filter.SetAllow(ThingCategoryDefOf.Weapons, true);
					param.qualityGenerator = QualityGenerator.Gift;
					param.totalMarketValueRange = new FloatRange((float)(valueBase - valueBase*.5), (float)(valueBase * 2));
					param.countRange = new IntRange(1, 1);
					break;
				case "apparel": //apparel
					param.filter.SetAllow(ThingCategoryDefOf.Apparel, true);
					param.qualityGenerator = QualityGenerator.Gift;
					param.totalMarketValueRange = new FloatRange((float)(valueBase - valueBase * .5), (float)(valueBase * 2));
					param.countRange = new IntRange(1, 1);
					break;
				case "armor": //armor
					param.qualityGenerator = QualityGenerator.Gift;
					param.filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamed("ApparelArmor"), true);
					param.filter.SetAllow(ThingCategoryDefOf.Apparel, true);
					param.countRange = new IntRange(1, 1);
					param.totalMarketValueRange = new FloatRange((float)(valueBase - valueBase * .5), (float)(valueBase*2));
					break;
				case "animals": //animals
					thingSetMaker = new ThingSetMaker_Animal();
					param.techLevel = TechLevel.Undefined;
					param.totalMarketValueRange = new FloatRange((float)(valueBase - valueBase * .5), (float)(valueBase * 1.5));
					//param.countRange = new IntRange(1,4);
					break;
				case "logging": //Logging
					param.filter.SetAllow(ThingDefOf.WoodLog, true);
					param.countRange = new IntRange(1, 10);
					break;
				case "mining": //Mining
					param.filter.SetAllow(StuffCategoryDefOf.Metallic, true);
					param.filter.SetAllow(ThingDefOf.Silver, false);
					//Android shit?
					param.filter.SetAllow(DefDatabase<ThingDef>.GetNamedSilentFail("Teachmat"), false);
					//Remove RimBees Beeswax
					param.filter.SetAllow(DefDatabase<StuffCategoryDef>.GetNamedSilentFail("RB_Waxy"), false);
					//Remove Alpha Animals skysteel
					param.filter.SetAllow(DefDatabase<ThingDef>.GetNamedSilentFail("AA_SkySteel"), false);
					param.countRange = new IntRange(1, 10);
					break;
				case "drugs": //drugs
					param.filter.SetAllow(ThingCategoryDefOf.Drugs, true);
					param.countRange = new IntRange(1, 2);
					break;
				default: //log error
					Log.Message("This is an error. Report this to the dev. generateThing - nonexistent case");
					Log.Message(resourceOfThing);
					break;
			}

			//Log.Message(resourceID.ToString());


			//thingSetMaker.root
			things = thingSetMaker.Generate(param);
			if(PaymentUtil.returnValueOfTithe(things) < param.totalMarketValueRange.Value.min)
			{
				goto regen;
			}

			return things;
		}

		public static double returnValueOfTithe(List<Thing> things)
		{
			double totalValue = 0;
			foreach (Thing thing in things)
			{
				//Log.Message(thing.def + " #" + thing.stackCount + " $" + thing.stackCount * thing.MarketValue);
				totalValue += thing.stackCount * thing.MarketValue;
			}
			//Log.Message("Total Value: $" + totalValue);
			return totalValue;
		}


	}

	public class ThingSetMaker_Animals : ThingSetMaker
	{
		protected override void Generate(ThingSetMakerParams parms, List<Thing> outThings)
		{
			List<PawnKindDef> things = new List<PawnKindDef>();
			List<PawnKindDef> allAnimalDefs = DefDatabase<PawnKindDef>.AllDefsListForReading;
			int value = 0;
			foreach (PawnKindDef def in allAnimalDefs)
			{
				if (def.race.race.Animal && def.RaceProps.IsFlesh && def.race.BaseMarketValue != 0 && def.race.tradeTags != null && !def.race.tradeTags.Contains("AnimalMonster") && !def.race.tradeTags.Contains("AnimalGenetic") && !def.race.tradeTags.Contains("AnimalAlpha"))
				{
					things.Add(def);
				}
			}

			int attempts = 0;
			//Log.Message("Min: " + parms.totalMarketValueRange.Value.min + " = " + parms.totalMarketValueRange.Value.max + " max");

			while (parms.totalMarketValueRange.Value.min > value && value < parms.totalMarketValueRange.Value.max)
			{
			Regen:
				if (parms.totalMarketValueRange.Value.min < value)
				{
					attempts += 1;
					//Log.Message("Attempts +1");
				}
				PawnGenerationRequest request = new PawnGenerationRequest(things.RandomElement<PawnKindDef>(), Find.FactionManager.OfPlayer, PawnGenerationContext.NonPlayer, -1, false, false, false, false, true, false, 1f, false, true, true, false, false, false, false, true, 0, null, 1, null, null, null, null);
				Pawn pawn = PawnGenerator.GeneratePawn(request);
				//Log.Message("Pawn generate: " + pawn.LabelCap + " value: " + pawn.MarketValue);
				if (pawn.MarketValue + value > parms.totalMarketValueRange.Value.max)
				{
					if (attempts >= 5)
					{
						goto Exit;
					} else { 
						goto Regen;
					}
				}
				//Log.Message(pawn.Name + "   " + pawn.MarketValue + "MarketValue: max value" + parms.totalMarketValueRange.Value.max + ", min val: " + parms.totalMarketValueRange.Value.min);
				value += (int)pawn.MarketValue;
				outThings.Add(pawn);
				//Log.Message(pawn.Label + "total cost: " + value);
				goto Regen;
				Exit:;

			}
		}
		protected override IEnumerable<ThingDef> AllGeneratableThingsDebugSub(ThingSetMakerParams parms)
		{
			yield return null;
			yield break;
		}
	}

	public class ThingSetMaker_Animal : ThingSetMaker
	{
		protected override void Generate(ThingSetMakerParams parms, List<Thing> outThings)
		{
			List<PawnKindDef> things = new List<PawnKindDef>();
			List<PawnKindDef> allAnimalDefs = DefDatabase<PawnKindDef>.AllDefsListForReading;

			foreach (PawnKindDef def in allAnimalDefs)
			{
				bool flag = def.race.race.Animal && def.RaceProps.IsFlesh && def.race.BaseMarketValue > parms.totalMarketValueRange.Value.min && def.race.tradeTags != null && !def.race.tradeTags.Contains("AnimalMonster") && !def.race.tradeTags.Contains("AnimalGenetic") && !def.race.tradeTags.Contains("AnimalAlpha");
				if (flag)
				{
					things.Add(def);
				}
			}

			regen:
				PawnGenerationRequest request = new PawnGenerationRequest(things.RandomElement<PawnKindDef>(), Find.FactionManager.OfPlayer, PawnGenerationContext.NonPlayer, -1, false, false, false, false, true, false, 1f, false, true, true, false, false, false, false, true, 0, null, 1, null, null, null, null);
				Pawn pawn = PawnGenerator.GeneratePawn(request);


			if (pawn.MarketValue > parms.totalMarketValueRange.Value.max)
			{
				goto regen;
			}
			outThings.Add(pawn);
		
		}


		protected override IEnumerable<ThingDef> AllGeneratableThingsDebugSub(ThingSetMakerParams parms)
		{
			yield return PawnKindDefOf.Thrumbo.race;
			yield break;
		}
	}





}
