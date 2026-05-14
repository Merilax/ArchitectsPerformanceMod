using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Localization.Components;
using TMPro;
using System.Collections.Generic;
using BepInEx.Configuration;
// using UnityEngine.Rendering;
// using UnityEngine.Rendering.HighDefinition;
// using UnityEngine.VFX;
// using Unity.Cinemachine;
using System;
using UnityEngine.Rendering;

namespace ArchPerformanceMod;

public class ModSettings
{
	private static bool init = true;
	public enum ToggleEnum { off, on }
	public enum TieredEnum { low, medium, high }
	public enum TieredWithOffEnum { off, low, medium, high }
	public enum RaceOnlyEnum { off, raceOnly, on }
	public enum QuantityEnum { none, reduced, full }
	public enum AntialiasingEnum { off, FXAA, MSAA, TAA }
	public enum StrengthEnum { def, optimized, aggresive }
	public static readonly Dictionary<Enum, Localization.Items> valueTextRelation = new(){
		{ToggleEnum.off, Localization.Items.setOff},
		{ToggleEnum.on, Localization.Items.setOn},

		{TieredEnum.low, Localization.Items.setLow},
		{TieredEnum.medium, Localization.Items.setMedium},
		{TieredEnum.high, Localization.Items.setHigh},

		{TieredWithOffEnum.off, Localization.Items.setOff},
		{TieredWithOffEnum.low, Localization.Items.setLow},
		{TieredWithOffEnum.medium, Localization.Items.setMedium},
		{TieredWithOffEnum.high, Localization.Items.setHigh},

		{RaceOnlyEnum.off, Localization.Items.setOff},
		{RaceOnlyEnum.raceOnly, Localization.Items.setRaceOnly},
		{RaceOnlyEnum.on, Localization.Items.setOn},

		{AntialiasingEnum.off, Localization.Items.setOff},
		{AntialiasingEnum.FXAA, Localization.Items.FXAA},
		{AntialiasingEnum.MSAA, Localization.Items.MSAA},
		{AntialiasingEnum.TAA, Localization.Items.TAA},

		{StrengthEnum.def, Localization.Items.def},
		{StrengthEnum.optimized, Localization.Items.optimized},
		{StrengthEnum.aggresive, Localization.Items.aggresive},

		{QuantityEnum.none, Localization.Items.none},
		{QuantityEnum.reduced, Localization.Items.reduced},
		{QuantityEnum.full, Localization.Items.full},
	};

	private static readonly IReadOnlyList<ToggleEnum> genericToggleValues = [ToggleEnum.off, ToggleEnum.on];
	private static readonly IReadOnlyList<TieredEnum> genericTierValues = [TieredEnum.low, TieredEnum.medium, TieredEnum.high];
	private static readonly IReadOnlyList<TieredWithOffEnum> genericTierValuesWithDisabled = [TieredWithOffEnum.off, TieredWithOffEnum.low, TieredWithOffEnum.medium, TieredWithOffEnum.high];
	private static readonly IReadOnlyList<RaceOnlyEnum> genericRaceOnlyValues = [RaceOnlyEnum.off, RaceOnlyEnum.raceOnly, RaceOnlyEnum.on];
	private static readonly IReadOnlyList<AntialiasingEnum> antialiasingValues = [AntialiasingEnum.off, AntialiasingEnum.FXAA, AntialiasingEnum.MSAA, AntialiasingEnum.TAA];
	private static readonly IReadOnlyList<StrengthEnum> genericStrengthValues = [StrengthEnum.def, StrengthEnum.optimized, StrengthEnum.aggresive];
	private static readonly IReadOnlyList<QuantityEnum> genericQuantityValues = [QuantityEnum.none, QuantityEnum.reduced, QuantityEnum.full];

	// Config entries
	private static ConfigFile config;
	public static ConfigEntry<ToggleEnum> confGlobalIllumination;
	public static ConfigEntry<StrengthEnum> confReflections;
	public static ConfigEntry<ToggleEnum> confAmbientOcclusion;
	public static ConfigEntry<StrengthEnum> confShadowQuality;
	public static ConfigEntry<ToggleEnum> confChromaAberration;
	public static ConfigEntry<ToggleEnum> confVignette;
	public static ConfigEntry<ToggleEnum> confShadowTones;
	public static ConfigEntry<ToggleEnum> confDockLights;
	public static ConfigEntry<AntialiasingEnum> confAntialiasing;
	public static ConfigEntry<QuantityEnum> confMachineParticles;

