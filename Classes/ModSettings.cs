using System;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine.Rendering;
using UnityEngine.EventSystems;
using ArchEmperorLib;

namespace ArchPerformanceMod;

public class ModSettings
{
	// Enums
	private static PerformancePresets currentPreset;
	public enum ToggleEnum { off, on }
	public enum TieredEnum { low, medium, high }
	public enum TieredWithOffEnum { off, low, medium, high }
	public enum RaceOnlyEnum { off, raceOnly, on }
	public enum DioramaOnlyEnum { off, dioramaOnly, on }
	public enum QuantityEnum { none, reduced, full }
	public enum AntialiasingEnum { off, FXAA, MSAA, TAA }
	public enum StrengthEnum { def, optimized, aggresive }
	public static readonly Dictionary<Enum, string> valueTextRelation = new(){
		{ToggleEnum.off, Localization.Items.OFF},
		{ToggleEnum.on, Localization.Items.ON},

		{TieredEnum.low, Localization.Items.LOW},
		{TieredEnum.medium, Localization.Items.MEDIUM},
		{TieredEnum.high, Localization.Items.HIGH},

		{TieredWithOffEnum.off, Localization.Items.OFF},
		{TieredWithOffEnum.low, Localization.Items.LOW},
		{TieredWithOffEnum.medium, Localization.Items.MEDIUM},
		{TieredWithOffEnum.high, Localization.Items.HIGH},

		{DioramaOnlyEnum.off, Localization.Items.OFF},
		{DioramaOnlyEnum.dioramaOnly, Localization.Items.DIORAMA_ONLY},
		{DioramaOnlyEnum.on, Localization.Items.ON},

		{RaceOnlyEnum.off, Localization.Items.OFF},
		{RaceOnlyEnum.raceOnly, Localization.Items.RACE_ONLY},
		{RaceOnlyEnum.on, Localization.Items.ON},

		{AntialiasingEnum.off, Localization.Items.OFF},
		{AntialiasingEnum.FXAA, Localization.Items.FXAA},
		{AntialiasingEnum.MSAA, Localization.Items.MSAA},
		{AntialiasingEnum.TAA, Localization.Items.TAA},

		{StrengthEnum.def, Localization.Items.DEF},
		{StrengthEnum.optimized, Localization.Items.OPTIMIZED},
		{StrengthEnum.aggresive, Localization.Items.AGGRESIVE},

		{QuantityEnum.none, Localization.Items.NONE},
		{QuantityEnum.reduced, Localization.Items.REDUCED},
		{QuantityEnum.full, Localization.Items.FULL},

		{PerformancePresets.Vanilla, Localization.Items.PRESET_VANILLA},
		{PerformancePresets.Optimized, Localization.Items.PRESET_OPTIMIZED},
		{PerformancePresets.Overdrive, Localization.Items.PRESET_OVERDRIVE},
		{PerformancePresets.Custom, Localization.Items.PRESET_CUSTOM},
	};

	public static readonly IReadOnlyList<ToggleEnum> toggleEnums = [ToggleEnum.off, ToggleEnum.on];
	public static readonly IReadOnlyList<TieredEnum> tieredEnums = [TieredEnum.low, TieredEnum.medium, TieredEnum.high];
	public static readonly IReadOnlyList<TieredWithOffEnum> tieredWithDisabledEnums = [TieredWithOffEnum.off, TieredWithOffEnum.low, TieredWithOffEnum.medium, TieredWithOffEnum.high];
	public static readonly IReadOnlyList<RaceOnlyEnum> raceOnlyEnums = [RaceOnlyEnum.off, RaceOnlyEnum.raceOnly, RaceOnlyEnum.on];
	public static readonly IReadOnlyList<DioramaOnlyEnum> dioramaOnlyEnums = [DioramaOnlyEnum.off, DioramaOnlyEnum.dioramaOnly, DioramaOnlyEnum.on];
	public static readonly IReadOnlyList<AntialiasingEnum> antialiasingEnums = [AntialiasingEnum.off, AntialiasingEnum.FXAA, AntialiasingEnum.MSAA, AntialiasingEnum.TAA];
	public static readonly IReadOnlyList<StrengthEnum> strengthEnums = [StrengthEnum.def, StrengthEnum.optimized, StrengthEnum.aggresive];
	public static readonly IReadOnlyList<QuantityEnum> quantityEnums = [QuantityEnum.none, QuantityEnum.reduced, QuantityEnum.full];
	public static readonly IReadOnlyList<PerformancePresets> presetEnums = [PerformancePresets.Vanilla, PerformancePresets.Optimized, PerformancePresets.Overdrive, PerformancePresets.Custom];


