﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;
using RimWorld.Planet;

namespace FactionColonies
{
    public class createColonyWindowFC : Window
    {
        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(300f, 600f);
            }
        }

        public int currentTileSelected = -1;
        public BiomeResourceDef currentBiomeSelected; //DefDatabase<BiomeResourceDef>.GetNamed(this.biome)
        public BiomeResourceDef currentHillinessSelected;
        public int timeToTravel = -1;

        public createColonyWindowFC()
        {
            this.forcePause = false;
            this.draggable = false;
            this.preventCameraMotion = false;
            this.doCloseX = true;
            this.windowRect = new Rect(UI.screenWidth - this.InitialSize.x, (UI.screenHeight - this.InitialSize.y) / 2f - (UI.screenHeight/8f), this.InitialSize.x, this.InitialSize.y);
        }



        //Pre-Opening
        public override void PreOpen()
        {

        }

        //Drawing
        public override void DoWindowContents(Rect inRect)
        {
            FactionFC faction = Find.World.GetComponent<FactionFC>();
            if (Find.WorldSelector.selectedTile != -1 && Find.WorldSelector.selectedTile != currentTileSelected)
            {
                currentTileSelected = Find.WorldSelector.selectedTile;
                //Log.Message("Current: " + currentTileSelected + ", Selected: " + Find.WorldSelector.selectedTile);
                currentBiomeSelected = DefDatabase<BiomeResourceDef>.GetNamed(Find.WorldGrid.tiles[currentTileSelected].biome.ToString(), false);
                //default biome
                if (currentBiomeSelected == default(BiomeResourceDef))
                {
                    //Log Modded Biome
                    currentBiomeSelected = BiomeResourceDefOf.defaultBiome;
                }
                currentHillinessSelected = DefDatabase<BiomeResourceDef>.GetNamed(Find.WorldGrid.tiles[currentTileSelected].hilliness.ToString());
                if (currentBiomeSelected.canSettle == true && currentHillinessSelected.canSettle == true && currentTileSelected != 1)
                {
                    timeToTravel = FactionColonies.ReturnTicksToArrive(Find.World.GetComponent<FactionFC>().capitalLocation, currentTileSelected);
                }
                else
                {
                    timeToTravel = 0;
                }
            }


            //grab before anchor/font
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;


            int silverToCreateSettlement = (int)(traitUtilsFC.cycleTraits(new double(), "createSettlementMultiplier", Find.World.GetComponent<FactionFC>().traits, "multiply") * (FactionColonies.silverToCreateSettlement + (500 * (Find.World.GetComponent<FactionFC>().settlements.Count() + Find.World.GetComponent<FactionFC>().settlementCaravansList.Count())) + (traitUtilsFC.cycleTraits(new double(), "createSettlementBaseCost", Find.World.GetComponent<FactionFC>().traits, "add"))));



            //Draw Label
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(0, 0, 268, 40), "SettleANewColony".Translate());

            //hori line
            Widgets.DrawLineHorizontal(0, 40, 300);


            //Upper menu
            Widgets.DrawMenuSection(new Rect(5, 45, 258, 220));

            DrawLabelBox(10, 50, 100, 100, "TravelTime".Translate(), GenDate.ToStringTicksToDays(timeToTravel));
            DrawLabelBox(153, 50, 100, 100, "InitialCost".Translate(), silverToCreateSettlement + " " + "Silver".Translate());


            //Lower Menu label
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(0, 270, 268, 40), "BaseProductionStats".Translate());


            //Lower menu
            Widgets.DrawMenuSection(new Rect(5, 310, 258, 220));


            //Draw production
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;

            //Production headers
            Widgets.Label(new Rect(40, 310, 60, 25), "Base".Translate());
            Widgets.Label(new Rect(110, 310, 60, 25), "Modifier".Translate());
            Widgets.Label(new Rect(180, 310, 60, 25), "Final".Translate());

            if (currentTileSelected != -1)
            {
                for (int i = 0; i < Find.World.GetComponent<FactionFC>().returnNumberResource(); i++)
                {
                    int height = 15;
                    if(Widgets.ButtonImage(new Rect(20, 335 + i * (5 + height), height, height), faction.returnResourceByInt(i).getIcon()))
                    {
                        Find.WindowStack.Add(new descWindowFC("SettlementProductionOf".Translate() + ": " + faction.returnResourceByInt(i).label, char.ToUpper(faction.returnResourceByInt(i).label[0]) + faction.returnResourceByInt(i).label.Substring(1)));
                    }
                    Widgets.Label(new Rect(40, 335 + i * (5 + height), 60, height+2), (currentBiomeSelected.BaseProductionAdditive[i] + currentHillinessSelected.BaseProductionAdditive[i]).ToString());
                    Widgets.Label(new Rect(110, 335 + i * (5 + height), 60, height+2), (currentBiomeSelected.BaseProductionMultiplicative[i] * currentHillinessSelected.BaseProductionMultiplicative[i]).ToString());
                    Widgets.Label(new Rect(180, 335 + i * (5 + height), 60, height+2), ((currentBiomeSelected.BaseProductionAdditive[i] + currentHillinessSelected.BaseProductionAdditive[i])*(currentBiomeSelected.BaseProductionMultiplicative[i] * currentHillinessSelected.BaseProductionMultiplicative[i])).ToString());
                }
            }




            //Settle button
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            int buttonLength = 130;
            if(Widgets.ButtonText(new Rect((InitialSize.x - 32 - buttonLength)/2f, 535, buttonLength, 32), "Settle".Translate() + ": (" + silverToCreateSettlement + ")")) //add inital cost
            { //if click button to settle
                if (PaymentUtil.getSilver() >= silverToCreateSettlement) //if have enough monies to make new settlement
                {
                    StringBuilder reason = new StringBuilder();
                    if (!TileFinder.IsValidTileForNewSettlement(currentTileSelected, reason) || currentTileSelected == -1 || Find.World.GetComponent<FactionFC>().checkSettlementCaravansList(currentTileSelected.ToString()))
                    {
                        //Alert Error to User
                        Messages.Message(reason.ToString() ?? "CaravanOnWay".Translate() + "!", MessageTypeDefOf.NegativeEvent);
                    }
                    else
                    {   //Else if valid tile

                        PaymentUtil.paySilver(silverToCreateSettlement);
                        //if PROCESS MONEY HERE

                        //create settle event
                        FCEvent tmp = FCEventMaker.MakeEvent(FCEventDefOf.settleNewColony);
                        tmp.location = currentTileSelected;
                        tmp.timeTillTrigger = Find.TickManager.TicksGame + timeToTravel;
                        tmp.source = Find.World.GetComponent<FactionFC>().capitalLocation;
                        Find.World.GetComponent<FactionFC>().addEvent(tmp);

                        Find.World.GetComponent<FactionFC>().settlementCaravansList.Add(tmp.location.ToString());
                        Messages.Message("CaravanSentToLocation".Translate() + " " + GenDate.ToStringTicksToDays((tmp.timeTillTrigger-Find.TickManager.TicksGame)) + "!", MessageTypeDefOf.PositiveEvent);
                        // when event activate FactionColonies.createPlayerColonySettlement(currentTileSelected);
                    }
                } else
                {  //if don't have enough monies to make settlement
                    Messages.Message("NotEnoughSilverToSettle".Translate() + "!", MessageTypeDefOf.NeutralEvent);
                }
            }

            



            //reset anchor/font
            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;
        }

        public void DrawLabelBox(int x, int y, int length, int height, string text1, string text2)
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            //Draw highlight
            Widgets.DrawHighlight(new Rect(x, y+height/8, length, height / 4));
            Widgets.Label(new Rect(x, y, length, height / 2), text1);

            //divider
            Widgets.DrawLineHorizontal(x + 5, y + height / 2, length - 10);

            //Bottom Text - Gamers Rise Up
            Widgets.Label(new Rect(x, y + height / 2, length, height / 2), text2);
        }

    }
}
