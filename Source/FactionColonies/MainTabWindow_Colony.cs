using System;
using System.Collections.Generic;
using System.Linq;

using RimWorld.Planet;
using RimWorld;
using UnityEngine;
using Verse;


namespace FactionColonies
{
	public class MainTabWindow_Colony : MainTabWindow
	{
		public bool selectingColonyFC = false;
		public override Vector2 InitialSize
		{
			get
			{
				return new Vector2(350f, 400f);
			}
		}

		public void MainTabWindow()
		{
			this.closeOnClickedOutside = false;
		}



		public int tab = 0;
		public int tabSize = 78;
		public int resourceSize;
		public FactionFC faction;
		public List<SettlementFC> settlementList;
		public int scroll = 0;
		public int maxScroll = 0;

		private int xspacing = 45;
		private int yspacing = 30;
		private int yoffset = 100;
		private int headerSpacing = 30;

		public List<string> stats = new List<string> { }; //button list to draw
		public int statSize = 25; // height size of the stats when drawing
		public List<string> buttons = new List<string> { }; //button list to draw
		public int buttonSize = 25; // height size of the buttons when drawing


		public List<string> stats_tab_0 = new List<string> {"happiness", "loyalty", "unrest", "prosperity"};

		public List<string> buttons_tab_0 = new List<string> { "Policies-Traits".Translate(), "Military".Translate(), "Actions".Translate() };


		public override void PostOpen()
		{
			base.PostOpen();
		}

		public override void PreOpen()
		{
			base.PreOpen();
			stats = stats_tab_0;
			statSize = 25;
			buttons = buttons_tab_0;
			resourceSize = 40;
			faction = Find.World.GetComponent<FactionFC>();
			settlementList = faction.settlements;
			faction.updateAverages();

			//Initial release - Autocreate faction
			//Faction faction = FactionColonies.getPlayerColonyFaction();
			//if (faction == null)
			//{
			//	FactionColonies.createPlayerColonyFaction();
			//}

			//if (faction.capitalLocation == -1)
			//{
			//	faction.setCapital();
			//}

				faction.updateTotalProfit();
			
		}

		public override void PostClose()
		{
			base.PostClose();
			//If selecting colony
			selectingColonyFC = false;


		}

		public override void WindowUpdate()
		{
			base.WindowUpdate();
			UiUpdate();
		}

		//UI STUFF
		//time variables
		private int UIUpdateTimer = 0;

		public void WindowUpdateFC()
		{
			faction.updateAverages();
			maxScroll = (settlementList.Count() * yspacing) - 264;
		}



		public void UiUpdate()
		{
			if (UIUpdateTimer < Find.TickManager.TicksAbs)
			{
				UIUpdateTimer = Find.TickManager.TicksAbs + FactionColonies.updateUiTimer;
				WindowUpdateFC();
			}
		}

		public override void DoWindowContents(Rect inRect)
		{
			base.DoWindowContents(inRect);

			
			//set text anchor and font
			GameFont fontBefore = Text.Font;
			TextAnchor anchorBefore = Text.Anchor;

			//Draw tabs
			DrawTabFaction(inRect);
			DrawTabColony(inRect);
			DrawTabReports(inRect);
			DrawTabEvent(inRect);

			//DrawColonySettlementCreationButton(inRect); //used for debugging

			if(tab == 0)
			{
				DrawFactionTopMenu(inRect);
				DrawFactionMiddleMenu(inRect);
				DrawFactionBottomMenu(inRect);


			}

			//Draw window based on tab
			if(tab == 1)
			{
				DrawColonySettlementCreationButton(inRect);
				DrawSettlementMenu(inRect);
				//DrawDebugButton(inRect);
				
				if (Event.current.type == EventType.ScrollWheel)
				{
					
					scrollWindow(Event.current.delta.y);
				}

			}

			//draw event select tab
			if (tab == 2)
			{

			}


			//first tests
			//DrawHeader(inRect);
			//DrawColonySettlementCreationButton(inRect);

			//Debug
			//DrawDebugButton(inRect);


			//Reset Text anchor and font
			Text.Font = fontBefore;
			Text.Anchor = anchorBefore;
			

		}
		private void DrawHeader(Rect inRect)
		{
			Rect header = new Rect(0, 45, 150, 35);
			Text.Anchor = TextAnchor.UpperCenter;
			Text.Font = GameFont.Medium;
			Widgets.Label(header, "SettlementManager".Translate());


		}

