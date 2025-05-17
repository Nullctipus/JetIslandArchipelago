using System;
using System.Reflection;
using System.Text;
using HarmonyLib;
using JetIslandArchipelago.UI;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

// ReSharper disable InconsistentNaming

namespace JetIslandArchipelago.Patches;

[HarmonyPatch(typeof(PlayerBody))]
public class PlayerBodyPatches
{
    [HarmonyPrefix]
    [HarmonyPatch("Start")]
    static void PrefixPlayerBody(PlayerBody __instance)
    {
        PhotonView pv = PhotonView.Get(__instance.gameObject);
        if (StartGameScript.playingOnline && pv && !pv.isMine) return;
        PlayerBody.localPlayer = __instance;
        Plugin.Logger.LogDebug("Patching player body");
        var player = __instance;

        PlayerPrefs.SetString($"{SaveData.FakeProfile}ProfileName", ArchipelagoWrapper.Instance.Slot);
        player.playerProfile.profileName = ArchipelagoWrapper.Instance.Slot;

        PlayerPrefs.SetInt("CurrentAndLastUsedProfile", SaveData.FakeProfile);
        for (int i = 0; i < PlayerBody.upgradeBotTypes.Length; i++)
            PlayerPrefs.SetInt($"{SaveData.FakeProfile}UpgradeBot{i}", 0);

        for (int i = 0; i < __instance.modifiers.modifierUnlockStrings.Length; i++)
            PlayerPrefs.SetInt($"{SaveData.FakeProfile}ModifierUnlocked{player.modifiers.modifierUnlockStrings[i]}", 0);

        for (int i = 0; i < 4; i++)
        {
            PlayerPrefs.SetInt($"{SaveData.FakeProfile}Miniboss{i + 1}BeatAtLeastOnce", 0);
            PlayerPrefs.SetInt($"{SaveData.FakeProfile}Miniboss{i + 1}Beat", 0);
        }

        PlayerPrefs.SetInt($"{SaveData.FakeProfile}FinalBossBeat", 0);
        if (ArchipelagoWrapper.CheckedLocations.Count == 0)
            for (int i = 0; i < __instance.respawning.respawnPoints.Length; i++)
            {
                PlayerPrefs.SetInt($"{SaveData.FakeProfile}CheckpointGotten{i}", 0);

            }
        __instance.respawning.lastGottenCheckpoint = ArchipelagoWrapper.Instance.GetDataStorage("Checkpoint", SaveData.Instance.StartingCheckpoint);
        PlayerPrefs.SetInt(SaveData.FakeProfile + "LastCheckpointGotten",
            __instance.respawning.currentlyCheckingRespawnPoint);

        if (__instance.modifiers.modifierUnlockStrings == null || __instance.modifiers.modifierUnlockStrings.Length == 0)
        {
            __instance.modifiers.modifierUnlockStrings =
                Object.FindObjectOfType<StartGameScript>().modifierUnlockStrings;
        }
        
        __instance.modifiers.gottenModifiers = new bool[__instance.modifiers.modifierOrbVectors.Length];
        
        foreach (var kvp in ArchipelagoWrapper.CheckedLocations)
            SaveData.Instance.OnCheck(kvp.Key, kvp.Value.Item1, kvp.Value.Item2);

        PlayerPrefs.SetInt($"{SaveData.FakeProfile}Head", Random.Range(0, player.body.headPrefabs.Length));
        PlayerPrefs.SetInt($"{SaveData.FakeProfile}Torso", Random.Range(0, player.body.torsoPrefabs.Length));
        PlayerPrefs.SetInt($"{SaveData.FakeProfile}Board", Random.Range(0, player.body.hoverboardPrefabs.Length));
        PlayerPrefs.SetInt($"{SaveData.FakeProfile}BodyMaterialColor",
            Random.Range(0, player.body.bodyColorMaterials.Length));
        PlayerPrefs.SetInt($"{SaveData.FakeProfile}BodyMaterialFill",
            Random.Range(0, player.body.bodyFillMaterials.Length));
    }

