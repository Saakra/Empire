﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;
using UnityEngine;


namespace FactionColonies
{
	public class settlementWindowFC : Window
	{
		public override Vector2 InitialSize
		{
			get
			{
				return new Vector2(1000f, 545f);
			}
		}


		//UI STUFF
		//time variables
		private int UIUpdateTimer = 0;
		public int scroll;
		public int maxScroll;
		public int scrollSpacing = 45;
		public int scrollHeight = 315;

		public void WindowUpdateFC()
		{
			settlement.updateProfitAndProduction();
			settlement.updateDescription();

			
		}

		public override void PreOpen()
		{
			base.PreOpen();
			settlement.updateDescription();
			maxScroll = (settlement.getNumberResource() * scrollSpacing) - scrollHeight;
			//settlement.update description

		}

			public void UiUpdate()
		{
			if (UIUpdateTimer == 0)
			{
				UIUpdateTimer = FactionColonies.updateUiTimer;
				WindowUpdateFC();
			}
			else
			{
				UIUpdateTimer -= 1;
			}
		}

		public void UiUpdate(bool var)
		{
			switch (var)
			{
				case true:
					UIUpdateTimer = 0;
					break;
			}
		}

		public override void WindowUpdate()
		{
			base.WindowUpdate();
			UiUpdate();
		}

		private List<string> stats = new List<string>() {"militaryLevel","happiness","loyalty","unrest","prosperity"};
		private List<string> buttons = new List<string>() { "DeleteSettlement".Translate(), "UpgradeTown".Translate(), "GoToLocation".Translate(), "PrisonersMenu".Translate() };
		//private List<string> productionButtons = new List<string>() { "Collect Tithe", "View Tithe" };

		public SettlementFC settlement;  //Don't expose

		public settlementWindowFC(SettlementFC settlement)
		{
			if (settlement == null)
			{
				this.Close();
			}
			this.settlement = settlement;
			this.forcePause = false;
			this.draggable = true;
			this.doCloseX = true;
			this.preventCameraMotion = false;
		}


		public override void DoWindowContents(Rect inRect)
		{
			//grab before anchor/font
			GameFont fontBefore = Text.Font;
			TextAnchor anchorBefore = Text.Anchor;

			// 1000x600


			DrawHeader();
			DrawSettlementStats(0, 80);
			//set 1 = settlement, set 2 = production
			DrawButtons(370, 336, 145, 30, 1);

			if (settlement != null)
			{
				//Upgrades
				DrawUpgrades(0, 295, 220, 80);
				DrawDescription(150, 80, 370, 220);

				//Divider
				Widgets.DrawLineVertical(530, 0, 564);

				//ProDuctTion
				DrawProductionHeader(550, 0);
				DrawButtons(560, 40, 100, 24, 2);
				DrawEconomicStats(687, 0, 139, 15);
				//lowerProDucTion
				Widgets.DrawLineHorizontal(546, 80, 422);
				DrawProductionHeaderLower(550, 80, 5);
			}




			//reset anchor/font
			Text.Font = fontBefore;
			Text.Anchor = anchorBefore;
		}

