using HarmonyLib;
using UnityEngine;

// ReSharper disable InconsistentNaming

namespace JetIslandArchipelago.Patches;
[HarmonyPatch(typeof(MonsterScript))]
public class MonsterScriptPatches
{
    private static readonly int[] bossBeat = new int[4];
    [HarmonyPrefix]
    [HarmonyPatch("Update")]
    static void PrefixMonsterScriptUpdate(MonsterScript __instance)
    {
        for (int i = 0; i < 4; i++)
            bossBeat[i] = PlayerPrefs.GetInt($"{SaveData.FakeProfile}Miniboss{i + 1}Beat", 0);
        
        PlayerPrefs.SetInt($"{SaveData.FakeProfile}Miniboss4Beat", (bossBeat[3] == 1 && SaveData.Instance.HookshotReel) ? 1 : 0);
        PlayerPrefs.SetInt($"{SaveData.FakeProfile}Miniboss1Beat", (bossBeat[0] == 1 && SaveData.Instance.LongShot) ? 1 : 0);
        PlayerPrefs.SetInt($"{SaveData.FakeProfile}Miniboss2Beat", (bossBeat[1] == 1 && SaveData.Instance.BunnyHop) ? 1 : 0);
        PlayerPrefs.SetInt($"{SaveData.FakeProfile}Miniboss3Beat", (bossBeat[2] == 1 && SaveData.Instance.SuperShot) ? 1 : 0);
        
        if (!__instance.defeated || MonsterScript.currentPhase < __instance.phases.Length - 1) return;
        ArchipelagoWrapper.Instance.Release();
    }

    [HarmonyPostfix]
    [HarmonyPatch("Update")]
    static void PostfixMonsterScriptUpdate(MonsterScript __instance)
    {
        for (int i = 0; i < 4; i++)
        {
            PlayerPrefs.SetInt($"{SaveData.FakeProfile}Miniboss{i+1}Beat", bossBeat[i]);
        }
    }
}