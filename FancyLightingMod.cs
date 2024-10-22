using System;
using System.Collections.Generic;
using System.Reflection;
using FancyLighting.Config;
using FancyLighting.LightingEngines;
using FancyLighting.Util;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.Drawing;
using Terraria.Graphics.Capture;
using Terraria.Graphics.Light;
using Terraria.ID;
using Terraria.ModLoader;

namespace FancyLighting;

public sealed class FancyLightingMod : Mod
{
    public static BlendState MultiplyBlend { get; private set; }

    internal static bool _overrideLightColor;
    internal static bool _overrideLightColorGamma;
    internal static bool _inCameraMode;

    private SmoothLighting _smoothLightingInstance;
    private AmbientOcclusion _ambientOcclusionInstance;
    private ICustomLightingEngine _fancyLightingEngineInstance;

    internal FieldInfo field_activeEngine;
    private FieldInfo field_activeLightMap;
    internal FieldInfo field_workingProcessedArea;
    internal FieldInfo field_colors;
    internal FieldInfo field_mask;

    private delegate void TileDrawingMethod(
        Terraria.GameContent.Drawing.TileDrawing self
    );

    private TileDrawingMethod method_DrawMultiTileVines;
    private TileDrawingMethod method_DrawMultiTileGrass;
    private TileDrawingMethod method_DrawVoidLenses;
    private TileDrawingMethod method_DrawTeleportationPylons;
    private TileDrawingMethod method_DrawMasterTrophies;
    private TileDrawingMethod method_DrawGrass;
    private TileDrawingMethod method_DrawAnyDirectionalGrass;
    private TileDrawingMethod method_DrawTrees;
    private TileDrawingMethod method_DrawVines;
    private TileDrawingMethod method_DrawReverseVines;

    private static RenderTarget2D _cameraModeTarget;
    internal static Rectangle _cameraModeArea;
    private static CaptureBiome _cameraModeBiome;

    private static RenderTarget2D _screenTarget1;
    private static RenderTarget2D _screenTarget2;

    public bool OverrideLightColor
    {
        get => _overrideLightColor;
        internal set
        {
            if (value == _overrideLightColor)
            {
                return;
            }

            if (!Lighting.UsingNewLighting)
            {
                return;
            }

            var activeEngine = field_activeEngine.GetValue(null);
            if (activeEngine is not LightingEngine lightingEngine)
            {
                return;
            }

            if (value && _smoothLightingInstance._whiteLights is null)
            {
                return;
            }

            var lightMapInstance = (LightMap)
                field_activeLightMap.GetValue(lightingEngine);

            if (value)
            {
                _smoothLightingInstance._tmpLights = (Vector3[])
                    field_colors.GetValue(lightMapInstance);
            }

            field_colors.SetValue(
                lightMapInstance,
                value
                    ? _smoothLightingInstance._whiteLights
                    : _smoothLightingInstance._tmpLights
            );

            if (!value)
            {
                _smoothLightingInstance._tmpLights = null;
            }

            _overrideLightColor = value;
        }
    }

    public override void Load()
    {
        if (Main.netMode == NetmodeID.Server)
        {
            return;
        }

        _overrideLightColor = false;
        _overrideLightColorGamma = false;
        _inCameraMode = false;

        MultiplyBlend = new()
        {
            ColorBlendFunction = BlendFunction.Add,
            ColorSourceBlend = Blend.Zero,
            ColorDestinationBlend = Blend.SourceColor,
        };

        _smoothLightingInstance = new SmoothLighting(this);
        _ambientOcclusionInstance = new AmbientOcclusion();
        SetFancyLightingEngineInstance();

        field_activeEngine = typeof(Lighting).GetField(
            "_activeEngine",
            BindingFlags.NonPublic | BindingFlags.Static
        );
        field_activeLightMap = typeof(LightingEngine).GetField(
            "_activeLightMap",
            BindingFlags.NonPublic | BindingFlags.Instance
        );
        field_workingProcessedArea = typeof(LightingEngine).GetField(
            "_workingProcessedArea",
            BindingFlags.NonPublic | BindingFlags.Instance
        );
        field_colors = typeof(LightMap).GetField(
            "_colors",
            BindingFlags.NonPublic | BindingFlags.Instance
        );
        field_mask = typeof(LightMap).GetField(
            "_mask",
            BindingFlags.NonPublic | BindingFlags.Instance
        );

        method_DrawMultiTileVines = (TileDrawingMethod)
            Delegate.CreateDelegate(
                typeof(TileDrawingMethod),
                typeof(TileDrawing).GetMethod(
                    "DrawMultiTileVines",
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    []
                )
            );
        method_DrawMultiTileGrass = (TileDrawingMethod)
            Delegate.CreateDelegate(
                typeof(TileDrawingMethod),
                typeof(TileDrawing).GetMethod(
                    "DrawMultiTileGrass",
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    []
                )
            );
        method_DrawVoidLenses = (TileDrawingMethod)
            Delegate.CreateDelegate(
                typeof(TileDrawingMethod),
                typeof(TileDrawing).GetMethod(
                    "DrawVoidLenses",
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    []
                )
            );
        method_DrawTeleportationPylons = (TileDrawingMethod)
            Delegate.CreateDelegate(
                typeof(TileDrawingMethod),
                typeof(TileDrawing).GetMethod(
                    "DrawTeleportationPylons",
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    []
                )
            );
        method_DrawMasterTrophies = (TileDrawingMethod)
            Delegate.CreateDelegate(
                typeof(TileDrawingMethod),
                typeof(TileDrawing).GetMethod(
                    "DrawMasterTrophies",
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    []
                )
            );
        method_DrawGrass = (TileDrawingMethod)
            Delegate.CreateDelegate(
                typeof(TileDrawingMethod),
                typeof(TileDrawing).GetMethod(
                    "DrawGrass",
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    []
                )
            );
        method_DrawAnyDirectionalGrass = (TileDrawingMethod)
            Delegate.CreateDelegate(
                typeof(TileDrawingMethod),
                typeof(TileDrawing).GetMethod(
                    "DrawAnyDirectionalGrass",
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    []
                )
            );
        method_DrawTrees = (TileDrawingMethod)
            Delegate.CreateDelegate(
                typeof(TileDrawingMethod),
                typeof(TileDrawing).GetMethod(
                    "DrawTrees",
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    []
                )
            );
        method_DrawVines = (TileDrawingMethod)
            Delegate.CreateDelegate(
                typeof(TileDrawingMethod),
                typeof(TileDrawing).GetMethod(
                    "DrawVines",
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    []
                )
            );
        method_DrawReverseVines = (TileDrawingMethod)
            Delegate.CreateDelegate(
                typeof(TileDrawingMethod),
                typeof(TileDrawing).GetMethod(
                    "DrawReverseVines",
                    BindingFlags.NonPublic | BindingFlags.Instance,
                    []
                )
            );

        AddHooks();
        SkyColors.AddSkyColorsHooks();
    }

