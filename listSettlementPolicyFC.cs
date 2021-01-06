using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using RimWorld;
using Verse;
using RimWorld.Planet;

namespace FactionColonies
{
	public class listSettlementPolicyFC : Window
	{

		public override Vector2 InitialSize
		{
			get
			{
				return new Vector2(380f, 380f);
			}
		}

		//declare variables
		public int scroll = 0;
		public int maxScroll = 0;

		private int xspacing = 60;
		private int yspacing = 30;
		private int yoffset = 90;
		private int headerSpacing = 30;
		private int length = 335;
		private int xoffset = 0;
		private int height = 200;


		public List<PolicyFCDef> list = new List<PolicyFCDef>();
		public SettlementFC settlement;
		public int policySlot;
		public PolicyFCDef def;



		public string returnPolicyTypeBySlot(int policySlot)
		{
			switch (policySlot)
			{
				case -1:
					Log.Message("policytypebyslot was given -1. report this bug");
					return "policytypebyslot was given -1";
				default:
					return "settlementTrait";
			}
		}

		public listSettlementPolicyFC(PolicyFCDef def, int policySlot, SettlementFC settlement)
		{
			List<PolicyFCDef> tmp = DefDatabase<PolicyFCDef>.AllDefsListForReading;
			repeat:
			foreach (PolicyFCDef policy in tmp)  //Remove null options 
			{
				if (policy.type == returnPolicyTypeBySlot(policySlot))
				{
					list.Add(policy);
					tmp.Remove(policy);
					goto repeat;
				}
			}

			this.forcePause = false;
			this.draggable = true;
			this.doCloseX = true;
			this.preventCameraMotion = false;
			this.settlement = settlement;
			this.policySlot = policySlot;
			this.def = def;
		}

		public override void PreOpen()
		{
			base.PreOpen();
			scroll = 0;
			maxScroll = (list.Count() * yspacing) - height;
		}

		public override void WindowUpdate()
		{
			base.WindowUpdate();
		}

		public override void DoWindowContents(Rect inRect)
		{





			//grab before anchor/font
			GameFont fontBefore = Text.Font;
			TextAnchor anchorBefore = Text.Anchor;



			//Settlement Tax Collection Header
			Text.Anchor = TextAnchor.MiddleLeft;
			Text.Font = GameFont.Medium;
			Widgets.Label(new Rect(2, 0, 300, 60), "EnactPolicy".Translate());




			//settlement buttons

			Text.Anchor = TextAnchor.MiddleCenter;
			Text.Font = GameFont.Tiny;

			//0 tithe total string
			//1 source - -1
			//2 due/delivery date
			//3 Silver (- || +)
			//4 tithe

			List<String> headerList = new List<String>() { "Name".Translate(), "Cost".Translate(), "Effect".Translate(), "Enact".Translate() };
			for (int i = 0; i < 3; i++)  //-2 to exclude location and ID
			{
				if (i == 0)
				{
					Widgets.Label(new Rect(xoffset + 2 + i * xspacing, yoffset - yspacing, xspacing + headerSpacing, yspacing), headerList[i]);
				}
				else
				{
					Widgets.Label(new Rect(xoffset + headerSpacing + 2 + i * xspacing, yoffset - yspacing, xspacing, yspacing), headerList[i]);
				}
			}

			for (int i = 0; i < list.Count(); i++) //browse through policy list
			{
				if (i * yspacing + scroll >= 0 && i * yspacing + scroll <= height)
				{
					if (i % 2 == 0)
					{
						Widgets.DrawHighlight(new Rect(xoffset, yoffset + i * yspacing + scroll, length, yspacing));
					}
					for (int k = 0; k < 4; k++)  //Browse through thing information
					{
						if (k == 0) //name of policy
						{

							Widgets.Label(new Rect(xoffset + 2 + k * xspacing, yoffset + i * yspacing + scroll, xspacing + headerSpacing, yspacing), list[i].label); //timedue is date made
						}
						else
						if (k == 1) //Cost of policy
						{
							Widgets.Label(new Rect(xoffset + headerSpacing + 2 + k * xspacing, yoffset + i * yspacing + scroll, xspacing, yspacing), list[i].cost.ToString());
						}
						else
						if (k == 2) // Desc/Effect of policy
						{

							if (Widgets.ButtonText(new Rect(xoffset + headerSpacing + 2 + k * xspacing, yoffset + i * yspacing + scroll, xspacing, yspacing), "Info".Translate()))
							{
								Find.WindowStack.Add(new descWindowFC(list[i].desc, list[i].label));
							}
						}
						else
						if (k == 3) //Build button
						{
							if (list[i].defName == def.defName && list[i].defName != "Empty")
							{
								Widgets.Label(new Rect(xoffset + headerSpacing + 2 + k * xspacing, yoffset + i * yspacing + scroll, xspacing, yspacing), "Enacted".Translate());
								if (Widgets.ButtonText(new Rect(xoffset + headerSpacing + 2 + (k + 1) * xspacing, yoffset + i * yspacing + scroll, xspacing, yspacing), "Repeal".Translate()))
								{

									settlement.repealPolicy(policySlot);
									Find.WindowStack.TryRemove(this);
									Find.WindowStack.WindowOfType<settlementWindowFC>().WindowUpdateFC();
								}
							}
							else
							{
								if (Widgets.ButtonText(new Rect(xoffset + headerSpacing + 2 + k * xspacing, yoffset + i * yspacing + scroll, xspacing, yspacing), "Enact".Translate()))
								{


									if (settlement.validEnactPolicy(list[i], i))
									{
										FCEvent tmp = new FCEvent(true);
										tmp.def = FCEventDefOf.enactSettlementPolicy;
										tmp.source = settlement.mapLocation;
										tmp.policy = list[i];
										tmp.policySlot = policySlot;
										tmp.timeTillTrigger = Find.TickManager.TicksGame + list[i].enactDuration;
										//Log.Message(list[i].enactDuration.ToString());
										//Log.Message(tmp.timeTillTrigger.ToString());
										Find.World.GetComponent<FactionFC>().addEvent(tmp);
										PaymentUtil.paySilver(Convert.ToInt32(list[i].cost));
										settlement.repealPolicy(policySlot);
										settlement.policies[policySlot] = PolicyFCDefOf.Enacting;
										Messages.Message(list[i].label + " " + "WillBeEnactedIn".Translate() + " " + GenDate.ToStringTicksToDays(tmp.timeTillTrigger - Find.TickManager.TicksGame), MessageTypeDefOf.PositiveEvent);
										Find.WindowStack.TryRemove(this);
										Find.WindowStack.WindowOfType<settlementWindowFC>().WindowUpdateFC();
									}
								}
							}
						}
						else //Catch all
						{
							Widgets.Label(new Rect(xoffset + headerSpacing + 2 + k * xspacing, yoffset + i * yspacing + scroll, xspacing, yspacing), "REPORT THIS listpolicyfc");
						}
					}
				}


			}

			Widgets.DrawBox(new Rect(xoffset, yoffset - yspacing, length, height + yspacing * 2));

			//reset anchor/font
			Text.Font = fontBefore;
			Text.Anchor = anchorBefore;

			if (Event.current.type == EventType.ScrollWheel)
			{

				scrollWindow(Event.current.delta.y);
			}
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
