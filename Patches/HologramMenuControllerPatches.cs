using System;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JetIslandArchipelago.Patches;

[HarmonyPatch(typeof(HologramMenuController))]
public class HologramMenuControllerPatches
{
    [HarmonyPostfix]
    [HarmonyPatch("Update")]
    static void PostfixUpdate(HologramMenuController __instance)
    {
        if (__instance.currentMenu == HologramButtonScript.Menu.SelectProfile)
        {
            __instance.currentMenu = HologramButtonScript.Menu.newProfile;
            HologramButtonScriptPatches._headerText.text = "Archipelago Host:";
            if (ArchipelagoWrapper.Instance.Connected)
            {
                GameObject tmpGameObject = new GameObject();
                HologramButtonScript hologramButtonScript = tmpGameObject.AddComponent<HologramButtonScript>();
                hologramButtonScript.buttonFunction = HologramButtonScript.ButtonFunction.SelectProfile;
                hologramButtonScript.selectProfile_profileInt = SaveData.FakeProfile;
                hologramButtonScript.PressButton();
                
                hologramButtonScript.buttonFunction = HologramButtonScript.ButtonFunction.PlayOffline;
                hologramButtonScript.playOffline_IfPlayerPrefOpenMenu = String.Empty;
                hologramButtonScript.playOffline_Scene = "JetIsland Main Scene";
                hologramButtonScript.PressButton();
                Object.Destroy(tmpGameObject);
            }
        }
    }
}