    public override void Unload()
    {
        Main.QueueMainThreadAction(() =>
        {
            // Do not dispose _cameraModeTarget
            // _cameraModeTarget comes from the Main class, so we don't own it
            _screenTarget1?.Dispose();
            _screenTarget2?.Dispose();
            _cameraModeTarget = null;
            _smoothLightingInstance?.Unload();
            _ambientOcclusionInstance?.Unload();
            _fancyLightingEngineInstance?.Unload();

            if (PreferencesConfig.Instance is not null)
            {
                foreach (var tileId in PreferencesConfig.Instance._vinesBlockingLight)
                {
                    if (tileId > Main.tileBlockLight.Length)
                    {
                        continue;
                    }

                    Main.tileBlockLight[tileId] = true;
                }
            }
        });

        base.Unload();
    }

    private void SetFancyLightingEngineInstance()
    {
        var mode =
            LightingConfig.Instance?.FancyLightingEngineMode ?? LightingEngineMode.One;
        switch (mode)
        {
            default:
            case LightingEngineMode.One:
                if (_fancyLightingEngineInstance is not FancyLightingEngine1X)
                {
                    _fancyLightingEngineInstance?.Unload();
                    _fancyLightingEngineInstance = new FancyLightingEngine1X();
                }
                break;

            case LightingEngineMode.Two:
                if (_fancyLightingEngineInstance is not FancyLightingEngine2X)
                {
                    _fancyLightingEngineInstance?.Unload();
                    _fancyLightingEngineInstance = new FancyLightingEngine2X();
                }
                break;

            case LightingEngineMode.Four:
                if (_fancyLightingEngineInstance is not FancyLightingEngine4X)
                {
                    _fancyLightingEngineInstance?.Unload();
                    _fancyLightingEngineInstance = new FancyLightingEngine4X();
                }
                break;
        }
    }

    internal void OnConfigChange()
    {
        _smoothLightingInstance?.CalculateSmoothLighting(false, false, true);
        _smoothLightingInstance?.CalculateSmoothLighting(true, false, true);

        if (_fancyLightingEngineInstance is not null)
        {
            SetFancyLightingEngineInstance();
        }
    }

    private void AddHooks()
    {
        Terraria.GameContent.Drawing.On_TileDrawing.ShouldTileShine +=
            _TileDrawing_ShouldTileShine;
        Terraria.Graphics.Effects.On_FilterManager.EndCapture +=
            _FilterManager_EndCapture;
        Terraria.GameContent.Drawing.On_TileDrawing.PostDrawTiles +=
            _TileDrawing_PostDrawTiles;
        Terraria.On_Main.DrawSurfaceBG += _Main_DrawSurfaceBG;
        Terraria.On_WaterfallManager.Draw += _WaterfallManager_Draw;
        Terraria.On_Main.DrawNPCs += _Main_DrawNPCs;
        Terraria.On_Main.DrawCachedNPCs += _Main_DrawCachedNPCs;
        Terraria.On_Main.DrawPlayers_BehindNPCs += _Main_DrawPlayers_BehindNPCs;
        Terraria.On_Main.DrawPlayers_AfterProjectiles +=
            _Main_DrawPlayers_AfterProjectiles;
        Terraria.On_Main.DrawProjectiles += _Main_DrawProjectiles;
        Terraria.On_Main.DrawCachedProjs += _Main_DrawCachedProjs;
        Terraria.On_Main.DrawDust += _Main_DrawDust;
        Terraria.On_Main.DrawGore += _Main_DrawGore;
        Terraria.On_Main.DrawItems += _Main_DrawItems;
        Terraria.On_Main.DrawRain += _Main_DrawRain;
        Terraria.On_Main.RenderWater += _Main_RenderWater;
        Terraria.On_Main.DrawWaters += _Main_DrawWaters;
        Terraria.On_Main.RenderBackground += _Main_RenderBackground;
        Terraria.On_Main.DrawBackground += _Main_DrawBackground;
        Terraria.On_Main.RenderBlack += _Main_RenderBlack;
        Terraria.On_Main.RenderTiles += _Main_RenderTiles;
        Terraria.On_Main.RenderTiles2 += _Main_RenderTiles2;
        Terraria.On_Main.RenderWalls += _Main_RenderWalls;
        Terraria.Graphics.Light.On_LightingEngine.ProcessBlur +=
            _LightingEngine_ProcessBlur;
        Terraria.Graphics.Light.On_LightMap.Blur += _LightMap_Blur;
        // Camera mode hooks added below
        // For some reason the order in which these are added matters to ensure that camera mode works
        // Maybe DrawCapture needs to be added last
        Terraria.On_Main.DrawLiquid += _Main_DrawLiquid;
        Terraria.On_Main.DrawWalls += _Main_DrawWalls;
        Terraria.On_Main.DrawTiles += _Main_DrawTiles;
        Terraria.On_Main.DrawCapture += _Main_DrawCapture;
    }

