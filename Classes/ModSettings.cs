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
using System.Collections;
using System.Runtime.CompilerServices;

namespace PerformanceMod;

public class ModSettings
{
	private static bool init = true;
	public enum GenericToggleEnum { off, on }
	public enum GenericTieredEnum { low, medium, high }
	public enum GenericTieredWithOffEnum { off, low, medium, high }
	public enum GenericRaceOnlyEnum { off, raceOnly, on }
	public enum GenericQuantityEnum { none, reduced, full }
	public enum AntialiasingEnum { off, FXAA, MSAA, TAA }
	public static readonly Dictionary<Enum, Localization.LocaleItems> valueTextRelation = new(){
		{GenericToggleEnum.off, Localization.LocaleItems.setOff},
		{GenericToggleEnum.on, Localization.LocaleItems.setOn},

		{GenericTieredEnum.low, Localization.LocaleItems.setLow},
		{GenericTieredEnum.medium, Localization.LocaleItems.setMedium},
		{GenericTieredEnum.high, Localization.LocaleItems.setHigh},

		{GenericTieredWithOffEnum.off, Localization.LocaleItems.setOff},
		{GenericTieredWithOffEnum.low, Localization.LocaleItems.setLow},
		{GenericTieredWithOffEnum.medium, Localization.LocaleItems.setMedium},
		{GenericTieredWithOffEnum.high, Localization.LocaleItems.setHigh},

		{GenericRaceOnlyEnum.off, Localization.LocaleItems.setOff},
		{GenericRaceOnlyEnum.raceOnly, Localization.LocaleItems.setRaceOnly},
		{GenericRaceOnlyEnum.on, Localization.LocaleItems.setOn},

		{AntialiasingEnum.off, Localization.LocaleItems.setOff},
		{AntialiasingEnum.FXAA, Localization.LocaleItems.FXAA},
		{AntialiasingEnum.MSAA, Localization.LocaleItems.MSAA},
		{AntialiasingEnum.TAA, Localization.LocaleItems.TAA},

		{GenericQuantityEnum.none, Localization.LocaleItems.none},
		{GenericQuantityEnum.reduced, Localization.LocaleItems.reduced},
		{GenericQuantityEnum.full, Localization.LocaleItems.full},
	};

	private static readonly IReadOnlyList<GenericToggleEnum> genericToggleValues = [GenericToggleEnum.off, GenericToggleEnum.on];
	private static readonly IReadOnlyList<GenericTieredEnum> genericTierValues = [GenericTieredEnum.low, GenericTieredEnum.medium, GenericTieredEnum.high];
	private static readonly IReadOnlyList<GenericTieredWithOffEnum> genericTierValuesWithDisabled = [GenericTieredWithOffEnum.off, GenericTieredWithOffEnum.low, GenericTieredWithOffEnum.medium, GenericTieredWithOffEnum.high];
	private static readonly IReadOnlyList<GenericRaceOnlyEnum> genericRaceOnlyValues = [GenericRaceOnlyEnum.off, GenericRaceOnlyEnum.raceOnly, GenericRaceOnlyEnum.on];
	private static readonly IReadOnlyList<AntialiasingEnum> antialiasingValues = [AntialiasingEnum.off, AntialiasingEnum.FXAA, AntialiasingEnum.MSAA, AntialiasingEnum.TAA];
	private static readonly IReadOnlyList<GenericQuantityEnum> genericQuantityValues = [GenericQuantityEnum.none, GenericQuantityEnum.reduced, GenericQuantityEnum.full];

