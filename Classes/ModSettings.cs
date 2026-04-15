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

namespace BA3StandardMod;

public class ModSettings
{
	public enum ValueEnum { off, on, low, medium, high }
	// public enum GenericToggleEnum { off, on }
	// public enum GenericTieredEnum { low, medium, high }
	// public enum GenericTieredWithOffEnum { off, low, medium, high }
	public static readonly Dictionary<ValueEnum, Localization.LocaleItems> valueTextRelation = new(){
		{ValueEnum.off, Localization.LocaleItems.setOff},
		{ValueEnum.on, Localization.LocaleItems.setOn},
		{ValueEnum.low, Localization.LocaleItems.setLow},
		{ValueEnum.medium, Localization.LocaleItems.setMedium},
		{ValueEnum.high, Localization.LocaleItems.setHigh},
	};

	private static readonly IReadOnlyList<ValueEnum> genericToggleValues = [ValueEnum.off, ValueEnum.on];
	private static readonly IReadOnlyList<ValueEnum> genericTierValues = [ValueEnum.low, ValueEnum.medium, ValueEnum.high];
	private static readonly IReadOnlyList<ValueEnum> genericTierValuesWithDisabled = [ValueEnum.off, ValueEnum.low, ValueEnum.medium, ValueEnum.high];

	// Config entries
	private static ConfigFile config;
	public static ConfigEntry<ValueEnum> confGlobalIllumination;
	public static ConfigEntry<ValueEnum> confReflections;
	public static ConfigEntry<ValueEnum> confAmbientOcclusion;
	private static CycleConfigEntry<ValueEnum> _confGlobalIllumination;
	private static CycleConfigEntry<ValueEnum> _confReflections;
	private static CycleConfigEntry<ValueEnum> _confAmbientOcclusion;

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

			LoadConfig();
		}
		catch (System.Exception err)
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
			Localization.LocaleItems.settingsValueGlobalIllumination, // Very expensive
			Localization.LocaleItems.settingsValueReflections, // Almost free
			Localization.LocaleItems.settingsValueAmbientOcclusion, // Almost free
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
			newRow.active = true; // Doesn't animate the vanilla menus, but that's fine.}
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

		CycleConfigEntry<ValueEnum> entry = null;
		switch (title)
		{
			case Localization.LocaleItems.settingsValueGlobalIllumination:
				_confGlobalIllumination = new CycleConfigEntry<ValueEnum>(confGlobalIllumination, genericToggleValues, valueText);
				entry = _confGlobalIllumination;
				break;
			case Localization.LocaleItems.settingsValueReflections:
				_confReflections = new CycleConfigEntry<ValueEnum>(confReflections, genericToggleValues, valueText);
				entry = _confReflections;
				break;
			case Localization.LocaleItems.settingsValueAmbientOcclusion:
				_confAmbientOcclusion = new CycleConfigEntry<ValueEnum>(confAmbientOcclusion, genericToggleValues, valueText);
				entry = _confAmbientOcclusion;
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
		config = Plugin.config;

		confGlobalIllumination = config.Bind("Graphics", "GlobalIllumination", ValueEnum.on, "Toggles volumetric lighting and reflections around the game world.");
		confReflections = config.Bind("Graphics", "Reflections", ValueEnum.on, "Toggles reflecting surfaces.");
		confAmbientOcclusion = config.Bind("Graphics", "AmbientOcclusion", ValueEnum.on, "Toggles ambient occlusion.");

		ApplyChanges();
	}

	public static void OnSettingsApply()
	{
		_confGlobalIllumination.Confirm();
		_confReflections.Confirm();
		_confAmbientOcclusion.Confirm();

		config.Save();

		ApplyChanges();
	}

	public static void ApplyChanges()
	{
		ModPerformance.SetGlobalIllumination(confGlobalIllumination.Value);
		ModPerformance.SetReflections(confReflections.Value);
		ModPerformance.SetAmbientOcclusion(confAmbientOcclusion.Value);
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(Scene_Settings), nameof(Scene_Settings.ApplySettings_Graphic))]
	public static void RescaleUI()
	{
		Plugin.Log.LogInfo("Rescaling...");
		settingsButton.transform.localScale = new Vector3(1, 1, 1);
		applyButton.transform.localScale = new Vector3(1, 1, 1);
		settingsView.transform.localScale = new Vector3(1, 1, 1);
	}
}

public class CycleConfigEntry<T>
{
	private readonly ConfigEntry<ModSettings.ValueEnum> _configEntry;
	private readonly IReadOnlyList<ModSettings.ValueEnum> _options;
	private readonly TextMeshProUGUI _text;
	private ModSettings.ValueEnum _pendingValue;
	public ModSettings.ValueEnum Pending => _pendingValue;
	public ModSettings.ValueEnum Value => _configEntry.Value;

	public CycleConfigEntry(ConfigEntry<ModSettings.ValueEnum> configEntry, IReadOnlyList<ModSettings.ValueEnum> options, TextMeshProUGUI textMesh = null)
	{
		_configEntry = configEntry;
		_options = options;
		_pendingValue = configEntry.Value;
		_text = textMesh;
		_text?.text = Localization.GetText(ModSettings.valueTextRelation[_pendingValue]);
		Localization.OnLocaleChanged += () => { _text?.text = Localization.GetText(ModSettings.valueTextRelation[_pendingValue]); };
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
		_text?.text = Localization.GetText(ModSettings.valueTextRelation[_pendingValue]);
	}
	public void OnRightButton()
	{
		int next = (GetPendingIndex() + 1) % _options.Count;
		_pendingValue = _options[next];
		_text?.text = Localization.GetText(ModSettings.valueTextRelation[_pendingValue]);
	}
	public void Confirm()
	{
		_configEntry.Value = _pendingValue;
	}
	public void Cancel()
	{
		_pendingValue = _configEntry.Value;
		_text?.text = Localization.GetText(ModSettings.valueTextRelation[_pendingValue]);
	}
}