	private static CycleConfigEntry<ToggleEnum> _confGlobalIllumination;
	private static CycleConfigEntry<StrengthEnum> _confReflections;
	private static CycleConfigEntry<StrengthEnum> _confShadowQuality;
	private static CycleConfigEntry<ToggleEnum> _confAmbientOcclusion;
	private static CycleConfigEntry<ToggleEnum> _confChromaAberration;
	private static CycleConfigEntry<ToggleEnum> _confVignette;
	private static CycleConfigEntry<ToggleEnum> _confShadowTones;
	private static CycleConfigEntry<ToggleEnum> _confDockLights;
	private static CycleConfigEntry<AntialiasingEnum> _confAntialiasing;
	private static CycleConfigEntry<QuantityEnum> _confMachineParticles;

	// Objects
	private static Scene rootScene;
	private static GameObject canvasOriginal;
	private static GameObject settingsOriginal;
	private static GameObject scrollViewOriginal;
	private static GameObject applyButtonOriginal;
	private static GameObject settingsButton;
	private static GameObject applyButton;
	private static GameObject settingsView;

	[HarmonyPostfix]
	[HarmonyPatch(typeof(Scene_Settings), nameof(Scene_Settings.Start))]
	public static void CreateModSettings(Scene_Settings __instance)
	{
		try
		{
			rootScene = SceneManager.GetSceneByName("Settings");
			canvasOriginal = rootScene.GetRootGameObjects().First((obj) => obj.name == "Canvas");
			settingsOriginal = canvasOriginal.transform.Find("Settings").gameObject;
			scrollViewOriginal = settingsOriginal.transform.GetChild(0).gameObject;

			// Hook every vanilla button to close modded settings
			MethodInfo mi = typeof(ModSettings).GetMethod(nameof(OnHideModSettings));
			settingsOriginal.transform.Find("Button_General").GetComponent<Button>().onClick.AddListener((UnityAction)System.Delegate.CreateDelegate(typeof(System.Action), mi));
			settingsOriginal.transform.Find("Button_Graphic").GetComponent<Button>().onClick.AddListener((UnityAction)System.Delegate.CreateDelegate(typeof(System.Action), mi));
			settingsOriginal.transform.Find("Button_Sound").GetComponent<Button>().onClick.AddListener((UnityAction)System.Delegate.CreateDelegate(typeof(System.Action), mi));
			settingsOriginal.transform.Find("Button_Assist").GetComponent<Button>().onClick.AddListener((UnityAction)System.Delegate.CreateDelegate(typeof(System.Action), mi));
			settingsOriginal.transform.Find("Button_Keyassign").GetComponent<Button>().onClick.AddListener((UnityAction)System.Delegate.CreateDelegate(typeof(System.Action), mi));

			// Create mod settings button
			settingsButton = CreateModSettingButton();

			// Create our own Apply button
			applyButton = CreateModApplyButton();

			// Create the settings table
			settingsView = CreateDataView();
		}
		catch (Exception err)
		{
			Plugin.Log.LogError(err);
		}
	}

	public static GameObject CreateModSettingButton()
	{
		GameObject btn = GameObject.Instantiate(settingsOriginal.transform.Find("Button_General").gameObject);
		btn.name = "Button Mod Config";
		btn.transform.SetParent(settingsOriginal.transform);
		btn.transform.SetSiblingIndex(6);
		btn.transform.localPosition = settingsOriginal.transform.Find("Button_Keyassign").localPosition - new Vector3(0, 50, 0);//new Vector3(410, 720, 0);
		btn.transform.localScale = new Vector3(1, 1, 1);

		GameObject textLabel = btn.transform.GetChild(1).gameObject;
		textLabel.GetComponent<LocalizeStringEvent>().enabled = false;
		textLabel.GetComponent<TextMeshProUGUI>().text = Localization.GetText(Localization.Items.settingsModButton);
		Localization.OnLocaleChanged += () => { if (textLabel) textLabel.GetComponent<TextMeshProUGUI>().text = Localization.GetText(Localization.Items.settingsModButton); };

		btn.GetComponent<Button>().onClick.RemoveAllListeners();
		btn.GetComponent<Button>().onClick.AddListener((System.Action)(() => OnShowModSettings()));

		return btn;
	}