	// Config entries
	private static ConfigFile config;
	public static ConfigEntry<GenericToggleEnum> confGlobalIllumination;
	public static ConfigEntry<GenericRaceOnlyEnum> confReflections;
	public static ConfigEntry<GenericToggleEnum> confAmbientOcclusion;
	public static ConfigEntry<GenericToggleEnum> confChromaAberration;
	public static ConfigEntry<GenericToggleEnum> confVignette;
	public static ConfigEntry<GenericToggleEnum> confShadowTones;
	public static ConfigEntry<GenericToggleEnum> confDockLights;
	public static ConfigEntry<AntialiasingEnum> confAntialiasing;
	public static ConfigEntry<GenericQuantityEnum> confMachineParticles;
	private static CycleConfigEntry<GenericToggleEnum> _confGlobalIllumination;
	private static CycleConfigEntry<GenericRaceOnlyEnum> _confReflections;
	private static CycleConfigEntry<GenericToggleEnum> _confAmbientOcclusion;
	private static CycleConfigEntry<GenericToggleEnum> _confChromaAberration;
	private static CycleConfigEntry<GenericToggleEnum> _confVignette;
	private static CycleConfigEntry<GenericToggleEnum> _confShadowTones;
	private static CycleConfigEntry<GenericToggleEnum> _confDockLights;
	private static CycleConfigEntry<AntialiasingEnum> _confAntialiasing;
	private static CycleConfigEntry<GenericQuantityEnum> _confMachineParticles;

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
		textLabel.GetComponent<TextMeshProUGUI>().text = Localization.GetText(Localization.LocaleItems.settingsModButton);
		Localization.OnLocaleChanged += () => { if (textLabel) textLabel.GetComponent<TextMeshProUGUI>().text = Localization.GetText(Localization.LocaleItems.settingsModButton); };

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
		List<Localization.LocaleItems> buttons = [
			Localization.LocaleItems.SET_GI_ENTRY, // Very heavy
			Localization.LocaleItems.SET_AO_ENTRY, // Free
			Localization.LocaleItems.SET_SSR_ENTRY, // Light, Very light in races
			Localization.LocaleItems.SET_CHROMAABERRATION_ENTRY, // Free
			Localization.LocaleItems.SET_VIGNETTE_ENTRY, // Free
			Localization.LocaleItems.SET_SHADOWTONES_ENTRY, // Free
			Localization.LocaleItems.SET_DOCKLIGHTS_ENTRY, // Light
			Localization.LocaleItems.SET_AA_ENTRY, // Light
			Localization.LocaleItems.SET_MACHINEPARTICLES_ENTRY, // Light
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

	public static GameObject DuplicateSettingRow(GameObject origin, Localization.LocaleItems title)
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
			case Localization.LocaleItems.SET_GI_ENTRY:
				entry = _confGlobalIllumination = new CycleConfigEntry<GenericToggleEnum>(confGlobalIllumination, genericToggleValues, valueText);
				break;
			case Localization.LocaleItems.SET_SSR_ENTRY:
				entry = _confReflections = new CycleConfigEntry<GenericRaceOnlyEnum>(confReflections, genericRaceOnlyValues, valueText);
				break;
			case Localization.LocaleItems.SET_AO_ENTRY:
				entry = _confAmbientOcclusion = new CycleConfigEntry<GenericToggleEnum>(confAmbientOcclusion, genericToggleValues, valueText);
				break;
			case Localization.LocaleItems.SET_CHROMAABERRATION_ENTRY:
				entry = _confChromaAberration = new CycleConfigEntry<GenericToggleEnum>(confChromaAberration, genericToggleValues, valueText);
				break;
			case Localization.LocaleItems.SET_VIGNETTE_ENTRY:
				entry = _confVignette = new CycleConfigEntry<GenericToggleEnum>(confVignette, genericToggleValues, valueText);
				break;
			case Localization.LocaleItems.SET_SHADOWTONES_ENTRY:
				entry = _confShadowTones = new CycleConfigEntry<GenericToggleEnum>(confShadowTones, genericToggleValues, valueText);
				break;
			case Localization.LocaleItems.SET_DOCKLIGHTS_ENTRY:
				entry = _confDockLights = new CycleConfigEntry<GenericToggleEnum>(confDockLights, genericToggleValues, valueText);
				break;
			case Localization.LocaleItems.SET_AA_ENTRY:
				entry = _confAntialiasing = new CycleConfigEntry<AntialiasingEnum>(confAntialiasing, antialiasingValues, valueText);
				break;
			case Localization.LocaleItems.SET_MACHINEPARTICLES_ENTRY:
				entry = _confMachineParticles = new CycleConfigEntry<GenericQuantityEnum>(confMachineParticles, genericQuantityValues, valueText);
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

		confGlobalIllumination = config.Bind("Graphics", "GlobalIllumination", GenericToggleEnum.on, "Toggles Global Illumination, volumetric lighting within the main world. Cost: Very expensive.");
		confReflections = config.Bind("Graphics", "Reflections", GenericRaceOnlyEnum.on, "Toggles SSR reflecting surfaces. Cost: Light.");
		confAmbientOcclusion = config.Bind("Graphics", "AmbientOcclusion", GenericToggleEnum.on, "Toggles Ambient Occlusion. Cost: Very light.");
		confChromaAberration = config.Bind("Graphics", "ChromaticAberration", GenericToggleEnum.on, "Toggles Chromatic Aberration. Cost: Very light.");
		confVignette = config.Bind("Graphics", "Vignette", GenericToggleEnum.on, "Toggles a vignette effect. Cost: Very light.");
		confShadowTones = config.Bind("Graphics", "ShadowToneMapping", GenericToggleEnum.on, "Toggles remapping of shadow midtones and highlights. Cost: Very light.");
		confDockLights = config.Bind("Graphics", "DockLights", GenericToggleEnum.on, "Sets the quality of lights in the player dock. Useful if GI and SSR are off, which makes some shadows look weird. Cost: Light.");
		confAntialiasing = config.Bind("Graphics", "AntiAliasing", AntialiasingEnum.MSAA, "Sets the AntiAliasing type to use, if any. Cost: Very light.");
		confMachineParticles = config.Bind("Graphics", "MachineParticles", GenericQuantityEnum.full, "Sets the amount of particles and other machine-related effects. Cost: Light.");

		ApplyChanges();

		// Config.Debug_OutputRawSaveData = true;
	}

	public static void OnSettingsApply()
	{
		_confGlobalIllumination.Confirm();
		_confReflections.Confirm();
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
		ModPerformance.SetAmbientOcclusion(confAmbientOcclusion.Value);
		ModPerformance.SetChromaAberration(confChromaAberration.Value);
		ModPerformance.SetVignette(confVignette.Value);
		ModPerformance.SetShadowTones(confShadowTones.Value);
		ModPerformance.SetDockLights(confDockLights.Value);
		ModPerformance.SetAntialiasing(confAntialiasing.Value);
		if (!init) ModPerformance.SetMachineParticles(confMachineParticles.Value);

		init = false;
	}

	[HarmonyPostfix]
	// [HarmonyPatch(typeof(MachineDataManager), nameof(MachineDataManager.LoadMachine))] // Doesn't work
    [HarmonyPatch(typeof(Scene_MainMenu), nameof(Scene_MainMenu.ShiftMainMenu))]
	public static void LoadAfterInit()
	{
		ModPerformance.SetDockLights(confDockLights.Value);
		ModPerformance.SetMachineParticles(confMachineParticles.Value);
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(Scene_Settings), nameof(Scene_Settings.ApplySettings_Graphic))]
	public static void RescaleUI()
	{
		// Why this isn't automatically handled is beyond me.
		settingsButton.transform.localScale = new Vector3(1, 1, 1);
		applyButton.transform.localScale = new Vector3(1, 1, 1);
		settingsView.transform.localScale = new Vector3(1, 1, 1);
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