		private void DrawColonySettlementCreationButton(Rect inRect)
		{
			Rect button = new Rect(InitialSize.x - 215, 40, 190, 20);
			Text.Anchor = TextAnchor.MiddleLeft;
			Text.Font = GameFont.Small;

			Faction gfaction = FactionColonies.getPlayerColonyFaction();
			if (gfaction != null)
			{

				if (Widgets.ButtonText(button, "CreateNewColony".Translate()))
				{
					Find.WindowStack.Add(new createColonyWindowFC());

					//Move player to world map
					Find.World.renderer.wantedMode = WorldRenderMode.Planet;

					Messages.Message("SelectTile".Translate(), MessageTypeDefOf.NegativeEvent);

				}
			} else //create new faction
			{
				if (Widgets.ButtonText(button, "Create New Faction"))
				{ 
					FactionColonies.createPlayerColonyFaction();
					Find.WindowStack.Add(new factionCustomizeWindowFC(faction));
					//Initial release - Autocreate faction
					Messages.Message(Find.WorldObjects.SettlementAt(Find.CurrentMap.Parent.Tile).Name + " " + "SetAsFactionCapital".Translate() + "!", MessageTypeDefOf.NeutralEvent);

				}
			}
		}


		private void DrawTabFaction(Rect inRect)
		{
			Text.Anchor = TextAnchor.MiddleCenter;
			Text.Font = GameFont.Small;
			
			if(Widgets.ButtonTextSubtle(new Rect(0, 0, tabSize, 30), "Faction".Translate(), 0f, 8f, SoundDefOf.Mouseover_Category, new Vector2(-1f, -1f)))
			{
				tab = 0;
				faction.updateTotalProfit();
			}
		}
		private void DrawTabColony(Rect inRect)
		{
			Text.Anchor = TextAnchor.MiddleCenter;
			Text.Font = GameFont.Small;

			if (Widgets.ButtonTextSubtle(new Rect(tabSize, 0, tabSize, 30), "Colonies".Translate(), 0f, 8f, SoundDefOf.Mouseover_Category, new Vector2(-1f, -1f)))
			{
				tab = 1;
				scroll = 0;
				maxScroll = (settlementList.Count() * yspacing)-264;
			}
		}
		private void DrawTabReports(Rect inRect)
		{
			Text.Anchor = TextAnchor.MiddleCenter;
			Text.Font = GameFont.Small;

			if (Widgets.ButtonTextSubtle(new Rect(tabSize*2, 0, tabSize, 30), "Bills".Translate(), 0f, 8f, SoundDefOf.Mouseover_Category, new Vector2(-1f, -1f)))
			{
				Find.WindowStack.Add(new FCBillWindow());
				//Log.Message("Try open bills");
				
			}
		}
		private void DrawTabEvent(Rect inRect)
		{
			Text.Anchor = TextAnchor.MiddleCenter;
			Text.Font = GameFont.Small;

			if (Widgets.ButtonTextSubtle(new Rect(tabSize*3, 0, tabSize, 30), "Events".Translate(), 0f, 8f, SoundDefOf.Mouseover_Category, new Vector2(-1f, -1f)))
			{
				//tab = 0;
				//Open Event window
				Find.WindowStack.Add(new FCEventWindow());
			}
		}

		private void DrawSettlementMenu(Rect inRect)
		{
			DrawSettlementHeader(inRect);
			DrawSettlementButtons(inRect);
		}
		private void DrawSettlementHeader(Rect inRect)
		{
			Text.Anchor = TextAnchor.MiddleLeft;
			Text.Font = GameFont.Medium;
			Widgets.Label(new Rect(2, 32, 200, 40), "Settlements".Translate());
		}