	public static GameObject CreateModApplyButton()
	{
		applyButtonOriginal = settingsOriginal.transform.Find("Button_Apply").gameObject;
		GameObject btn = GameObject.Instantiate(applyButtonOriginal);

		btn.name = "Button Apply Mod";
		btn.transform.SetParent(settingsOriginal.transform);
		btn.transform.SetSiblingIndex(8);
		btn.transform.localPosition = applyButtonOriginal.transform.localPosition;// new Vector3(720, 970, 0);
		btn.transform.localScale = new Vector3(1, 1, 1);

		btn.GetComponent<Button>().onClick.RemoveAllListeners();
		btn.GetComponent<Button>().onClick.AddListener((UnityAction)System.Delegate.CreateDelegate(
					typeof(System.Action),
					typeof(ModSettings).GetMethod(nameof(OnSettingsApply))
				));

		btn.SetActive(false);
		return btn;
	}

	public static GameObject CreateDataView()
	{
		// Settings/Scroll View/Viewport/Content
		GameObject obj = GameObject.Instantiate(scrollViewOriginal);
		obj.name = "Mod Scroll View";
		obj.transform.SetParent(settingsOriginal.transform);
		obj.transform.SetSiblingIndex(1);
		obj.transform.localPosition = scrollViewOriginal.transform.localPosition;
		obj.transform.localScale = new Vector3(1, 1, 1);

		// Keep one row used by the original UI, and destroy the rest.
		GameObject originalContent = scrollViewOriginal.transform.GetChild(0).GetChild(0).gameObject;
		GameObject content = obj.transform.GetChild(0).GetChild(0).gameObject;

		content.transform.localPosition = originalContent.transform.localPosition;
		content.transform.localScale = new Vector3(1, 1, 1);
		GameObject row = content.transform.GetChild(0).gameObject;

		// Populate with custom settings
		List<Localization.Items> buttons = [
			Localization.Items.SET_GI_ENTRY, // Very heavy
			Localization.Items.SET_SSR_ENTRY, // Light
			Localization.Items.SET_SHADOWQUALITY_ENTRY, // Medium
			Localization.Items.SET_DOCKLIGHTS_ENTRY, // Heavy
			Localization.Items.SET_AO_ENTRY, // Free
			Localization.Items.SET_CHROMAABERRATION_ENTRY, // Free
			Localization.Items.SET_VIGNETTE_ENTRY, // Free
			Localization.Items.SET_SHADOWTONES_ENTRY, // Free
			Localization.Items.SET_AA_ENTRY, // Light
			Localization.Items.SET_MACHINEPARTICLES_ENTRY, // Light
		];
		for (int i = 0; i < content.transform.childCount; i++)
		{
			content.transform.GetChild(i).gameObject.active = false;
		}
		for (int i = 0; i < buttons.Count; i++)
		{
			GameObject newRow = DuplicateSettingRow(row, buttons[i]);
			newRow.transform.SetParent(content.transform);
			newRow.transform.localPosition = row.transform.localPosition - new Vector3(0, 60 * i, 0);
			newRow.transform.localScale = new Vector3(1, 1, 1);

			newRow.GetComponent<RectTransform>().sizeDelta = row.GetComponent<RectTransform>().sizeDelta + new Vector2(30, 10);
			newRow.active = true; // Doesn't animate the vanilla menus, but that's fine.
		}

		// TODO Do something about scrolling.

		obj.active = false;
		return obj;
	}

