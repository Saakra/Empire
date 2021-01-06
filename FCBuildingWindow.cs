﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RimWorld;
using Verse;
using RimWorld.Planet;

namespace FactionColonies
{
    class FCBuildingWindow : Window
    {

        SettlementFC settlement;
        int buildingSlot;
        BuildingFCDef buildingDef;
        List<BuildingFCDef> buildingList;

        int scroll;
        int maxScroll;

        static int rowHeight = 90;
        int scrollHeight = 350;

        Rect TopWindow = new Rect(0, 0, 400, 150);
        Rect TopIcon = new Rect(15, 60, 64, 64);
        Rect TopName = new Rect(15, 15, 370, 30);
        Rect TopDescription = new Rect(95, 60, 305, 90);

        Rect BaseBuildingWindow = new Rect(0, 150, 400, rowHeight);
        Rect BaseBuildingIcon = new Rect(8, 150 + 8, 64, 64);
        Rect BaseBuildingLabel = new Rect(80, 150 + 5, 320, 20);
        Rect BaseBuildingDesc = new Rect(80, 150 + 25, 320, 65);

        Rect newBuildingWindow;
        Rect newBuildingIcon;
        Rect newBuildingLabel;
        Rect newBuildingDesc;

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(436f, 536f);
            }
        }


        public override void DoWindowContents(Rect inRect)
        {
            //grab before anchor/font
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;





            //Buildings
            int i = 0;
            foreach (BuildingFCDef building in buildingList)
            {
                newBuildingWindow = new Rect(BaseBuildingWindow.x, BaseBuildingWindow.y + (i * (rowHeight)) + scroll, BaseBuildingWindow.width, BaseBuildingWindow.height);
                newBuildingIcon = new Rect(BaseBuildingIcon.x, BaseBuildingIcon.y + (i * (rowHeight)) + scroll, BaseBuildingIcon.width, BaseBuildingIcon.height);
                newBuildingLabel = new Rect(BaseBuildingLabel.x, BaseBuildingLabel.y + (i * (rowHeight)) + scroll, BaseBuildingLabel.width, BaseBuildingLabel.height);
                newBuildingDesc = new Rect(BaseBuildingDesc.x, BaseBuildingDesc.y + (i * (rowHeight)) + scroll, BaseBuildingDesc.width, BaseBuildingDesc.height);

                if (Widgets.ButtonInvisible(newBuildingWindow))
                {
                    //If click on building
                    List<FloatMenuOption> list = new List<FloatMenuOption>();

                    if(building == buildingDef)
                    {
                        //if the same building
                        list.Add(new FloatMenuOption("Destroy".Translate(), delegate
                        {
                            settlement.deconstructBuilding(buildingSlot);
                            Find.WindowStack.TryRemove(this);
                            Find.WindowStack.WindowOfType<settlementWindowFC>().WindowUpdateFC();
                        }));
                    } else
                    {
                        //if not the same building
                        list.Add(new FloatMenuOption("Build".Translate(), delegate
                        {
                            if( settlement.validConstructBuilding(building, buildingSlot, settlement))
                            {
                                FCEvent tmpEvt = new FCEvent(true);
                                tmpEvt.def = FCEventDefOf.constructBuilding;
                                tmpEvt.source = settlement.mapLocation;
                                tmpEvt.building = building;
                                tmpEvt.buildingSlot = buildingSlot;
                                tmpEvt.timeTillTrigger = Find.TickManager.TicksGame + building.constructionDuration;
                                Find.World.GetComponent<FactionFC>().addEvent(tmpEvt);

                                PaymentUtil.paySilver(Convert.ToInt32(building.cost));
                                settlement.deconstructBuilding(buildingSlot);
                                Messages.Message(building.label + " " + "WillBeConstructedIn".Translate() + " " + GenDate.ToStringTicksToDays(tmpEvt.timeTillTrigger - Find.TickManager.TicksGame), MessageTypeDefOf.PositiveEvent);
                                settlement.buildings[buildingSlot] = BuildingFCDefOf.Construction;
                                Find.WindowStack.TryRemove(this);
                                Find.WindowStack.WindowOfType<settlementWindowFC>().WindowUpdateFC();
                            }
                        }));
                    }



                    FloatMenu menu = new FloatMenu(list);
                    Find.WindowStack.Add(menu);
                }


                Widgets.DrawMenuSection(newBuildingWindow);
                Widgets.DrawMenuSection(newBuildingIcon);
                Widgets.DrawLightHighlight(newBuildingIcon);
                Widgets.ButtonImage(newBuildingIcon, building.icon);

                Text.Font = GameFont.Small;
                Widgets.ButtonTextSubtle(newBuildingLabel, "");
                Widgets.Label(newBuildingLabel, "  " + building.LabelCap + " - " + "Cost".Translate() + ": " + building.cost);

                Text.Font = GameFont.Tiny;
                Widgets.Label(newBuildingDesc, building.desc);



                i++;
            }


            //Top Window 
            Widgets.DrawMenuSection(TopWindow);
            Widgets.DrawHighlight(TopWindow);
            Widgets.DrawMenuSection(TopIcon);
            Widgets.DrawLightHighlight(TopIcon);

            Widgets.DrawBox(new Rect(0, 0, 400, 500));
            Widgets.ButtonImage(TopIcon, buildingDef.icon);

            Widgets.ButtonTextSubtle(TopName, "");
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(new Rect(TopName.x + 5, TopName.y, TopName.width, TopName.height), buildingDef.LabelCap);

            Widgets.DrawMenuSection(new Rect(TopDescription.x - 5, TopDescription.y - 5, TopDescription.width, TopDescription.height));
            Text.Font = GameFont.Small;
            Widgets.Label(TopDescription, buildingDef.desc);

            Widgets.DrawLineHorizontal(0, TopWindow.y + TopWindow.height, 400);









            //reset anchor/font
            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;

            if (Event.current.type == EventType.ScrollWheel)
            {
                scrollWindow(Event.current.delta.y);
            }
        }


        public FCBuildingWindow(SettlementFC settlement, int buildingSlot)
        {
            buildingList = new List<BuildingFCDef>();
            foreach (BuildingFCDef building in DefDatabase<BuildingFCDef>.AllDefsListForReading)
            {
                if(building.defName != "Empty" && building.defName != "Construction")
                {
                    //If not a building that shouldn't appear on the list
                    if (building.techLevel <= Find.World.GetComponent<FactionFC>().techLevel)
                    {
                        //If building techlevel requirement is met
                        if (building.applicableBiomes.Count() == 0 || (building.applicableBiomes.Count() > 0 && building.applicableBiomes.Contains(settlement.biome))){
                            //If building meets the biome requirements
                            buildingList.Add(building);
                        }
                    }
                }
            }

            buildingList.Sort(FactionColonies.CompareBuildingDef);

            this.forcePause = false;
            this.draggable = true;
            this.doCloseX = true;
            this.preventCameraMotion = false;

            this.settlement = settlement;
            this.buildingSlot = buildingSlot;
            this.buildingDef = settlement.buildings[buildingSlot];
        }

        public override void PreOpen()
        {
            base.PreOpen();
            scroll = 0;
            maxScroll = (buildingList.Count() * rowHeight) - scrollHeight;
        }




        private void scrollWindow(float num)
        {
            if (scroll - num * 10 < -1 * maxScroll)
            {
                scroll = -1 * maxScroll;
            }
            else if (scroll - num * 10 > 0)
            {
                scroll = 0;
            }
            else
            {
                scroll -= (int)Event.current.delta.y * 10;
            }
            Event.current.Use();
        }
    }
}