	// Config entries
	private static ConfigFile config;

	private static List<dynamic> configEntries = [];
	public static ConfigEntry<PerformancePresets> confPreset;
	public static ConfigEntry<ModSettingsManager.ToggleEnum> confGlobalIllumination;
	public static ConfigEntry<StrengthEnum> confReflections;
	public static ConfigEntry<StrengthEnum> confShadowQuality;
	public static ConfigEntry<DioramaOnlyEnum> confVolumetrics;
	public static ConfigEntry<ModSettingsManager.ToggleEnum> confAmbientOcclusion;
	public static ConfigEntry<ModSettingsManager.ToggleEnum> confChromaAberration;
	public static ConfigEntry<ModSettingsManager.ToggleEnum> confVignette;
	public static ConfigEntry<ModSettingsManager.ToggleEnum> confShadowTones;
	public static ConfigEntry<ModSettingsManager.ToggleEnum> confDockLights;
	public static ConfigEntry<AntialiasingEnum> confAntialiasing;
	public static ConfigEntry<ModSettingsManager.QuantityEnum> confMachineParticles;
	public static ConfigEntry<ModSettingsManager.ToggleEnum> confCameraClipPlane;
	public static ConfigEntry<ModSettingsManager.ToggleEnum> confSunlightShadows;
	public static ConfigEntry<ModSettingsManager.ToggleEnum> confGaiaDetailManager;

	private static List<dynamic> cycleConfigEntries = [];
	private static CycleConfigEntry<PerformancePresets> _confPreset;
	private static CycleConfigEntry<StrengthEnum> _confReflections;
	private static CycleConfigEntry<StrengthEnum> _confShadowQuality;
	private static CycleConfigEntry<DioramaOnlyEnum> _confVolumetrics;
	private static CycleConfigEntry<ModSettingsManager.ToggleEnum> _confAmbientOcclusion;
	private static CycleConfigEntry<ModSettingsManager.ToggleEnum> _confChromaAberration;
	private static CycleConfigEntry<ModSettingsManager.ToggleEnum> _confVignette;
	private static CycleConfigEntry<ModSettingsManager.ToggleEnum> _confShadowTones;
	private static CycleConfigEntry<ModSettingsManager.ToggleEnum> _confDockLights;
	private static CycleConfigEntry<AntialiasingEnum> _confAntialiasing;
	private static CycleConfigEntry<ModSettingsManager.QuantityEnum> _confMachineParticles;
	private static CycleConfigEntry<ModSettingsManager.ToggleEnum> _confCameraClipPlane;
	private static CycleConfigEntry<ModSettingsManager.ToggleEnum> _confSunlightShadows;
	private static CycleConfigEntry<ModSettingsManager.ToggleEnum> _confGaiaDetailManager;

	public static SettingBlock settingBlock = new();

	// Vars
	private static bool once = false;
	private static bool init = false;
	private static bool delayedInit = false;

	public static void RefreshEntries()
	{
		cycleConfigEntries.Do(e => { e.Cancel(); });
	}

