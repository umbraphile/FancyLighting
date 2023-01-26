﻿using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using Terraria;
using Terraria.ModLoader.Config;

namespace FancyLighting.Config;

[Label("Fancy Lighting Settings")]
public sealed class LightingConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;

    // Handled automatically by tModLoader
    public static LightingConfig Instance;

    internal bool ModifyCameraModeRendering() => SmoothLightingEnabled() || AmbientOcclusionEnabled();
    internal bool ModifyBackgroundRendering()
        => SmoothLightingEnabled() && (FancyLightingEngineEnabled() || DrawOverbright());
    internal bool SmoothLightingEnabled() => UseSmoothLighting && Lighting.UsingNewLighting;
    internal bool UseBicubicScaling() => LightMapRenderMode != RenderMode.Bilinear;
    internal bool DrawOverbright() => LightMapRenderMode == RenderMode.BicubicOverbright;
    internal bool UseNormalMaps() => NormalMapsStrength != 0;
    internal float NormalMapsMultiplier() => NormalMapsStrength / 100f;
    internal bool AmbientOcclusionEnabled() => UseAmbientOcclusion && Lighting.UsingNewLighting;
    internal float AmbientOcclusionAlpha() => 1f - AmbientOcclusionIntensity / 100f;
    internal bool FancyLightingEngineEnabled() => UseFancyLightingEngine && Lighting.UsingNewLighting;
    internal float FancyLightingEngineExitMultiplier() => 1f - FancyLightingEngineLightLoss / 100f;
    internal bool CustomSkyColorsEnabled() => UseCustomSkyColors && Lighting.UsingNewLighting;
    internal bool HiDefFeaturesEnabled()
        => UseHiDefFeatures && Main.instance.GraphicsDevice.GraphicsProfile == GraphicsProfile.HiDef;

    // Presets
    [Header("Presets")]

    // Serialize this last
    [JsonProperty(Order = 1000)]
    [Label("Settings Preset")]
    [Tooltip("A preset for the above settings may be chosen")]
    [DefaultValue(DefaultOptions.ConfigPreset)]
    [DrawTicks]
    public Preset ConfigPreset
    {
        get => _preset;
        set
        {
            if (value == Preset.CustomPreset)
            {
                PresetOptions currentOptions = new(this);
                bool isPreset
                    = PresetOptions.PresetLookup.TryGetValue(currentOptions, out Preset preset);
                if (isPreset)
                {
                    _preset = preset;
                }
                else
                {
                    _preset = Preset.CustomPreset;
                }
            }
            else
            {
                bool isPresetOptions
                    = PresetOptions.PresetOptionsLookup.TryGetValue(value, out PresetOptions presetOptions);
                if (isPresetOptions)
                {
                    presetOptions.CopyTo(this);
                    _preset = value;
                }
                else
                {
                    _preset = Preset.CustomPreset;
                }
            }
        }
    }
    private Preset _preset;

    // Smooth Lighting, Normal Maps, Overbright
    [Header("Smooth Lighting")]

    [Label("Enable Smooth Lighting")]
    [Tooltip("Toggles whether to use smooth lighting\nIf disabled, vanilla lighting visuals are used\nRequires lighting to be set to color")]
    [DefaultValue(DefaultOptions.UseSmoothLighting)]
    public bool UseSmoothLighting
    {
        get => _useSmoothLighting;
        set
        {
            _useSmoothLighting = value;
            ConfigPreset = Preset.CustomPreset;
        }
    }
    private bool _useSmoothLighting;

    [Label("Blur Light Map")]
    [Tooltip("Toggles whether to blur the light map\nApplies a per-tile blur to the light map before rendering\nSmooths sharp light transitions\nDisabling this setting may slightly increase performance")]
    [DefaultValue(DefaultOptions.UseLightMapBlurring)]
    public bool UseLightMapBlurring
    {
        get => _useLightMapBlurring;
        set
        {
            _useLightMapBlurring = value;
            ConfigPreset = Preset.CustomPreset;
        }
    }
    private bool _useLightMapBlurring;

    [Label("Light Map Render Mode")]
    [Tooltip("Controls how the light map is rendered\nAffects the smoothness of lighting\nBicubic upscaling is smoother than bilinear upscaling\nOverbright rendering increases the maximum brightness of light")]
    [DefaultValue(DefaultOptions.LightMapRenderMode)]
    [DrawTicks]
    public RenderMode LightMapRenderMode
    {
        get => _lightMapRenderMode;
        set
        {
            _lightMapRenderMode = value;
            ConfigPreset = Preset.CustomPreset;
        }
    }
    private RenderMode _lightMapRenderMode;

    [Label("Simulated Normal Maps Strength")]
    [Tooltip("Controls the strength of simulated normal maps\nWhen not 0, tiles have simulated normal maps and appear bumpy\nSet to 0 to disable")]
    [Range(0, 200)]
    [Increment(25)]
    [DefaultValue(DefaultOptions.NormalMapsStrength)]
    [Slider]
    [DrawTicks]
    public int NormalMapsStrength
    {
        get => _normalMapsStrength;
        set
        {
            _normalMapsStrength = value;
            ConfigPreset = Preset.CustomPreset;
        }
    }
    private int _normalMapsStrength;

    [Label("Use Higher-Quality Normal Maps")]
    [Tooltip("Toggles between regular and higher-quality simulated normal map shaders\nWhen enabled, uses a higher-quality normal map simulation\nMay reduce performance when enabled")]
    [DefaultValue(DefaultOptions.QualityNormalMaps)]
    public bool QualityNormalMaps
    {
        get => _useQualityNormalMaps;
        set
        {
            _useQualityNormalMaps = value;
            ConfigPreset = Preset.CustomPreset;
        }
    }
    private bool _useQualityNormalMaps;

    [Label("Use Fine Normal Maps")]
    [Tooltip("Toggles between coarse and fine simulated normal maps\nCoarse normal maps have 2x2 resolution, and fine 1x1\nRecommended to enable when using HD textures")]
    [DefaultValue(DefaultOptions.FineNormalMaps)]
    public bool FineNormalMaps
    {
        get => _useFineNormalMaps;
        set
        {
            _useFineNormalMaps = value;
            ConfigPreset = Preset.CustomPreset;
        }
    }
    private bool _useFineNormalMaps;

    [Label("(Debug) Render Only Lighting")]
    [Tooltip("When enabled, tile, wall, and background textures aren't rendered")]
    [DefaultValue(DefaultOptions.RenderOnlyLight)]
    public bool RenderOnlyLight
    {
        get => _renderOnlyLight;
        set
        {
            _renderOnlyLight = value;
            ConfigPreset = Preset.CustomPreset;
        }
    }
    private bool _renderOnlyLight;

    // Ambient Occlusion
    [Header("Ambient Occlusion")]

    [Label("Enable Ambient Occlusion")]
    [Tooltip("Toggles whether to use ambient occlusion\nIf enabled, tiles produce shadows in front of walls\nRequires lighting to be set to color")]
    [DefaultValue(DefaultOptions.UseAmbientOcclusion)]
    public bool UseAmbientOcclusion
    {
        get => _useAmbientOcclusion;
        set
        {
            _useAmbientOcclusion = value;
            ConfigPreset = Preset.CustomPreset;
        }
    }
    private bool _useAmbientOcclusion;

    [Label("Enable Ambient Occlusion From Non-Solid Tiles")]
    [Tooltip("Toggles whether non-solid blocks generate ambient occlusion\nNon-solid tiles generate weaker ambient occlusion\nPrimarily affects furniture and torches\nNot all non-solid tiles are affected")]
    [DefaultValue(DefaultOptions.DoNonSolidAmbientOcclusion)]
    public bool DoNonSolidAmbientOcclusion
    {
        get => _doNonSolidAmbientOcclusion;
        set
        {
            _doNonSolidAmbientOcclusion = value;
            ConfigPreset = Preset.CustomPreset;
        }
    }
    private bool _doNonSolidAmbientOcclusion;

    [Label("Enable Ambient Occlusion From Tile Entities")]
    [Tooltip("Toggles whether tile entities generate ambient occlusion\nTile entities generate weaker ambient occlusion\nPrimarily affects moving, non-solid tiles (e.g., tiles affected by wind)")]
    [DefaultValue(DefaultOptions.DoTileEntityAmbientOcclusion)]
    public bool DoTileEntityAmbientOcclusion
    {
        get => _doTileEntityAmbientOcclusion;
        set
        {
            _doTileEntityAmbientOcclusion = value;
            ConfigPreset = Preset.CustomPreset;
        }
    }
    private bool _doTileEntityAmbientOcclusion;

    [Label("Ambient Occlusion Radius")]
    [Tooltip("Controls the radius of blur used in ambient occlusion\nHigher values correspond to a larger blur radius\nHigher values may reduce performance")]
    [Range(1, 6)]
    [Increment(1)]
    [DefaultValue(DefaultOptions.AmbientOcclusionRadius)]
    [Slider]
    [DrawTicks]
    public int AmbientOcclusionRadius
    {
        get => _ambientOcclusionRadius;
        set
        {
            _ambientOcclusionRadius = value;
            ConfigPreset = Preset.CustomPreset;
        }
    }
    private int _ambientOcclusionRadius;

    [Label("Ambient Occlusion Intensity")]
    [Tooltip("Controls the intensity of shadows in ambient occlusion\nHigher values correspond to darker ambient occlusion shadows")]
    [Range(5, 100)]
    [Increment(5)]
    [DefaultValue(DefaultOptions.AmbientOcclusionIntensity)]
    [Slider]
    [DrawTicks]
    public int AmbientOcclusionIntensity
    {
        get => _ambientOcclusionIntensity;
        set
        {
            _ambientOcclusionIntensity = value;
            ConfigPreset = Preset.CustomPreset;
        }
    }
    private int _ambientOcclusionIntensity;

    // Fancy Lighting Engine
    [Header("Lighting Engine")]

    [Label("Enable Fancy Lighting Engine")]
    [Tooltip("Toggles whether to use a modified lighting engine\nWhen enabled, light is spread more accurately\nShadows should face away from light sources and be more noticeable\nPerformance is significantly reduced in areas with more light sources\nRequires lighting to be set to color")]
    [DefaultValue(DefaultOptions.UseFancyLightingEngine)]
    public bool UseFancyLightingEngine
    {
        get => _useFancyLightingEngine;
        set
        {
            _useFancyLightingEngine = value;
            ConfigPreset = Preset.CustomPreset;
        }
    }
    private bool _useFancyLightingEngine;

    [Label("Temporal Optimization")]
    [Tooltip("Toggles whether to use temporal optimization with the fancy lighting engine\nWhen enabled, uses data from the previous update to optimize lighting calculations\nMakes lighting quicker in more intensly lit areas\nMay sometimes cause lighting quality to be slightly reduced")]
    [DefaultValue(DefaultOptions.FancyLightingEngineUseTemporal)]
    public bool FancyLightingEngineUseTemporal
    {
        get => _fancyLightingEngineUseTemporal;
        set
        {
            _fancyLightingEngineUseTemporal = value;
            ConfigPreset = Preset.CustomPreset;
        }
    }
    private bool _fancyLightingEngineUseTemporal;

    [Label("Brighter Lighting")]
    [Tooltip("Toggles whether to make lighting slightly brighter\nWhen disabled, lighting is slightly darker than with vanilla lighting\nMay reduce performance when enabled")]
    [DefaultValue(DefaultOptions.FancyLightingEngineMakeBrighter)]
    public bool FancyLightingEngineMakeBrighter
    {
        get => _fancyLightingEngineMakeBrighter;
        set
        {
            _fancyLightingEngineMakeBrighter = value;
            ConfigPreset = Preset.CustomPreset;
        }
    }
    private bool _fancyLightingEngineMakeBrighter;

    [Label("Light Loss (%) When Exiting Solid Blocks")]
    [Tooltip("Controls how much light is lost when light exits a solid block into the air\nHigher values correspond to darker shadows")]
    [Range(0, 100)]
    [Increment(5)]
    [DefaultValue(DefaultOptions.FancyLightingEngineLightLoss)]
    [Slider]
    [DrawTicks]
    public int FancyLightingEngineLightLoss
    {
        get => _fancyLightingEngineLightLoss;
        set
        {
            _fancyLightingEngineLightLoss = value;
            ConfigPreset = Preset.CustomPreset;
        }
    }
    private int _fancyLightingEngineLightLoss;

    // Sky Color
    [Header("Sky Color")]

    [Label("Enable Fancy Sky Colors")]
    [Tooltip("Toggles whether to use modified sky colors\nIf disabled, vanilla sky colors are used instead")]
    [DefaultValue(DefaultOptions.UseCustomSkyColors)]
    public bool UseCustomSkyColors
    {
        get => _useCustomSkyColors;
        set
        {
            _useCustomSkyColors = value;
            ConfigPreset = Preset.CustomPreset;
        }
    }
    private bool _useCustomSkyColors;

    // Other Settings
    [Header("General")]

    [Label("Thread Count")]
    [Tooltip("Controls how many threads smooth lighting and the fancy lighting engine use\nThe default value should result in the best performance")]
    [Range(1, 24)]
    [Increment(1)]
    [DefaultValue(DefaultOptions.ThreadCount)]
    public int ThreadCount
    {
        get => _threadCount;
        set
        {
            _threadCount = value;
            ConfigPreset = Preset.CustomPreset;
        }
    }
    private int _threadCount;

    [Label("Use HiDef Features")]
    [Tooltip("Toggles whether to use features of the HiDef graphics profile\nRequires roughly a DirectX 10-capable GPU to have any effect\nIf enabled, some visual effects are improved\nMay decrease rendering performance when enabled")]
    [DefaultValue(DefaultOptions.UseHiDefFeatures)]
    public bool UseHiDefFeatures
    {
        get => _useHiDefFeatures;
        set
        {
            _useHiDefFeatures = value;
            ConfigPreset = Preset.CustomPreset;
        }
    }
    private bool _useHiDefFeatures;
}
