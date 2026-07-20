using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ArchPerformanceMod;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("BREAK ARTS III.exe")]

public class Plugin : BasePlugin
{
	internal static new ManualLogSource Log;
	private static readonly bool verboseLogging = false;
	public static ConfigFile config;
	private static JsonSerializerOptions serializerOptions;
	public static ConfigData customConfig = new();
	public static UniverseLib.AssetBundle assets;
	public override void Load()
	{
		Console.InputEncoding = Encoding.UTF8;
		Console.OutputEncoding = Encoding.UTF8;

		// Plugin startup logic
		Log = BepInEx.Logging.Logger.CreateLogSource("ArchPerform");
		Log.LogInfo($"Loading plugin patches...");

		config = Config;

		string bundlePath = Path.Combine(Paths.PluginPath, "ArchitectsPerformanceMod", "assets");
		if (!File.Exists(bundlePath))
			throw new Exception($"AssetBundle could not be located found at: {bundlePath}. Aborting mod initializaton.");

		assets = UniverseLib.AssetBundle.LoadFromFile(bundlePath) ?? throw new Exception("Failed to load assets! Please report this bug to the developer, along with any game logs. Aborting mod initializaton.");

		var harmony = Harmony.CreateAndPatchAll(typeof(Localization));
		harmony.PatchAll(typeof(MainMenuPatch));
		harmony.PatchAll(typeof(QualityLevelPatch));
		harmony.PatchAll(typeof(GarageCameraPatch));
		harmony.PatchAll(typeof(ModSettings));
		harmony.PatchAll(typeof(ModPerformance));
		harmony.PatchAll(typeof(ModGameplay));
		harmony.PatchAll(typeof(EnvironmentUIPatch));

		serializerOptions = new JsonSerializerOptions() { WriteIndented = true, IncludeFields = true };
		serializerOptions.Converters.Add(new ColorConverter());

		bool configExists = File.Exists(Path.Join(Application.persistentDataPath, "/PerformanceModConfig.json"));
		if (configExists) LoadConfig();
		else SaveConfig();

		Log.LogInfo($"Done.");
	}

	public static void LogInfo(object data) => Log.LogInfo(data);
	public static void LogDebug(object data)
	{
		if (verboseLogging) LogInfo(data);
	}

	public static void SaveConfig()
	{
		try
		{
			Log.LogInfo("Saving additional mod configuration.");
			File.WriteAllText(Path.Join(Application.persistentDataPath, "/PerformanceModConfig.json"), JsonSerializer.Serialize(customConfig, serializerOptions));
		}
		catch (System.Exception ex)
		{
			Log.LogError(ex);
		}
	}
	private static void LoadConfig()
	{
		try
		{
			Log.LogInfo("Loading additional mod configuration.");
			customConfig = JsonSerializer.Deserialize<ConfigData>(File.ReadAllText(Path.Join(Application.persistentDataPath, "/PerformanceModConfig.json")), serializerOptions);
		}
		catch (System.Exception ex)
		{
			Log.LogError(ex);
		}
	}
}

public class ConfigData
{
	// Diorama
	public bool dioramaFog = true;
	public bool dioramaTerrain = true;
	public bool dioramaGround = true;
	public bool dioramaFrame = true;
	public bool dioramaCaptions = true;
	public Color dioramaFogColor = new(0.255f, 0.255f, 0.255f, 1);
	public Color dioramaBackgroundColor = new(1f, 1f, 1f, 1f);
	public Color dioramaGroundColor = new(0.392f, 0.392f, 0.392f, 1f);

	// Designer
	public bool designerFog = true;
	public Color designerGroundColor = new(0.392f, 0.392f, 0.392f, 1f);
}

public class MainMenuPatch
{
	public static TMP_FontAsset mainFont;
	public static Material mainFontMaterial;
	private static GameObject introSignature;
	private static Button mainModButton;
	private static Button returnFromCreditsBtn;
	private static Canvas canvas;
	private static GameObject credits;

