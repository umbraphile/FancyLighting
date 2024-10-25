﻿using System.Collections.Generic;
using System.Linq;

namespace FancyLighting.Config;

internal record PresetOptions
{
    public bool UseHiDefFeatures { get; init; } = DefaultOptions.UseHiDefFeatures;

    public bool UseSmoothLighting { get; init; } = DefaultOptions.UseSmoothLighting;
    public bool UseLightMapBlurring { get; init; } = DefaultOptions.UseLightMapBlurring;
    public bool UseEnhancedBlurring { get; init; } = DefaultOptions.UseEnhancedBlurring;
    public bool UseLightMapToneMapping { get; init; } =
        DefaultOptions.UseLightMapToneMapping;
    public bool SimulateNormalMaps { get; init; } = DefaultOptions.SimulateNormalMaps;
    public RenderMode LightMapRenderMode { get; init; } =
        DefaultOptions.LightMapRenderMode;
    public bool OverbrightWaterfalls { get; init; } = DefaultOptions.OverbrightWaterfalls;
    public bool OverbrightNPCsAndPlayer { get; init; } =
        DefaultOptions.OverbrightNPCsAndPlayer;
    public bool OverbrightProjectiles { get; init; } =
        DefaultOptions.OverbrightProjectiles;
    public bool OverbrightDustAndGore { get; init; } =
        DefaultOptions.OverbrightDustAndGore;
    public bool OverbrightItems { get; init; } = DefaultOptions.OverbrightItems;
    public bool OverbrightRain { get; init; } = DefaultOptions.OverbrightRain;

    public bool UseAmbientOcclusion { get; init; } = DefaultOptions.UseAmbientOcclusion;
    public bool DoNonSolidAmbientOcclusion { get; init; } =
        DefaultOptions.DoNonSolidAmbientOcclusion;
    public bool DoTileEntityAmbientOcclusion { get; init; } =
        DefaultOptions.DoTileEntityAmbientOcclusion;

    public bool UseFancyLightingEngine { get; init; } =
        DefaultOptions.UseFancyLightingEngine;
    public bool FancyLightingEngineUseTemporal { get; init; } =
        DefaultOptions.FancyLightingEngineUseTemporal;
    public LightingEngineMode FancyLightingEngineMode { get; init; } =
        DefaultOptions.FancyLightingEngineMode;
    public bool SimulateGlobalIllumination { get; init; } =
        DefaultOptions.SimulateGlobalIllumination;

    public PresetOptions() { }

    public PresetOptions(LightingConfig config)
    {
        UseHiDefFeatures = config.UseHiDefFeatures;

        UseSmoothLighting = config.UseSmoothLighting;
        UseLightMapBlurring = config.UseLightMapBlurring;
        UseEnhancedBlurring = config.UseEnhancedBlurring;
        UseLightMapToneMapping = config.UseLightMapToneMapping;
        SimulateNormalMaps = config.SimulateNormalMaps;
        LightMapRenderMode = config.LightMapRenderMode;
        OverbrightWaterfalls = config.OverbrightWaterfalls;
        OverbrightNPCsAndPlayer = config.OverbrightNPCsAndPlayer;
        OverbrightProjectiles = config.OverbrightProjectiles;
        OverbrightDustAndGore = config.OverbrightDustAndGore;
        OverbrightItems = config.OverbrightItems;
        OverbrightRain = config.OverbrightRain;

        UseAmbientOcclusion = config.UseAmbientOcclusion;
        DoNonSolidAmbientOcclusion = config.DoNonSolidAmbientOcclusion;
        DoTileEntityAmbientOcclusion = config.DoTileEntityAmbientOcclusion;

        UseFancyLightingEngine = config.UseFancyLightingEngine;
        FancyLightingEngineUseTemporal = config.FancyLightingEngineUseTemporal;
        FancyLightingEngineMode = config.FancyLightingEngineMode;
        SimulateGlobalIllumination = config.SimulateGlobalIllumination;
    }

    public static PresetOptions VanillaPresetOptions =
        new()
        {
            UseHiDefFeatures = false,

            UseSmoothLighting = false,
            UseEnhancedBlurring = false,
            UseLightMapToneMapping = false,
            SimulateNormalMaps = false,
            LightMapRenderMode = RenderMode.Bilinear,

            UseAmbientOcclusion = false,
            DoNonSolidAmbientOcclusion = false,
            DoTileEntityAmbientOcclusion = false,

            UseFancyLightingEngine = false,
            FancyLightingEngineMode = LightingEngineMode.One,
            SimulateGlobalIllumination = false,
        };

