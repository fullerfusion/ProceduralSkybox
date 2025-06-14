using System.Collections.Generic;
using UnityEngine;

public class DayNightCycleController : MonoBehaviour
{
    [Range(0f, 1f)]
    public float timeOfDay;
    public float dayLengthInMinutes = 2f; // full day = 2 minutes in real time
    public bool autoRun_TOD = true;

    [SerializeField] private Transform celestialPivot;
    [SerializeField] private float axialTilt = 23.5f; // earth-like
    public Vector2 celestialTilt = new Vector2(-90, 270);

    public Light mainLight;
    //public Light sunLight;
    public float sunOffset = 1;
    //public Light moonLight;
    public float moonOffset = 1;
    public Light ambientFillLight; // New ambient boost light
    public Material skyMaterial;

    [Header("Light Intensity")]
    public AnimationCurve sunIntensityCurve;
    public AnimationCurve moonIntensityCurve;
    public float sunMultiplier = 2.5f;

    [Header("Light Fade Controls")]
    public AnimationCurve sunBlendCurve;
    public AnimationCurve moonBlendCurve;

    [Header("Ambient Light")]
    public float ambientLight_Intensity = 0.25f;
    public float ambientZenith_multiplier = 1.2f;
    public float ambientMid_multiplier = 0.95f;
    public float ambientHorizon_multiplier = 0.8f;

    [Header("Color Tint")]
    public Gradient SunColorGradient; // Separate sun tint gradient
    public Gradient MoonColorGradient; // Separate sun tint gradient

    public enum SkyPhase { Sunrise, Day, Sunset, Night }

    [System.Serializable]
    public struct SkyPhaseColors
    {
        [ColorUsage(true, true)]
        public Color zenithColor;
        [ColorUsage(true, true)]
        public Color midColor;
        [ColorUsage(true, true)]
        public Color horizonColor;
    }

    [Header("Skybox Colors")]
    public SkyPhaseColors sunriseColors;
    public SkyPhaseColors dayColors;
    public SkyPhaseColors sunsetColors;
    public SkyPhaseColors nightColors;

    [Header("Ambient Colors")]
    public SkyPhaseColors amb_sunriseColors;
    public SkyPhaseColors amb_dayColors;
    public SkyPhaseColors amb_sunsetColors;
    public SkyPhaseColors amb_nightColors;
    public GameObject fakeMoon;
    public GameObject fakeSun;
    public List<float> phaseTimeShifts = new List<float>() { 0.125f,  0.375f, 0.62f, 0.80f };
    // Optimization cache
    private float lastEnvUpdate = 0f;
    public float envUpdateInterval = 0.1f;
    private Color lastSkyZenithColor;
    private Color lastSkyMidColor;
    private Color lastSkyHorizonColor;
    [ContextMenu("Tick")]
    void Update()
    {
        if(autoRun_TOD)
        {
            timeOfDay += Time.deltaTime / (dayLengthInMinutes * 60f);
            if (timeOfDay > 1f) timeOfDay -= 1f;
        }

        float sunIntensityEval = sunIntensityCurve.Evaluate(timeOfDay);
        float moonIntensityEval = moonIntensityCurve.Evaluate(timeOfDay);
        float sunBlend = Mathf.Clamp01(sunBlendCurve.Evaluate(timeOfDay));
        float moonBlend = Mathf.Clamp01(moonBlendCurve.Evaluate(timeOfDay));

        float sunLightIntensity = sunIntensityEval * sunMultiplier;
        float moonLightIntensity = moonIntensityEval * sunMultiplier;

        float sunAngle = Mathf.Lerp(celestialTilt.x, celestialTilt.y, timeOfDay);
        float moonAngle = sunAngle;

        skyMaterial.SetFloat("_TimeOfDay", timeOfDay);

        if (sunLightIntensity > moonLightIntensity)
        {
            mainLight.transform.localRotation = Quaternion.Euler(sunAngle + sunIntensityEval, 0, 0);
            mainLight.intensity = sunLightIntensity;
            mainLight.shadowStrength = sunBlend;
            Debug.Log("Enable sun color and moon color gradients");
            //sun gradient for different colored sun 
            //mainLight.color = SunColorGradient.Evaluate(timeOfDay);
        }
        else
        {
            mainLight.transform.localRotation = Quaternion.Euler((moonAngle - 180) + moonIntensityEval, 0, 0);
            mainLight.intensity = moonLightIntensity;
            mainLight.shadowStrength = moonBlend;
            //moon gradient for different colored sun 
            //mainLight.color = MoonColorGradient.Evaluate(timeOfDay);
        }


        fakeSun.transform.localRotation = Quaternion.Euler(sunAngle + sunIntensityEval * sunOffset, 0, 0);
        fakeMoon.transform.localRotation = Quaternion.Euler((moonAngle - 180) + moonIntensityEval * moonOffset, 0, 0);
        skyMaterial.SetVector("_SunRotation", fakeSun.transform.forward);
        skyMaterial.SetVector("_MoonRotation", fakeMoon.transform.forward);


        GetPhaseData(timeOfDay, out SkyPhase currentPhase, out SkyPhase nextPhase, out float t);

        // Throttled environment + ambient updates
        if (Time.time - lastEnvUpdate > envUpdateInterval)
        {
            //update emabient light source
            UpdateAmbientLighting(currentPhase, nextPhase, t, sunAngle);
            //update environement gradient in lighting tab
            UpdateAmbientEnvironment(currentPhase, nextPhase, t);
            lastEnvUpdate = Time.time;
        }

    }

