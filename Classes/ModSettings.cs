using System;
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
using UnityEngine.Rendering;
using UnityEngine.EventSystems;

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
	public static readonly Dictionary<Enum, Localization.Items> valueTextRelation = new(){
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

	private static readonly IReadOnlyList<ToggleEnum> toggleEnums = [ToggleEnum.off, ToggleEnum.on];
	private static readonly IReadOnlyList<TieredEnum> tieredEnums = [TieredEnum.low, TieredEnum.medium, TieredEnum.high];
	private static readonly IReadOnlyList<TieredWithOffEnum> tieredWithDisabledEnums = [TieredWithOffEnum.off, TieredWithOffEnum.low, TieredWithOffEnum.medium, TieredWithOffEnum.high];
	private static readonly IReadOnlyList<RaceOnlyEnum> raceOnlyEnums = [RaceOnlyEnum.off, RaceOnlyEnum.raceOnly, RaceOnlyEnum.on];
	private static readonly IReadOnlyList<DioramaOnlyEnum> dioramaOnlyEnums = [DioramaOnlyEnum.off, DioramaOnlyEnum.dioramaOnly, DioramaOnlyEnum.on];
	private static readonly IReadOnlyList<AntialiasingEnum> antialiasingEnums = [AntialiasingEnum.off, AntialiasingEnum.FXAA, AntialiasingEnum.MSAA, AntialiasingEnum.TAA];
	private static readonly IReadOnlyList<StrengthEnum> strengthEnums = [StrengthEnum.def, StrengthEnum.optimized, StrengthEnum.aggresive];
	private static readonly IReadOnlyList<QuantityEnum> quantityEnums = [QuantityEnum.none, QuantityEnum.reduced, QuantityEnum.full];
	private static readonly IReadOnlyList<PerformancePresets> presetEnums = [PerformancePresets.Vanilla, PerformancePresets.Optimized, PerformancePresets.Overdrive, PerformancePresets.Custom];


	// Config entries
	private static ConfigFile config;
	public static ConfigEntry<PerformancePresets> confPreset;
	public static ConfigEntry<ToggleEnum> confGlobalIllumination;
	public static ConfigEntry<StrengthEnum> confReflections;
	public static ConfigEntry<ToggleEnum> confAmbientOcclusion;
	public static ConfigEntry<StrengthEnum> confShadowQuality;
	public static ConfigEntry<DioramaOnlyEnum> confVolumetrics;
	public static ConfigEntry<ToggleEnum> confChromaAberration;
	public static ConfigEntry<ToggleEnum> confVignette;
	public static ConfigEntry<ToggleEnum> confShadowTones;
	public static ConfigEntry<ToggleEnum> confDockLights;
	public static ConfigEntry<AntialiasingEnum> confAntialiasing;
	public static ConfigEntry<QuantityEnum> confMachineParticles;

	private static CycleConfigEntry<PerformancePresets> _confPreset;
	private static CycleConfigEntry<ToggleEnum> _confGlobalIllumination;
	private static CycleConfigEntry<StrengthEnum> _confReflections;
	private static CycleConfigEntry<StrengthEnum> _confShadowQuality;
	private static CycleConfigEntry<DioramaOnlyEnum> _confVolumetrics;
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
	private static GameObject modConfObj;
	private static GameObject applyConfObj;
	private static GameObject settingsView;
	private static Button modConfBtn;
	private static Button applyConfBtn;

	// Vars
	private static bool once = false;
	private static bool init = false;
	private static bool delayedInit = false;

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
			modConfObj = CreateModSettingButton();

			// Create our own Apply button
			applyConfObj = CreateModApplyButton();
			// modConfObj.GetComponent<Button>().onClick.AddListener((Action) applyConfObj.GetComponent<Button>().);

			// Create the settings table
			settingsView = CreateDataView();

			// Set up UI navigation. Very roundabout because Unity is garbage.
			Button keyAssignButton = settingsOriginal.transform.Find("Button_Keyassign").GetComponent<Button>();
			Utils.SetUINavigation(keyAssignButton, NavDirEnum.DONW, modConfBtn);

			Button exitButton = settingsOriginal.transform.Find("Button_Exit").GetComponent<Button>();
			Utils.SetUINavigation(exitButton, NavDirEnum.UP, modConfBtn);

			Utils.SetUINavigation(modConfBtn, NavDirEnum.UP, keyAssignButton);
			Utils.SetUINavigation(modConfBtn, NavDirEnum.DONW, exitButton);
			Utils.SetUINavigation(modConfBtn, NavDirEnum.RIGHT, applyConfBtn);

			Utils.SetUINavigation(applyConfBtn, NavDirEnum.LEFT, modConfBtn);
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
		TextMeshProUGUI textComp = textLabel.GetComponent<TextMeshProUGUI>();
		textComp.text = Localization.GetText(Localization.Items.MOD_BUTTON);
		Localization.OnLocaleChanged += () => { textComp?.text = Localization.GetText(Localization.Items.MOD_BUTTON); };

		btn.GetComponent<Button>().onClick.RemoveAllListeners();
		btn.GetComponent<Button>().onClick.AddListener((System.Action)(() => OnShowModSettings()));

		modConfBtn = btn.GetComponent<Button>();
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
		applyConfBtn = btn.GetComponent<Button>();
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
		GameObject rowSource = content.transform.GetChild(0).gameObject;

		// Populate with custom settings
		List<Localization.Items> buttons = [
			Localization.Items.SET_PRESET,
			Localization.Items.SET_GI_ENTRY, // Very heavy
			Localization.Items.SET_SSR_ENTRY, // Light
			Localization.Items.SET_SHADOWQUALITY_ENTRY, // Medium
			Localization.Items.SET_VOLUMETRICS, // Medium
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
		List<GameObject> newRows = [];
		for (int i = 0; i < buttons.Count; i++)
		{
			GameObject newRow = DuplicateSettingRow(rowSource, buttons[i]);
			newRows.Add(newRow);
			newRow.transform.SetParent(content.transform);
			newRow.transform.localPosition = rowSource.transform.localPosition - new Vector3(0, 60 * i, 0);
			newRow.transform.localScale = new Vector3(1, 1, 1);

			newRow.GetComponent<RectTransform>().sizeDelta = rowSource.GetComponent<RectTransform>().sizeDelta + new Vector2(30, 10);
			newRow.active = true; // Doesn't animate the vanilla menus, but that's fine.
		}

		// Set up UI navigation
		for (int i = 0; i < newRows.Count; i++)
		{
			GameObject row = newRows[i];
			Button btnLeft = row.transform.GetChild(1).GetComponent<Button>();
			Button btnRight = row.transform.GetChild(2).GetComponent<Button>();

			if (i == 0)
			{
				Utils.SetUINavigation(btnLeft, NavDirEnum.UP, applyConfBtn);
				Utils.SetUINavigation(btnRight, NavDirEnum.UP, applyConfBtn);

				Utils.SetUINavigation(applyConfBtn, NavDirEnum.DONW, btnLeft);
			}
			else
			{
				Utils.SetUINavigation(btnLeft, NavDirEnum.UP, newRows[i - 1].transform.GetChild(1).GetComponent<Button>());
				Utils.SetUINavigation(btnRight, NavDirEnum.UP, newRows[i - 1].transform.GetChild(2).GetComponent<Button>());
			}

			if (i == newRows.Count - 1)
			{
				Utils.SetUINavigation(btnLeft, NavDirEnum.DONW, null);
				Utils.SetUINavigation(btnRight, NavDirEnum.DONW, null);
			}
			else
			{
				Utils.SetUINavigation(btnLeft, NavDirEnum.DONW, newRows[i + 1].transform.GetChild(1).GetComponent<Button>());
				Utils.SetUINavigation(btnRight, NavDirEnum.DONW, newRows[i + 1].transform.GetChild(2).GetComponent<Button>());
			}

			Utils.SetUINavigation(btnLeft, NavDirEnum.LEFT, modConfBtn);
			Utils.SetUINavigation(btnLeft, NavDirEnum.RIGHT, btnRight);

			Utils.SetUINavigation(btnRight, NavDirEnum.LEFT, btnLeft);
			Utils.SetUINavigation(btnRight, NavDirEnum.RIGHT, null);
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
		Localization.OnLocaleChanged += () => { titleText?.text = Localization.GetText(title); };

		TextMeshProUGUI valueText = obj.transform.GetChild(3).GetComponent<TextMeshProUGUI>();
		valueText.text = "N/A";

		dynamic entry = null;
		switch (title)
		{
			case Localization.Items.SET_PRESET:
				entry = _confPreset = new CycleConfigEntry<PerformancePresets>(confPreset, presetEnums, valueText);
				break;
			case Localization.Items.SET_GI_ENTRY:
				entry = _confGlobalIllumination = new CycleConfigEntry<ToggleEnum>(confGlobalIllumination, toggleEnums, valueText);
				break;
			case Localization.Items.SET_SSR_ENTRY:
				entry = _confReflections = new CycleConfigEntry<StrengthEnum>(confReflections, strengthEnums, valueText);
				break;
			case Localization.Items.SET_SHADOWQUALITY_ENTRY:
				entry = _confShadowQuality = new CycleConfigEntry<StrengthEnum>(confShadowQuality, strengthEnums, valueText);
				break;
			case Localization.Items.SET_AO_ENTRY:
				entry = _confAmbientOcclusion = new CycleConfigEntry<ToggleEnum>(confAmbientOcclusion, toggleEnums, valueText);
				break;
			case Localization.Items.SET_CHROMAABERRATION_ENTRY:
				entry = _confChromaAberration = new CycleConfigEntry<ToggleEnum>(confChromaAberration, toggleEnums, valueText);
				break;
			case Localization.Items.SET_VIGNETTE_ENTRY:
				entry = _confVignette = new CycleConfigEntry<ToggleEnum>(confVignette, toggleEnums, valueText);
				break;
			case Localization.Items.SET_SHADOWTONES_ENTRY:
				entry = _confShadowTones = new CycleConfigEntry<ToggleEnum>(confShadowTones, toggleEnums, valueText);
				break;
			case Localization.Items.SET_DOCKLIGHTS_ENTRY:
				entry = _confDockLights = new CycleConfigEntry<ToggleEnum>(confDockLights, toggleEnums, valueText);
				break;
			case Localization.Items.SET_AA_ENTRY:
				entry = _confAntialiasing = new CycleConfigEntry<AntialiasingEnum>(confAntialiasing, antialiasingEnums, valueText);
				break;
			case Localization.Items.SET_MACHINEPARTICLES_ENTRY:
				entry = _confMachineParticles = new CycleConfigEntry<QuantityEnum>(confMachineParticles, quantityEnums, valueText);
				break;
			case Localization.Items.SET_VOLUMETRICS:
				entry = _confVolumetrics = new CycleConfigEntry<DioramaOnlyEnum>(confVolumetrics, dioramaOnlyEnums, valueText);
				break;
		}

		Button btnLeft = obj.transform.GetChild(1).GetComponent<Button>();
		Button btnRight = obj.transform.GetChild(2).GetComponent<Button>();
		btnLeft.onClick.RemoveAllListeners();
		btnRight.onClick.RemoveAllListeners();
		btnLeft.onClick.AddListener((System.Action)(() => { entry.OnLeftButton(); }));
		btnRight.onClick.AddListener((System.Action)(() => { entry.OnRightButton(); }));
		if (title != Localization.Items.SET_PRESET)
		{
			btnLeft.onClick.AddListener((System.Action)(() => { confPreset?.Value = PerformancePresets.Custom; _confPreset?.Cancel(); }));
			btnRight.onClick.AddListener((System.Action)(() => { confPreset?.Value = PerformancePresets.Custom; _confPreset?.Cancel(); }));
		}

		return obj;
	}

	public static void OnShowModSettings()
	{
		applyConfObj.active = true;
		settingsView.active = true;
		scrollViewOriginal.active = false;
		applyButtonOriginal.active = false;

		EventSystem.current.SetSelectedGameObject(applyConfObj, null);
		RefreshEntries();
	}

	public static void RefreshEntries()
	{
		if (!settingsView) return;
		_confPreset.Cancel();
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
		applyConfObj.active = false;
		settingsView.active = false;
		scrollViewOriginal.active = true;
		applyButtonOriginal.active = true;
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(Scene_MainMenu), nameof(Scene_MainMenu.Start))]
	public static void LoadConfig()
	{
		// Localization.SetLocale(); // Config.Language
		config = Plugin.config;

		confPreset = config.Bind("Graphics", "GraphicsPreset", PerformancePresets.Vanilla, "Preconfigured set of options.");
		confGlobalIllumination = config.Bind("Graphics", "GlobalIllumination", ToggleEnum.on, "Toggles Global Illumination, volumetric lighting within the main world. Cost: Very expensive.");
		confReflections = config.Bind("Graphics", "Reflections", StrengthEnum.def, "Toggles SSR and reflection probes surfaces. Cost: Light.");
		confShadowQuality = config.Bind("Graphics", "ShadowQuality", StrengthEnum.def, "Adjusts quality of all shadows. Cost: Medium.");
		confVolumetrics = config.Bind("Graphics", "Volumetrics", DioramaOnlyEnum.on, "Toggles Volumetric effects. Mostly found in Diorama. Warning: This will disable some Diorama effects. Cost: Medium.");
		confAmbientOcclusion = config.Bind("Graphics", "AmbientOcclusion", ToggleEnum.on, "Toggles Ambient Occlusion. Cost: Very light.");
		confChromaAberration = config.Bind("Graphics", "ChromaticAberration", ToggleEnum.on, "Toggles Chromatic Aberration. Cost: Very light.");
		confVignette = config.Bind("Graphics", "Vignette", ToggleEnum.on, "Toggles a vignette effect. Cost: Very light.");
		confShadowTones = config.Bind("Graphics", "ShadowToneMapping", ToggleEnum.on, "Toggles remapping of shadow midtones and highlights. Cost: Very light.");
		confDockLights = config.Bind("Graphics", "DockLights", ToggleEnum.on, "Sets the quality of lights in the player dock. Useful if GI and SSR are off, which makes some shadows look weird. Cost: Light.");
		confAntialiasing = config.Bind("Graphics", "AntiAliasing", AntialiasingEnum.MSAA, "Sets the AntiAliasing type to use, if any. Cost: Very light.");
		confMachineParticles = config.Bind("Graphics", "MachineParticles", QuantityEnum.full, "Sets the amount of particles and other machine-related effects. Cost: Light.");

		currentPreset = confPreset.Value;
		ApplyChanges();

		if (!once) BridgedSceneManager.OnSceneLoadComplete.AddListener((Action)(() => DelayedInit()));
		once = true;
	}

	public static void OnSettingsApply()
	{
		_confPreset.Confirm();
		_confGlobalIllumination.Confirm();
		_confReflections.Confirm();
		_confShadowQuality.Confirm();
		_confVolumetrics.Confirm();
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

	public static void SetPreset(PerformancePresets preset)
	{
		currentPreset = preset;
		if (currentPreset != PerformancePresets.Custom)
		{
			switch (currentPreset)
			{
				case PerformancePresets.Overdrive:
					confGlobalIllumination.Value = ToggleEnum.off;
					confReflections.Value = StrengthEnum.aggresive;
					confShadowQuality.Value = StrengthEnum.aggresive;
					confVolumetrics.Value = DioramaOnlyEnum.off;
					confAmbientOcclusion.Value = ToggleEnum.off;
					confChromaAberration.Value = ToggleEnum.off;
					confVignette.Value = ToggleEnum.off;
					confShadowTones.Value = ToggleEnum.off;
					confDockLights.Value = ToggleEnum.off;
					confAntialiasing.Value = AntialiasingEnum.off;
					confMachineParticles.Value = QuantityEnum.none;
					break;
				case PerformancePresets.Optimized:
					confGlobalIllumination.Value = ToggleEnum.off;
					confReflections.Value = StrengthEnum.optimized;
					confShadowQuality.Value = StrengthEnum.optimized;
					confVolumetrics.Value = DioramaOnlyEnum.dioramaOnly;
					confAmbientOcclusion.Value = ToggleEnum.off;
					confChromaAberration.Value = ToggleEnum.on;
					confVignette.Value = ToggleEnum.on;
					confShadowTones.Value = ToggleEnum.on;
					confDockLights.Value = ToggleEnum.off;
					confAntialiasing.Value = AntialiasingEnum.MSAA;
					confMachineParticles.Value = QuantityEnum.reduced;
					break;
				case PerformancePresets.Vanilla:
				default:
					confGlobalIllumination.Value = ToggleEnum.on;
					confReflections.Value = StrengthEnum.def;
					confShadowQuality.Value = StrengthEnum.def;
					confVolumetrics.Value = DioramaOnlyEnum.on;
					confAmbientOcclusion.Value = ToggleEnum.on;
					confChromaAberration.Value = ToggleEnum.on;
					confVignette.Value = ToggleEnum.on;
					confShadowTones.Value = ToggleEnum.on;
					confDockLights.Value = ToggleEnum.on;
					confAntialiasing.Value = AntialiasingEnum.MSAA;
					confMachineParticles.Value = QuantityEnum.full;
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

		ModPerformance.SetGlobalIllumination(confGlobalIllumination.Value);
		ModPerformance.SetReflections(confReflections.Value);
		ModPerformance.SetShadowQuality(confShadowQuality.Value);
		ModPerformance.SetChromaAberration(confChromaAberration.Value);
		ModPerformance.SetVignette(confVignette.Value);
		ModPerformance.SetShadowTones(confShadowTones.Value);
		ModPerformance.SetAntialiasing(confAntialiasing.Value);
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

		bool disableVolumetrics = confVolumetrics.Value != DioramaOnlyEnum.on;
		if (SceneManager.GetActiveScene().name == "Georama") disableVolumetrics = confVolumetrics.Value == DioramaOnlyEnum.off;

		HDRPReflectionHelper.ApplyPipelineSupportFlagBatch( // true = disabled
			confReflections.Value != StrengthEnum.def, // SSR
			confAmbientOcclusion.Value == ToggleEnum.off, // Ambient Occlusion
			disableVolumetrics, // Volumetrics // Disabled if not on
			true, // Vol Clouds
			true, // Subsurface Scattering
			true, // Decals (already disabled by default)
			confMachineParticles.Value != QuantityEnum.full, // Distortion
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

	[HarmonyPostfix]
	[HarmonyPatch(typeof(SettingsManager), nameof(SettingsManager.SetResolution))]
	private static void RescaleUI()
	{
		// Why this isn't automatically handled is beyond me.
		Utils.RescaleUI(modConfObj);
		Utils.RescaleUI(applyConfObj);
		Utils.RescaleUI(settingsView);
	}
}

public enum PerformancePresets
{
	Vanilla,
	Optimized,
	Overdrive,
	Custom,
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