using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Assets.Scripts.Networking;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System;

namespace ItemStatDisplay
{

    static class Consts
    {
        public const string guid = "com.isse.item_stat_display";
    }


    [BepInPlugin(Consts.guid, "Item Stat Display", "1.0")]
    public class MainClass : BaseUnityPlugin
    { 

        public static MainClass instance;
        public Harmony harmony;
        public ManualLogSource log;


        void Awake()
        {
            if (!instance) 
                instance = this;
            else
                Destroy(this);

            log = Logger;
            harmony = new Harmony(Consts.guid);


            harmony.PatchAll(typeof(PowerUpUIPatch));
            harmony.PatchAll(typeof(ExtraPowerupInfo));

            log.LogInfo("Item Stat Display Added!");
            
        }
    }

    static class LocalInventory
    {
        public static Dictionary<int, int> inventory;
        static LocalInventory()
        {
            inventory = new Dictionary<int, int>();
        }
    }

    class PowerUpUIPatch
    {
        [HarmonyPatch(typeof(PowerupUI), "AddPowerup")]
        [HarmonyPostfix]
        public static void Postfix(PowerupUI __instance, int powerupId)
        {
            if (LocalInventory.inventory.ContainsKey(powerupId))
            {
                LocalInventory.inventory[powerupId]++;
            }
            else
            {
                LocalInventory.inventory.Add(powerupId, 1);
            }
        }
    }

    public static class Helper
    {
        public static float CD(int amount, float scaleSpeed, float maxValue)
        {
            float f = 2.71828f;
            return (1f - Mathf.Pow(f, (float)(-(float)amount) * scaleSpeed)) * maxValue;
        }

        public static string Percent(float f)
        {
            return (f * 100f).ToString("N0") + "%";
        }
    }

    public static class ExtraPowerupInfo
    {
        static ExtraPowerupInfo()
        {
            infoFunctions = new Dictionary<string, Func<int, string>>();

            AddDefault();
        }
        static void AddDefault()
        {
            Add("Dumbbell", (amount) => "+" + (amount / 10f).ToString("N1") + " strength");
            Add("Berserk", (amount) => "+" + (amount / 100f).ToString("N2") + " strength per % of missing hp");
            Add("Peanut Butter", (amount) => "+" + (amount * 0.15f).ToString("N2") + " stamina");
            Add("Adrenaline", (amount) => "While active gain " + Helper.Percent(Helper.CD(amount, 1f, 2f)) + " stamina, movementspeed and attackspeed");
            Add("Sneaker", (amount) => Helper.Percent(1f + Helper.CD(amount, 0.08f, 1.75f)) + " movement speed");
            Add("Sniper Scope", (amount) => Helper.Percent(Helper.CD(amount, 0.14f, 0.15f)) + " chance to deal " + Helper.Percent(Helper.CD(amount, 0.25f, 50)) + " damage");
            Add("Blue Pill", (amount) => "+" + (amount * 10) + " shield");
            Add("Robin Hood Hat", (amount) => Helper.Percent(1f + Helper.CD(amount, 0.06f, 2f)) + " faster bow draw speed, faster arrow speed and arrow damage");
            Add("Checkered Shirt", (amount) => Helper.Percent(1 + Helper.CD(amount, 0.3f, 4f)) + " resource damage");
            Add("Dracula", (amount) => "Gain " + amount + " max hp on kill, stacks up to " + (amount * 40) + " bonus max hp");
            Add("Piggybank", (amount) => Helper.Percent((1f + Helper.CD(amount, 0.15f, 1.25f))) + " loot dropped from mobs and resources");
            Add("Knuts Hammer", (amount) => Helper.Percent(Helper.CD(amount, 0.12f, 0.4f)) + " chance to summon lightning that deals +" + Helper.Percent(2f + Helper.CD(amount, 0.12f, 1f)) + " of the damage dealt");
            Add("Crimson Dagger", (amount) => "Heal for " + Helper.Percent(Helper.CD(amount, 0.1f, 0.5f)) + " of damage dealt");
            Add("Bulldozer", (amount) => Helper.Percent(Helper.CD(amount, 0.15f, 1f)) + " chance to knock back target");
            Add("Jetpack", (amount) => Helper.Percent(1f + Helper.CD(amount, 0.075f, 2.5f)) + " jump height");
            Add("Juice", (amount) => Helper.Percent(1f + Helper.CD(amount, 0.3f, 1f)) + " attack speed for 2 seconds after a crit");
            Add("Spooo Bean", (amount) => Helper.Percent((1f - Helper.CD(amount, 0.2f, 0.5f))) + " hunger drain");
            Add("Red Pill", (amount) => "+" + (amount * 10) + " max hp");
            Add("Broccoli", (amount) => (100 + amount * 5) + "% healing");
            Add("Wings of Glory", (amount) => Helper.Percent((1f + Helper.CD(amount, 0.45f, 2.5f))) + " damage while falling");
            Add("Janniks Frog", (amount) => amount + " extra jumps");
            Add("Enforcer", (amount) => "+" + (5f * Helper.CD(amount, 0.4f, 2f)).ToString("N1") + "% damage for each unit moved per second");
            Add("Danis Milk", (amount) => "+" + (Helper.CD(amount, 0.1f, 40f)).ToString("N1") + " bonus armor");
            Add("Horseshoe", (amount) => "+" + Helper.Percent(Helper.CD(amount, 0.08f, 0.9f)) + " crit chance");
            Add("Orange Juice", (amount) => Helper.Percent((1f + Helper.CD(amount, 0.12f, 1f))) + " attack speed");
        }

        public static void Add(string name, Func<int, string> function)
        {
            if (infoFunctions.ContainsKey(name))
            {
                infoFunctions[name] = function;
            }
            else
            {
                infoFunctions.Add(name, function);
            }
        }

        static Dictionary<string, Func<int, string>> infoFunctions;



        static int Count(string name)
        {
            if (LocalInventory.inventory.TryGetValue(ItemManager.Instance.stringToPowerupId[name], out int count))
            {
                return count;
            }
            return 0;
        }

        static string GetStatInfo(string name)
        {
            if (infoFunctions.TryGetValue(name, out var func))
            {
                return func(Count(name));
            }
            return "NOT IMPLEMENTED";
        }
        
        
        [HarmonyPatch(typeof(PowerupInfo), "OnPointerEnter")]
        [HarmonyPostfix]
        static void Postfix(PowerupInfo __instance, PointerEventData eventData)
        {
            MainClass.instance.log.LogInfo(__instance.gameObject.transform.parent.gameObject.name);
            string statInfo = GetStatInfo(__instance.powerup.name);

            ItemInfo.Instance.SetText(__instance.powerup.name 
                                      + "\n<size=50%><i>" + __instance.powerup.description 
                                      + "</i></size>\n\n<size=60%>" + statInfo, true);
        }
    }
}