	public static GameObject DuplicateSettingRow(GameObject origin, Localization.Items title)
	{
		GameObject obj = GameObject.Instantiate(origin);
		obj.name = title.ToString();
		obj.transform.SetParent(origin.transform.parent);
		obj.transform.localPosition = origin.transform.localPosition;
		obj.transform.localScale = new Vector3(1, 1, 1);

		TextMeshProUGUI titleText = obj.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
		titleText.text = Localization.GetText(title);
		Localization.OnLocaleChanged += () => { if (obj) titleText.text = Localization.GetText(title); };

		TextMeshProUGUI valueText = obj.transform.GetChild(3).GetComponent<TextMeshProUGUI>();
		valueText.text = "N/A";

		dynamic entry = null;
		switch (title)
		{
			case Localization.Items.SET_GI_ENTRY:
				entry = _confGlobalIllumination = new CycleConfigEntry<ToggleEnum>(confGlobalIllumination, genericToggleValues, valueText);
				break;
			case Localization.Items.SET_SSR_ENTRY:
				entry = _confReflections = new CycleConfigEntry<StrengthEnum>(confReflections, genericStrengthValues, valueText);
				break;
			case Localization.Items.SET_SHADOWQUALITY_ENTRY:
				entry = _confShadowQuality = new CycleConfigEntry<StrengthEnum>(confShadowQuality, genericStrengthValues, valueText);
				break;
			case Localization.Items.SET_AO_ENTRY:
				entry = _confAmbientOcclusion = new CycleConfigEntry<ToggleEnum>(confAmbientOcclusion, genericToggleValues, valueText);
				break;
			case Localization.Items.SET_CHROMAABERRATION_ENTRY:
				entry = _confChromaAberration = new CycleConfigEntry<ToggleEnum>(confChromaAberration, genericToggleValues, valueText);
				break;
			case Localization.Items.SET_VIGNETTE_ENTRY:
				entry = _confVignette = new CycleConfigEntry<ToggleEnum>(confVignette, genericToggleValues, valueText);
				break;
			case Localization.Items.SET_SHADOWTONES_ENTRY:
				entry = _confShadowTones = new CycleConfigEntry<ToggleEnum>(confShadowTones, genericToggleValues, valueText);
				break;
			case Localization.Items.SET_DOCKLIGHTS_ENTRY:
				entry = _confDockLights = new CycleConfigEntry<ToggleEnum>(confDockLights, genericToggleValues, valueText);
				break;
			case Localization.Items.SET_AA_ENTRY:
				entry = _confAntialiasing = new CycleConfigEntry<AntialiasingEnum>(confAntialiasing, antialiasingValues, valueText);
				break;
			case Localization.Items.SET_MACHINEPARTICLES_ENTRY:
				entry = _confMachineParticles = new CycleConfigEntry<QuantityEnum>(confMachineParticles, genericQuantityValues, valueText);
				break;
		}

		Button btnLeft = obj.transform.GetChild(1).GetComponent<Button>();
		Button btnRight = obj.transform.GetChild(2).GetComponent<Button>();
		btnLeft.onClick.RemoveAllListeners();
		btnRight.onClick.RemoveAllListeners();
		btnLeft.onClick.AddListener((System.Action)(() => { entry.OnLeftButton(); }));
		btnRight.onClick.AddListener((System.Action)(() => { entry.OnRightButton(); }));

		return obj;
	}

	public static void OnShowModSettings()
	{
		applyButton.active = true;
		settingsView.active = true;
		scrollViewOriginal.active = false;
		applyButtonOriginal.active = false;

		_confGlobalIllumination.Cancel();
		_confReflections.Cancel();
		_confShadowQuality.Cancel();
		_confAmbientOcclusion.Cancel();
		_confChromaAberration.Cancel();
		_confVignette.Cancel();
		_confShadowTones.Cancel();
		_confDockLights.Cancel();
		_confAntialiasing.Cancel();
		_confMachineParticles.Cancel();
	}