		public void DrawProductionHeaderLower(int x, int y, int spacing)
		{
			Text.Anchor = TextAnchor.MiddleRight;
			Text.Font = GameFont.Small;

			//Assigned workers
			Widgets.Label(new Rect(x, y, 410, 30), "AssignedWorkers".Translate() + ": " + settlement.getTotalWorkers().ToString() + " / " + settlement.workersMax.ToString() + " / " + settlement.workersUltraMax.ToString());


			Text.Anchor = TextAnchor.MiddleCenter;
			Text.Font = GameFont.Tiny;

			//Item Headers
			Widgets.DrawHighlight(new Rect(x, y + 30, 40, 40));
			Widgets.Label(new Rect(x, y + 30, 40, 40), "IsTithe".Translate() + "?");

			Widgets.DrawHighlight(new Rect(x+80, y + 30, 100, 40));
			Widgets.Label(new Rect(x+80, y + 30, 100, 40), "ProductionEfficiency".Translate());

			Widgets.DrawHighlight(new Rect(x + 195, y + 30, 45, 40));
			Widgets.Label(new Rect(x + 195, y + 30, 45, 40), "Base".Translate());

			Widgets.DrawHighlight(new Rect(x + 250, y + 30, 50, 40));
			Widgets.Label(new Rect(x + 250, y + 30, 50, 40), "Modifier".Translate());

			Widgets.DrawHighlight(new Rect(x + 310, y + 30, 45, 40));
			Widgets.Label(new Rect(x + 310, y + 30, 45, 40), "Final".Translate());

			Widgets.DrawHighlight(new Rect(x + 365, y + 30, 45, 40));
			Widgets.Label(new Rect(x + 365, y + 30, 45, 40), "EstimatedProfit".Translate());


			//Per resource
			for (int i = 0; i < settlement.getNumberResource(); i++)
			{
				if ((i * scrollSpacing) + scroll < 0)
				{
					//if outside view
				}
				else
				{
					//loop through each resource
					//isTithe
					bool disabled = false;
					switch (i)
					{
						case 6:
						case 7:
							disabled = true;
							break;
					}
					Widgets.Checkbox(new Vector2(x + 8, scroll + y + 65 + (i * (45 + spacing)) + 8), ref settlement.returnResourceByInt(i).isTithe, 24, disabled);

					if (settlement.returnResourceByInt(i).isTithe != settlement.returnResourceByInt(i).isTitheBool)
					{
						settlement.returnResourceByInt(i).isTitheBool = settlement.returnResourceByInt(i).isTithe;
						//Log.Message("changed tithe");
						settlement.updateProfitAndProduction();
						WindowUpdateFC();
					}

					//Icon
					if (Widgets.ButtonImage(new Rect(x + 45, scroll + y + 75 + (i * (45 + spacing)), 30, 30), settlement.returnResourceByInt(i).getIcon()))
					{
						Find.WindowStack.Add(new descWindowFC("SettlementProductionOf".Translate() + ": " + settlement.returnResourceByInt(i).label, char.ToUpper(settlement.returnResourceByInt(i).label[0]) + settlement.returnResourceByInt(i).label.Substring(1)));
					};

					//Production Efficiency
					Widgets.DrawBox(new Rect(x + 80, scroll + y + 70 + (i * (45 + spacing)), 100, 20));
					Widgets.FillableBar(new Rect(x + 80, scroll + y + 70 + (i * (45 + spacing)), 100, 20), (float)Math.Min(settlement.returnResourceByInt(i).baseProductionMultiplier, 1.0));
					Widgets.Label(new Rect(x + 80, scroll + y + 90 + (i * (45 + spacing)), 100, 20), "Workers".Translate() + ": " + settlement.returnResourceByInt(i).assignedWorkers.ToString());
					if (Widgets.ButtonText(new Rect(x + 80, scroll + y + 90 + (i * (45 + spacing)), 20, 20), "<"))
					{ //if clicked to lower amount of workers
						settlement.increaseWorkers(i, -1);
						WindowUpdateFC();
					}
					if (Widgets.ButtonText(new Rect(x + 160, scroll + y + 90 + (i * (45 + spacing)), 20, 20), ">"))
					{ //if clicked to lower amount of workers
						settlement.increaseWorkers(i, 1);
						WindowUpdateFC();
					}

					//Base Production
					Widgets.Label(new Rect(x + 195, scroll + y + 70 + (i * (45 + spacing)), 45, 40), FactionColonies.FloorStat(settlement.returnResourceByInt(i).baseProduction));

					//Final Modifier
					Widgets.Label(new Rect(x + 250, scroll + y + 70 + (i * (45 + spacing)), 50, 40), FactionColonies.FloorStat(settlement.returnResourceByInt(i).endProductionMultiplier));

					//Final Base
					Widgets.Label(new Rect(x + 310, scroll + y + 70 + (i * (45 + spacing)), 45, 40), (FactionColonies.FloorStat(settlement.returnResourceByInt(i).endProduction)));

					//Est Income
					Widgets.Label(new Rect(x + 365, scroll + y + 70 + (i * (45 + spacing)), 45, 40), (FactionColonies.FloorStat(settlement.returnResourceByInt(i).endProduction * LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>().silverPerResource)));
				}
			}

			//Scroll window for resources
			if (Event.current.type == EventType.ScrollWheel)
			{

				scrollWindow(Event.current.delta.y);
			}
		}

