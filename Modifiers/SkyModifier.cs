using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace JetIslandArchipelago.Modifiers;

public class SkyModifier(object obj, FieldInfo booleanEnabled, CameraClearFlags cameraClearFlags,
    Material skyMaterial, Color lightColor, float ambientLightStrength, Color jumpColor) 
    : Modifier(obj, booleanEnabled)
{
    private static readonly int TintColor = Shader.PropertyToID("_TintColor");
    private float _defaultFogDensity;
    private Color _defaultFogColor;
    private float _defaultAmbientIntensity;
    private Material _defaultSkyMaterial;
    private Color _defaultLightColor;
    private Color _defaultJumpColor;
    private float _defaultReflection;


    public override void Initialize()
    {
        _defaultLightColor = GameObject.FindWithTag("MainLight").GetComponent<Light>().color;
        _defaultFogColor = RenderSettings.fogColor;
        _defaultFogDensity = RenderSettings.fogDensity;
        _defaultAmbientIntensity = RenderSettings.ambientIntensity;
        _defaultSkyMaterial = RenderSettings.skybox; 
        _defaultReflection = RenderSettings.reflectionIntensity;
        _defaultJumpColor = GameObject.FindWithTag("JumpPad").GetComponent<Renderer>().sharedMaterial.GetColor(TintColor);
    }

    public override void Enable()
    {
        base.Enable();
        GameObject.FindWithTag("MainLight").GetComponent<Light>().color = lightColor;
        RenderSettings.skybox = skyMaterial;
        var camera = Camera.main;
        camera.clearFlags = cameraClearFlags;
        if(cameraClearFlags == CameraClearFlags.Color)
            camera.backgroundColor = Color.black;
        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.ambientIntensity = ambientLightStrength;
        RenderSettings.reflectionIntensity = 0f;
        RenderSettings.fogDensity = 0f;
        GameObject.FindWithTag("JumpPad").GetComponent<Renderer>().sharedMaterial.SetColor(TintColor, jumpColor);
        var sgs = Object.FindObjectOfType<StartGameScript>();
        sgs.modifierWaterDay.SetActive(value: false);
        sgs.modifierWaterNight.SetActive(value: true);
        sgs.modifierUnderGroundWaterDay.SetActive(value: false);
        sgs.modifierUnderGroundWaterNight.SetActive(value: true);
        Light[] nightSkyLightsToActivate = playerBody.modifiers.nightSkyLightsToActivate;
        foreach (var t in nightSkyLightsToActivate)
        {
            t.enabled = true;
        }
    }

    public override void Disable()
    {
        base.Disable();
        GameObject.FindWithTag("MainLight").GetComponent<Light>().color = _defaultLightColor;
        var camera = Camera.main;
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.backgroundColor = Color.white;
        RenderSettings.skybox = _defaultSkyMaterial;
        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.ambientIntensity = _defaultAmbientIntensity;
        RenderSettings.reflectionIntensity = _defaultReflection;
        RenderSettings.fogDensity = _defaultFogDensity;
        GameObject.FindWithTag("JumpPad").GetComponent<Renderer>().sharedMaterial.SetColor(TintColor, _defaultJumpColor);
        var sgs = Object.FindObjectOfType<StartGameScript>();
        sgs.modifierWaterDay.SetActive(value: true);
        sgs.modifierWaterNight.SetActive(value: false);
        sgs.modifierUnderGroundWaterDay.SetActive(value: true);
        sgs.modifierUnderGroundWaterNight.SetActive(value: false);
        Light[] nightSkyLightsToActivate = playerBody.modifiers.nightSkyLightsToActivate;
        foreach (var t in nightSkyLightsToActivate)
        {
            t.enabled = false;
        }
    }
}