		private void DrawSettlementButtons(Rect inRect)
		{
			Text.Anchor = TextAnchor.MiddleCenter;
			Text.Font = GameFont.Tiny;

			//Reference
			//ist[0] = name;   //settlement name
			//list[1] = settlementLevel.ToString(); //settlement level
			//list[2] = settlementMilitaryLevel.ToString(); //settlement military level
			//list[3] = unrest.ToString(); //settlement unrest
			//list[4] = loyalty.ToString(); //settlement loyalty
			//list[5] = getTotalProfit().ToString(); //settlement profit
			//list[6] = mapLocation.ToString(); //settlement location
			//list[7] = ID

			List<String> headerList = new List<String>() {"Settlement".Translate(), "ColonyLevel".Translate(), "MilitaryLevel".Translate(), "Unrest".Translate(), "Loyalty".Translate(), "Profit".Translate(), "Location", "ID" };
			for (int i = 0; i < headerList.Count()-2; i++)  //-2 to exclude location and ID
			{
				if (i == 0)
				{
					Widgets.Label(new Rect(2 + i * xspacing, 60, xspacing+headerSpacing, 30), headerList[i]);
				}
				else
				{
					Widgets.Label(new Rect(headerSpacing+2 + i * xspacing, 60, xspacing, 30), headerList[i]);
				}
			}

			for (int i = 0; i < settlementList.Count(); i++) //browse through list.  settlementList[i] = a settlement
			{
				SettlementFC settlement = settlementList[i];
				if (i*yspacing + scroll >= 0 && i*yspacing + scroll <= 264) 
				{ 
					if (i % 2 == 0)
					{
						Widgets.DrawHighlight(new Rect(0, yoffset + i * yspacing + scroll, 312, 30));
					}
					for (int k = 0; k < 6; k++)  //Browse through settlement information    -2 to exclude location and ID
					{
						if (k == 0)
						{
							if(Widgets.ButtonText(new Rect(2 + k * xspacing, yoffset + i * yspacing + scroll, xspacing + headerSpacing, 30), ""))
							{  //When button of settlement name is pressed
							   //Log.Message(settlementList[i][k] + " Pressed!");
								faction.settlements[i].updateProfitAndProduction();
								Find.WindowStack.Add(new settlementWindowFC(settlement));

							}
							Widgets.Label(new Rect(2 + k * xspacing, yoffset + i * yspacing + scroll, xspacing + headerSpacing, 30), settlement.name);
						}
						else
						{
							string str;
							switch (k)
							{
								case 0:
									str = settlement.name;
									break;
								case 1:
									str = settlement.settlementLevel.ToString();
									break;
								case 2:
									str = settlement.settlementMilitaryLevel.ToString();
									break;
								case 3:
									str = settlement.unrest.ToString();
									break;
								case 4:
									str = settlement.loyalty.ToString();
									break;
								case 5:
									str = settlement.totalProfit.ToString();
									break;
								default:
									str = "ERROR";
									break;
							}

							//ist[0] = name;   //settlement name
							//list[1] = settlementLevel.ToString(); //settlement level
							//list[2] = settlementMilitaryLevel.ToString(); //settlement military level
							//list[3] = unrest.ToString(); //settlement unrest
							//list[4] = loyalty.ToString(); //settlement loyalty
							//list[5] = getTotalProfit().ToString(); //settlement profit
							//list[6] = mapLocation.ToString(); //settlement location
							//list[7] = ID
							Widgets.Label(new Rect(headerSpacing + 2 + k * xspacing, yoffset + i * yspacing + scroll, xspacing, 30), str);
						}
					}
				}
			}
			//box outline
			Widgets.DrawBox(new Rect(0, 100, 312, 264));

		}

		private void DrawFactionName(Rect inRect)
		{
			Text.Anchor = TextAnchor.MiddleLeft;
			Text.Font = GameFont.Medium;
			Widgets.Label(new Rect(7,32,200,40),faction.name);
			if(Widgets.ButtonImage(new Rect(210, 37, 20, 20), texLoad.iconCustomize))
			{ //if click faction customize button
			  //Log.Message("Faction customize clicked");
				Find.WindowStack.Add(new factionCustomizeWindowFC(faction));
			}
		}
		private void DrawFactionTitle(Rect inRect)
		{
			Text.Anchor = TextAnchor.UpperCenter;
			Text.Font = GameFont.Tiny;
			Widgets.Label(new Rect(0, 60, 200, 20), faction.title);
		}

		private void DrawFactionIcon(Rect inRect)
		{
			Widgets.ButtonImage(new Rect(245, 40, 50, 50), texLoad.iconTest100);
			
		}

		private void DrawFactionTopMenu(Rect inRect)
		{
			Widgets.DrawMenuSection(new Rect(0, 32, 312, 65));
			DrawFactionName(inRect);
			DrawFactionTitle(inRect);
			DrawFactionIcon(inRect);
		}

		private void DrawFactionMiddleMenu(Rect inRect)
		{
			DrawFactionStats(inRect, statSize);
			DrawFactionButtons(inRect, buttonSize);
		}