    private void DrawScreenOverbright(
        Action draw,
        bool spriteBatchAlreadyBegan,
        bool checkLightOnly
    )
    {
        if (_inCameraMode)
        {
            if (spriteBatchAlreadyBegan)
            {
                Main.spriteBatch.End();
            }

            Main.graphics.GraphicsDevice.SetRenderTarget(
                _smoothLightingInstance.GetCameraModeRenderTarget(_cameraModeTarget)
            );
            Main.graphics.GraphicsDevice.Clear(Color.Transparent);

            if (spriteBatchAlreadyBegan)
            {
                Main.spriteBatch.Begin(
                    SpriteSortMode.Deferred,
                    BlendState.AlphaBlend,
                    Main.DefaultSamplerState,
                    DepthStencilState.None,
                    Main.Rasterizer
                );
            }
            draw();
            if (spriteBatchAlreadyBegan)
            {
                Main.spriteBatch.End();
            }

            _smoothLightingInstance.CalculateSmoothLighting(false, true);
            _smoothLightingInstance.DrawSmoothLightingCameraMode(
                _cameraModeTarget,
                _smoothLightingInstance._cameraModeTarget1,
                false,
                false,
                true,
                true
            );

            if (spriteBatchAlreadyBegan)
            {
                Main.spriteBatch.Begin(
                    SpriteSortMode.Deferred,
                    BlendState.AlphaBlend,
                    Main.DefaultSamplerState,
                    DepthStencilState.None,
                    Main.Rasterizer
                );
            }

            return;
        }

        var target =
            checkLightOnly && PreferencesConfig.Instance.RenderOnlyLight
                ? null
                : MainRenderTarget.Get();
        if (target is null)
        {
            draw();
            return;
        }

        if (spriteBatchAlreadyBegan)
        {
            Main.spriteBatch.End();
        }

        TextureUtil.MakeSize(ref _screenTarget1, target.Width, target.Height);
        TextureUtil.MakeSize(ref _screenTarget2, target.Width, target.Height);

        Main.graphics.GraphicsDevice.SetRenderTarget(_screenTarget1);
        Main.spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.Opaque,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone
        );
        Main.spriteBatch.Draw(target, Vector2.Zero, Color.White);
        Main.spriteBatch.End();