		public void DrawHeader()
		{
			//Draw Settlement Header Highlight
			Widgets.DrawHighlight(new Rect(0, 0, 520, 60));
			Widgets.DrawBox(new Rect(0, 0, 520, 60));

			//Draw town level and shadow backing
			Text.Font = GameFont.Medium;
			Text.Anchor = TextAnchor.MiddleCenter;
			Widgets.DrawShadowAround(new Rect(5, 15, 30, 30));
			Widgets.Label(new Rect(5, 15, 30, 30), settlement.settlementLevel.ToString());

			//Draw town name
			Text.Anchor = TextAnchor.MiddleLeft;
			Widgets.Label(new Rect(40, 0, 520, 30), settlement.name);

			//Draw town title
			Text.Font = GameFont.Tiny;
			Widgets.Label(new Rect(50, 25, 150, 20), "Hamlet".Translate()); //returnSettlement().title);

			//Draw town location flabor text
			Text.Font = GameFont.Tiny;
			Widgets.Label(new Rect(55, 40, 470, 20), "Located".Translate() + " " + HillinessUtility.GetLabel(Find.WorldGrid.tiles[settlement.mapLocation].hilliness) + " " + "LandOf".Translate() + " " + Find.WorldGrid.tiles[settlement.mapLocation].biome.LabelCap.ToLower()); //returnSettlement().title);

			//Draw header Settings button
			if (Widgets.ButtonImage(new Rect(495, 5, 20, 20), texLoad.iconCustomize))
			{ //if click faction customize button
				Find.WindowStack.Add(new settlementCustomizeWindowFC(settlement));
				//Log.Message("Settlement customize clicked");
			}
		}
	
		public void DrawSettlementStats(int x, int y)
		{
			int statSize = 30;
			Text.Anchor = TextAnchor.MiddleLeft;
			Text.Font = GameFont.Medium;
			for (int i = 0; i < stats.Count(); i++)
			{
				Widgets.DrawMenuSection(new Rect(x, y + ((statSize + 15) * i), 125, statSize + 10));
				//Widgets.DrawHighlight(new Rect(x, y + ((statSize + 15) * i), 125, statSize + 10));
				if (stats[i] == "militaryLevel")
				{
					if(Widgets.ButtonImage(new Rect(x + 5 - 2, y + 5 + ((statSize + 15) * i) - 2, statSize + 4, statSize + 4), texLoad.iconMilitary))
					{
						Find.WindowStack.Add(new descWindowFC("SettlementMilitaryLevelDesc".Translate(),"SettlementMilitaryLevel".Translate()));
					};
					Widgets.Label(new Rect(x + 50, y + ((statSize + 15) * i), 80, statSize + 10), settlement.settlementMilitaryLevel.ToString());
				}
				if (stats[i] == "happiness")
				{
					if(Widgets.ButtonImage(new Rect(x + 5 - 2, y + 5 + ((statSize + 15) * i) - 2, statSize + 4, statSize + 4), texLoad.iconHappiness)) 
					{
				Find.WindowStack.Add(new descWindowFC("SettlementHappinessDesc".Translate(), "SettlementHappiness".Translate()));
				};
					Widgets.Label(new Rect(x + 50, y + ((statSize + 15) * i), 80, statSize + 10), settlement.happiness + "%");
				}
				if (stats[i] == "loyalty")
				{
					if(Widgets.ButtonImage(new Rect(x + 5 - 2, y + 5 + ((statSize + 15) * i) - 2, statSize + 4, statSize + 4), texLoad.iconLoyalty))
					{
						Find.WindowStack.Add(new descWindowFC("SettlementLoyaltyDesc".Translate(), "SettlementLoyalty".Translate()));
					};
					Widgets.Label(new Rect(x + 50, y + ((statSize + 15) * i), 80, statSize + 10), settlement.loyalty + "%");
				}
				if (stats[i] == "unrest")
				{
					if(Widgets.ButtonImage(new Rect(x + 5 - 2, y + 5 + ((statSize + 15) * i) - 2, statSize + 4, statSize + 4), texLoad.iconUnrest))
					{
						Find.WindowStack.Add(new descWindowFC("SettlementUnrestDesc".Translate(), "SettlementUnrest".Translate()));
					};
					Widgets.Label(new Rect(x + 50, y + ((statSize + 15) * i), 80, statSize + 10), settlement.unrest + "%");
				}
				if (stats[i] == "prosperity")
				{
					if(Widgets.ButtonImage(new Rect(x + 5 - 2, y + 5 + ((statSize + 15) * i) - 2, statSize + 4, statSize + 4), texLoad.iconProsperity))
					{
						Find.WindowStack.Add(new descWindowFC("SettlementProsperityDesc".Translate(), "SettlementProsperity".Translate()));
					};
					Widgets.Label(new Rect(x + 50, y + ((statSize + 15) * i), 80, statSize + 10), settlement.prosperity + "%");
				}
			}
		}

