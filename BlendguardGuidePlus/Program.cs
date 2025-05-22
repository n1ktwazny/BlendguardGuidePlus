using BepInEx;
using BepInEx.Configuration;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;
using System;

[BepInPlugin("com.nikt.BlendguardGuidePlus", "Blendguard Guide+", "0.1.3")]

public class BlendguardGuidePlus : BaseUnityPlugin{
    private static ConfigEntry<bool> enableGuide;
    private static ConfigEntry<bool> enableTooltip;
    private static ConfigEntry<bool> blenderguardCompat;
    private static ConfigEntry<bool> badTowerWarning;
    private static bool blenderguardCompatDetected;
    
    void Awake(){
        enableGuide = Config.Bind("General", "Guide+", true, "Enable the Overriding of the Guide");
        enableTooltip = Config.Bind("General", "Tooltip+", true, "Enable the Overriding of the Tooltip");
        blenderguardCompat = Config.Bind("General", "Check for Blenderguard", true, "Enable to detect and show Blenderguard stats in the guide.");
        badTowerWarning = Config.Bind("General", "Bad Tower warnings", true, "Enable show the *Bad Tower* Warning");
        
        Logger.LogInfo("Guide+: Mod loaded");
        
        //Blenderguard Compat detection
        blenderguardCompatDetected = blenderguardCompat.Value;
        if (blenderguardCompatDetected){
            if (Chainloader.PluginInfos.ContainsKey("com.nikt.BlenderGuardRebalance")){
                blenderguardCompatDetected = true;
                Logger.LogInfo("Guide+: Blenderguard Compat enabled");
            }else{
                Logger.LogInfo("Guide+: Blenderguard not detected");
            }
        }
        
        Harmony.CreateAndPatchAll(typeof(BlendguardGuidePlus));
    }

    static void Main(){}
    
    // Text formatting for the guide and bottom-left tooltip-like menu
    private static string NewGuideFormat(bool smallUi, string name, int hp, int regen, int damage, float AtkSpeed, int generation){
        string text;
        float tempDps = Mathf.Round(damage / AtkSpeed * 100) / 100;
        if (smallUi){  // Tooltip Format
            text = "Max HP: " + hp + " (Regen: " + regen + ")";
            if (damage > 0){
                text += "\n\nDamage: " + damage + " (Rate: " + AtkSpeed + "s)\nDPS: " + tempDps;
            }
        }else{  // Guide Format 
            text = "Max HP: " + hp + "\nRegen: " + regen + "\nFull HP in: " + (Mathf.Ceil(hp / regen)) + " secs";
            if (damage > 0){
                text += "\n\nDamage: " + damage + "\nFire Rate: " + AtkSpeed + "s\nDPS: " + tempDps;
            }
        }

        if (generation > 0){
            text += "\n\nQ Gen: " + generation + "/sec; " + generation * 60 + "/min";
        }
        
        if (badTowerWarning.Value && ((damage <= 0 && generation <= 0) || name == "Guardian" || name == "Sentinel"|| name == "Vanguard")){
            text += "\n\n(This tower is kinda bad :P)";
        }

        //BlenderGuard compat
        if (blenderguardCompatDetected && (name == "Conduit"|| name == "Harvester"|| name == "Reaper")){
            int onKillEarn = 0;
            switch (name){
                case "Conduit":
                    onKillEarn = 30;
                    break;
                case "Harvester":
                    onKillEarn = 110;
                    break;
                case "Reaper":
                    onKillEarn = 400;
                    break;
            }
            text += "\n\nQ Gen: " + onKillEarn + "/kill";
        }
        return text;
    }

    //Override for the mini UI in-game
    [HarmonyPatch(typeof(UIManager), "GetInfoFormat")]
    [HarmonyPatch(new Type[] { typeof(StructureInfo) })]
    [HarmonyPrefix]
    static bool GetInfoFormat(StructureInfo structureInfo, ref string __result){
        if (enableTooltip.Value){
            __result = NewGuideFormat(true, structureInfo.structureName, structureInfo.maxHealth, structureInfo.regeneration, structureInfo.damage, structureInfo.fireRate, structureInfo.generation);
            return false;
        }
        return true;
    }

    //Override for the Guide Menu
    [HarmonyPatch(typeof(StructureInfoDisplay), "GetFormattedInfo")]
    [HarmonyPatch(new Type[] { typeof(StructureInfo) })]
    [HarmonyPrefix]
    static bool GetFormattedInfo(StructureInfo structureInfo, ref string __result){
        if (enableGuide.Value){
            __result = NewGuideFormat(false, structureInfo.structureName, structureInfo.maxHealth, structureInfo.regeneration, structureInfo.damage,
                structureInfo.fireRate, structureInfo.generation);
            return false;
        }
        return true;
    }
}