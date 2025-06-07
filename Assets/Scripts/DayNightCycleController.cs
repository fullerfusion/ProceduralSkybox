using UnityEngine;
using UnityEngine.UIElements;

public class DayNightCycleController : MonoBehaviour
{
    [Range(0f, 1f)]
    public float timeOfDay;
    public float dayLengthInMinutes = 2f; // full day = 2 minutes in real time
    public bool autoRun_TOD = true;

    [SerializeField] private Transform celestialPivot;
    [SerializeField] private float axialTilt = 23.5f; // earth-like
    public Vector2 celestialTilt = new Vector2(-90,270);
    public Light sunLight;
    public Light moonLight;
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


    void Update()
    {
        if(autoRun_TOD)
        {
            timeOfDay += Time.deltaTime / (dayLengthInMinutes * 60f);
            if (timeOfDay > 1f) timeOfDay -= 1f;
        }

        // Compute blend weights using artist-defined curves
        float sunBlend = Mathf.Clamp01(sunBlendCurve.Evaluate(timeOfDay));
        float moonBlend = Mathf.Clamp01(moonBlendCurve.Evaluate(timeOfDay));

        // Apply intensity and shadow transitions
        sunLight.intensity = sunIntensityCurve.Evaluate(timeOfDay) * sunBlend * sunMultiplier;
        moonLight.intensity = moonIntensityCurve.Evaluate(timeOfDay) * moonBlend * sunMultiplier;

        sunLight.shadowStrength = sunBlend;
        moonLight.shadowStrength = moonBlend;

        skyMaterial.SetFloat("_TimeOfDay", timeOfDay);

        float sunAngle = Mathf.Lerp(celestialTilt.x, celestialTilt.y, timeOfDay);        
        float moonAngle = sunAngle - 180f;

        sunLight.transform.localRotation = Quaternion.Euler(sunAngle, 0, 0);
        moonLight.transform.localRotation = Quaternion.Euler(moonAngle, 0, 0);
        skyMaterial.SetVector("_SunRotation", sunLight.transform.forward);

        SkyPhase currentPhase;
        SkyPhase nextPhase;
        float t;
        GetPhaseData(timeOfDay, out currentPhase, out nextPhase, out t);

        //sun gradient for different colored sun 
        sunLight.color = SunColorGradient.Evaluate(timeOfDay);
        //sun intensity
        sunLight.intensity = sunIntensityCurve.Evaluate(timeOfDay) * sunMultiplier;
        //moon intensity
        moonLight.intensity = moonIntensityCurve.Evaluate(timeOfDay) * sunMultiplier;

        //update emabient light source
        UpdateAmbientLighting(currentPhase, nextPhase, t, sunAngle);
        //update environement gradient in lighting tab
        UpdateAmbientEnvironment(currentPhase, nextPhase, t, sunAngle);

    }

    public void UpdateAmbientEnvironment(SkyPhase currentPhase, SkyPhase nextPhase, float t, float sunAngle)
    {
        Color blendedHorizon, blendedMid, blendedZenith;
        GetBlendedSkyColor(currentPhase, nextPhase, t, out blendedHorizon, out blendedMid, out blendedZenith);
        skyMaterial.SetColor("_DayZenithColor", blendedZenith);
        skyMaterial.SetColor("_DayMidColor", blendedMid);
        skyMaterial.SetColor("_DayHorizonColor", blendedHorizon);
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
            ambientFillLight.intensity = ambientLight_Intensity;
        }
    }

    void GetPhaseData(float time, out SkyPhase current, out SkyPhase next, out float t)
    {
        if (time > 0.125f && time < 0.375f)
        {
            current = SkyPhase.Sunrise;
            next = SkyPhase.Day;
            t = Mathf.InverseLerp(0.15f, 0.375f, time);
        }
        else if (time > 0.125f && time < 0.625f)
        {
            current = SkyPhase.Day;
            next = SkyPhase.Sunset;
            t = Mathf.InverseLerp(0.375f, 0.625f, time);
        }
        else if(time > 0.125f && time < 0.875f)
        {
            current = SkyPhase.Sunset;
            next = SkyPhase.Night;
            t = Mathf.InverseLerp(0.625f, 0.875f, time);
        }
        else if (time > 0.125f && time >= 0.875f)
        {
            current = SkyPhase.Night;
            next = SkyPhase.Sunrise;
            t = Mathf.InverseLerp(.875f, 1f, time);
        }
        else
        {
            current = SkyPhase.Sunrise;
            next = SkyPhase.Day;
            t = Mathf.InverseLerp(0, 1f, time);
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