		public void DrawButtons(int x, int y, int length, int size, int set)
		{
			Text.Anchor = TextAnchor.MiddleCenter;
			Text.Font = GameFont.Tiny;

			if (set == 1)
			{
				for (int i = 0; i < buttons.Count(); i++)
				{
					if (Widgets.ButtonText(new Rect(x, y + ((size + 10) * i), length, size), buttons[i]))
					{ //If click a button button
						if (buttons[i] == "UpgradeTown".Translate())
						{ //if click upgrade town button
							Find.WindowStack.Add(new settlementUpgradeWindowFC(settlement));
							//Log.Message(buttons[i]);
						}
						if (buttons[i] == "GoToLocation".Translate())
						{ //if click go to location
						  //Log.Message(buttons[i]);
							Find.WindowStack.TryRemove(this);
							settlement.goTo();
						}
						if (buttons[i] == "AreYouSureRemove".Translate())
						{ //if click to delete colony
							Find.WindowStack.TryRemove(this);
							FactionColonies.removePlayerSettlement(settlement);

							
						}
						if (buttons[i] == "DeleteSettlement".Translate())
						{ //if click town log button
						  //Log.Message(buttons[i]);
							buttons[i] = "AreYouSureRemove".Translate();
						}
						if (buttons[i] == "PrisonersMenu".Translate())
						{
							Find.WindowStack.Add(new FCPrisonerMenu(settlement));    //put prisoner window here.
						}

					}
				}
			}
			//set two buttons
		}

