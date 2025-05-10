using System;
using System.Data;
using BepInEx.Configuration;
using UnityEngine;

namespace JetIslandArchipelago;

public class Configuration
{
    static Configuration _instance;
    public static Configuration Instance => _instance ?? throw new NoNullAllowedException();

    internal Configuration(ConfigFile config)
    {
        _instance = this;
        MessageLogOrigin = config.Bind(ArchipelagoHeader, "MessageLogOrigin", TextAnchor.LowerRight);
        MessageLogCount = config.Bind(ArchipelagoHeader, "MessageLogCount", 5);
        ActiveModifierOrigin = config.Bind(ArchipelagoHeader, "ActiveModifierOrigin", TextAnchor.UpperRight);
        Host = config.Bind(ArchipelagoHeader, "Host", "wss://Archipelago.gg:");
        SlotName = config.Bind(ArchipelagoHeader, "SlotName", string.Empty);
        Password = config.Bind(ArchipelagoHeader, "Password", string.Empty);
        
        UpgradeBotColor = config.Bind(MapHeader, "UpgradeBotColor", new Color(246/255f,193/255f, 119/255f,1f));
        UpgradeBotFontSize = config.Bind(MapHeader, "UpgradeBotFontSize", .75f);
        ModifierColor = config.Bind(MapHeader, "ModifierColor", new Color(196/255f,167/255f, 231/255f,1f));
        ModifierFontSize = config.Bind(MapHeader, "ModifierFontSize", .5f);    
        
        ToggleMessageLogKey = config.Bind(KeysHeader, "ToggleMessageLogKey", KeyCode.F3);
        ToggleConnectionWindowKey = config.Bind(KeysHeader, "ToggleConnectionWindowKey", KeyCode.F4);
        ToggleActiveModifierDisplayKey = config.Bind(KeysHeader, "ToggleActiveModifierDisplayKey", KeyCode.F5);
        
        #if DEBUG
        ToggleDebugKey = config.Bind(DebugHeader, "ToggleDebugWindowKey", KeyCode.F6);
        #endif  
    }

    private const string ArchipelagoHeader = "Archipelago"; 
    public readonly ConfigEntry<TextAnchor> MessageLogOrigin,
        ActiveModifierOrigin;
    public readonly ConfigEntry<int> MessageLogCount;
    public readonly ConfigEntry<string> Host,
        SlotName,
        Password;
    private const string MapHeader = "Map";
    public readonly ConfigEntry<Color> UpgradeBotColor,
        ModifierColor;
    public readonly ConfigEntry<float> UpgradeBotFontSize,
        ModifierFontSize;
    
    private const string KeysHeader = "Keys";
    public readonly ConfigEntry<KeyCode> ToggleMessageLogKey,
        ToggleConnectionWindowKey,
        ToggleActiveModifierDisplayKey;
    
#if DEBUG
    private const string DebugHeader = "Debug";
    public readonly ConfigEntry<KeyCode> ToggleDebugKey;
#endif
}

