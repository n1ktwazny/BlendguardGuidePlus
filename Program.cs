using BepInEx;
using BepInEx.Configuration;
using BepInEx.Bootstrap;
using HarmonyLib;
using UnityEngine;
using System;

[BepInPlugin("com.nikt.BlendguardGuidePlus", "Blendguard Guide+", "0.2.2")]

public class BlendguardGuidePlus : BaseUnityPlugin{
    private static ConfigEntry<bool> enableTowerGuide;
    private static ConfigEntry<bool> enableTowerTooltip;
    private static ConfigEntry<bool> enableInvaderGuide;
    private static ConfigEntry<bool> blenderguardianCompat;
    private static ConfigEntry<bool> noSpeedWarning;
    private static ConfigEntry<bool> badTowerWarning;
    private static bool bGuardianCompat;
    private static bool generalFixesInstalled;
    
    void Awake(){
        enableTowerGuide = Config.Bind("General", "Tower Guide+", true, "Enable the Overriding of the Tower Guide");
        enableTowerTooltip = Config.Bind("General", "Tower Tooltip+", true, "Enable the Overriding of the Tower Tooltip");
        enableInvaderGuide = Config.Bind("General", "Invader Guide+", true, "Enable the Overriding of the Invader Guide");
        blenderguardianCompat = Config.Bind("General", "Check for Blenderguard", true, "Enable to detect and show Blenderguard stats in the guide.");
        noSpeedWarning = Config.Bind("General", "No Speed Warning", true, "Enable the 'speed not implemented' warning(Blendguard without the General Fixes mod does implement the speed of invaders)");
        badTowerWarning = Config.Bind("General", "Bad Tower warnings", true, "Enable show the *Bad Tower* Warning");
        
        Logger.LogInfo("Guide+: Mod loaded");
        
        Harmony.CreateAndPatchAll(typeof(BlendguardGuidePlus));
    }

    private void Start(){
        //Blenderguard Compat detection
        bGuardianCompat = blenderguardianCompat.Value;
        if (bGuardianCompat){
            if (Chainloader.PluginInfos.ContainsKey("com.nikt.BlendGuardRebalance")){
                bGuardianCompat = true;
                Logger.LogInfo("Guide+: Blendguard rebalanced Compat enabled");
            }else{
                Logger.LogInfo("Guide+: Blendguard rebalanced not detected");
            }
        }

        if (Chainloader.PluginInfos.ContainsKey("com.nikt.BlendguardGFixes")){
            generalFixesInstalled = true;
        }else{
            generalFixesInstalled = false;
        }
    }

    static void Main(){}
    
    // Text formatting for the guide and bottom-left tooltip-like menu
    private static string NewTowerGuideFormat(bool smallUi, string name, int hp, int regen, int damage, float AtkSpeed, int generation){
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
        return text;
    }
    private static string NewInvaderGuideFormat(bool smallUi, string name, int hp, int damage, float speed, string focus, bool canFly){
        string text;
        text = $"Max HP: {hp}";
        if (generalFixesInstalled){
            string spd = "??? (mod broke, report pls)";
            switch(speed){
                case 1f:
                    spd = "Normal";
                    break;
                case 2f:
                    spd = "Fast";
                    break;
                case 0.5f:
                    spd = "Slow";
                    break;
            }
            text += $"\nSpeed: {spd}";
        }else{
            text += "\n";
            if (noSpeedWarning.Value){
                text += "Speed: Normal*";
            }
        }
        if (canFly){
            text += "(Flying)";
        }
        text += $"\n\nFocuses: {focus} \nDamage: {damage} ";
        
        if (!generalFixesInstalled && noSpeedWarning.Value){
            text += "\n\nInstall 'General Fixes' mod for speed <3";
        }
        return text;
    }

    //Override for the Towers' mini UI in-game
    [HarmonyPatch(typeof(UIManager), "GetInfoFormat")]
    [HarmonyPatch(new Type[] { typeof(StructureInfo) })]
    [HarmonyPrefix]
    static bool GetTowerInfoFormat(StructureInfo structureInfo, ref string __result){
        if (enableTowerTooltip.Value){
            __result = NewTowerGuideFormat(true, structureInfo.structureName, structureInfo.maxHealth, structureInfo.regeneration, structureInfo.damage, structureInfo.fireRate, structureInfo.generation);
            return false;
        }
        return true;
    }
    //Override for the Tower Guide Menu
    [HarmonyPatch(typeof(StructureInfoDisplay), "GetFormattedInfo")]
    [HarmonyPatch(new Type[] { typeof(StructureInfo) })]
    [HarmonyPrefix]
    static bool GetTowerInfo(StructureInfo structureInfo, ref string __result){
        if (enableTowerGuide.Value){
            __result = NewTowerGuideFormat(false, structureInfo.structureName, structureInfo.maxHealth, structureInfo.regeneration, structureInfo.damage, structureInfo.fireRate, structureInfo.generation);
            return false;
        }
        return true;
    }
    
    //Override for the Invader Guide Menu
    [HarmonyPatch(typeof(InvaderInfoDisplay), "GetFormattedInfo")]
    [HarmonyPatch(new Type[] { typeof(InvaderInfo) })]
    [HarmonyPrefix]
    static bool GetInvaderInfo(InvaderInfo invaderInfo, ref string __result){
        if (enableInvaderGuide.Value){
            __result = NewInvaderGuideFormat(false, invaderInfo.invaderName, invaderInfo.maxHealth, invaderInfo.attackPower, invaderInfo.speed, invaderInfo.targetFocus, invaderInfo.canFly);
            return false;
        }
        return true;
    }
}