    public void UpdateAmbientEnvironment(SkyPhase currentPhase, SkyPhase nextPhase, float t)
    {
        Color blendedHorizon, blendedMid, blendedZenith;
        GetBlendedSkyColor(currentPhase, nextPhase, t, out blendedHorizon, out blendedMid, out blendedZenith);

        if(lastSkyZenithColor != blendedZenith)
        {
            skyMaterial.SetColor("_DayZenithColor", blendedZenith);
            lastSkyZenithColor = blendedZenith;
        }

        if(lastSkyMidColor != blendedMid)
        {
            skyMaterial.SetColor("_DayMidColor", blendedMid);
            lastSkyMidColor = blendedMid;
        }

        if(lastSkyHorizonColor != blendedHorizon)
        {
            skyMaterial.SetColor("_DayHorizonColor", blendedHorizon);
            lastSkyHorizonColor = blendedHorizon;
        }
    }

    public void UpdateAmbientLighting(SkyPhase currentPhase, SkyPhase nextPhase, float t, float sunAngle)
    {
        //get ambient light values
        Color amb_blendedHorizon, amb_blendedMid, amb_blendedZenith;
        GetBlendedAmbientSkyColor(currentPhase, nextPhase, t, out amb_blendedHorizon, out amb_blendedMid, out amb_blendedZenith);

        // Override environment lighting using separate components
        RenderSettings.ambientSkyColor = amb_blendedZenith * ambientZenith_multiplier;
        RenderSettings.ambientEquatorColor = amb_blendedMid * ambientMid_multiplier;
        RenderSettings.ambientGroundColor = amb_blendedHorizon * ambientHorizon_multiplier;

        //setup ambient light color
        AmbientLightSource(sunAngle, amb_blendedMid);

    }

    public void AmbientLightSource(float sunAngle, Color blendedMid)
    {
        // Set the ambient fill light (reverse angle of sun, low intensity)
        if (ambientFillLight != null)
        {
            ambientFillLight.transform.rotation = Quaternion.Euler(-sunAngle, 0, 0);
            ambientFillLight.color = blendedMid;
            float _amb = mainLight.intensity - .1f;
            _amb = Mathf.Min(ambientLight_Intensity, _amb);
            ambientFillLight.intensity = _amb;
        }
    }

    void GetPhaseData(float time, out SkyPhase current, out SkyPhase next, out float t)
    {
        if (time > phaseTimeShifts[0] && time < phaseTimeShifts[1])
        {
            current = SkyPhase.Sunrise;
            next = SkyPhase.Day;
            t = Mathf.InverseLerp(phaseTimeShifts[0], phaseTimeShifts[1], time);

        }
        else if (time > phaseTimeShifts[0] && time < phaseTimeShifts[2])
        {
            current = SkyPhase.Day;
            next = SkyPhase.Sunset;
            t = Mathf.InverseLerp(phaseTimeShifts[1], phaseTimeShifts[2], time);

        }
        else if(time > phaseTimeShifts[0] && time < phaseTimeShifts[3])
        {
            current = SkyPhase.Sunset;
            next = SkyPhase.Night;
            t = Mathf.InverseLerp(phaseTimeShifts[2], phaseTimeShifts[3], time);

        }
        else if (time > phaseTimeShifts[0] && time >= phaseTimeShifts[3])
        {
            current = SkyPhase.Night;
            next = SkyPhase.Sunrise;
            t = Mathf.InverseLerp(phaseTimeShifts[3], 1f, time);

        }
        else
        {
            current = SkyPhase.Sunrise;
            next = SkyPhase.Day;
            t = Mathf.InverseLerp(0f, 1f, time);

        }
    }

    void GetBlendedSkyColor(SkyPhase current, SkyPhase next, float t, out Color horizon, out Color mid, out Color zenith)
    {
        GetSkyColorsForPhase(current, out Color h1, out Color m1, out Color z1);
        GetSkyColorsForPhase(next, out Color h2, out Color m2, out Color z2);

        horizon = Color.Lerp(h1, h2, t);
        mid = Color.Lerp(m1, m2, t);
        zenith = Color.Lerp(z1, z2, t);
    }