		private void DrawFactionBottomMenu(Rect inRect)
		{
			Text.Anchor = TextAnchor.MiddleCenter;
			Text.Font = GameFont.Small;
			Widgets.DrawMenuSection(new Rect(0, 270, 312, 90));
			Widgets.Label(new Rect(0, 270, 250, 30), "TotalProduction".Translate());
			DrawFactionResourceIcons(inRect, 0, 300, 20);

			DrawFactionEconomicStats(inRect);
		}

		private void DrawFactionStats(Rect inRect, int statSize)
		{
			Text.Anchor = TextAnchor.MiddleLeft;
			Text.Font = GameFont.Medium;
			for (int i = 0; i < stats.Count(); i++)
			{
				if (stats[i] == "happiness")
				{
					Widgets.DrawBox(new Rect(0, 105+ ((statSize + 15) * i), 125, statSize+10));
					Widgets.DrawHighlight(new Rect(0, 105 + ((statSize + 15) * i), 125, statSize + 10));
					if(Widgets.ButtonImage(new Rect(5-2, 110 + ((statSize + 15) * i)-2, statSize+4, statSize+4), texLoad.iconHappiness)) 
					{
						Find.WindowStack.Add(new descWindowFC("FactionHappinessDesc".Translate(), "FactionHappiness".Translate()));
					};
					Widgets.Label(new Rect(50, 110 + ((statSize + 15) * i), 80, statSize), Convert.ToInt32(faction.averageHappiness) + "%");
				}
				if (stats[i] == "loyalty")
				{
					Widgets.DrawBox(new Rect(0, 105 + ((statSize + 15) * i), 125, statSize + 10));
					Widgets.DrawHighlight(new Rect(0, 105 + ((statSize + 15) * i), 125, statSize+10));
					if(Widgets.ButtonImage(new Rect(5-2, 110 + ((statSize + 15) * i)-2, statSize+4, statSize+4), texLoad.iconLoyalty))
					{
						Find.WindowStack.Add(new descWindowFC("FactionLoyaltyDesc".Translate(), "FactionLoyalty".Translate()));
					};
					Widgets.Label(new Rect(50, 110 + ((statSize+15) * i), 80, statSize), Convert.ToInt32(faction.averageLoyalty) + "%");
				}
				if (stats[i] == "unrest")
				{
					Widgets.DrawBox(new Rect(0, 105 + ((statSize + 15) * i), 125, statSize + 10));
					Widgets.DrawHighlight(new Rect(0, 105 + ((statSize + 15) * i), 125, statSize + 10));
					if(Widgets.ButtonImage(new Rect(5 - 2, 110 + ((statSize + 15) * i) - 2, statSize + 4, statSize + 4), texLoad.iconUnrest))
					{
						Find.WindowStack.Add(new descWindowFC("FactionUnrestDesc".Translate(), "FactionUnrest".Translate()));
					};
					Widgets.Label(new Rect(50, 110 + ((statSize + 15) * i), 80, statSize), Convert.ToInt32(faction.averageUnrest) + "%");
				}
				if (stats[i] == "prosperity")
				{
					Widgets.DrawBox(new Rect(0, 105 + ((statSize + 15) * i), 125, statSize + 10));
					Widgets.DrawHighlight(new Rect(0, 105 + ((statSize + 15) * i), 125, statSize + 10));
					if(Widgets.ButtonImage(new Rect(5 - 2, 110 + ((statSize + 15) * i) - 2, statSize + 4, statSize + 4), texLoad.iconProsperity))
					{
						Find.WindowStack.Add(new descWindowFC("FactionProsperityDesc", "FactionProsperity".Translate()));
					};
					Widgets.Label(new Rect(50, 110 + ((statSize + 15) * i), 80, statSize), Convert.ToInt32(faction.averageProsperity) + "%");
				}
			}
			
		}