    public static PresetOptions MinimalPresetOptions =
        new()
        {
            UseHiDefFeatures = false,

            UseSmoothLighting = true,
            UseEnhancedBlurring = false,
            UseLightMapToneMapping = false,
            SimulateNormalMaps = false,
            LightMapRenderMode = RenderMode.Bilinear,

            UseAmbientOcclusion = false,
            DoNonSolidAmbientOcclusion = false,
            DoTileEntityAmbientOcclusion = false,

            UseFancyLightingEngine = false,
            FancyLightingEngineMode = LightingEngineMode.One,
            SimulateGlobalIllumination = false,
        };

    public static PresetOptions LowPresetOptions = new();

    public static PresetOptions MediumPresetOptions =
        new()
        {
            UseHiDefFeatures = false,

            UseSmoothLighting = true,
            UseEnhancedBlurring = true,
            UseLightMapToneMapping = true,
            SimulateNormalMaps = false,
            LightMapRenderMode = RenderMode.Bicubic,

            UseAmbientOcclusion = true,
            DoNonSolidAmbientOcclusion = true,
            DoTileEntityAmbientOcclusion = true,

            UseFancyLightingEngine = true,
            FancyLightingEngineMode = LightingEngineMode.Two,
            SimulateGlobalIllumination = false,
        };

    public static PresetOptions HighPresetOptions =
        new()
        {
            UseHiDefFeatures = false,

            UseSmoothLighting = true,
            UseEnhancedBlurring = true,
            UseLightMapToneMapping = true,
            SimulateNormalMaps = true,
            LightMapRenderMode = RenderMode.BicubicOverbright,

            UseAmbientOcclusion = true,
            DoNonSolidAmbientOcclusion = true,
            DoTileEntityAmbientOcclusion = true,

            UseFancyLightingEngine = true,
            FancyLightingEngineMode = LightingEngineMode.Two,
            SimulateGlobalIllumination = true,
        };

    public static PresetOptions UltraPresetOptions =
        new()
        {
            UseHiDefFeatures = true,

            UseSmoothLighting = true,
            UseEnhancedBlurring = true,
            UseLightMapToneMapping = true,
            SimulateNormalMaps = true,
            LightMapRenderMode = RenderMode.BicubicOverbright,
            OverbrightWaterfalls = true,

            UseAmbientOcclusion = true,
            DoNonSolidAmbientOcclusion = true,
            DoTileEntityAmbientOcclusion = true,

            UseFancyLightingEngine = true,
            FancyLightingEngineMode = LightingEngineMode.Four,
            SimulateGlobalIllumination = true,
        };

    public static PresetOptions ExtremePresetOptions =
        new()
        {
            UseHiDefFeatures = true,

            UseSmoothLighting = true,
            UseEnhancedBlurring = true,
            UseLightMapToneMapping = true,
            SimulateNormalMaps = true,
            LightMapRenderMode = RenderMode.BicubicOverbright,
            OverbrightWaterfalls = true,
            OverbrightNPCsAndPlayer = true,
            OverbrightProjectiles = true,
            OverbrightDustAndGore = true,
            OverbrightRain = true,
            OverbrightItems = true,

            UseAmbientOcclusion = true,
            DoNonSolidAmbientOcclusion = true,
            DoTileEntityAmbientOcclusion = true,

            UseFancyLightingEngine = true,
            FancyLightingEngineMode = LightingEngineMode.Four,
            SimulateGlobalIllumination = true,
        };

    public static Dictionary<PresetOptions, Preset> PresetLookup =
        new()
        {
            [VanillaPresetOptions] = Preset.VanillaPreset,
            [MinimalPresetOptions] = Preset.MinimalPreset,
            [LowPresetOptions] = Preset.LowPreset,
            [MediumPresetOptions] = Preset.MediumPreset,
            [HighPresetOptions] = Preset.HighPreset,
            [UltraPresetOptions] = Preset.UltraPreset,
            [ExtremePresetOptions] = Preset.ExtremePreset,
        };

    public static Dictionary<Preset, PresetOptions> PresetOptionsLookup =
        PresetLookup.ToDictionary(entry => entry.Value, entry => entry.Key);
}