    void GetSkyColorsForPhase(SkyPhase phase, out Color horizon, out Color mid, out Color zenith)
    {
        switch (phase)
        {
            case SkyPhase.Sunrise:
                horizon = sunriseColors.horizonColor;
                mid = sunriseColors.midColor;
                zenith = sunriseColors.zenithColor;
                break;
            case SkyPhase.Day:
                horizon = dayColors.horizonColor;
                mid = dayColors.midColor;
                zenith = dayColors.zenithColor;
                break;
            case SkyPhase.Sunset:
                horizon = sunsetColors.horizonColor;
                mid = sunsetColors.midColor;
                zenith = sunsetColors.zenithColor;
                break;
            case SkyPhase.Night:
                horizon = nightColors.horizonColor;
                mid = nightColors.midColor;
                zenith = nightColors.zenithColor;
                break;
            default:
                horizon = Color.black;
                mid = Color.black;
                zenith = Color.black;
                break;
        }
    }

    void GetBlendedAmbientSkyColor(SkyPhase current, SkyPhase next, float t, out Color horizon, out Color mid, out Color zenith)
    {
        GetAmbientSkyColorsForPhase(current, out Color h1, out Color m1, out Color z1);
        GetAmbientSkyColorsForPhase(next, out Color h2, out Color m2, out Color z2);

        horizon = Color.Lerp(h1, h2, t);
        mid = Color.Lerp(m1, m2, t);
        zenith = Color.Lerp(z1, z2, t);
    }

    void GetAmbientSkyColorsForPhase(SkyPhase phase, out Color horizon, out Color mid, out Color zenith)
    {
        switch (phase)
        {
            case SkyPhase.Sunrise:
                horizon = amb_sunriseColors.horizonColor;
                mid = amb_sunriseColors.midColor;
                zenith = amb_sunriseColors.zenithColor;
                break;
            case SkyPhase.Day:
                horizon = amb_dayColors.horizonColor;
                mid = amb_dayColors.midColor;
                zenith = amb_dayColors.zenithColor;
                break;
            case SkyPhase.Sunset:
                horizon = amb_sunsetColors.horizonColor;
                mid = amb_sunsetColors.midColor;
                zenith = amb_sunsetColors.zenithColor;
                break;
            case SkyPhase.Night:
                horizon = amb_nightColors.horizonColor;
                mid = amb_nightColors.midColor;
                zenith = amb_nightColors.zenithColor;
                break;
            default:
                horizon = Color.black;
                mid = Color.black;
                zenith = Color.black;
                break;
        }
    }


    Color CalculateBlendedSkyColor(Vector3 worldDirection, Color horizonColor, Color midColor, Color zenithColor)
    {
        Vector3 dir = worldDirection.normalized;
        float y = Mathf.Clamp(dir.y, -1f, 1f);
        float t = Mathf.InverseLerp(-1f, 1f, y);

        Color bottomBlend = Color.Lerp(horizonColor, midColor, t);
        Color finalColor = Color.Lerp(bottomBlend, zenithColor, t);

        return finalColor;
    }

    [ContextMenu("CopyColors")]
    public void CopyColors()
    {
        sunriseColors.horizonColor = skyMaterial.GetColor("_SunriseHorizonColor");
        sunriseColors.midColor = skyMaterial.GetColor("_SunriseMidColor");
        sunriseColors.zenithColor = skyMaterial.GetColor("_SunriseZenithColor");

        dayColors.horizonColor = skyMaterial.GetColor("_DayHorizonColor");
        dayColors.midColor = skyMaterial.GetColor("_DayMidColor");
        dayColors.zenithColor = skyMaterial.GetColor("_DayZenithColor");

        sunsetColors.horizonColor = skyMaterial.GetColor("_SunsetHorizonColor");
        sunsetColors.midColor = skyMaterial.GetColor("_SunsetMidColor");
        sunsetColors.zenithColor = skyMaterial.GetColor("_SunsetZenithColor");

        nightColors.horizonColor = skyMaterial.GetColor("_NightHorizonColor");
        nightColors.midColor = skyMaterial.GetColor("_NightMidColor");
        nightColors.zenithColor = skyMaterial.GetColor("_NightZenithColor");
    }

    [ContextMenu("CopyToAmbient")]
    public void CopyToAmbient()
    {
        amb_sunriseColors.horizonColor = sunriseColors.horizonColor;
        amb_sunriseColors.midColor = sunriseColors.midColor;
        amb_sunriseColors.zenithColor = sunriseColors.zenithColor;

        amb_dayColors.horizonColor = dayColors.horizonColor;
        amb_dayColors.midColor = dayColors.midColor;
        amb_dayColors.zenithColor = dayColors.zenithColor;

        amb_sunsetColors.horizonColor = sunsetColors.horizonColor;
        amb_sunsetColors.midColor = sunsetColors.midColor;
        amb_sunsetColors.zenithColor = sunsetColors.zenithColor ;

        amb_nightColors.horizonColor = nightColors.horizonColor;
        amb_nightColors.midColor = nightColors.midColor;
        amb_nightColors.zenithColor = nightColors.zenithColor;
    }
}