	public static void OnHideModSettings()
	{
		applyButton.active = false;
		settingsView.active = false;
		scrollViewOriginal.active = true;
		applyButtonOriginal.active = true;
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(Scene_MainMenu), nameof(Scene_MainMenu.Start))]
	public static void LoadConfig()
	{
		Localization.SetLocale(); // Config.Language
		config = Plugin.config;

		confGlobalIllumination = config.Bind("Graphics", "GlobalIllumination", ToggleEnum.on, "Toggles Global Illumination, volumetric lighting within the main world. Cost: Very expensive.");
		confReflections = config.Bind("Graphics", "Reflections", StrengthEnum.def, "Toggles SSR and reflection probes surfaces. Cost: Light.");
		confShadowQuality = config.Bind("Graphics", "ShadowQuality", StrengthEnum.def, "Adjusts quality of all shadows. Cost: Medium.");
		confAmbientOcclusion = config.Bind("Graphics", "AmbientOcclusion", ToggleEnum.on, "Toggles Ambient Occlusion. Cost: Very light.");
		confChromaAberration = config.Bind("Graphics", "ChromaticAberration", ToggleEnum.on, "Toggles Chromatic Aberration. Cost: Very light.");
		confVignette = config.Bind("Graphics", "Vignette", ToggleEnum.on, "Toggles a vignette effect. Cost: Very light.");
		confShadowTones = config.Bind("Graphics", "ShadowToneMapping", ToggleEnum.on, "Toggles remapping of shadow midtones and highlights. Cost: Very light.");
		confDockLights = config.Bind("Graphics", "DockLights", ToggleEnum.on, "Sets the quality of lights in the player dock. Useful if GI and SSR are off, which makes some shadows look weird. Cost: Light.");
		confAntialiasing = config.Bind("Graphics", "AntiAliasing", AntialiasingEnum.MSAA, "Sets the AntiAliasing type to use, if any. Cost: Very light.");
		confMachineParticles = config.Bind("Graphics", "MachineParticles", QuantityEnum.full, "Sets the amount of particles and other machine-related effects. Cost: Light.");

		ApplyChanges();

		// Config.Debug_OutputRawSaveData = true;
	}

	public static void OnSettingsApply()
	{
		_confGlobalIllumination.Confirm();
		_confReflections.Confirm();
		_confShadowQuality.Confirm();
		_confAmbientOcclusion.Confirm();
		_confChromaAberration.Confirm();
		_confVignette.Confirm();
		_confShadowTones.Confirm();
		_confDockLights.Confirm();
		_confAntialiasing.Confirm();
		_confMachineParticles.Confirm();

		config.Save();

		ApplyChanges();
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(Scene_Settings), nameof(Scene_Settings.ApplySettings_Graphic))]
	public static void ApplyChanges()
	{
		ModPerformance.SetGlobalIllumination(confGlobalIllumination.Value);
		ModPerformance.SetReflections(confReflections.Value);
		ModPerformance.SetShadowQuality(confShadowQuality.Value);
		// ModPerformance.SetAmbientOcclusion(confAmbientOcclusion.Value);
		ModPerformance.SetChromaAberration(confChromaAberration.Value);
		ModPerformance.SetVignette(confVignette.Value);
		ModPerformance.SetShadowTones(confShadowTones.Value);
		ModPerformance.SetDockLights(confDockLights.Value);
		ModPerformance.SetAntialiasing(confAntialiasing.Value);
		if (!init) ModPerformance.SetMachineParticles(confMachineParticles.Value);

		init = false;

		if (QualityLevelPatch.QualityLevelChanged)
		{
			HDRPReflectionHelper.InvalidateCache();
			QualityLevelPatch.QualityLevelChanged = false;
		}
		HDRPReflectionHelper.CheckHdrpAssetStale();
		HDRPReflectionHelper.CacheHDRPReflection();

		GraphicsSettings.useScriptableRenderPipelineBatching = true;
		QualitySettings.lodBias = 0.75f;

		HDRPReflectionHelper.ApplyPipelineSupportFlagBatch(
			confReflections.Value != StrengthEnum.def, // SSR
			confAmbientOcclusion.Value == ToggleEnum.off, // Ambient Occlusion
			true, // Volumetrics
			true, // Vol Clouds
			true, // Subsurface Scattering
			true, // Decals (already disabled by default)
			confMachineParticles.Value != QuantityEnum.full, // Distortion
			confReflections.Value != StrengthEnum.def, // SSR Transparency
			confReflections.Value != StrengthEnum.def, // Screen Space Lens Flare
			confReflections.Value != StrengthEnum.def  // Data Driven Lens Flare
		);
	}
}

public class CycleConfigEntry<T>
{
	private readonly ConfigEntry<T> _configEntry;
	private readonly IReadOnlyList<T> _options;
	private readonly TextMeshProUGUI _text;
	private T _pendingValue;
	public T Pending => _pendingValue;
	public T Value => _configEntry.Value;

	public CycleConfigEntry(ConfigEntry<T> configEntry, IReadOnlyList<T> options, TextMeshProUGUI textMesh = null)
	{
		_configEntry = configEntry;
		_options = options;
		_pendingValue = configEntry.Value;
		_text = textMesh;
		if (_pendingValue is Enum)
		{
			_text?.text = Localization.GetText(ModSettings.valueTextRelation[_pendingValue as Enum]);
			Localization.OnLocaleChanged += () => { _text?.text = Localization.GetText(ModSettings.valueTextRelation[_pendingValue as Enum]); };
		}
	}

	private int GetPendingIndex()
	{
		for (int i = 0; i < _options.Count; i++)
		{
			if (_options[i].Equals(_pendingValue))
				return i;
		}
		return 0;
	}
	public void OnLeftButton()
	{
		int prev = (GetPendingIndex() - 1 + _options.Count) % _options.Count;
		_pendingValue = _options[prev];
		if (_pendingValue is Enum)
			_text?.text = Localization.GetText(ModSettings.valueTextRelation[_pendingValue as Enum]);
	}
	public void OnRightButton()
	{
		int next = (GetPendingIndex() + 1) % _options.Count;
		_pendingValue = _options[next];
		if (_pendingValue is Enum)
			_text?.text = Localization.GetText(ModSettings.valueTextRelation[_pendingValue as Enum]);
	}
	public void Confirm()
	{
		_configEntry.Value = _pendingValue;
	}
	public void Cancel()
	{
		_pendingValue = _configEntry.Value;
		if (_pendingValue is Enum)
			_text?.text = Localization.GetText(ModSettings.valueTextRelation[_pendingValue as Enum]);
	}
}