	[HarmonyPostfix]
	[HarmonyPatch(typeof(Scene_MainMenu), nameof(Scene_MainMenu.Start))]
	public static void InitializeMain(ref Scene_MainMenu __instance)
	{
		TextMeshProUGUI sampledText = __instance.MainMenuRoot.transform.Find("StartMenu").GetChild(1).GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>();
		mainFont = sampledText.font;
		mainFontMaterial = sampledText.fontSharedMaterial;

		Cursor.lockState = CursorLockMode.Confined;

		AddModSignature(ref __instance);
		AddCanvas(ref __instance);
		AddModButton(ref __instance);
		AddCredits(ref __instance);
	}

	public static void AddCanvas(ref Scene_MainMenu __instance)
	{
		GameObject modCanvas = new("Mod Canvas");
		RectTransform rect = modCanvas.AddComponent<RectTransform>();
		// rect.pivot = new(0, 0);
		// rect.anchorMin = new(0, 0);
		// rect.anchorMax = new(1, 1);
		// rect.sizeDelta = new(1920, 1080);
		canvas = modCanvas.AddComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = 1;
		CanvasScaler scaler = modCanvas.AddComponent<CanvasScaler>();
		scaler.matchWidthOrHeight = 1;
		scaler.referenceResolution = new(1920, 1080);
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
		CanvasGroup group = modCanvas.AddComponent<CanvasGroup>();
		Image bg = modCanvas.AddComponent<Image>();
		bg.color = new(0, 0, 0, .9f);
		GraphicRaycaster raycast = modCanvas.AddComponent<GraphicRaycaster>();

		SceneManager.MoveGameObjectToScene(modCanvas, SceneManager.GetSceneByName("MainMenu"));
		// canvas.transform.SetParent(__instance.transform.root);
		modCanvas.gameObject.active = false;
	}

	public static void AddModSignature(ref Scene_MainMenu __instance)
	{
		Transform introMenu = __instance.MainMenuRoot.transform.Find("StartMenu");

		GameObject signatureObj = new("Mod signature");
		signatureObj.transform.SetParent(introMenu);

		RectTransform rect = signatureObj.AddComponent<RectTransform>();
		rect.pivot = new(0, 0);
		rect.anchorMin = new(0, 0);
		rect.anchorMax = new(0, 0);
		rect.sizeDelta = new(250, 20);
		rect.anchoredPosition = new(20, 0);

		TextMeshProUGUI signature = signatureObj.AddComponent<TextMeshProUGUI>();
		signature.name = "Mod Signature";
		signature.text = "Architect's Optimizations " + MyPluginInfo.PLUGIN_VERSION;
		signature.fontSize = 16f;
		signature.font = mainFont;
		signature.fontMaterial = mainFontMaterial;
		signature.transform.localScale = Vector3.one;

		introSignature = signatureObj;
	}

