using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

// ReSharper disable InconsistentNaming

namespace JetIslandArchipelago.Patches;
[HarmonyPatch(typeof(HologramButtonScript))]
public class HologramButtonScriptPatches
{
    private static readonly List<(HologramButtonScript, TMPro.TMP_Text)> letters = [];
    private const float buttonDist = 0.0577f;
    private const string defaultTitle =
        "<color=#ff0000>A</color><color=#ff3d00>r</color><color=#ff7a00>c</color><color=#ffb800>h</color><color=#fff500>i</color><color=#ccff00>p</color><color=#8fff00>e</color><color=#52ff00>l</color><color=#14ff00>a</color><color=#00ff29>g</color><color=#00ff66>o</color><color=#00ffa3> </color><color=#00ffe0>M</color><color=#00e0ff>o</color><color=#00a3ff>d</color><color=#0066ff> </color><color=#0029ff>I</color><color=#1400ff>n</color><color=#5200ff>s</color><color=#8f00ff>t</color><color=#cc00ff>a</color><color=#ff00f5>l</color><color=#ff00b8>l</color><color=#ff007a>e</color><color=#ff003d>d</color>";
    [HarmonyPostfix]
    [HarmonyPatch("Start")]
    static void PostFixHologramButtonScript(HologramButtonScript __instance)
    {
        if (__instance.buttonFunction == HologramButtonScript.ButtonFunction.AddLetter)
        {
            if(__instance.addLetter_letter == " ")
                return;
            letters.Add((__instance,null));
        }
        if(__instance.buttonFunction != HologramButtonScript.ButtonFunction.SavePlayerName) return;
        
        __instance.savePlayerName_TextMesh.text = Configuration.Instance.Host.Value;
        
        Transform menu = __instance.transform.parent;
        _headerText = Object.Instantiate(__instance.savePlayerName_TextMesh, menu, true);
        _headerText.transform.localPosition+=Vector3.up*0.1f;
        _headerText.text = defaultTitle;

        var questionmark = menu.Find("?");
        {
            var colon = Object.Instantiate(questionmark.gameObject, menu);
            colon.name = ":";
            var pos = colon.transform.localPosition;
            colon.transform.localPosition = new Vector3(pos.x + buttonDist, pos.y, pos.z);
            colon.transform.localRotation = Quaternion.identity;
            var colonScript = colon.GetComponent<HologramButtonScript>();
            var colonText = colon.GetComponentInChildren<TMPro.TMP_Text>();
            colonText.text = ":";
            colonScript.addLetter_letter = ":";
        }
        {
            var paste = Object.Instantiate(questionmark.gameObject, menu);
            paste.name = "paste";
            var pos = paste.transform.localPosition;
            paste.transform.localPosition = new Vector3(pos.x - buttonDist*0.5f, pos.y- buttonDist, pos.z);
            paste.transform.localRotation = Quaternion.identity;
            var scale = paste.transform.localScale;
            paste.transform.localScale = new Vector3(3f * scale.x, scale.y, scale.z);
            var pasteScript = paste.GetComponent<HologramButtonScript>();
            var pasteText = paste.GetComponentInChildren<TMPro.TMP_Text>();
            scale = pasteText.transform.localScale;
            pasteText.transform.localScale = new Vector3(scale.x/3f, scale.y, scale.z);
            pasteText.text = "paste";
            pasteScript.addLetter_letter = "paste";
        }
        
        var z = menu.Find("Z");
        {
            var shift = Object.Instantiate(z.gameObject, menu);
            shift.name = "shift";
            var pos = shift.transform.localPosition;
            shift.transform.localPosition = new Vector3(pos.x + buttonDist*2f, pos.y, pos.z);
            var scale = shift.transform.localScale;
            shift.transform.localScale = new Vector3(3f * scale.x, scale.y, scale.z);
            var shiftScript = shift.GetComponent<HologramButtonScript>();
            var shiftText = shift.GetComponentInChildren<TMPro.TMP_Text>();
            scale = shiftText.transform.localScale;
            shiftText.transform.localScale = new Vector3(scale.x/3f, scale.y, scale.z);
            shiftText.text = "shift";
            shiftScript.addLetter_letter = "shift";
        }
    }

    static void UpdateCase()
    {
        for (var index = 0; index < letters.Count; index++)
        {
            var key = letters[index];
            if (key.Item2 == null)
            {
                key = (key.Item1, key.Item1.GetComponentInChildren<TMP_Text>());
                letters[index] = key;
            }

            if (key.Item2 == null || string.IsNullOrEmpty(key.Item1.addLetter_letter)) continue;
            key.Item2.text = _useCaps
                ? key.Item1.addLetter_letter
                : key.Item1.addLetter_letter.ToLowerInvariant();
        }
    }

