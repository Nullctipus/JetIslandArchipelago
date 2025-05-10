using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Archipelago.MultiClient.Net.Enums;
using UnityEngine;
using Debug = System.Diagnostics.Debug;
using Random = UnityEngine.Random;

namespace JetIslandArchipelago.UI;

public class UIManager : MonoBehaviour
{
    private GUIStyle _messageStyle;
    private GUIStyle _modifierStyle;
    private readonly LinkedList<string> _messages = [];

    public void PushMessage(string text)
    {
        while (_messages.Count > Configuration.Instance.MessageLogCount.Value)
            _messages.RemoveLast();
        _messages.AddFirst(text);
    }

    private Coroutine messageCoroutine;
    IEnumerator DisplayMessage(string text)
    {
        //PlayerBody.localPlayer.DisplayMessageInFrontOfPlayer(); doesn't work correctly
        if (!PlayerBody.localPlayer) yield break;
        PlayerBody.localPlayer.hud.messageInFrontOfPlayerText.text = text;
        PlayerBody.localPlayer.hud.messageInFrontOfPlayerTrackTime = 5f;
        yield return new WaitForSecondsRealtime(5f);
            
        PlayerBody.localPlayer.hud.messageInFrontOfPlayerText.text = string.Empty;
    }
    private void Awake()
    {
        Configuration.Instance.MessageLogOrigin.SettingChanged += (sender, args) =>
        {
            if (_messageStyle == null) return;

            _messageStyle.alignment = Configuration.Instance.MessageLogOrigin.Value;
        };
        Configuration.Instance.ActiveModifierOrigin.SettingChanged += (sender, args) =>
        {
            if (_modifierStyle == null) return;

            _modifierStyle.alignment = Configuration.Instance.ActiveModifierOrigin.Value;
        };
        ArchipelagoWrapper.Instance.OnMessage += text =>
        {
            MainThreadHelper.Enqueue(() =>
            {
                if (messageCoroutine != null)
                    StopCoroutine(messageCoroutine);
                
                messageCoroutine = StartCoroutine(DisplayMessage(text));
            });
            PushMessage(text);
        };
    }
    private bool _showMessageLog = true,
        _showConnectionWindow = true,
        _showActiveModifiers = true;
    
    private Rect _connectionWindow = new Rect(10, 10, 320, 500);
    private readonly int _connectionWindowId = Random.RandomRangeInt(0, int.MaxValue);

    private void Update()
    {
        if(_tryDisconnect > 0)
            _tryDisconnect-=Time.deltaTime;
        if(Input.GetKeyDown(Configuration.Instance.ToggleMessageLogKey.Value))
            _showMessageLog = !_showMessageLog;
        if(Input.GetKeyDown(Configuration.Instance.ToggleConnectionWindowKey.Value))
            _showConnectionWindow = !_showConnectionWindow;
        if(Input.GetKeyDown(Configuration.Instance.ToggleActiveModifierDisplayKey.Value))
            _showActiveModifiers = !_showActiveModifiers;
#if DEBUG
        if (Input.GetKeyDown(Configuration.Instance.ToggleDebugKey.Value))
            _showDebugWindow = !_showDebugWindow;
#endif
    }

    private void OnGUI()
    {
        _messageStyle ??= new GUIStyle(GUI.skin.label)
        {
            richText = true,
            alignment = Configuration.Instance.MessageLogOrigin.Value
        };
        _modifierStyle ??= new GUIStyle(GUI.skin.label)
        {
            richText = true,
            alignment = Configuration.Instance.ActiveModifierOrigin.Value
        };
        
        if(_showMessageLog)
            DrawMessageLog();
        
        if(_showActiveModifiers)
            DrawActiveModifiers();
        
        if (_showConnectionWindow && !ArchipelagoWrapper.Instance.Connecting)
            _connectionWindow = GUI.Window(_connectionWindowId, _connectionWindow, ArchipelagoWrapper.Instance.Connected ? DrawConnected : DrawDisconnected,"Connection");
        
        #if DEBUG
        if(_showDebugWindow)
            _debugWindow = GUI.Window(_debugWindowId,_debugWindow,DrawDebugWindow, "Debug");
        #endif
    }