	public static void AddModButton(ref Scene_MainMenu __instance)
	{
		Transform mainMenu = __instance.MainMenuRoot.transform.Find("MainMenu_All");

		GameObject buttonObj = new("Mod Button");
		buttonObj.transform.SetParent(mainMenu);

		RectTransform modRect = buttonObj.AddComponent<RectTransform>();
		modRect.pivot = new(0, 0);
		modRect.anchorMin = new(0, 0);
		modRect.anchorMax = new(0, 0);
		modRect.sizeDelta = new(260, 40);
		modRect.anchoredPosition = new(20, 4);
		Image btnImage = buttonObj.AddComponent<Image>();
		btnImage.color = new(1, 1, 1, 1);
		Button button = buttonObj.AddComponent<Button>();
		button.targetGraphic = btnImage;
		button.transition = Selectable.Transition.ColorTint;
		ColorBlock colorBlock = new()
		{
			normalColor = new Color(0, 0, 0, 1),
			highlightedColor = new Color(.3f, .3f, .3f, 1),
			pressedColor = new Color(.4f, .6f, .7f, 1),
			selectedColor = new Color(0, 0, 0, 1),
			disabledColor = new Color(0, 0, 0, 1),
			colorMultiplier = 1,
			fadeDuration = .15f
		};
		button.colors = colorBlock;
		button.onClick.AddListener((Action)(() => { OpenModScreen(); GotoCredits(); }));

		// Add logo
		GameObject logoObj = new("ModLogo");
		logoObj.transform.SetParent(buttonObj.transform);
		RectTransform logoRect = logoObj.AddComponent<RectTransform>();
		logoRect.pivot = new(0, 0);
		logoRect.anchorMin = new(0, 0);
		logoRect.anchorMax = new(0, 0);
		logoRect.sizeDelta = new(36, 36);
		logoRect.anchoredPosition = new(2, 2);
		Sprite modLogo = Plugin.assets.LoadAsset<Sprite>("ModLogo");
		Image logoImg = logoObj.AddComponent<Image>();
		logoImg.sprite = modLogo;

		// Add text
		GameObject signatureObj = new("Mod signature");
		signatureObj.transform.SetParent(buttonObj.transform);
		RectTransform rect = signatureObj.AddComponent<RectTransform>();
		rect.pivot = new(0, 0);
		rect.anchorMin = new(0, 0);
		rect.anchorMax = new(0, 0);
		rect.sizeDelta = new(250, 26);
		rect.anchoredPosition = new(40, 0);
		TextMeshProUGUI signature = signatureObj.AddComponent<TextMeshProUGUI>();
		signature.name = "Mod Signature";
		signature.text = "Architect's Optimizations " + MyPluginInfo.PLUGIN_VERSION;
		signature.fontSize = 16f;
		signature.font = mainFont;
		signature.fontMaterial = mainFontMaterial;
		signature.transform.localScale = Vector3.one;

		mainModButton = button;
		Utils.SetUINavigation(__instance.NavButtons[__instance.NavButtons.Count - 1], NavDirEnum.DOWN, mainModButton);
		Utils.SetUINavigation(mainModButton, NavDirEnum.UP, __instance.NavButtons[__instance.NavButtons.Count - 1]);
	}

