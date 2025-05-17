using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using JetIslandArchipelago.Modifiers;
using JetIslandArchipelago.Patches;
using UnityEngine;
using Debug = System.Diagnostics.Debug;
using Random = UnityEngine.Random;

namespace JetIslandArchipelago;

public class SaveData
{
    private static SaveData _instance;
    public static SaveData Instance => _instance ??= new SaveData();
    public const int FakeProfile = 69;


    public static List<Modifier> ModifierList;
    private static readonly FieldInfo PlayerMonsterScript = AccessTools.Field(typeof(SaveData), "monsterScript");
    public void CreateModifiers()
    {
        Renderer[] ground = GameObject.FindGameObjectsWithTag("Ground").Select(x=>x.GetComponent<Renderer>()).ToArray();
        Material[] gMaterials = ground.Select(x=>x.sharedMaterial).ToArray();
        Color gColor = RenderSettings.ambientGroundColor;
        Color fColor = RenderSettings.fogColor;
        float fDensity = RenderSettings.fogDensity;
        
        Quaternion ljRot = PlayerBody.localPlayer.movement.leftJetTransform.localRotation;
        Vector3 ljpos = PlayerBody.localPlayer.movement.leftJetTransform.localPosition;

        Quaternion rjRot = PlayerBody.localPlayer.movement.rightJetTransform.localRotation;
        Vector3 rjpos = PlayerBody.localPlayer.movement.rightJetTransform.localPosition;
        
        Type modifierType = typeof(PlayerBody.ModifiersActive);
        var activeModifiers = PlayerBody.localPlayer.modifiers.modifiersActive;
        ModifierList?.Clear();
        ModifierList =
        [
            new Modifier(activeModifiers,
                AccessTools.Field(modifierType, nameof(activeModifiers.ExtremeRotationMode))),
            new Modifier(activeModifiers,
                AccessTools.Field(modifierType, nameof(activeModifiers.SlowMoHookshots))),
            new GravityModifier(activeModifiers,
                AccessTools.Field(modifierType, nameof(activeModifiers.MarsGravity)),
                3.711f),
            new QuickModifier(activeModifiers,
                AccessTools.Field(modifierType, nameof(activeModifiers.FallingUp)), (player) =>
                {
                    player.movement.gravityDirection = player.transform.root.up;
                }, (player) =>
                {
                    player.movement.gravityDirection = -player.transform.root.up;
                }),
            new Modifier(activeModifiers,
                AccessTools.Field(modifierType, nameof(activeModifiers.JetsOnly))),
            new RotateWorldModifier(activeModifiers,
                AccessTools.Field(modifierType, nameof(activeModifiers.UpsideDownWorld)), 
                new Vector3(180f,0f,0f),Vector3.up*1.8f,Vector3.up),
            new GravityModifier(activeModifiers,
                AccessTools.Field(modifierType, nameof(activeModifiers.JupiterGravity)),
                24.79f),
            new Modifier(activeModifiers,
                AccessTools.Field(modifierType, nameof(activeModifiers.HookshotsOnly))),
            new RotateWorldModifier(activeModifiers,
                AccessTools.Field(modifierType, nameof(activeModifiers.SidewaysWorld)), 
                new Vector3(0f,0f,90f),Vector3.up*0.8f,new Vector3(0f,0f,-1f)),
            new GravityModifier(activeModifiers,
                AccessTools.Field(modifierType, nameof(activeModifiers.SunGravity)),
                274f),
            new Modifier(activeModifiers,
                AccessTools.Field(modifierType, nameof(activeModifiers.SuperExplosions))),
            new GravityModifier(activeModifiers,
                AccessTools.Field(modifierType, nameof(activeModifiers.ZeroGravity)),
                0f),
            new QuickModifier(activeModifiers,
                AccessTools.Field(modifierType, nameof(activeModifiers.TooMuchJets)), (player) =>
                {
                    player.movement.jetForce = 500f;
                }, (player) =>
                {
                    player.movement.jetForce = player.movement.jetForce =
                        player.stats.jetForceAmounts[JetForce];
                }),
            new GravityModifier(activeModifiers,
                AccessTools.Field(modifierType, nameof(activeModifiers.MoonGravity)),
                1.62f),
            new Modifier(activeModifiers,
                AccessTools.Field(modifierType, nameof(activeModifiers.InfiniteJets))),
            new Modifier(activeModifiers,
                AccessTools.Field(modifierType, nameof(activeModifiers.SuperHookshots))),
            new Modifier(activeModifiers,
                AccessTools.Field(modifierType, nameof(activeModifiers.RandomWorldRotOnRespawn))),
            null, // MultiModifier
            new Modifier(activeModifiers,
                AccessTools.Field(modifierType, nameof(activeModifiers.TenXSpeed))),
            new QuickModifier(activeModifiers,
                AccessTools.Field(modifierType, nameof(activeModifiers.BackwardsJets)), (player) =>
                {
                    player.movement.leftJetTransform.Rotate(180f, 0f, 0f, Space.Self);
                    player.movement.rightJetTransform.Rotate(180f, 0f, 0f, Space.Self);
                }, (player) =>
                {
                    player.movement.leftJetTransform.Rotate(-180f, 0f, 0f, Space.Self);
                    player.movement.rightJetTransform.Rotate(-180f, 0f, 0f, Space.Self);
                }),
            new Modifier(activeModifiers,
                AccessTools.Field(modifierType, nameof(activeModifiers.StretchyHookshots))),
            new Modifier(activeModifiers,
                AccessTools.Field(modifierType, nameof(activeModifiers.HookshotsAreStilts))),
            new Modifier(activeModifiers,
                AccessTools.Field(modifierType, nameof(activeModifiers.BouncyGround))),
            new QuickModifier(activeModifiers,
                AccessTools.Field(modifierType, nameof(activeModifiers.GroundIsLava)), (player) =>
                {
                    foreach (var r in ground)
                    {
                        r.sharedMaterial = player.modifiers.groundIsLavaModifierMaterials[
                            Random.Range(0, player.modifiers.groundIsLavaModifierMaterials.Length)];
                        r.gameObject.tag = "Spikes";
                    }
                    RenderSettings.ambientGroundColor = Color.red;
                }, (_) =>
                {
                    foreach (var z in ground.Zip(gMaterials,(r,m)=>(r,m)))
                    {
                        z.r.sharedMaterial = z.m;
                        z.r.gameObject.tag = "Ground";
                    }
                    RenderSettings.ambientGroundColor = gColor;
                }),
            new Modifier(activeModifiers,
                AccessTools.Field(modifierType, nameof(activeModifiers.GunOnly))),
            new Modifier(activeModifiers,
                AccessTools.Field(modifierType, nameof(activeModifiers.GunAlwaysCharged))),
            new Modifier(activeModifiers,
                AccessTools.Field(modifierType, nameof(activeModifiers.GunRapidFire))),
            new SkyModifier(activeModifiers,
                AccessTools.Field(modifierType, nameof(activeModifiers.NightSky)),
                CameraClearFlags.Skybox,PlayerBody.localPlayer.modifiers.nightSkyMaterial,
                PlayerBody.localPlayer.modifiers.nightSkyLightColor,
                PlayerBody.localPlayer.modifiers.nightSkyAmbientLightStrength,
                new Color(0f, 0.05f, 0.05f, 1f)),
            new SkyModifier(activeModifiers,
                AccessTools.Field(modifierType, nameof(activeModifiers.PitchBlack)),
                CameraClearFlags.Color,PlayerBody.localPlayer.modifiers.nightSkyMaterial,
                Color.black, 0,
                new Color(0f, 0.05f, 0.05f, 1f)),
            new QuickModifier(activeModifiers,
                AccessTools.Field(modifierType, nameof(activeModifiers.Foggy)), (player) =>
                {
                    var camera = Camera.main;
                    Debug.Assert(camera != null, nameof(camera) + " != null");
                    camera.clearFlags = CameraClearFlags.Color;
                    camera.backgroundColor = player.modifiers.foggyColor;
                    RenderSettings.fogColor = player.modifiers.foggyColor;
                    RenderSettings.fogDensity = player.modifiers.foggyFogDensity;
                }, (player) =>
                {
                    var camera = Camera.main;
                    Debug.Assert(camera != null, nameof(camera) + " != null");
                    camera.clearFlags = CameraClearFlags.Skybox;
                    camera.backgroundColor = Color.white;
                    RenderSettings.fogColor = fColor;
                    RenderSettings.fogDensity = fDensity;
                }),
            new Modifier(activeModifiers,
                AccessTools.Field(modifierType, nameof(activeModifiers.DeathOnHardImpact))),
            new QuickModifier(activeModifiers, 
                AccessTools.Field(modifierType, nameof(activeModifiers.PalmJets)), (player) =>
                {
                    player.movement.leftJetTransform.localRotation = Quaternion.Euler(17f, -94f, -143f);
                    player.movement.rightJetTransform.localRotation = Quaternion.Euler(17f, -94f, -143f);
                    player.movement.leftJetTransform.localPosition = new Vector3(0.0732f, -0.028f, 0.122f);
                    player.movement.rightJetTransform.localPosition = new Vector3(0.062f, 0.016f, 0.087f);
                }, (player) =>
                {
                    player.movement.leftJetTransform.localRotation = ljRot;
                    player.movement.rightJetTransform.localRotation = rjRot;
                    player.movement.leftJetTransform.localPosition = ljpos;
                    player.movement.rightJetTransform.localPosition = rjpos;
                }),
            new QuickModifier(activeModifiers,
                AccessTools.Field(modifierType, nameof(activeModifiers.DelicatePlayer)), (player) =>
                {
                    player.particles.speedSmallBump *= 0.2f;
                    player.particles.speedMediumBump *= 0.2f;
                    player.particles.speedFastHit *= 0.2f;
                    player.particles.speedSuperFastCrash *= 0.2f;
                }, (player) =>
                {
                    player.particles.speedSmallBump /= 0.2f;
                    player.particles.speedMediumBump /= 0.2f;
                    player.particles.speedFastHit /= 0.2f;
                    player.particles.speedSuperFastCrash /= 0.2f;
                }),
            new QuickModifier(activeModifiers,
                AccessTools.Field(modifierType, nameof(activeModifiers.SuperWormy)), (player) =>
                {
                    MonsterScript ms = PlayerMonsterScript.GetValue(player) as MonsterScript;
                    if (!ms) return;
                    
                    for (int i = 0; i < ms.bodyPieces.Length; i++)
                    {
                        ms.bodyPieces[i].localScale *= 10f;
                        if (i > 0)
                        {
                            ms.bodyPieces[i].position = ms.bodyPieces[i - 1].position - ms.bodyPieces[i - 1].forward * (ms.distBetweenPieces * 2f * 10f);
                        }
                    }
                    ms.distBetweenPieces *= 10f;
                    ms.sinHorizontalSize = 0f;
                }, (player) =>
                {
                    MonsterScript ms = PlayerMonsterScript.GetValue(player) as MonsterScript;
                    if (!ms) return;
                    
                    for (int i = 0; i < ms.bodyPieces.Length; i++)
                    {
                        ms.bodyPieces[i].localScale *= .1f;
                        if (i > 0)
                        {
                            ms.bodyPieces[i].position = ms.bodyPieces[i - 1].position - ms.bodyPieces[i - 1].forward * (ms.distBetweenPieces * 2f * .1f);
                        }
                    }
                    ms.distBetweenPieces *= .1f;
                    ms.sinHorizontalSize = 0f;
                })
        ];
        foreach(var m in ModifierList)
            m?.Initialize();
    }
    public void SyncActiveModifiers(bool invokeSetupModifiers = true)
    {
        if(!PlayerBody.localPlayer || !invokeSetupModifiers) return;

        foreach (var modifier in EnabledModifiers)
        {
            if(!ModifierList[modifier].Enabled)
                ModifierList[modifier].Enable();
        }
        PlayerBody.localPlayer.modifiers.modifiersActive.anyModifiersActive = false;
    }