    [HarmonyPostfix]
    [HarmonyPatch("Start")]
    internal static void PostfixPlayerBody(PlayerBody __instance)
    {
        if (!__instance.pvIsMine) return;
        if (!PlayerBody.localPlayer) return;
        var player = __instance;
        player.tutorial.tutorialOver = true;

        StringBuilder sb = new();
        foreach (var n in __instance.modifiers.modifierDisplayStrings)
        {
            sb.AppendLine(n);
        }

        sb.AppendLine();
        foreach (var n in __instance.modifiers.modifierUnlockStrings)
        {
            sb.AppendLine(n);
        }
        Plugin.Logger.LogDebug(sb.ToString());

        int checkpoint = ArchipelagoWrapper.Instance.GetDataStorage("Checkpoint", SaveData.Instance.StartingCheckpoint);
        __instance.respawning.lastGottenCheckpoint = checkpoint;
        MainThreadHelper.Enqueue(() =>
        {
            SaveData.Instance.CreateModifiers();
            SaveData.Instance.SyncActiveModifiers();
            PlayerPrefs.SetInt(SaveData.FakeProfile + "LastCheckpointGotten",
                __instance.respawning.currentlyCheckingRespawnPoint);
            __instance.Respawn();
        });
    }

    [HarmonyPrefix]
    [HarmonyPatch("SetupModifiers")]
    static bool PrefixSetupModifiers(PlayerBody __instance)
    {
        if (!__instance.pvIsMine) return true;
        return false;
    }

    [HarmonyPrefix]
    [HarmonyPatch("PerformActionBasedOnTag")]
    static bool PrefixPerformActionBasedOnTag(PlayerBody __instance, string tag)
    {
        switch (tag)
        {
            case "Water":
                if (SaveData.Instance.Swimming)
                    return false;
                ArchipelagoWrapper.Instance.SendDeathLink("Tried to swim");
                break;
            case "Spikes":
                ArchipelagoWrapper.Instance.SendDeathLink("Was Impaled by spikes");
                break;
        }

        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch("ShatterScreen")]
    static void PrefixShatterScreen(PlayerBody __instance)
    {
        if (__instance.modifiers.modifiersActive.DeathOnHardImpact && __instance.pvIsMine)
        {
            ArchipelagoWrapper.Instance.SendDeathLink("Did not understand kinetic energy");
        }
    }
#if DEBUG
    [HarmonyFinalizer]
    [HarmonyPatch("LateUpdate")]
    static void FinalizerLateUpdate(PlayerBody __instance, Exception __exception)
    {
        
        if(__exception == null) return;
        Plugin.Logger.LogError(__exception);
        StringBuilder sb = new();
        foreach (var field in UIManager._playerFieldInfos)
        {
            sb.Append($"{field.Name}: ");
            object v = field.GetValue(field.IsStatic ? null : PlayerBody.localPlayer);
            UIManager.AppendString(sb, v);
        }
        Plugin.Logger.LogDebug(sb.ToString());
        
    }
#endif