    private static bool _useCaps = true;
    private static int _connectPhase;
    private static readonly FieldInfo selectProfile_isNewProfileButton = AccessTools.Field(typeof(HologramButtonScript), "selectProfile_isNewProfileButton");
    private static readonly FieldInfo playerMovedFingerAway = AccessTools.Field(typeof(HologramButtonScript), "playerMovedFingerAway");
    private static readonly FieldInfo pressedState = AccessTools.Field(typeof(HologramButtonScript), "pressedState");
    internal static TMPro.TMP_Text _headerText;    
    [HarmonyPrefix]
    [HarmonyPatch("PressButton")]
    static bool PrefixHologramButtonScript(HologramButtonScript __instance, MethodBase __originalMethod)
    {
        if(ArchipelagoWrapper.Instance.Connecting) return false;
        if (__instance.buttonFunction == HologramButtonScript.ButtonFunction.AddLetter)
        {
            if (__instance.menuSettings.pressSound)
            {
                AudioSource.PlayClipAtPoint(__instance.menuSettings.pressSound, __instance.transform.position);
            }
            playerMovedFingerAway.SetValue(__instance,false);
            pressedState.SetValue(__instance, 1);
            string text = _connectPhase switch
            {
                0 => Configuration.Instance.Host.Value,
                1 => Configuration.Instance.SlotName.Value,
                2 => Configuration.Instance.Password.Value,
                _ => throw new ArgumentOutOfRangeException()
            };

            if (!__instance.addLetter_isBackSpace)
            {
                switch (__instance.addLetter_letter)
                {
                    case "paste":
                        text += GUIUtility.systemCopyBuffer;
                        break;
                    case "shift":
                        _useCaps = !_useCaps;
                        UpdateCase();
                        break;
                    default:
                        text += _useCaps ? __instance.addLetter_letter : __instance.addLetter_letter.ToLowerInvariant();
                        if (_useCaps)
                        {
                            _useCaps = false;
                            UpdateCase();
                        }
                        break;
                }
            }
            else if (text.Length > 1)
                text = text.Substring(0, text.Length - 1);
            else
                text = string.Empty;

            switch (_connectPhase)
            {
                case 0:
                    Configuration.Instance.Host.Value = text;
                    break;
                case 1:
                    Configuration.Instance.SlotName.Value = text;
                    break;
                case 2:
                    Configuration.Instance.Password.Value = text;
                    break;
            }
            __instance.addLetter_TextMesh.text = text;
            return false;
        }

        if (__instance.buttonFunction == HologramButtonScript.ButtonFunction.SavePlayerName)
        {
            if (__instance.menuSettings.pressSound)
            {
                AudioSource.PlayClipAtPoint(__instance.menuSettings.pressSound, __instance.transform.position);
            }
            playerMovedFingerAway.SetValue(__instance,false);
            pressedState.SetValue(__instance, 1);
            switch (_connectPhase)
            {
                case 0:
                    _headerText.text = "SlotName:";
                    __instance.savePlayerName_TextMesh.text = Configuration.Instance.SlotName.Value;
                    _connectPhase = 1;
                    break;
                
                case 1:
                    _headerText.text = "Password (Leave blank if none):";
                    __instance.savePlayerName_TextMesh.text = Configuration.Instance.Password.Value;
                    _connectPhase = 2;
                    break;
                case 2:
                    _headerText.text = "Connecting...";
                    __instance.savePlayerName_TextMesh.text = "";
                    _ = ArchipelagoWrapper.Instance.Connect(Configuration.Instance.Host.Value,
                        Configuration.Instance.SlotName.Value,
                        false,
                        string.IsNullOrEmpty(Configuration.Instance.Password.Value)
                            ? null
                            : Configuration.Instance.Password.Value);
                    ArchipelagoWrapper.Instance.OnDisconnected += OnDisconnect;
                    ArchipelagoWrapper.Instance.OnConnect += OnConnect;

                    _connectPhase = 0;
                    break;

                    void OnDisconnect()
                    {
                        ArchipelagoWrapper.Instance.OnDisconnected -= OnDisconnect;
                        ArchipelagoWrapper.Instance.OnConnect -= OnConnect;
                        Plugin.Logger.LogDebug("Going Back");
                        
                        MainThreadHelper.Enqueue(() =>
                        {
                            _headerText.text = "Archipelago Host:";
                            if(!__instance || !__instance.savePlayerName_TextMesh) return;
                            __instance.savePlayerName_TextMesh.text = Configuration.Instance.Host.Value;
                        });
                        
                    }

                    void OnConnect(int _0, int _1, Dictionary<string, object> slotData)
                    {
                        ArchipelagoWrapper.Instance.OnConnect -= OnConnect;
                        ArchipelagoWrapper.Instance.OnDisconnected -= OnDisconnect;
                        
                        Plugin.Logger.LogDebug("Starting Game");

                        MainThreadHelper.Enqueue(() =>
                        {
                            __instance.buttonFunction = HologramButtonScript.ButtonFunction.SelectProfile;
                            selectProfile_isNewProfileButton.SetValue(__instance,true);
                            __instance.selectProfile_profileInt = SaveData.FakeProfile;
                            __originalMethod.Invoke(__instance, null);

                            __instance.buttonFunction = HologramButtonScript.ButtonFunction.PlayOffline;
                            __instance.playOffline_IfPlayerPrefOpenMenu = "";
                            __instance.playOffline_Scene = "JetIsland Main Scene";
                            __originalMethod.Invoke(__instance, null);
                            __instance.buttonFunction = HologramButtonScript.ButtonFunction.SavePlayerName;
                        });
                    }
            }
            return false;
        }

        return true;
    }
}