    private string _sendMessage = string.Empty;
    private bool _deathLink;
    private float _tryDisconnect;
    private void DrawConnected(int windowId)
    {
        if (GUILayout.Button(_tryDisconnect > 0 ? "Are You Sure?" : "Disconnect"))
        {
            if(_tryDisconnect > 0)
                _ = ArchipelagoWrapper.Instance.Disconnect();
            else
                _tryDisconnect = 5f;
        }
        GUILayout.Label($"Connected to {ArchipelagoWrapper.Instance.Host}");
        var locationInfo = ArchipelagoWrapper.Instance.GetLocationInfo();
        GUILayout.Label($"{locationInfo.LocationsChecked}/{locationInfo.AllLocations} locations checked");
        //GUILayout.Label($"{Plugin.Data.CompletedLevels.Count}/{Plugin.Data.GoalRequirement} levels to unlock the goal.");
        ArchipelagoWrapper.DeathLinkEnabled = GUILayout.Toggle(ArchipelagoWrapper.DeathLinkEnabled, "Death Link");
        _sendMessage = GUILayout.TextArea(_sendMessage);
        if (GUILayout.Button("Send Message"))
        {
            ArchipelagoWrapper.Instance.Say(_sendMessage);
            _sendMessage = string.Empty;
        }
        GUI.DragWindow(_connectionWindow);
    }

    private void DrawDisconnected(int windowId)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Host:",GUILayout.Width(100));
        Configuration.Instance.Host.Value = GUILayout.TextField(Configuration.Instance.Host.Value);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Slot Name:",GUILayout.Width(100));
        Configuration.Instance.SlotName.Value = GUILayout.TextField(Configuration.Instance.SlotName.Value);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Password:",GUILayout.Width(100));
        Configuration.Instance.Password.Value = GUILayout.TextField(Configuration.Instance.Password.Value);
        GUILayout.EndHorizontal();
        _deathLink = GUILayout.Toggle(_deathLink, "Death Link");

        if (GUILayout.Button("Connect"))
        {
            _ = ArchipelagoWrapper.Instance.Connect(Configuration.Instance.Host.Value, Configuration.Instance.SlotName.Value,
                _deathLink,
                string.IsNullOrEmpty(Configuration.Instance.Password.Value)
                    ? null
                    : Configuration.Instance.Password.Value);
            Configuration.Instance.Host.ConfigFile.Save();
        }