		private void DrawFactionButtons(Rect inRect, int buttonSize) //Used to draw a list of buttons from the 'buttons' list
		{
			Text.Anchor = TextAnchor.MiddleCenter;
			Text.Font = GameFont.Small;
			for (int i = 0; i < buttons.Count(); i++)
			{
				if (Widgets.ButtonText(new Rect(140, 110 + ((buttonSize + 5) * i), 170, buttonSize), buttons[i]))
				{
					if (buttons[i] == "Policies-Traits".Translate())
					{ //if click trade policy button
						//Log.Message(buttons[i]);
						Find.WindowStack.Add(new traitWindowFC());
					}
				

					if (buttons[i] == "Military".Translate())
					{
						Find.WindowStack.Add(new militaryCustomizationWindowFC());
					}

					if (buttons[i] == "Actions".Translate())
					{
						List<FloatMenuOption> list = new List<FloatMenuOption>();

						list.Add(new FloatMenuOption("TaxDeliveryMap".Translate(), delegate 
						{
							List<FloatMenuOption> list2 = new List<FloatMenuOption>();


							list2.Add(new FloatMenuOption("SetMap".Translate(), delegate
							{
								List<FloatMenuOption> settlementList = new List<FloatMenuOption>();

								foreach (Map map in Find.Maps)
								{
									if (map.IsPlayerHome == true)
									{

										settlementList.Add(new FloatMenuOption(map.Parent.LabelCap, delegate
										{
											faction.taxMap = map;
											Find.LetterStack.ReceiveLetter("Map Set!", "The tax delivery map has been set to the player colony of " + map.Parent.LabelCap + ".\n All taxes and other goods will be delivered there", LetterDefOf.NeutralEvent);
										}, MenuOptionPriority.Default, null, null, 0f, null, null
										));
									}


								}

								if (settlementList.Count == 0)
								{
									settlementList.Add(new FloatMenuOption("No valid settlements to use.", null));
								}

								FloatMenu floatMenu2 = new FloatMenu(settlementList);
								floatMenu2.vanishIfMouseDistant = true;
								Find.WindowStack.Add(floatMenu2);
							}, MenuOptionPriority.Default, null, null, 0f, null, null));

							FloatMenu floatMenu = new FloatMenu(list2);
							floatMenu.vanishIfMouseDistant = true;
							Find.WindowStack.Add(floatMenu);
						}));

						list.Add(new FloatMenuOption("SetCapital".Translate(), delegate
						{
							faction.setCapital();
						}));

						list.Add(new FloatMenuOption("ActivateResearch".Translate(), delegate
						{
							faction.updateDailyResearch();
						}));



						FloatMenu menu = new FloatMenu(list);
						Find.WindowStack.Add(menu);
					}
				}
			}
		}

		private void DrawFactionResourceIcons(Rect inRect, int x, int y, int resourceSize) //Used to draw a list of resources from the faction
		{
			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.MiddleCenter;
			int k;
			int j;
			float resourcesPerRow = 7;
			int ySpacing = 30;

			for (int i = 0; i < faction.returnNumberResource(); i++) 
			{
				k = (int)Math.Floor(i / resourcesPerRow);
				j = (int)(i % resourcesPerRow);
				if(Widgets.ButtonImage(new Rect(5 + x + (j * (resourceSize+5)), y-5 + ySpacing*k, resourceSize, resourceSize), faction.returnResourceByInt(i).getIcon()))
				{
					Find.WindowStack.Add(new descWindowFC("TotalFactionProduction".Translate() + ": " + faction.returnResourceByInt(i).name, char.ToUpper(faction.returnResourceByInt(i).name[0]) + faction.returnResourceByInt(i).name.Substring(1)));
				}
				Widgets.Label(new Rect(5 + x + (j * (resourceSize + 5)), y+resourceSize-10 + ySpacing * k, resourceSize, resourceSize), faction.returnResourceByInt(i).amount.ToString());
			}
		}

		private void DrawFactionEconomicStats(Rect inRect)
		{
			Text.Anchor = TextAnchor.MiddleCenter;
			Text.Font = GameFont.Tiny;
			Widgets.Label(new Rect(195, 270, 115, 30), "EstimatedProfit".Translate());
			Widgets.Label(new Rect(195, 300, 115, 20), Convert.ToInt32(faction.profit).ToString() + " " + "Silver".Translate().ToLower());

			Widgets.Label(new Rect(195, 315, 115, 30), "TimeTillTax".Translate() + ":");
			Widgets.Label(new Rect(195, 340, 115, 20), GenDate.ToStringTicksToDays(faction.taxTimeDue-Find.TickManager.TicksGame));
		}
	
		private void scrollWindow(float num)
		{
			if (scroll - num * 5 < -1 * maxScroll)
			{
				scroll = -1 * maxScroll;
			} else if (scroll - num*5 > 0){
				scroll = 0;
			} else
			{
				scroll -= (int)Event.current.delta.y * 5;
			}
			Event.current.Use();
		}
	
	}

}