    private readonly HashSet<long> _received = [];

    void ReceiveItem(long id, string name, bool newItem)
    {
        if(!_received.Add(id)) return;
        var info = ArchipelagoWrapper.Instance
            .GetItemInfo(id);
        Plugin.Logger.LogDebug($"{id},{name}: {info.Type} index={info.Index}");
        switch (info.Type)
        {
            case ArchipelagoWrapper.ItemInfo.ItemType.Upgrade:
                switch (info.Index)
                {
                    case 0:
                        JetForce++;
                        break;
                    case 1:
                        JetFuel++;
                        break;
                    case 2:
                        HookshotLength++;
                        break;
                    case 3:
                        WindResistance++;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                PlayerBody.localPlayer?.SetupStats();
                break;
            case ArchipelagoWrapper.ItemInfo.ItemType.Modifier:
                Modifiers.Add(info.Index);
                EnabledModifiers.Enqueue(info.Index);
                if(PlayerBody.localPlayer)
                    MainThreadHelper.Enqueue(() =>
                    {
                        int? disable = null;  
                        if(EnabledModifiers.Count > AutoUseModifiersCount)
                            disable = EnabledModifiers.Dequeue();
                        if (disable.HasValue)
                            ModifierList[disable.Value].Disable();
                        ModifierList[info.Index].Enable();
                        
                    });
                break;
            case ArchipelagoWrapper.ItemInfo.ItemType.BossUpgrade:
                switch (info.Index)
                {
                    case 0:
                        LongShot = true;
                        break;
                    case 1:
                        BunnyHop = true;
                        break;
                    case 2:
                        SuperShot = true;
                        break;
                    case 3:
                        HookshotReel = true;
                        break;
                    case 4:
                        AutoCharge = true;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                PlayerBody.localPlayer?.SetupStats();
                break;
            case ArchipelagoWrapper.ItemInfo.ItemType.Filler:
                if(!PlayerBody.localPlayer || !newItem) break;
                switch (info.Index)
                {
                    case 0: // 10s no jets
                        PlayerBody.localPlayer.StartCoroutine(NoJets());
                        break;
                    case 1: // 10s no hookshots
                        PlayerBody.localPlayer.StartCoroutine(NoHookshots());
                        break;
                    case 2: // 10s wind
                        PlayerBody.localPlayer.StartCoroutine(NoWind());
                        break;
                    case 3: // 10s infinite jets
                        PlayerBody.localPlayer.StartCoroutine(InfiniteJets());
                        break;
                    case 4: // Get Rotated
                        float angle = Random.Range(30, 180) * (Random.value > 0.5f ? -1 : 1);
                        PlayerBody.localPlayer.transform.root.Rotate(PlayerBody.localPlayer.transform.root.up, angle);
                        PlayerBody.localPlayer.skateboard.velocity =
                            Quaternion.AngleAxis(angle, PlayerBody.localPlayer.transform.root.up)*PlayerBody.localPlayer.skateboard.velocity;
                        break;
                    case 5: // Swimming
                        PlayerBody.localPlayer.StartCoroutine(Swim());
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();

                        IEnumerator NoJets()
                        {
                            float jf =PlayerBody.localPlayer.movement.jetForce;
                            PlayerBody.localPlayer.movement.jetForce = 0f;
                            yield return new WaitForSecondsRealtime(10f);
                            if(PlayerBody.localPlayer.movement.jetForce != 0)
                                PlayerBody.localPlayer.movement.jetForce = jf;
                        }
                        IEnumerator NoHookshots()
                        {
                            float hl = PlayerBody.localPlayer.hookshot.maxLength;
                            PlayerBody.localPlayer.hookshot.maxLength = 0f;
                            yield return new WaitForSecondsRealtime(10f);
                            if(PlayerBody.localPlayer.hookshot.maxLength != 0)
                                PlayerBody.localPlayer.hookshot.maxLength = hl;
                        }
                        IEnumerator NoWind()
                        {
                            float wr = PlayerBody.localPlayer.movement.windResistanceStanding;
                            PlayerBody.localPlayer.movement.windResistanceStanding = 0;
                            PlayerBody.localPlayer.movement.windResistanceCrouching = 0;
                            yield return new WaitForSecondsRealtime(10f);
                            if (PlayerBody.localPlayer.movement.windResistanceStanding != 0)
                            {
                                PlayerBody.localPlayer.movement.windResistanceStanding = wr;
                                PlayerBody.localPlayer.movement.windResistanceCrouching = wr*0.5f;
                            }
                        }
                        IEnumerator InfiniteJets()
                        {
                            bool infinite = PlayerBody.localPlayer.modifiers.modifiersActive.InfiniteJets;
                            PlayerBody.localPlayer.modifiers.modifiersActive.InfiniteJets = true;
                            yield return new WaitForSecondsRealtime(10f);
                            if (infinite)
                                PlayerBody.localPlayer.modifiers.modifiersActive.InfiniteJets = false;
                        }
                        IEnumerator Swim()
                        {
                            Swimming = true;
                            yield return new WaitForSecondsRealtime(10f);
                            Swimming = false;
                        }
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    internal void OnCheck(long id, ArchipelagoWrapper.CheckType type, int index)
    {
        if(!PlayerBody.localPlayer) return;
        try
        {
            var player = PlayerBody.localPlayer;
            switch (type)
            {
                case ArchipelagoWrapper.CheckType.Stat:
                    PlayerPrefs.SetInt($"{FakeProfile}UpgradeBot{index}", 1);
                    break;
                case ArchipelagoWrapper.CheckType.Modifier:
                    PlayerPrefs.SetInt($"{FakeProfile}ModifierUnlocked{player.modifiers.modifierUnlockStrings[index]}",
                        1);
                    if(player.modifiers.gottenModifiers != null && player.modifiers.gottenModifiers.Length > index)
                        player.modifiers.gottenModifiers[index] = true;
                    break;
                case ArchipelagoWrapper.CheckType.Boss:
                    PlayerPrefs.SetInt($"{FakeProfile}Miniboss{index + 1}BeatAtLeastOnce", 1);
                    PlayerPrefs.SetInt($"{FakeProfile}Miniboss{index + 1}Beat", 1);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError(e);
        }
    }

    private SaveData()
    {
        ArchipelagoWrapper.Instance.OnReceiveItem += ReceiveItem;
        ArchipelagoWrapper.Instance.OnConnect += OnConnect;
        ArchipelagoWrapper.Instance.OnDisconnected += OnDisconnect;
        ArchipelagoWrapper.Instance.OnCheck += OnCheck;
    }

    private void OnConnect(int _0, int _1, Dictionary<string, object> slotData)
    {

        AutoUseModifiersCount = (int)((long)slotData["auto_modifier"]);
        ShowMapPoints = (bool)slotData["show_map_points"];
        StartingCheckpoint = (int)((long)slotData["starting_checkpoint"]);
        
    }

    private void OnDisconnect()
    {
        _received.Clear();
        Modifiers.Clear();
        EnabledModifiers.Clear();
        JetForce = 0;
        JetFuel = 0;
        HookshotLength = 0;
        WindResistance = 0;
        LongShot = false;
        BunnyHop = false;
        SuperShot = false;
        HookshotReel = false;
        AutoUseModifiersCount = 0;

    }

    public readonly HashSet<int> Modifiers = [];
    public readonly Queue<int> EnabledModifiers = [];
    public int AutoUseModifiersCount;
    public bool ShowMapPoints;
    public int StartingCheckpoint;

    public bool Swimming;
    
    public int 
        JetForce,
        JetFuel,
        HookshotLength,
        WindResistance;
    
    public bool 
        LongShot,
        HookshotReel,
        BunnyHop,
        SuperShot,
        AutoCharge;

}