        GUI.DragWindow(_connectionWindow);
    }

    private readonly StringBuilder _stringBuilder = new();
    private void DrawMessageLog()
    {
        Rect fullScreen = new Rect(0, 0, Screen.width, Screen.height);
        _stringBuilder.Clear();
        LinkedListNode<string> current = _messages.First;
        while (current != null)
        {
            _stringBuilder.AppendLine(current.Value);
            current = current.Next;
        }
        GUI.Label(fullScreen, _stringBuilder.ToString(), _messageStyle);
    }

    private void DrawActiveModifiers()
    {
        if (!PlayerBody.localPlayer) return;
        Rect fullScreen = new Rect(0, 0, Screen.width, Screen.height);
        _stringBuilder.Clear();
        _stringBuilder.AppendLine("<b>[Active Modifiers]</b>");
        foreach (var enabledModifier in SaveData.Instance.EnabledModifiers)
            _stringBuilder.AppendLine(PlayerBody.localPlayer.modifiers.modifierDisplayStrings[enabledModifier]);
        
        GUI.Label(fullScreen, _stringBuilder.ToString(), _modifierStyle);
    }
    
    #if DEBUG
    private bool _showDebugWindow = false;
    private Rect _debugWindow = new Rect(170, 10, 1000, 1000);
    private readonly int _debugWindowId = Random.RandomRangeInt(0, int.MaxValue);

    private int _debugTab = 2;
    private readonly string[] _tabs = [ "Locations", "Player"];
    private GUIStyle _debugStyle;

    private void DrawDebugWindow(int windowId)
    {
        _debugStyle ??= new GUIStyle(GUI.skin.label)
        {
            richText = true,
        };
        
        if (!ArchipelagoWrapper.Instance.Connected)
        {
            GUILayout.Label("Not connected to any session (location)");
            return;
        }
        GUILayout.BeginHorizontal();
        for (int i = 0; i < _tabs.Length; i++){
            if (GUILayout.Button(_tabs[i]))
            {
                _debugTab = i;
                Plugin.Logger.LogInfo("Switching tab "+ i);
            }
        }

        GUILayout.EndHorizontal();
        switch (_debugTab)
        {
            case 0:
                DebugDrawLocationTab();
                break;
            case 1:
                DebugDrawPlayer();
                break;
        }
        GUI.DragWindow(_debugWindow);
    }
    Vector2 _debugScroll = Vector2.zero;
    void DebugDrawLocationTab()
    {
        _debugScroll = GUILayout.BeginScrollView(_debugScroll);
        foreach (var locationID in ArchipelagoWrapper.Instance.GetAllLocations())
        {
            bool check = ArchipelagoWrapper.Instance.GetCheckedLocations().Contains(locationID);
            string locationName = ArchipelagoWrapper.Instance.GetLocationName(locationID);
            GUILayout.BeginHorizontal();
            _debugStyle.normal.textColor = check ? Color.green : Color.white;
            bool newCheck = GUILayout.Toggle(check, locationName, _debugStyle);
            if (newCheck != check && newCheck)
            {
                _ = ArchipelagoWrapper.Instance.Check(locationID);
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();
    }

    internal static readonly FieldInfo[] _playerFieldInfos = typeof(PlayerBody).GetFields();
    private static readonly Dictionary<Type,FieldInfo[]> _fields = new();
    

    internal static void AppendString(StringBuilder stringBuilder, object value,int depth = 1)
    {
        if (depth > 4)
        {
            stringBuilder.Append("Depth Limit");
            return;
        }
        switch (value)
        {
            case null:
                stringBuilder.Append("Null");
                break;
            case bool:
            case byte:
            case sbyte:
            case short: 
            case ushort:
            case int:   
            case uint:
            case long:  
            case ulong:
            case double:
            case float:
            case string:
            case char:
            case decimal:
                stringBuilder.Append(Convert.ToString(value));
                break;
            case Enum e:
                stringBuilder.Append(Enum.GetName(e.GetType(), value));
                break;
            case Vector3 v3:
                stringBuilder.Append($"({v3.x}, {v3.y}, {v3.z})");
                break;
            case Quaternion q:
                stringBuilder.Append($"({q.x}, {q.y}, {q.z}, {q.w})");
                break;
            case Transform t:
                stringBuilder.Append($"{t.name} at ({t.position.x}, {t.position.y}, {t.position.z})");
                break;
            case GameObject g:
                stringBuilder.Append($"{g.name} at ({g.transform.position.x}, {g.transform.position.y}, {g.transform.position.z})");
                break;
            case Array arr:
                stringBuilder.AppendLine();
                for (int i = 0; i < arr.Length; i++)
                {
                    stringBuilder.Append('\t', depth);
                    AppendString(stringBuilder, arr.GetValue(i), depth + 1);
                }
                break;
            case PlayerBody:
                stringBuilder.Append("some player body");
                break;
            case var c:
                stringBuilder.AppendLine();
                if (!_fields.ContainsKey(c.GetType()))
                {
                    Plugin.Logger.LogDebug($"fetching fields for {c.GetType().FullName}");
                    _fields.Add(c.GetType(), c.GetType().GetFields());
                }

                foreach (var field in _fields[c.GetType()])
                {
                    stringBuilder.Append('\t', depth);
                    stringBuilder.Append($"{field.Name}: ");
                    object v = field.GetValue(field.IsStatic ? null : c);
                    AppendString(stringBuilder, v, depth + 1);
                }
                break;
        }
        stringBuilder.AppendLine();
        
    }
    void DebugDrawPlayer()
    {
        if (!PlayerBody.localPlayer)
        {
            GUILayout.Label("No local player");
            return;
        }
        _stringBuilder.Clear();
        foreach (var field in _playerFieldInfos)
        {
            _stringBuilder.Append($"{field.Name}: ");
            object v = field.GetValue(field.IsStatic ? null : PlayerBody.localPlayer);
            AppendString(_stringBuilder, v);
        }
        
        _debugScroll = GUILayout.BeginScrollView(_debugScroll);
        GUILayout.Label(_stringBuilder.ToString(),GUILayout.ExpandHeight(true));
        GUILayout.EndScrollView();
        
    }

   
    #endif
}