	public static void InitModConfig()
	{
		config = Plugin.config;

		confPreset = config.Bind("Graphics", "GraphicsPreset", PerformancePresets.Vanilla, "Preconfigured set of options.");
		confGlobalIllumination = config.Bind("Graphics", "GlobalIllumination", ModSettingsManager.ToggleEnum.on, "[OBSOLETE] Toggles Global Illumination, volumetric lighting within the main world. Cost: Very expensive.");
		confReflections = config.Bind("Graphics", "Reflections", StrengthEnum.def, "Toggles SSR and reflection probes surfaces. Cost: Light.");
		confShadowQuality = config.Bind("Graphics", "ShadowQuality", StrengthEnum.def, "Adjusts quality of all shadows. Cost: Medium.");
		confVolumetrics = config.Bind("Graphics", "Volumetrics", DioramaOnlyEnum.on, "Toggles Volumetric effects. Mostly found in Diorama. Warning: This will disable some Diorama effects. Cost: Medium.");
		confAmbientOcclusion = config.Bind("Graphics", "AmbientOcclusion", ModSettingsManager.ToggleEnum.on, "Toggles Ambient Occlusion. Cost: Very light.");
		confChromaAberration = config.Bind("Graphics", "ChromaticAberration", ModSettingsManager.ToggleEnum.on, "Toggles Chromatic Aberration. Cost: Very light.");
		confVignette = config.Bind("Graphics", "Vignette", ModSettingsManager.ToggleEnum.on, "Toggles a vignette effect. Cost: Very light.");
		confShadowTones = config.Bind("Graphics", "ShadowToneMapping", ModSettingsManager.ToggleEnum.on, "Toggles remapping of shadow midtones and highlights. Cost: Very light.");
		confDockLights = config.Bind("Graphics", "DockLights", ModSettingsManager.ToggleEnum.on, "Sets the quality of lights in the player dock. Useful if GI and SSR are off, which makes some shadows look weird. Cost: Light.");
		confAntialiasing = config.Bind("Graphics", "AntiAliasing", AntialiasingEnum.MSAA, "Sets the AntiAliasing type to use, if any. Cost: Very light.");
		confMachineParticles = config.Bind("Graphics", "MachineParticles", ModSettingsManager.QuantityEnum.full, "Sets the amount of particles and other machine-related effects. Cost: Light.");
		// confCameraClipPlane = config.Bind("Graphics", "CameraClipPlane", ModSettingsManager.ToggleEnum.off, "Extends the render distance of the main camera. Cost: Variable.");
		confSunlightShadows = config.Bind("Graphics", "SunlightShadows", ModSettingsManager.ToggleEnum.on, "Extends the render distance of the main camera. Cost: Variable.");
		confGaiaDetailManager = config.Bind("Graphics", "GAIADetailManager", ModSettingsManager.ToggleEnum.on, "Toggles the GAIA Detail Manager.");

		_confPreset = new CycleConfigEntry<PerformancePresets>(MyPluginInfo.PLUGIN_GUID, confPreset, presetEnums);
		_confReflections = new CycleConfigEntry<StrengthEnum>(MyPluginInfo.PLUGIN_GUID, confReflections, strengthEnums);
		_confShadowQuality = new CycleConfigEntry<StrengthEnum>(MyPluginInfo.PLUGIN_GUID, confShadowQuality, strengthEnums);
		_confVolumetrics = new CycleConfigEntry<DioramaOnlyEnum>(MyPluginInfo.PLUGIN_GUID, confVolumetrics, dioramaOnlyEnums);
		_confAmbientOcclusion = new CycleConfigEntry<ModSettingsManager.ToggleEnum>(ArchEmperorLib.MyPluginInfo.PLUGIN_GUID, confAmbientOcclusion, ModSettingsManager.ConfigEnums.toggleEnums);
		_confChromaAberration = new CycleConfigEntry<ModSettingsManager.ToggleEnum>(ArchEmperorLib.MyPluginInfo.PLUGIN_GUID, confChromaAberration, ModSettingsManager.ConfigEnums.toggleEnums);
		_confVignette = new CycleConfigEntry<ModSettingsManager.ToggleEnum>(ArchEmperorLib.MyPluginInfo.PLUGIN_GUID, confVignette, ModSettingsManager.ConfigEnums.toggleEnums);
		_confShadowTones = new CycleConfigEntry<ModSettingsManager.ToggleEnum>(ArchEmperorLib.MyPluginInfo.PLUGIN_GUID, confShadowTones, ModSettingsManager.ConfigEnums.toggleEnums);
		_confDockLights = new CycleConfigEntry<ModSettingsManager.ToggleEnum>(ArchEmperorLib.MyPluginInfo.PLUGIN_GUID, confDockLights, ModSettingsManager.ConfigEnums.toggleEnums);
		_confAntialiasing = new CycleConfigEntry<AntialiasingEnum>(MyPluginInfo.PLUGIN_GUID, confAntialiasing, antialiasingEnums);
		_confMachineParticles = new CycleConfigEntry<ModSettingsManager.QuantityEnum>(ArchEmperorLib.MyPluginInfo.PLUGIN_GUID, confMachineParticles, ModSettingsManager.ConfigEnums.quantityEnums);
		// _confCameraClipPlane = new CycleConfigEntry<ModSettingsManager.ToggleEnum>(ArchEmperorLib.MyPluginInfo.PLUGIN_GUID, confCameraClipPlane, ModSettingsManager.ConfigEnums.toggleEnums);
		_confSunlightShadows = new CycleConfigEntry<ModSettingsManager.ToggleEnum>(ArchEmperorLib.MyPluginInfo.PLUGIN_GUID, confSunlightShadows, ModSettingsManager.ConfigEnums.toggleEnums);
		_confGaiaDetailManager = new CycleConfigEntry<ModSettingsManager.ToggleEnum>(ArchEmperorLib.MyPluginInfo.PLUGIN_GUID, confGaiaDetailManager, ModSettingsManager.ConfigEnums.toggleEnums);

		configEntries = [confPreset, confReflections, confShadowQuality, confVolumetrics, confAmbientOcclusion, confChromaAberration, confVignette, confShadowQuality, confDockLights, confAntialiasing, confMachineParticles, confSunlightShadows, confGaiaDetailManager];
		cycleConfigEntries = [_confPreset, _confReflections, _confShadowQuality, _confVolumetrics, _confAmbientOcclusion, _confChromaAberration, _confVignette, _confShadowQuality, _confDockLights, _confAntialiasing, _confMachineParticles, _confSunlightShadows, _confGaiaDetailManager];

		settingBlock.AddSetting(Localization.Items.SET_PRESET, _confPreset);
		settingBlock.AddSetting(Localization.Items.SET_SSR_ENTRY, _confReflections);
		settingBlock.AddSetting(Localization.Items.SET_SHADOWQUALITY_ENTRY, _confShadowQuality);
		settingBlock.AddSetting(Localization.Items.SET_SUNLIGHTSHADOWS_ENTRY, _confSunlightShadows);
		settingBlock.AddSetting(Localization.Items.SET_VOLUMETRICS, _confVolumetrics);
		settingBlock.AddSetting(Localization.Items.SET_DOCKLIGHTS_ENTRY, _confDockLights);
		settingBlock.AddSetting(Localization.Items.SET_GAIADETAIL_ENTRY, _confGaiaDetailManager);
		settingBlock.AddSetting(Localization.Items.SET_AO_ENTRY, _confAmbientOcclusion);
		settingBlock.AddSetting(Localization.Items.SET_CHROMAABERRATION_ENTRY, _confChromaAberration);
		settingBlock.AddSetting(Localization.Items.SET_VIGNETTE_ENTRY, _confVignette);
		settingBlock.AddSetting(Localization.Items.SET_SHADOWTONES_ENTRY, _confShadowTones);
		settingBlock.AddSetting(Localization.Items.SET_AA_ENTRY, _confAntialiasing);
		settingBlock.AddSetting(Localization.Items.SET_MACHINEPARTICLES_ENTRY, _confMachineParticles);

		currentPreset = confPreset.Value;
		ApplyChanges();

		if (!once) BridgedSceneManager.OnSceneLoadComplete.AddListener((Action)(() => DelayedInit()));
		once = true;
	}