		public void DrawUpgrades(int x, int y, int length, int size)
		{
			//Widgets.DrawHighlight(new Rect(x, y, 500, 209));
			//Widgets.DrawBox(new Rect(x, y, 500, 209));

			Text.Font = GameFont.Medium;
			Text.Anchor = TextAnchor.MiddleLeft;

			Widgets.Label(new Rect(x + 5, y + 5, 450, 30), "BuildingUpgrades".Translate());

			Text.Font = GameFont.Tiny;
			Text.Anchor = TextAnchor.LowerCenter;


			//Establish variables to customize the UI

			int elementsPerRow = 4;
			int spacing = 15;
			int boxSide = 72;

			int spacingFromText = 45;
			int spacingFromSide = 15;// (494 - (spacing + boxSide) * elementsPerRow) / 2;

			int row;
			int column;

			Rect box = new Rect(0 + spacingFromSide, 0 + spacingFromText, boxSide, boxSide);
			Rect buildingIcon = new Rect(4 + box.x, 4 + box.y, boxSide - 8, boxSide - 8);

			Rect nBox;
			Rect nBuilding;

			

			int i = 0;

			foreach (BuildingFCDef building in settlement.buildings)
			{
				//Update Variables for List
				row = (int)Math.Floor((double)(i / elementsPerRow));
				column = (int)(i % elementsPerRow);

				nBox = new Rect(new Vector2(box.x + x + ((box.width + spacing) * column), box.y + y + ((box.height + 10) * row)), box.size);
				nBuilding = new Rect(new Vector2(buildingIcon.x + x + ((box.width + spacing) * column), buildingIcon.y + y + ((box.height + 10) * row)), buildingIcon.size);

				

				//Actual UI Code
				Widgets.DrawMenuSection(nBox);
				if (i < settlement.numberBuildings)
				{
					if (Widgets.ButtonImage(nBuilding, building.icon))
					{
						//Find.WindowStack.Add(new listBuildingFC(building, i, settlement));
						Find.WindowStack.Add(new FCBuildingWindow(settlement, i));
					}
				} else
				{
					if (Widgets.ButtonImage(nBuilding, texLoad.buildingLocked))
					{
						Messages.Message("That Building is locked", MessageTypeDefOf.RejectInput);
					}
				}
				//Optional Label
				//Widgets.Label(nBuilding, building.LabelCap);

				i++;
			}



		}

		public void DrawDescription(int x, int y, int length, int size)
		{
			//Widgets.Label(new Rect(x, y - 20, 100, 30), "Description".Translate());
			Widgets.DrawMenuSection(new Rect(x, y, length, size));

			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.UpperLeft;
			Widgets.Label(new Rect(x+5, y+5, length-10, size-10), settlement.description);
		}

		public void DrawProductionHeader(int  x, int y)
		{
			Text.Font = GameFont.Medium;
			Text.Anchor = TextAnchor.UpperLeft;
			Widgets.Label(new Rect(x, y, 400, 30), "Production".Translate());
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.UpperLeft;
			Widgets.Label(new Rect(x + 5, y + 60, 150, 20), "TaxBase".Translate() + ": " +  ((100 + traitUtilsFC.cycleTraits(new double(), "taxBasePercentage", settlement.traits, "add") + traitUtilsFC.cycleTraits(new double(), "taxBasePercentage", Find.World.GetComponent<FactionFC>().traits, "add"))).ToString() + "%");
		}

		public void DrawEconomicStats(int x, int y, int length, int size)
		{
			Text.Font = GameFont.Tiny;
			Text.Anchor = TextAnchor.MiddleCenter;

			Widgets.Label(new Rect(x, 0, length, size), "Total".Translate() + " " + "CashSymbol".Translate() + " "+ "Income".Translate());
			Widgets.Label(new Rect(x, size + 3, length, size), settlement.totalIncome.ToString());
			Widgets.Label(new Rect(x, size * 2 + 6, length, size), "Upkeep");
			Widgets.Label(new Rect(x, size * 3 + 9, length, size), settlement.totalUpkeep.ToString());
			Widgets.Label(new Rect(x + length + 10, 0, length, size), "Total".Translate() + " " + "CashSymbol".Translate() + " " + "Profit".Translate());
			Widgets.Label(new Rect(x + length + 10, size + 3, length, size), settlement.totalProfit.ToString());
			Widgets.Label(new Rect(x + length + 10, size * 2 + 6, length, size), "CostPerWorker".Translate());
			Widgets.Label(new Rect(x + length + 10, size * 3 + 9, length, size), settlement.workerCost.ToString());

		}




		private void scrollWindow(float num)
		{
			if (scroll - num * 5 < -1 * maxScroll)
			{
				scroll = -1 * maxScroll;
			}
			else if (scroll - num * 5 > 0)
			{
				scroll = 0;
			}
			else
			{
				scroll -= (int)Event.current.delta.y * 5;
			}
			Event.current.Use();
		}
	}
}