        Main.graphics.GraphicsDevice.SetRenderTarget(_screenTarget2);
        Main.graphics.GraphicsDevice.Clear(Color.Transparent);
        if (spriteBatchAlreadyBegan)
        {
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.Transform
            );
        }
        draw();
        if (spriteBatchAlreadyBegan)
        {
            Main.spriteBatch.End();
        }

        _smoothLightingInstance.CalculateSmoothLighting(false, false);
        _smoothLightingInstance.DrawSmoothLighting(_screenTarget2, false, true, target);

        Main.graphics.GraphicsDevice.SetRenderTarget(target);
        Main.graphics.GraphicsDevice.Clear(Color.Transparent);
        Main.spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone
        );
        Main.spriteBatch.Draw(_screenTarget1, Vector2.Zero, Color.White);
        Main.spriteBatch.Draw(_screenTarget2, Vector2.Zero, Color.White);
        Main.spriteBatch.End();

        if (spriteBatchAlreadyBegan)
        {
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                Main.DefaultSamplerState,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.Transform
            );
        }
    }

    private bool _TileDrawing_ShouldTileShine(
        Terraria.GameContent.Drawing.On_TileDrawing.orig_ShouldTileShine orig,
        ushort type,
        short frameX
    ) => !_overrideLightColor && orig(type, frameX);

    // Post-processing
    private void _FilterManager_EndCapture(
        Terraria.Graphics.Effects.On_FilterManager.orig_EndCapture orig,
        Terraria.Graphics.Effects.FilterManager self,
        RenderTarget2D finalTexture,
        RenderTarget2D screenTarget1,
        RenderTarget2D screenTarget2,
        Color clearColor
    )
    {
        if (PreferencesConfig.Instance.UseSrgb)
        {
            _smoothLightingInstance.GammaToSrgb(screenTarget1, screenTarget2, clearColor);
        }

        orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);
    }

    // Tile entities
    private void _TileDrawing_PostDrawTiles(
        Terraria.GameContent.Drawing.On_TileDrawing.orig_PostDrawTiles orig,
        Terraria.GameContent.Drawing.TileDrawing self,
        bool solidLayer,
        bool forRenderTargets,
        bool intoRenderTargets
    )
    {
        if (
            intoRenderTargets
            || _ambientOcclusionInstance._drawingTileEntities
            || !LightingConfig.Instance.SmoothLightingEnabled()
            || !LightingConfig.Instance.DrawOverbright()
        )
        {
            orig(self, solidLayer, forRenderTargets, intoRenderTargets);
            return;
        }

        DrawScreenOverbright(
            () =>
                _TileDrawing_PostDrawTiles_inner(
                    orig,
                    self,
                    solidLayer,
                    forRenderTargets,
                    intoRenderTargets
                ),
            false,
            false
        );
    }

    private void _TileDrawing_PostDrawTiles_inner(
        Terraria.GameContent.Drawing.On_TileDrawing.orig_PostDrawTiles orig,
        Terraria.GameContent.Drawing.TileDrawing self,
        bool solidLayer,
        bool forRenderTargets,
        bool intoRenderTargets
    )
    {
        if (
            solidLayer
            || intoRenderTargets
            || !PreferencesConfig.Instance.RenderOnlyLight
        )
        {
            orig(self, solidLayer, forRenderTargets, intoRenderTargets);
            return;
        }

        Main.spriteBatch.Begin(
            SpriteSortMode.Immediate,
            BlendState.AlphaBlend,
            Main.DefaultSamplerState,
            DepthStencilState.None,
            Main.Rasterizer,
            null,
            Main.Transform
        );

        _smoothLightingInstance.ApplyLightOnlyShader();

        method_DrawMultiTileVines(self);
        method_DrawMultiTileGrass(self);
        method_DrawVoidLenses(self);
        method_DrawTeleportationPylons(self);
        method_DrawMasterTrophies(self);
        method_DrawGrass(self);
        method_DrawAnyDirectionalGrass(self);
        method_DrawTrees(self);
        method_DrawVines(self);
        method_DrawReverseVines(self);

        Main.spriteBatch.End();
    }

    private void _Main_DrawSurfaceBG(
        Terraria.On_Main.orig_DrawSurfaceBG orig,
        Terraria.Main self
    )
    {
        if (
            !LightingConfig.Instance.SmoothLightingEnabled()
            || !LightingConfig.Instance.DrawOverbright()
            || !LightingConfig.Instance.UseHiDefFeatures
            || PreferencesConfig.Instance.RenderOnlyLight
        )
        {
            orig(self);
            return;
        }

        Matrix transform;
        if (_inCameraMode)
        {
            transform = Main.Transform;
        }
        else
        {
            transform = Main.BackgroundViewMatrix.TransformationMatrix;
            transform.Translation -=
                Main.BackgroundViewMatrix.ZoomMatrix.Translation
                * new Vector3(
                    1f,
                    Main.BackgroundViewMatrix.Effects.HasFlag(
                        SpriteEffects.FlipVertically
                    )
                        ? -1f
                        : 1f,
                    1f
                );
        }

        Main.spriteBatch.End();
        Main.spriteBatch.Begin(
            SpriteSortMode.Immediate,
            BlendState.AlphaBlend,
            _inCameraMode ? SamplerState.AnisotropicClamp : SamplerState.LinearClamp,
            DepthStencilState.Default,
            RasterizerState.CullNone,
            null,
            transform
        );

        _smoothLightingInstance.ApplyBrightenBackgroundShader();
        orig(self);
    }

    // Waterfalls
    private void _WaterfallManager_Draw(
        Terraria.On_WaterfallManager.orig_Draw orig,
        Terraria.WaterfallManager self,
        SpriteBatch spriteBatch
    )
    {
        if (
            !LightingConfig.Instance.SmoothLightingEnabled()
            || !LightingConfig.Instance.DrawOverbright()
            || !LightingConfig.Instance.OverbrightWaterfalls
            || spriteBatch.GraphicsDevice != Main.graphics.GraphicsDevice
        )
        {
            orig(self, spriteBatch);
            return;
        }

        Main.tileBatch.End();
        spriteBatch.End();

        if (_inCameraMode)
        {
            spriteBatch.GraphicsDevice.SetRenderTarget(
                _smoothLightingInstance.GetCameraModeRenderTarget(_cameraModeTarget)
            );
            spriteBatch.GraphicsDevice.Clear(Color.Transparent);

            _WaterfallManager_Draw_inner(orig, self, spriteBatch);
            Main.tileBatch.End(); // Needed to draw shimmer waterfalls
            spriteBatch.End();

            _smoothLightingInstance.CalculateSmoothLighting(false, true);
            _smoothLightingInstance.DrawSmoothLightingCameraMode(
                _cameraModeTarget,
                _smoothLightingInstance._cameraModeTarget1,
                false,
                false,
                true,
                true
            );

            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                Main.DefaultSamplerState,
                DepthStencilState.None,
                Main.Rasterizer
            );

            Main.tileBatch.Begin(Main.Rasterizer, Main.Transform);
            return;
        }

        var target = PreferencesConfig.Instance.RenderOnlyLight
            ? null
            : MainRenderTarget.Get();
        if (target is null)
        {
            _WaterfallManager_Draw_inner(orig, self, spriteBatch);
            return;
        }

        TextureUtil.MakeSize(ref _screenTarget1, target.Width, target.Height);
        TextureUtil.MakeSize(ref _screenTarget2, target.Width, target.Height);

        spriteBatch.GraphicsDevice.SetRenderTarget(_screenTarget1);
        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.Opaque,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone
        );
        spriteBatch.Draw(target, Vector2.Zero, Color.White);
        spriteBatch.End();

        spriteBatch.GraphicsDevice.SetRenderTarget(_screenTarget2);
        spriteBatch.GraphicsDevice.Clear(Color.Transparent);
        _WaterfallManager_Draw_inner(orig, self, spriteBatch);
        Main.tileBatch.End(); // Needed to draw shimmer waterfalls
        spriteBatch.End();

        _smoothLightingInstance.CalculateSmoothLighting(false, false);
        _smoothLightingInstance.DrawSmoothLighting(_screenTarget2, false, true, target);

        spriteBatch.GraphicsDevice.SetRenderTarget(target);
        spriteBatch.GraphicsDevice.Clear(Color.Transparent);
        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone
        );
        spriteBatch.Draw(_screenTarget1, Vector2.Zero, Color.White);
        spriteBatch.Draw(_screenTarget2, Vector2.Zero, Color.White);
        spriteBatch.End();
        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            Main.DefaultSamplerState,
            DepthStencilState.None,
            Main.Rasterizer,
            null,
            Main.Transform
        );
        Main.tileBatch.Begin(Main.Rasterizer, Main.Transform);
    }

    private void _WaterfallManager_Draw_inner(
        Terraria.On_WaterfallManager.orig_Draw orig,
        Terraria.WaterfallManager self,
        SpriteBatch spriteBatch
    )
    {
        if (_inCameraMode)
        {
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                Main.DefaultSamplerState,
                DepthStencilState.None,
                Main.Rasterizer
            );
        }
        else
        {
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                Main.DefaultSamplerState,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.Transform
            );
        }

        // Applying the light-only shader would require using SpriteSortMode.Immediate
        // This causes glitches, so we unfortunately can't do it

        Main.tileBatch.Begin(Main.Rasterizer, Main.Transform);

        orig(self, spriteBatch);
    }

    // NPCs

    private void _Main_DrawNPCs(
        Terraria.On_Main.orig_DrawNPCs orig,
        Terraria.Main self,
        bool behindTiles
    )
    {
        if (
            !LightingConfig.Instance.SmoothLightingEnabled()
            || !LightingConfig.Instance.DrawOverbright()
            || !LightingConfig.Instance.OverbrightNPCsAndPlayer
        )
        {
            orig(self, behindTiles);
            return;
        }

        DrawScreenOverbright(() => orig(self, behindTiles), true, false);
    }

    private void _Main_DrawCachedNPCs(
        Terraria.On_Main.orig_DrawCachedNPCs orig,
        Terraria.Main self,
        List<int> npcCache,
        bool behindTiles
    )
    {
        if (
            !LightingConfig.Instance.SmoothLightingEnabled()
            || !LightingConfig.Instance.DrawOverbright()
            || !LightingConfig.Instance.OverbrightNPCsAndPlayer
        )
        {
            orig(self, npcCache, behindTiles);
            return;
        }

        DrawScreenOverbright(() => orig(self, npcCache, behindTiles), true, false);
    }

    // Players

    private void _Main_DrawPlayers_BehindNPCs(
        Terraria.On_Main.orig_DrawPlayers_BehindNPCs orig,
        Terraria.Main self
    )
    {
        if (
            !LightingConfig.Instance.SmoothLightingEnabled()
            || !LightingConfig.Instance.DrawOverbright()
            || !LightingConfig.Instance.OverbrightNPCsAndPlayer
        )
        {
            orig(self);
            return;
        }

        DrawScreenOverbright(() => orig(self), false, false);
    }

    private void _Main_DrawPlayers_AfterProjectiles(
        Terraria.On_Main.orig_DrawPlayers_AfterProjectiles orig,
        Terraria.Main self
    )
    {
        if (
            !LightingConfig.Instance.SmoothLightingEnabled()
            || !LightingConfig.Instance.DrawOverbright()
            || !LightingConfig.Instance.OverbrightNPCsAndPlayer
        )
        {
            orig(self);
            return;
        }

        DrawScreenOverbright(() => orig(self), false, false);
    }

    // Projectiles
    // Main.DrawSuperSpecialProjectiles seems to be for the First Fractal (an unobtainable item),
    // so we don't override it

    private void _Main_DrawProjectiles(
        Terraria.On_Main.orig_DrawProjectiles orig,
        Terraria.Main self
    )
    {
        if (
            !LightingConfig.Instance.SmoothLightingEnabled()
            || !LightingConfig.Instance.DrawOverbright()
            || !LightingConfig.Instance.OverbrightProjectiles
        )
        {
            orig(self);
            return;
        }

        DrawScreenOverbright(() => orig(self), false, false);
    }

    private void _Main_DrawCachedProjs(
        Terraria.On_Main.orig_DrawCachedProjs orig,
        Terraria.Main self,
        List<int> projCache,
        bool startSpriteBatch
    )
    {
        if (
            !LightingConfig.Instance.SmoothLightingEnabled()
            || !LightingConfig.Instance.DrawOverbright()
            || !LightingConfig.Instance.OverbrightProjectiles
        )
        {
            orig(self, projCache, startSpriteBatch);
            return;
        }

        DrawScreenOverbright(
            () => orig(self, projCache, startSpriteBatch),
            !startSpriteBatch,
            false
        );
    }

    // Dust
    private void _Main_DrawDust(Terraria.On_Main.orig_DrawDust orig, Terraria.Main self)
    {
        if (
            !LightingConfig.Instance.SmoothLightingEnabled()
            || !LightingConfig.Instance.DrawOverbright()
            || !LightingConfig.Instance.OverbrightDustAndGore
        )
        {
            orig(self);
            return;
        }

        DrawScreenOverbright(() => orig(self), false, false);
    }

    // Gore
    private void _Main_DrawGore(Terraria.On_Main.orig_DrawGore orig, Terraria.Main self)
    {
        if (
            !LightingConfig.Instance.SmoothLightingEnabled()
            || !LightingConfig.Instance.DrawOverbright()
            || !LightingConfig.Instance.OverbrightDustAndGore
        )
        {
            orig(self);
            return;
        }

        DrawScreenOverbright(() => orig(self), true, false);
    }

    // Dropped Items
    private void _Main_DrawItems(Terraria.On_Main.orig_DrawItems orig, Terraria.Main self)
    {
        if (
            !LightingConfig.Instance.SmoothLightingEnabled()
            || !LightingConfig.Instance.DrawOverbright()
            || !LightingConfig.Instance.OverbrightItems
        )
        {
            orig(self);
            return;
        }

        DrawScreenOverbright(() => orig(self), true, false);
    }

    // Rain
    private void _Main_DrawRain(Terraria.On_Main.orig_DrawRain orig, Terraria.Main self)
    {
        if (
            !LightingConfig.Instance.SmoothLightingEnabled()
            || !LightingConfig.Instance.DrawOverbright()
            || !LightingConfig.Instance.OverbrightRain
        )
        {
            orig(self);
            return;
        }

        DrawScreenOverbright(() => orig(self), true, false);
    }

    // Non-moving objects (tiles, walls, etc)

    private void _Main_RenderWater(
        Terraria.On_Main.orig_RenderWater orig,
        Terraria.Main self
    )
    {
        if (
            LightingConfig.Instance.SmoothLightingEnabled()
            && PreferencesConfig.Instance.RenderOnlyLight
            && !LightingConfig.Instance.DrawOverbright()
        )
        {
            Main.graphics.GraphicsDevice.SetRenderTarget(Main.waterTarget);
            Main.graphics.GraphicsDevice.Clear(Color.Transparent);
            Main.graphics.GraphicsDevice.SetRenderTarget(null);
            return;
        }

        if (!LightingConfig.Instance.SmoothLightingEnabled())
        {
            orig(self);
            return;
        }

        _smoothLightingInstance.CalculateSmoothLighting(false);
        orig(self);

        if (Main.drawToScreen)
        {
            return;
        }

        _smoothLightingInstance.DrawSmoothLighting(Main.waterTarget, false, true);
    }

    private void _Main_DrawWaters(
        Terraria.On_Main.orig_DrawWaters orig,
        Terraria.Main self,
        bool isBackground
    )
    {
        if (
            LightingConfig.Instance.SmoothLightingEnabled()
            && PreferencesConfig.Instance.RenderOnlyLight
            && !LightingConfig.Instance.DrawOverbright()
        )
        {
            return;
        }

        if (_inCameraMode || !LightingConfig.Instance.SmoothLightingEnabled())
        {
            orig(self, isBackground);
            return;
        }

        OverrideLightColor = isBackground
            ? _smoothLightingInstance.DrawSmoothLightingBack
            : _smoothLightingInstance.DrawSmoothLightingFore;
        try
        {
            orig(self, isBackground);
        }
        finally
        {
            OverrideLightColor = false;
        }
    }

    // Cave backgrounds
    private void _Main_RenderBackground(
        Terraria.On_Main.orig_RenderBackground orig,
        Terraria.Main self
    )
    {
        if (!LightingConfig.Instance.SmoothLightingEnabled())
        {
            orig(self);
            return;
        }

        if (PreferencesConfig.Instance.RenderOnlyLight)
        {
            Main.graphics.GraphicsDevice.SetRenderTarget(Main.instance.backgroundTarget);
            Main.graphics.GraphicsDevice.Clear(Color.Transparent);
            Main.graphics.GraphicsDevice.SetRenderTarget(Main.instance.backWaterTarget);
            Main.graphics.GraphicsDevice.Clear(Color.Transparent);
            Main.graphics.GraphicsDevice.SetRenderTarget(null);
            return;
        }

        _smoothLightingInstance.CalculateSmoothLighting(true);
        orig(self);

        if (Main.drawToScreen)
        {
            return;
        }

        _smoothLightingInstance.DrawSmoothLighting(
            Main.instance.backgroundTarget,
            true,
            true
        );
        _smoothLightingInstance.DrawSmoothLighting(
            Main.instance.backWaterTarget,
            true,
            true
        );
    }

    private void _Main_DrawBackground(
        Terraria.On_Main.orig_DrawBackground orig,
        Terraria.Main self
    )
    {
        if (!LightingConfig.Instance.SmoothLightingEnabled())
        {
            orig(self);
            return;
        }

        if (
            _inCameraMode
            && LightingConfig.Instance.SmoothLightingEnabled()
            && PreferencesConfig.Instance.RenderOnlyLight
        )
        {
            return;
        }

        if (_inCameraMode)
        {
            _smoothLightingInstance.CalculateSmoothLighting(true, true);

            Main.tileBatch.End();
            Main.spriteBatch.End();
            Main.graphics.GraphicsDevice.SetRenderTarget(
                _smoothLightingInstance.GetCameraModeRenderTarget(_cameraModeTarget)
            );
            Main.graphics.GraphicsDevice.Clear(Color.Transparent);
            Main.tileBatch.Begin();
            Main.spriteBatch.Begin();
            OverrideLightColor = _smoothLightingInstance.DrawSmoothLightingBack;
            try
            {
                orig(self);
            }
            finally
            {
                OverrideLightColor = false;
            }
            Main.tileBatch.End();
            Main.spriteBatch.End();

            _smoothLightingInstance.DrawSmoothLightingCameraMode(
                _cameraModeTarget,
                _smoothLightingInstance._cameraModeTarget1,
                true,
                false,
                true
            );

            Main.tileBatch.Begin();
            Main.spriteBatch.Begin();
        }
        else
        {
            OverrideLightColor = _smoothLightingInstance.DrawSmoothLightingBack;
            try
            {
                orig(self);
            }
            finally
            {
                OverrideLightColor = false;
            }
        }
    }

    private void _Main_RenderBlack(
        Terraria.On_Main.orig_RenderBlack orig,
        Terraria.Main self
    )
    {
        if (!LightingConfig.Instance.SmoothLightingEnabled())
        {
            orig(self);
            return;
        }

        if (PreferencesConfig.Instance.RenderOnlyLight)
        {
            return;
        }

        var initialLightingOverride = OverrideLightColor;
        OverrideLightColor = false;
        try
        {
            orig(self);
        }
        finally
        {
            OverrideLightColor = initialLightingOverride;
        }
    }

    private void _Main_RenderTiles(
        Terraria.On_Main.orig_RenderTiles orig,
        Terraria.Main self
    )
    {
        if (!LightingConfig.Instance.SmoothLightingEnabled())
        {
            orig(self);
            return;
        }

        _smoothLightingInstance.CalculateSmoothLighting(false);
        OverrideLightColor = _smoothLightingInstance.DrawSmoothLightingFore;
        try
        {
            orig(self);
        }
        finally
        {
            OverrideLightColor = false;
        }

        if (Main.drawToScreen)
        {
            return;
        }

        _smoothLightingInstance.DrawSmoothLighting(Main.instance.tileTarget, false);
    }

    private void _Main_RenderTiles2(
        Terraria.On_Main.orig_RenderTiles2 orig,
        Terraria.Main self
    )
    {
        if (!LightingConfig.Instance.SmoothLightingEnabled())
        {
            orig(self);
            return;
        }

        _smoothLightingInstance.CalculateSmoothLighting(false);
        OverrideLightColor = _smoothLightingInstance.DrawSmoothLightingFore;
        try
        {
            orig(self);
        }
        finally
        {
            OverrideLightColor = false;
        }

        if (Main.drawToScreen)
        {
            return;
        }

        _smoothLightingInstance.DrawSmoothLighting(Main.instance.tile2Target, false);
    }

    private void _Main_RenderWalls(
        Terraria.On_Main.orig_RenderWalls orig,
        Terraria.Main self
    )
    {
        if (!LightingConfig.Instance.SmoothLightingEnabled())
        {
            orig(self);
            if (LightingConfig.Instance.AmbientOcclusionEnabled())
            {
                _ambientOcclusionInstance.ApplyAmbientOcclusion();
            }
            return;
        }

        if (
            PreferencesConfig.Instance.RenderOnlyLight
            && !LightingConfig.Instance.DrawOverbright()
        )
        {
            Main.graphics.GraphicsDevice.SetRenderTarget(Main.instance.wallTarget);
            Main.graphics.GraphicsDevice.Clear(Color.Transparent);
            Main.graphics.GraphicsDevice.SetRenderTarget(null);
            return;
        }

        _smoothLightingInstance.CalculateSmoothLighting(true);
        OverrideLightColor = _smoothLightingInstance.DrawSmoothLightingBack;
        try
        {
            orig(self);
        }
        finally
        {
            OverrideLightColor = false;
        }

        if (Main.drawToScreen)
        {
            return;
        }

        var doAmbientOcclusion = LightingConfig.Instance.AmbientOcclusionEnabled();
        var doOverbright = LightingConfig.Instance.DrawOverbright();

        RenderTarget2D ambientOcclusionTarget = null;
        if (doAmbientOcclusion && doOverbright)
        {
            ambientOcclusionTarget = _ambientOcclusionInstance.ApplyAmbientOcclusion(
                false
            );
        }

        _smoothLightingInstance.DrawSmoothLighting(
            Main.instance.wallTarget,
            true,
            ambientOcclusionTarget: ambientOcclusionTarget
        );

        if (doAmbientOcclusion && !doOverbright)
        {
            _ambientOcclusionInstance.ApplyAmbientOcclusion();
        }
    }

    private void _LightingEngine_ProcessBlur(
        Terraria.Graphics.Light.On_LightingEngine.orig_ProcessBlur orig,
        Terraria.Graphics.Light.LightingEngine self
    )
    {
        if (!LightingConfig.Instance.FancyLightingEngineEnabled())
        {
            orig(self);
            return;
        }

        _fancyLightingEngineInstance.SetLightMapArea(
            (Rectangle)field_workingProcessedArea.GetValue(self)
        );
        orig(self);
    }

    private void _LightMap_Blur(
        Terraria.Graphics.Light.On_LightMap.orig_Blur orig,
        Terraria.Graphics.Light.LightMap self
    )
    {
        if (
            !LightingConfig.Instance.SmoothLightingEnabled()
            && !LightingConfig.Instance.FancyLightingEngineEnabled()
        )
        {
            orig(self);
            return;
        }

        var colors = (Vector3[])field_colors.GetValue(self);
        var lightMasks = (LightMaskMode[])field_mask.GetValue(self);
        if (colors is null || lightMasks is null)
        {
            orig(self);
            return;
        }

        if (LightingConfig.Instance.FancyLightingEngineEnabled())
        {
            _fancyLightingEngineInstance.SpreadLight(
                self,
                colors,
                lightMasks,
                self.Width,
                self.Height
            );
        }
        else
        {
            orig(self);
        }

        if (LightingConfig.Instance.SmoothLightingEnabled())
        {
            _smoothLightingInstance.GetAndBlurLightMap(
                colors,
                lightMasks,
                self.Width,
                self.Height
            );
        }
    }

    // Camera mode hooks below

    private void _Main_DrawLiquid(
        Terraria.On_Main.orig_DrawLiquid orig,
        Terraria.Main self,
        bool bg,
        int Style,
        float Alpha,
        bool drawSinglePassLiquids
    )
    {
        if (!_inCameraMode || !LightingConfig.Instance.SmoothLightingEnabled())
        {
            orig(self, bg, Style, Alpha, drawSinglePassLiquids);
            return;
        }

        if (
            PreferencesConfig.Instance.RenderOnlyLight
            && !LightingConfig.Instance.DrawOverbright()
        )
        {
            return;
        }

        _smoothLightingInstance.CalculateSmoothLighting(bg, true);
        OverrideLightColor = true;

        Main.spriteBatch.End();
        Main.graphics.GraphicsDevice.SetRenderTarget(
            _smoothLightingInstance.GetCameraModeRenderTarget(_cameraModeTarget)
        );
        Main.graphics.GraphicsDevice.Clear(Color.Transparent);
        Main.spriteBatch.Begin();
        try
        {
            orig(self, bg, Style, Alpha, drawSinglePassLiquids);
        }
        finally
        {
            OverrideLightColor = false;
        }
        Main.spriteBatch.End();

        _smoothLightingInstance.DrawSmoothLightingCameraMode(
            _cameraModeTarget,
            _smoothLightingInstance._cameraModeTarget1,
            bg,
            false,
            true
        );

        Main.spriteBatch.Begin();
    }

    private void _Main_DrawWalls(Terraria.On_Main.orig_DrawWalls orig, Terraria.Main self)
    {
        if (!_inCameraMode)
        {
            orig(self);
            return;
        }

        if (
            LightingConfig.Instance.SmoothLightingEnabled()
            && PreferencesConfig.Instance.RenderOnlyLight
            && !LightingConfig.Instance.DrawOverbright()
        )
        {
            return;
        }

        _smoothLightingInstance.CalculateSmoothLighting(true, true);
        OverrideLightColor = LightingConfig.Instance.SmoothLightingEnabled();

        var wallTarget = _smoothLightingInstance.GetCameraModeRenderTarget(
            _cameraModeTarget
        );

        Main.tileBatch.End();
        Main.spriteBatch.End();
        Main.graphics.GraphicsDevice.SetRenderTarget(wallTarget);
        Main.graphics.GraphicsDevice.Clear(Color.Transparent);
        Main.tileBatch.Begin();
        Main.spriteBatch.Begin();
        try
        {
            orig(self);
        }
        finally
        {
            OverrideLightColor = false;
        }
        Main.tileBatch.End();
        Main.spriteBatch.End();

        var doAmbientOcclusion = LightingConfig.Instance.AmbientOcclusionEnabled();
        var doOverbright =
            LightingConfig.Instance.DrawOverbright()
            && LightingConfig.Instance.SmoothLightingEnabled();

        RenderTarget2D ambientOcclusionTarget = null;
        if (doAmbientOcclusion && doOverbright)
        {
            ambientOcclusionTarget =
                _ambientOcclusionInstance.ApplyAmbientOcclusionCameraMode(
                    _cameraModeTarget,
                    wallTarget,
                    _cameraModeBiome,
                    false
                );
        }

        _smoothLightingInstance.DrawSmoothLightingCameraMode(
            _cameraModeTarget,
            wallTarget,
            true,
            doAmbientOcclusion && !doOverbright,
            ambientOcclusionTarget: ambientOcclusionTarget
        );

        if (doAmbientOcclusion && !doOverbright)
        {
            _ambientOcclusionInstance.ApplyAmbientOcclusionCameraMode(
                _cameraModeTarget,
                _smoothLightingInstance._cameraModeTarget2,
                _cameraModeBiome
            );
        }

        Main.tileBatch.Begin();
        Main.spriteBatch.Begin();
    }

    private void _Main_DrawTiles(
        Terraria.On_Main.orig_DrawTiles orig,
        Terraria.Main self,
        bool solidLayer,
        bool forRenderTargets,
        bool intoRenderTargets,
        int waterStyleOverride
    )
    {
        if (!_inCameraMode)
        {
            orig(
                self,
                solidLayer,
                forRenderTargets,
                intoRenderTargets,
                waterStyleOverride
            );
            return;
        }

        _smoothLightingInstance.CalculateSmoothLighting(false, true);
        OverrideLightColor = LightingConfig.Instance.SmoothLightingEnabled();

        Main.tileBatch.End();
        Main.spriteBatch.End();
        Main.graphics.GraphicsDevice.SetRenderTarget(
            _smoothLightingInstance.GetCameraModeRenderTarget(_cameraModeTarget)
        );
        Main.graphics.GraphicsDevice.Clear(Color.Transparent);
        Main.tileBatch.Begin();
        Main.spriteBatch.Begin();
        try
        {
            orig(
                self,
                solidLayer,
                forRenderTargets,
                intoRenderTargets,
                waterStyleOverride
            );
        }
        finally
        {
            OverrideLightColor = false;
        }
        Main.tileBatch.End();
        Main.spriteBatch.End();

        _smoothLightingInstance.DrawSmoothLightingCameraMode(
            _cameraModeTarget,
            _smoothLightingInstance._cameraModeTarget1,
            false,
            false
        );

        Main.tileBatch.Begin();
        Main.spriteBatch.Begin();
    }

    private void _Main_DrawCapture(
        Terraria.On_Main.orig_DrawCapture orig,
        Terraria.Main self,
        Rectangle area,
        CaptureSettings settings
    )
    {
        if (LightingConfig.Instance.ModifyCameraModeRendering())
        {
            _cameraModeTarget = MainRenderTarget.Get();
            _inCameraMode = _cameraModeTarget is not null;
        }
        else
        {
            _inCameraMode = false;
        }

        if (_inCameraMode)
        {
            _cameraModeArea = area;
            _cameraModeBiome = settings.Biome;
        }

        try
        {
            orig(self, area, settings);
        }
        finally
        {
            _inCameraMode = false;
        }
    }
}
