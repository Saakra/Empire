using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace FactionColonies
{
    public class ResourceFC : IExposable
    {
        public ResourceFC()
        {
        }

        public ResourceFC(string name, string label, double baseProduction)
        {
            this.name = name;
            this.label = label;
            this.baseProduction = baseProduction;
            this.endProduction = baseProduction;
            this.amount = 0;
            this.baseProductionMultiplier = 1;
            this.baseProductionAdditives.Add(new ProductionAdditive("", 0, ""));
            this.baseProductionMultipliers.Add(new ProductionMultiplier("", 0, ""));
        }


        public Texture2D getIcon()
        {
            for(int i = 0; i < texLoad.textures.Count(); i++)
            {
                if (texLoad.textures[i].Key == name)
                {
                    return texLoad.textures[i].Value;
                }
            }
            return null;
        }


        public void ExposeData()
        {
            Scribe_Values.Look<string>(ref name, "name");
            Scribe_Values.Look<string>(ref label, "label");
            Scribe_Values.Look<double>(ref baseProduction, "baseProduction");
            Scribe_Values.Look<double>(ref endProduction, "endProduction");
            Scribe_Values.Look<double>(ref baseProductionMultiplier, "baseProductionMultiplier");
            Scribe_Values.Look<double>(ref endProductionMultiplier, "endProductionMultiplier");
            Scribe_Values.Look<double>(ref amount, "amount");
            Scribe_Collections.Look<ProductionAdditive>(ref baseProductionAdditives, "baseProductionAdditives", LookMode.Deep);
            Scribe_Collections.Look<ProductionMultiplier>(ref baseProductionMultipliers, "baseProductionMultipliers", LookMode.Deep);

            //tithe and income data
            Scribe_Values.Look<bool>(ref isTithe, "isTithe");
            Scribe_Values.Look<bool>(ref isTitheBool, "isTitheBool");
            Scribe_Values.Look<int>(ref assignedWorkers, "assignedWorkers");
        }

        public string name;
        public string label;
        public double baseProduction = 0; //base production for resource
        public double endProduction = 0;  //production after modifiers
        public double baseProductionMultiplier = 1;  //base production modifier for resource
        public double endProductionMultiplier = 1;  //end production modifier for resource
        public List<ProductionAdditive> baseProductionAdditives = new List<ProductionAdditive>();    // {ID, Value, Desc}
        public List<ProductionMultiplier> baseProductionMultipliers = new List<ProductionMultiplier>();  // {ID, Value, Desc}
        public double amount;
        public int assignedWorkers = 0;
        public bool isTithe = false;
        public bool isTitheBool = false; //used to track if isTithe is changed. AGHHH

    }

    public class ProductionAdditive : IExposable
    {
        public ProductionAdditive()
        {

        }

        public ProductionAdditive(string id, double value, string desc)
        {
            this.id = id;
            this.value = value;
            this.desc = desc;
        }

        public void ExposeData()
        {
            Scribe_Values.Look<string>(ref id, "id");
            Scribe_Values.Look<double>(ref value, "value");
            Scribe_Values.Look<string>(ref desc, "desc");
        }

        public string id;
        public double value;
        public string desc;
    }

    public class ProductionMultiplier : IExposable
    {
        public ProductionMultiplier()
        {

        }
        public ProductionMultiplier(string id, double value, string desc)
        {
            this.id = id;
            this.value = value;
            this.desc = desc;
        }

        public void ExposeData()
        {
            Scribe_Values.Look<string>(ref id, "id");
            Scribe_Values.Look<double>(ref value, "value");
            Scribe_Values.Look<string>(ref desc, "desc");
        }

        public string id;
        public double value;
        public string desc;
    }
}
