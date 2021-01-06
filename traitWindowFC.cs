using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace FactionColonies
{
	public class traitWindowFC : Window
    {

		private int xspacing = 5;
		private int yspacing = 30;
		private int xoffset = 0;
		private int yoffset = 40;
		//private int headerSpacing = 60;
		private int length = 180;
		private int height = 100;
		private SettlementFC settlement = null;


		public override Vector2 InitialSize
		{
			get
			{
				return new Vector2(586f, 310f);
			}
		}

		public traitWindowFC()
		{
			this.forcePause = false;
			this.draggable = true;
			this.doCloseX = true;
			this.preventCameraMotion = false;
		}

		public List<PolicyFCDef> returnPolicies()
		{
			if (settlement == null) //if faction policy
			{
				return Find.World.GetComponent<FactionFC>().policies;
			} else
			{ //if settlement policy
				return settlement.policies;
			}
		}

		//UI STUFF
		//time variables
		private int UIUpdateTimer = 0;

		public void WindowUpdateFC()
		{
			//do updates here if need-be
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

		public override void DoWindowContents(Rect inRect)
		{




			//grab before anchor/font
			GameFont fontBefore = Text.Font;
			TextAnchor anchorBefore = Text.Anchor;

			Text.Font = GameFont.Medium;
			Text.Anchor = TextAnchor.UpperCenter;

			Widgets.Label(new Rect(0, 0, InitialSize.x, 30), "FactionPoliciesAndTraits".Translate());

			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.MiddleLeft;
			Widgets.Label(new Rect(xoffset, yoffset - 20, length, 25), "TaxLaw".Translate());
			Widgets.Label(new Rect(xoffset + (xspacing + length) * 1, yoffset - 20, length, 25), "SocialPolicy".Translate());
			Widgets.Label(new Rect(xoffset + (xspacing + length) * 2, yoffset - 20, length, 25), "MilitaryPolicy".Translate());

			Text.Font = GameFont.Tiny;
			for (int i = 0; i < 3; i++) //from first trait to third
			{
				int basex = xoffset + (xspacing + length) * i;
				int basey = yoffset;
				Widgets.DrawMenuSection(new Rect(basex, basey, length, height));
				Widgets.DrawMenuSection(new Rect(basex, basey, (length / 4) * 3, height / 4));
				Text.Anchor = TextAnchor.MiddleLeft;
				Widgets.Label(new Rect(basex+3, basey, (length / 4) * 3, height / 4), returnPolicies()[i].label);
				Text.Anchor = TextAnchor.UpperLeft;
				Widgets.Label(new Rect(basex + 3, basey + 3 + height / 4, length, (height / 4) * 3 - 3), returnPolicies()[i].desc);
				if (Widgets.ButtonInvisible(new Rect(basex, basey, length, height)))
				{ //if click on policy
					Find.WindowStack.Add(new listFactionPolicyFC(returnPolicies()[i], i, "EnactPolicy".Translate()));
				}
			}


			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.MiddleLeft;
			Widgets.Label(new Rect(xoffset, yoffset - 20 + height + yspacing, length, 25), "FactionTraits".Translate());

			Text.Font = GameFont.Tiny;
			for (int i = 0; i < 3; i++) //from fourth to sixth
			{
				int basex = xoffset + (xspacing + length) * i;
				int basey = yoffset + yspacing + height;
				Widgets.DrawMenuSection(new Rect(basex, basey, length, height));
				Widgets.DrawMenuSection(new Rect(basex, basey, (length / 4) * 3, height / 4));
				Text.Anchor = TextAnchor.MiddleLeft;
				Widgets.Label(new Rect(basex + 3, basey, (length / 4) * 3, height / 4), returnPolicies()[i+3].label);
				Text.Anchor = TextAnchor.UpperLeft;
				Widgets.Label(new Rect(basex + 3, basey + 3 + height/4, length, (height / 4) * 3 - 3), returnPolicies()[i+3].desc);
				if(Widgets.ButtonInvisible(new Rect(basex, basey, length, height)))
				{ //if click on policy
					Find.WindowStack.Add(new listFactionPolicyFC(returnPolicies()[(i + 3)], (i + 3), "AddTrait".Translate()));
				}

			}




			//reset anchor/font
			Text.Font = fontBefore;
			Text.Anchor = anchorBefore;

		}

	}
}