    [HarmonyPrefix]
    [HarmonyPatch("LateUpdate")]
    static void PrefixPlayerBodyLateUpdate(PlayerBody __instance)
    {
        if(!__instance.pvIsMine) return;
        GetModifiers();
        GetCheckpoint();
        return;
        
        void GetModifiers()
        {
            bool noModifiers = __instance.modifiers.gottenModifiers.Length == 0;
            if(noModifiers) return;
            
            bool orbExists = __instance.modifiers.modifierOrbTransform && __instance
                .modifiers.modifierOrbTransform.gameObject
                .activeInHierarchy;
            if(!orbExists) return;
            
            bool inModifier = Vector3.Distance(__instance.body.torsoParent.position,
                                  __instance.modifiers.modifierOrbTransform.position) <=
                              __instance.modifiers.distToPickupModifier;
            if(!inModifier) return;
            
            bool havntgotten = !__instance.modifiers.gottenModifiers[__instance.modifiers.currentNearestModifierSpot];
            if(!havntgotten) return;


            _ = ArchipelagoWrapper.Instance.CheckModifier(__instance.modifiers.currentNearestModifierSpot);

            __instance.modifiers.gottenModifiers[__instance.modifiers.currentNearestModifierSpot] = true;
            
        }

        void GetCheckpoint()
        {
            bool hasRespawnPoints = __instance.respawning.respawnPoints.Length != 0;
            if(!hasRespawnPoints) return;
            
            bool notGotPoint =
                !__instance.respawning.respawnPointGotten[__instance.respawning.currentlyCheckingRespawnPoint];
            bool notLastRespawnPoint = __instance.respawning.alwaysGotoLastRespawnPoint &&
                                       __instance.respawning.lastGottenCheckpoint !=
                                       __instance.respawning.currentlyCheckingRespawnPoint;
            if(!notGotPoint && !notLastRespawnPoint) return;
            
            bool inDistance =
                Vector3.Distance(__instance.body.torsoParent.position,
                    __instance.respawning.respawnPoints[__instance.respawning.currentlyCheckingRespawnPoint]) <=
                __instance.respawning.distanceToGetRespawnPoint;
            if(!inDistance) return;

            //TODO: Verify
            Plugin.Logger.LogDebug(
                "Setting Respawn Point to " + __instance.respawning.currentlyCheckingRespawnPoint);
            __instance.respawning.lastGottenCheckpoint = __instance.respawning.currentlyCheckingRespawnPoint;
            PlayerPrefs.SetInt(SaveData.FakeProfile + "LastCheckpointGotten",
                __instance.respawning.currentlyCheckingRespawnPoint);
            ArchipelagoWrapper.Instance.SetDataStorage("Checkpoint",
                __instance.respawning.currentlyCheckingRespawnPoint);
            
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch("RefreshRemotePlayerMapPoints")]
    static void PostfixPlayerBodyRefreshRemotePlayerMapPoints(PlayerBody __instance)
    {
        int offset = __instance.pauseMenu.mapPoints.Length - PlayerBody.upgradeBotSpawnVectors.Length -
                     __instance.modifiers.modifierOrbVectors.Length;
        

        if(__instance.pauseMenu.mapPoints[offset-1] == null || !__instance.pauseMenu.mapPoints[offset-1].mapPoint) return;
        try
        {
            Plugin.Logger.LogDebug("upgrade bot");
            for (int i = 0; i < PlayerBody.upgradeBotSpawnVectors.Length; i++)
            {
                if (__instance.pauseMenu.mapPoints[offset + i] == null ||
                    !__instance.pauseMenu.mapPoints[offset + i].mapPoint) continue;
                
                var obj = __instance.pauseMenu.mapPoints[offset-1].nameTag;
                TextMesh text = Object.Instantiate(obj, __instance.pauseMenu.mapPoints[offset + i].mapPoint.transform)
                    .GetComponent<TextMesh>();
                text.color = Configuration.Instance.UpgradeBotColor.Value;
                text.fontSize = Mathf.CeilToInt(text.fontSize * Configuration.Instance.UpgradeBotFontSize.Value);
                text.anchor = TextAnchor.LowerCenter;
                text.transform.localPosition = Vector3.up;

                text.text = $"Upgrade Bot {i}";

                __instance.pauseMenu.mapPoints[offset + i].nameTag = text.transform;
            }
            Plugin.Logger.LogDebug("modifiers");
            for (int i = 0; i < __instance.modifiers.modifierOrbVectors.Length; i++)
            {
                if (__instance.pauseMenu.mapPoints[offset + PlayerBody.upgradeBotSpawnVectors.Length + i] == null ||
                    !__instance.pauseMenu.mapPoints[offset + PlayerBody.upgradeBotSpawnVectors.Length + i]
                        .mapPoint) continue;
                __instance.pauseMenu.mapPoints[offset + PlayerBody.upgradeBotSpawnVectors.Length + i].mapPoint
                        .localScale *=
                    0.05f;
                var r = __instance.pauseMenu.mapPoints[offset + PlayerBody.upgradeBotSpawnVectors.Length + i].mapPoint
                    .GetComponent<Renderer>();
                r.material.color = Configuration.Instance.ModifierColor.Value;
                    
                var obj = __instance.pauseMenu.mapPoints[offset-1].nameTag;
                TextMesh text = Object.Instantiate(obj, __instance.pauseMenu.mapPoints[offset + PlayerBody.upgradeBotSpawnVectors.Length + i].mapPoint.transform)
                    .GetComponent<TextMesh>();
                text.color = Configuration.Instance.ModifierColor.Value;
                text.fontSize = Mathf.CeilToInt(text.fontSize * Configuration.Instance.ModifierFontSize.Value);
                text.anchor = TextAnchor.LowerCenter;
                text.transform.localPosition = Vector3.up;
                

                text.text = $"Modifier {__instance.modifiers.modifierDisplayStrings[i]}";

                __instance.pauseMenu.mapPoints[offset + PlayerBody.upgradeBotSpawnVectors.Length + i].nameTag =
                    text.transform;
            }
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError(e);
        }
    }

    private static readonly MethodInfo PlayerBodySetupHookshot = AccessTools.Method(typeof(PlayerBody), "SetupHookshot", [typeof(int)]);
    [HarmonyPrefix]
    [HarmonyPatch("SetupStats")]
    static bool PrefixSetupStats(PlayerBody __instance)
    {
        if(!__instance.pvIsMine) return true;

        __instance.pauseMenu.mapDroneLocationsUnlocked = SaveData.Instance.ShowMapPoints;

        __instance.stats.currentJetForceLevel = SaveData.Instance.JetForce;
        __instance.stats.currentJetFuelLevel = SaveData.Instance.JetFuel;
        __instance.stats.currentHookshotLengthLevel = SaveData.Instance.HookshotLength;
        __instance.stats.currentWindResistanceLevel = SaveData.Instance.WindResistance;

        __instance.movement.jetForce = __instance.stats.jetForceAmounts[SaveData.Instance.JetForce];
        __instance.movement.jetFuelDepleteSpeed = __instance.stats.jetFuelAmounts[SaveData.Instance.JetFuel];
        __instance.hookshot.maxLength = __instance.stats.hookshotLengthAmounts[SaveData.Instance.HookshotLength];
        __instance.movement.windResistanceStanding = __instance.stats.windResistanceAmounts[SaveData.Instance.WindResistance];
        __instance.movement.windResistanceCrouching =
            __instance.stats.windResistanceAmounts[SaveData.Instance.WindResistance] * 0.75f;

        __instance.hookshot.alreadyUpdatedLeftDisplayMaxLength = false;
        __instance.hookshot.alreadyUpdatedRightDisplayMaxLength = false;
        
        if (__instance.modifiers.modifiersActive.TooMuchJets)
        {
            __instance.movement.jetForce = 500f;
        }
        if (__instance.modifiers.modifiersActive.SuperHookshots)
        {
            __instance.hookshot.maxLength = 10000f;
        }
        if (__instance.modifiers.modifiersActive.HookshotsAreStilts)
        {
            __instance.hookshot.maxLength *= 4f;
        }

        __instance.playerProfile.level = (__instance.stats.currentJetForceLevel +
                                          __instance.stats.currentJetFuelLevel +
                                          __instance.stats.currentHookshotLengthLevel +
                                          __instance.stats.currentWindResistanceLevel) / 4;
        
        if (__instance.hookshot.currentState.Length != 0)
        {
            if (__instance.hookshot.currentState[0] != 0)
            {
                PlayerBodySetupHookshot.Invoke(__instance, [0]);
            }
            if (__instance.hookshot.currentState[1] != 0)
            {
                PlayerBodySetupHookshot.Invoke(__instance, [1]);
            }
        }

        __instance.guns.guns[1].longShotUnlocked = SaveData.Instance.LongShot;
        __instance.hookshot.unlockedHookshotReel = SaveData.Instance.HookshotReel;
        __instance.movement.bunnyHopUnlocked = SaveData.Instance.BunnyHop;
        __instance.guns.guns[1].superShotUnlocked = SaveData.Instance.SuperShot;
        __instance.guns.guns[1].autoCharge = SaveData.Instance.AutoCharge;
        return false;
    }
}