	public static void OnSettingsApply()
	{
		cycleConfigEntries.Do(e => e.Confirm());
		config.Save();
		ApplyChanges();
	}

	public static void SetPreset(PerformancePresets preset)
	{
		currentPreset = preset;
		if (currentPreset != PerformancePresets.Custom)
		{
			switch (currentPreset)
			{
				case PerformancePresets.Overdrive:
					// confGlobalIllumination.Value = ToggleEnum.off;
					confReflections.Value = StrengthEnum.aggresive;
					confShadowQuality.Value = StrengthEnum.aggresive;
					confVolumetrics.Value = DioramaOnlyEnum.off;
					confAmbientOcclusion.Value = ModSettingsManager.ToggleEnum.off;
					confChromaAberration.Value = ModSettingsManager.ToggleEnum.off;
					confVignette.Value = ModSettingsManager.ToggleEnum.off;
					confShadowTones.Value = ModSettingsManager.ToggleEnum.off;
					confDockLights.Value = ModSettingsManager.ToggleEnum.off;
					confAntialiasing.Value = AntialiasingEnum.off;
					confMachineParticles.Value = ModSettingsManager.QuantityEnum.none;
					// confCameraClipPlane.Value = ToggleEnum.off;
					confSunlightShadows.Value = ModSettingsManager.ToggleEnum.off;
					confGaiaDetailManager.Value = ModSettingsManager.ToggleEnum.off;
					break;
				case PerformancePresets.Optimized:
					// confGlobalIllumination.Value = ToggleEnum.off;
					confReflections.Value = StrengthEnum.optimized;
					confShadowQuality.Value = StrengthEnum.optimized;
					confVolumetrics.Value = DioramaOnlyEnum.dioramaOnly;
					confAmbientOcclusion.Value = ModSettingsManager.ToggleEnum.off;
					confChromaAberration.Value = ModSettingsManager.ToggleEnum.on;
					confVignette.Value = ModSettingsManager.ToggleEnum.on;
					confShadowTones.Value = ModSettingsManager.ToggleEnum.on;
					confDockLights.Value = ModSettingsManager.ToggleEnum.off;
					confAntialiasing.Value = AntialiasingEnum.MSAA;
					confMachineParticles.Value = ModSettingsManager.QuantityEnum.reduced;
					// confCameraClipPlane.Value = ToggleEnum.off;
					confSunlightShadows.Value = ModSettingsManager.ToggleEnum.on;
					confGaiaDetailManager.Value = ModSettingsManager.ToggleEnum.off;
					break;
				case PerformancePresets.Vanilla:
				default:
					// confGlobalIllumination.Value = ToggleEnum.on;
					confReflections.Value = StrengthEnum.def;
					confShadowQuality.Value = StrengthEnum.def;
					confVolumetrics.Value = DioramaOnlyEnum.on;
					confAmbientOcclusion.Value = ModSettingsManager.ToggleEnum.on;
					confChromaAberration.Value = ModSettingsManager.ToggleEnum.on;
					confVignette.Value = ModSettingsManager.ToggleEnum.on;
					confShadowTones.Value = ModSettingsManager.ToggleEnum.on;
					confDockLights.Value = ModSettingsManager.ToggleEnum.on;
					confAntialiasing.Value = AntialiasingEnum.MSAA;
					confMachineParticles.Value = ModSettingsManager.QuantityEnum.full;
					// confCameraClipPlane.Value = ToggleEnum.off;
					confSunlightShadows.Value = ModSettingsManager.ToggleEnum.on;
					confGaiaDetailManager.Value = ModSettingsManager.ToggleEnum.on;
					break;
			}
		}
		RefreshEntries();
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(Scene_Settings), nameof(Scene_Settings.ApplySettings_Graphic))]
	public static void ApplyChanges()
	{
		SetPreset(confPreset.Value);

		ModPerformance.SetGlobalIllumination(Config.UseGI);
		ModPerformance.SetReflections(confReflections.Value);
		ModPerformance.SetShadowQuality(confShadowQuality.Value);
		ModPerformance.SetChromaAberration(confChromaAberration.Value);
		ModPerformance.SetVignette(confVignette.Value);
		ModPerformance.SetShadowTones(confShadowTones.Value);
		ModPerformance.SetAntialiasing(confAntialiasing.Value);
		// ModPerformance.SetExtendCameraRenderDistance(confCameraClipPlane.Value);
		ModPerformance.SetSunShadows(confSunlightShadows.Value);
		ModPerformance.SetGaiaDetailManager(confGaiaDetailManager.Value);
		
		if (delayedInit)
		{
			ModPerformance.SetDockLights(confDockLights.Value);
			ModPerformance.SetMachineParticles(confMachineParticles.Value);
		}

		if (QualityLevelPatch.QualityLevelChanged)
		{
			HDRPReflectionHelper.InvalidateCache();
			QualityLevelPatch.QualityLevelChanged = false;
		}
		HDRPReflectionHelper.CheckHdrpAssetStale();
		HDRPReflectionHelper.CacheHDRPReflection();

		GraphicsSettings.useScriptableRenderPipelineBatching = true;
		QualitySettings.lodBias = 0.75f;

		HDRPReflectionHelper.ApplyPipelineSupportFlagBatch( // true = disabled
			confReflections.Value != StrengthEnum.def, // SSR
			confAmbientOcclusion.Value == ModSettingsManager.ToggleEnum.off, // Ambient Occlusion
			confVolumetrics.Value != DioramaOnlyEnum.on, // Volumetrics // Disabled if not on
			true, // Vol Clouds
			true, // Subsurface Scattering
			true, // Decals (already disabled by default)
			confMachineParticles.Value != ModSettingsManager.QuantityEnum.full, // Distortion
			confReflections.Value != StrengthEnum.def, // SSR Transparency
			confReflections.Value != StrengthEnum.def, // Screen Space Lens Flare
			confReflections.Value != StrengthEnum.def  // Data Driven Lens Flare
		);

		init = true;
	}

	private static void DelayedInit()
	{
		if (!init || delayedInit) return;
		ModPerformance.SetDockLights(confDockLights.Value);
		ModPerformance.SetMachineParticles(confMachineParticles.Value);
		delayedInit = true;
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(GeoramaSystem), nameof(GeoramaSystem.Start))]
	public static void ResetMainConditionals()
	{
		delayedInit = false;
	}
}

public enum PerformancePresets
{
	Vanilla,
	Optimized,
	Overdrive,
	Custom,
}