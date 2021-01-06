using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace FactionColonies
{
    public class PolicyFCDef : Def, IExposable
    {

        public PolicyFCDef()
        {
        }

        public void ExposeData()
        {

            Scribe_Values.Look<string>(ref desc, "desc");
            Scribe_Values.Look<double>(ref cost, "cost");
            Scribe_Values.Look<TechLevel>(ref techLevel, "techLevel");
            Scribe_Values.Look<string>(ref type, "type");
            Scribe_Values.Look<int>(ref enactDuration, "enactDuration");
            Scribe_Collections.Look<FCTraitEffectDef>(ref traits, "traits", LookMode.Def);
        }

        public string desc;
        public double cost;
        public int enactDuration;
        public string type;
        public TechLevel techLevel;
        public List<FCTraitEffectDef> traits;
        //public required research
    }

    [DefOf]
    public class PolicyFCDefOf
    {
        public static PolicyFCDef Empty;
        public static PolicyFCDef Enacting;
        static PolicyFCDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(PolicyFCDefOf));
        }
    }

}
