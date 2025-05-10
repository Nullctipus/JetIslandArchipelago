using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using JetIslandArchipelago.UI;
using UnityEngine;

namespace JetIslandArchipelago;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private GameObject _extraObject;
    internal new static ManualLogSource Logger;
    private static Harmony _harmony;
        
    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        _ = new Configuration(Config);
        
        
        _extraObject = new GameObject("Archipelago")
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        DontDestroyOnLoad(_extraObject);
        _extraObject.AddComponent<UIManager>();
        _extraObject.AddComponent<MainThreadHelper>();
        Logger.LogInfo($"UIManager is loaded!");

        _harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), MyPluginInfo.PLUGIN_GUID);
        ArchipelagoWrapper.Instance.OnDeathReceived += (_, _, _) =>
        {
            PlayerBody.localPlayer?.Respawn();
        };
        //Ensure SaveData Loaded
        _ = SaveData.Instance;
    }

    /*private void OnDestroy()
    {
        _harmony.UnpatchSelf();
        Destroy(_extraObject);
    }*/
}