	public static void AddCredits(ref Scene_MainMenu __instance)
	{
		CreditBlock creditData = new();
		creditData.Add(CreditBlock.SIZES.TITLE, "ARCHITECT'S PERFORMANCE MOD");
		creditData.Add(CreditBlock.SIZES.SEPARATOR, "");
		creditData.Add(CreditBlock.SIZES.ROLE, "DEVELOPER");
		creditData.Add(CreditBlock.SIZES.TEXT, "Merilax");
		creditData.Add(CreditBlock.SIZES.SEPARATOR, "");
		creditData.Add(CreditBlock.SIZES.ROLE, "SPECIAL THANKS");
		creditData.Add(CreditBlock.SIZES.TEXT, "Ara. - For being there in my worst times.");
		creditData.Add(CreditBlock.SIZES.TEXT, "CloudCluster - For taking care of the BreakArts Builders Discord.");
		creditData.Add(CreditBlock.SIZES.TEXT, "The Break Arts community - For making this possible.");
		creditData.Add(CreditBlock.SIZES.SEPARATOR, "");
		creditData.Add(CreditBlock.SIZES.ROLE, "TRANSLATION CONTRIBUTORS");
		creditData.Add(CreditBlock.SIZES.ROLE, "Japanese");
		creditData.Add(CreditBlock.SIZES.TEXT, "oresi1034");
		creditData.Add(CreditBlock.SIZES.TEXT, "Kai5378");

		credits = new("Credits");
		credits.transform.SetParent(canvas.transform);
		RectTransform rect = credits.AddComponent<RectTransform>();
		rect.pivot = new(.5f, .5f);
		rect.anchorMin = new(.5f, .5f);
		rect.anchorMax = new(.5f, .5f);
		rect.sizeDelta = new(1000, 960);
		rect.anchoredPosition = new(0, 0);
		VerticalLayoutGroup layout = credits.AddComponent<VerticalLayoutGroup>();
		layout.childForceExpandHeight = false;
		layout.spacing = 20;

		GameObject scrollView = UIUtils.CreateScrollView(credits.transform, true, false);
		LayoutElement scrollLayout = scrollView.AddComponent<LayoutElement>();
		scrollLayout.flexibleHeight = 1;
		scrollLayout.flexibleWidth = 1;
		Image background = scrollView.AddComponent<Image>();
		background.color = new(.8f, .8f, .8f);

		GameObject scrollContent = scrollView.transform.GetChild(0).gameObject;
		VerticalLayoutGroup scrollContentLayout = scrollContent.GetComponent<VerticalLayoutGroup>();
		scrollContentLayout.childAlignment = TextAnchor.MiddleCenter;
		scrollContentLayout.padding = new(30, 30, 30, 30);
		scrollContentLayout.spacing = 20;
		scrollContentLayout.childForceExpandWidth = false;

		GameObject logo = new("Logo");
		logo.transform.SetParent(scrollContent.transform);
		LayoutElement layoutElement = logo.AddComponent<LayoutElement>();
		layoutElement.preferredHeight = 170;
		layoutElement.preferredWidth = 170;
		// rect = logo.AddComponent<RectTransform>();
		// rect.pivot = new(.5f, .5f);
		// rect.sizeDelta = new(170, 170);
		Sprite logoSprite = Plugin.assets.LoadAsset<Sprite>("ModLogo");
		Image logoImg = logo.AddComponent<Image>();
		logoImg.sprite = logoSprite;

		foreach (var item in creditData.rows)
		{
			if (item.Key == CreditBlock.SIZES.SEPARATOR)
			{
				GameObject separator = new();
				separator.transform.SetParent(scrollContent.transform);
				Image sepImg = separator.AddComponent<Image>();
				sepImg.color = new(.7f, .7f, .7f);
				layoutElement = separator.AddComponent<LayoutElement>();
				layoutElement.preferredHeight = 6;
				layoutElement.flexibleWidth = 1;

				continue;
			}
			GameObject row = new();
			row.transform.SetParent(scrollContent.transform);
			TextMeshProUGUI text = row.AddComponent<TextMeshProUGUI>();
			text.text = item.Value;
			text.font = mainFont;
			text.fontMaterial = mainFontMaterial;
			text.color = new(.1f, .1f, .1f);
			text.textWrappingMode = TextWrappingModes.Normal;
			text.alignment = TextAlignmentOptions.Center;
			switch (item.Key)
			{
				case CreditBlock.SIZES.TITLE:
					text.fontSize = 46;
					break;
				case CreditBlock.SIZES.ROLE:
					text.fontSize = 30;
					break;
				case CreditBlock.SIZES.TEXT:
				default:
					text.fontSize = 24;
					break;
			}
		}

		returnFromCreditsBtn = UIUtils.CreateButton(Localization.Items.RETURN, credits.transform, mainFont, mainFontMaterial, () => { GotoModScreen(); CloseModScreen(); }, UICommons.FONT_SIZE_LARGE, 50).GetComponent<Button>();

		credits.active = false;
	}

	public static void OpenModScreen()
	{
		Scene_MainMenu mainMenu = GameObject.FindFirstObjectByType<Scene_MainMenu>();
		mainMenu?.MainMenuGp.interactable = false;
		canvas.gameObject.active = true;
		credits.active = false;
	}

	public static void CloseModScreen()
	{
		Scene_MainMenu mainMenu = GameObject.FindFirstObjectByType<Scene_MainMenu>();
		mainMenu?.MainMenuGp.interactable = true;
		canvas.gameObject.active = false;
		credits.active = false;
	}

	public static void GotoModScreen()
	{
		credits.active = false;
	}

	public static void GotoCredits()
	{
		credits.active = true;
		EventSystem.current.SetSelectedGameObject(returnFromCreditsBtn.gameObject, null);
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(Scene_MainMenu), nameof(Scene_MainMenu.Update))]
	public static void MainMenuUpdate()
	{
		if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) CloseModScreen();
		if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame) CloseModScreen();
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(SettingsManager), nameof(SettingsManager.SetResolution))]
	public static void RescaleUI()
	{
		Utils.RescaleUI(introSignature);
		Utils.RescaleUI(mainModButton?.gameObject);
		Utils.RescaleUI(canvas?.gameObject);
	}
}

public class CreditBlock
{
	public enum SIZES { TITLE, ROLE, TEXT, SEPARATOR }
	public List<KeyValuePair<SIZES, string>> rows = [];
	public void Add(SIZES key, string value)
	{
		var element = new KeyValuePair<SIZES, string>(key, value);
		rows.Add(element);
	}
}