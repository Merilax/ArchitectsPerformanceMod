using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ArchPerformanceMod;

public class UICommons
{
	public const int FONT_SIZE_NORMAL = 20;
	public const int FONT_SIZE_SUB = 16;
}

public class GarageCameraPatch
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(Scene_MainMenu), nameof(Scene_MainMenu.Designer_Design))]
	[HarmonyPatch(typeof(Scene_MainMenu), nameof(Scene_MainMenu.Diorama_OpenMachineDesign))]
	public static void IncreaseCameraConstraints(Scene_MainMenu __instance)
	{
		// VC_Menu/Menu_FreeLookPivot
		var camera = __instance.MachineDesigner.freeCameraController;
		camera.maxDistance = 12;
		camera.maxHeight = 4.5f;
		camera.maxSquare = 3.5f;
	}
}

public class EnvironmentUIPatch()
{
	private static EnvironmentUI designerUI; // Remains forever (unless entering Diorama).
	private static EnvironmentUI dioramaUI; // Destroyed on exit.

	[HarmonyPostfix]
	[HarmonyPatch(typeof(Scene_MainMenu), nameof(Scene_MainMenu.Diorama_OpenMachineDesign))]
	[HarmonyPatch(typeof(Scene_MainMenu), nameof(Scene_MainMenu.Designer_Design))]
	public static void PrepareDesignerUI()
	{
		designerUI = new(false);
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(GeoramaSystem), nameof(GeoramaSystem.Start))]
	public static void PrepareDioramaUI(GeoramaSystem __instance) // Diorama
	{
		BridgedSceneManager.OnSceneLoadComplete.AddListener((Action)(() =>
		{
			dioramaUI = new(true)
			{
				currentLightPattern = __instance.currentLightPattern
			};
		}));
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(GeoramaSystem), nameof(GeoramaSystem.ReturnToMainMenu))]
	public static void OnCloseDiorama() // Diorama
	{
		dioramaUI?.allowInteraction = false;
		dioramaUI = null;
		Plugin.SaveConfig();
	}
	[HarmonyPrefix]
	[HarmonyPatch(typeof(Scene_MainMenu), nameof(Scene_MainMenu.Designer_CloseDesigner))]
	public static void OnCloseDesigner()
	{
		designerUI?.allowInteraction = false;
		designerUI = null;
		Plugin.SaveConfig();
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(GeoramaSystem), nameof(GeoramaSystem.Update))]
	public static void OnUpdate() // Diorama
	{
		if (dioramaUI == null) return;
		if (Keyboard.current.hKey.wasPressedThisFrame && dioramaUI.allowInteraction)
		{
			dioramaUI.HideAll();
		}
		if (Keyboard.current.tabKey.wasPressedThisFrame && dioramaUI.allowInteraction)
		{
			dioramaUI.modNav.active = !dioramaUI.modNav.active;
		}
	}
	[HarmonyPostfix]
	[HarmonyPatch(typeof(Scene_MainMenu), nameof(Scene_MainMenu.Update))]
	public static void OnUpdate(Scene_MainMenu __instance) // TODO Check when we are in design mode
	{
		// if (__instance.) 
		if (designerUI == null) return;
		if (Keyboard.current.lKey.wasPressedThisFrame && designerUI.allowInteraction)
		{
			designerUI.modNav.active = !designerUI.modNav.active;
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(GeoramaSystem), nameof(GeoramaSystem.Button_ChangeLight))]
	public static void OnLightUpdate(GeoramaSystem __instance) // Diorama
	{
		Plugin.LogDebug("OnLightUpdate In");
		if (dioramaUI == null) return;
		if (dioramaUI.currentLightPattern == __instance.currentLightPattern) return;
		dioramaUI.currentLightPattern = __instance.currentLightPattern;

		GameObject lightContainer = SceneManager.GetActiveScene().GetRootGameObjects().First(e => e.name == "Env").transform.Find("Lights").gameObject;

		dioramaUI.lightSystem.ClearLightList();
		dioramaUI.ReloadUILights(lightContainer.transform, dioramaUI.lightsUIContainer.transform);
		Plugin.LogDebug("OnLightUpdate Out");
	}
}

public class EnvironmentUI
{
	public bool isDiorama;
	public bool allowInteraction = false;
	private Fog fog;
	private HDAdditionalCameraData cameraData;
	private Material groundMat;
	public int currentLightPattern;

	// UI
	private TMP_FontAsset font;
	private Material fontMat;
	public GameObject modNav;
	private GameObject mainMenu;
	private GameObject environmentMenu;
	private GameObject lightingMenu;
	public GameObject lightsUIContainer;
	public LightSystem lightSystem;
	private Slider fogHue;
	private Slider fogSat;
	private Slider fogLum;
	private Slider backHue;
	private Slider backSat;
	private Slider backLum;
	private Slider gndHue;
	private Slider gndSat;
	private Slider gndLum;

	// GameObjects
	private GameObject groundObj;
	private GameObject terrainObj;
	private GameObject frameObj;
	private GameObject captionObj;

	public EnvironmentUI(bool isDioramaSet)
	{
		isDiorama = isDioramaSet;
		ApplyConfiguration();
	}

	public void ApplyConfiguration()
	{
		if (isDiorama)
		{
			Scene scene = SceneManager.GetSceneByName("Georama");
			var root = scene.GetRootGameObjects();
			cameraData = root.First(item => item.name == "Camera").GetComponent<HDAdditionalCameraData>();
			root.First(item => item.name == "Env").transform.Find("Vol").Find("Enviroments").GetComponent<Volume>().profile.TryGet(out Fog fogComp);
			fog = fogComp;
			groundMat = root.First(item => item.name == "Ground").transform.GetChild(0).GetComponent<MeshRenderer>().material;

			groundObj = root.First(e => e.name == "Ground").gameObject;
			terrainObj = root.First(e => e.name == "Ground").transform.Find("Terrains").gameObject;
			frameObj = root.First(e => e.name == "Frame").gameObject;
			captionObj = root.First(e => e.name == "Caption").gameObject;

			fog.active = Plugin.customConfig.dioramaFog;
			groundObj.active = Plugin.customConfig.dioramaGround;
			terrainObj.active = Plugin.customConfig.dioramaTerrain;
			frameObj.active = Plugin.customConfig.dioramaFrame;
			captionObj.active = Plugin.customConfig.dioramaCaptions;
		}
		else
		{
			Scene scene = SceneManager.GetSceneByName("MainMenu");
			var root = scene.GetRootGameObjects();
			cameraData = root.First(item => item.name == "Main Camera").GetComponent<HDAdditionalCameraData>();
			root.First(item => item.name == "World_DesignOnly").transform.Find("Volume").GetComponent<Volume>().profile.TryGet(out Fog fogComp);
			fog = fogComp;
			groundMat = root.First(item => item.name == "World_DesignOnly").transform.Find("Stage_ForDesign").GetComponent<MeshRenderer>().materials[1]; // 0 - light, 1 - base

			fog.active = Plugin.customConfig.designerFog;
		}

		lightSystem = new LightSystem();

		CreateUI();

		allowInteraction = true;
	}

	private void CreateUI()
	{
		GameObject canvas;
		if (isDiorama)
		{
			canvas = SceneManager.GetSceneByName("Georama").GetRootGameObjects().First(item => item.name == "Canvas");
			font = canvas.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().font;
			fontMat = canvas.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().fontSharedMaterial;
		}
		else
		{
			canvas = SceneManager.GetSceneByName("MainMenu").GetRootGameObjects().First(item => item.name == "Canvas_MainMenu").transform.Find("Design_All").gameObject;
			font = canvas.transform.GetChild(9).GetChild(0).GetComponent<TextMeshProUGUI>().font;
			fontMat = canvas.transform.GetChild(9).GetChild(0).GetComponent<TextMeshProUGUI>().fontSharedMaterial;
		}

		// Main nav block
		GameObject nav = new("Mod Nav");
		nav.transform.SetParent(canvas.transform);
		RectTransform rectNav = nav.AddComponent<RectTransform>();
		rectNav.pivot = new Vector2(1, 1);
		rectNav.anchorMin = new Vector2(1, 0);
		rectNav.anchorMax = new Vector2(1, 1);
		rectNav.sizeDelta = new Vector2(380, 0);
		rectNav.anchoredPosition = Vector2.zero;
		Image image = nav.AddComponent<Image>();
		image.color = new Color(.1f, .1f, .1f, .8f);
		if (!isDiorama) image.color = new Color(.1f, .1f, .1f, .95f);

		mainMenu = new("Main");
		mainMenu.transform.SetParent(nav.transform);
		RectTransform rectMain = mainMenu.AddComponent<RectTransform>();
		rectMain.pivot = new Vector2(1, 1);
		rectMain.anchorMin = new Vector2(0, 0);
		rectMain.anchorMax = new Vector2(1, 1);
		rectMain.sizeDelta = new Vector2(0, 0);
		rectMain.anchoredPosition = Vector2.zero;
		VerticalLayoutGroup layout = mainMenu.AddComponent<VerticalLayoutGroup>();
		layout.childForceExpandHeight = false;
		layout.spacing = 30;
		layout.padding = new RectOffset(20, 20, 100, 100);

		GameObject environmentButton = UIUtils.CreateButton("Environment", mainMenu.transform, font, fontMat, GotoEnvironment, UICommons.FONT_SIZE_NORMAL);
		GameObject lightsButton = UIUtils.CreateButton("Lighting", mainMenu.transform, font, fontMat, GotoLighting, UICommons.FONT_SIZE_NORMAL);

		environmentMenu = CreateEnviromentUIBlock(nav.transform);
		environmentMenu.active = false;
		lightingMenu = CreateLightingUIBlock(nav.transform);
		lightingMenu.active = false;

		Utils.RescaleUI(nav);
		modNav = nav;
		nav.active = false;
	}
	private GameObject CreateEnviromentUIBlock(Transform parent)
	{
		GameObject scrollView = UIUtils.CreateScrollView(parent.transform, true, false);
		scrollView.transform.GetChild(0).GetComponent<VerticalLayoutGroup>().padding = new(20, 20, 80, 80);
		Transform scrollContent = scrollView.transform.GetChild(0);

		UIUtils.CreateButton("Return to settings", scrollContent.transform, font, fontMat, GotoMain, UICommons.FONT_SIZE_NORMAL);

		if (isDiorama)
		{
			UIUtils.CreateLabel("Fog Color", scrollContent, font, fontMat, UICommons.FONT_SIZE_NORMAL);
			if (ModSettings.confVolumetrics.Value != ModSettings.DioramaOnlyEnum.off)
			{
				UIUtils.CreateLabel("Hue", scrollContent, font, fontMat, UICommons.FONT_SIZE_SUB);
				fogHue = UIUtils.CreateSlider(0.01f, 360, scrollContent, OnFogColorChanged).GetComponent<Slider>();
				UIUtils.CreateLabel("Saturation", scrollContent, font, fontMat, UICommons.FONT_SIZE_SUB);
				fogSat = UIUtils.CreateSlider(0.01f, 100, scrollContent, OnFogColorChanged).GetComponent<Slider>();
				UIUtils.CreateLabel("Value", scrollContent, font, fontMat, UICommons.FONT_SIZE_SUB);
				fogLum = UIUtils.CreateSlider(0.01f, 100, scrollContent, OnFogColorChanged).GetComponent<Slider>();
				UIUtils.CreateButton("Reset", scrollContent, font, fontMat, () => SetFogAlbedo(Color.white, true), UICommons.FONT_SIZE_NORMAL);
			}
			else
				UIUtils.CreateLabel("Volumetrics are disabled", scrollContent, font, fontMat, UICommons.FONT_SIZE_SUB);

			UIUtils.CreateLabel("Background Color", scrollContent, font, fontMat, UICommons.FONT_SIZE_NORMAL);
			UIUtils.CreateLabel("Hue", scrollContent, font, fontMat, UICommons.FONT_SIZE_SUB);
			backHue = UIUtils.CreateSlider(0.01f, 360, scrollContent, OnBackColorChanged).GetComponent<Slider>();
			UIUtils.CreateLabel("Saturation", scrollContent, font, fontMat, UICommons.FONT_SIZE_SUB);
			backSat = UIUtils.CreateSlider(0.01f, 100, scrollContent, OnBackColorChanged).GetComponent<Slider>();
			UIUtils.CreateLabel("Value", scrollContent, font, fontMat, UICommons.FONT_SIZE_SUB);
			backLum = UIUtils.CreateSlider(0.01f, 100, scrollContent, OnBackColorChanged).GetComponent<Slider>();
			UIUtils.CreateButton("Reset", scrollContent, font, fontMat, () => SetBackColor(Color.white, true), UICommons.FONT_SIZE_NORMAL);
		}

		UIUtils.CreateLabel("Ground Color", scrollContent, font, fontMat, UICommons.FONT_SIZE_NORMAL);
		UIUtils.CreateLabel("Hue", scrollContent, font, fontMat, UICommons.FONT_SIZE_SUB);
		gndHue = UIUtils.CreateSlider(0, 360, scrollContent, OnGndColorChanged).GetComponent<Slider>();
		UIUtils.CreateLabel("Saturation", scrollContent, font, fontMat, UICommons.FONT_SIZE_SUB);
		gndSat = UIUtils.CreateSlider(0, 100, scrollContent, OnGndColorChanged).GetComponent<Slider>();
		UIUtils.CreateLabel("Value", scrollContent, font, fontMat, UICommons.FONT_SIZE_SUB);
		gndLum = UIUtils.CreateSlider(0, 100, scrollContent, OnGndColorChanged).GetComponent<Slider>();
		UIUtils.CreateButton("Reset", scrollContent, font, fontMat, () => SetGroundColor(Color.white, true), UICommons.FONT_SIZE_NORMAL);

		UIUtils.CreateLabel("Toggles:", scrollContent, font, fontMat, UICommons.FONT_SIZE_NORMAL);
		UIUtils.CreateButton("Fog", scrollContent, font, fontMat, ToggleFog, UICommons.FONT_SIZE_NORMAL);
		if (isDiorama)
		{
			UIUtils.CreateButton("Ground", scrollContent, font, fontMat, ToggleGround, UICommons.FONT_SIZE_NORMAL);
			UIUtils.CreateButton("Terrain", scrollContent, font, fontMat, ToggleTerrain, UICommons.FONT_SIZE_NORMAL);
			UIUtils.CreateButton("Frame", scrollContent, font, fontMat, ToggleFrame, UICommons.FONT_SIZE_NORMAL);
			UIUtils.CreateButton("Captions", scrollContent, font, fontMat, ToggleCaptions, UICommons.FONT_SIZE_NORMAL);
		}

		if (isDiorama)
		{
			if (ModSettings.confVolumetrics.Value != ModSettings.DioramaOnlyEnum.off)
				SetFogAlbedo(Plugin.customConfig.dioramaFogColor, false, true);
			SetBackColor(Plugin.customConfig.dioramaBackgroundColor, false, true);
			SetGroundColor(Plugin.customConfig.dioramaGroundColor, false, true);
		}
		else
		{
			SetGroundColor(Plugin.customConfig.designerGroundColor, false, true);
		}

		Utils.RescaleUI(scrollView);
		return scrollView;
	}
	private GameObject CreateLightingUIBlock(Transform parent)
	{
		GameObject lightContainer;
		if (isDiorama)
		{
			lightContainer = SceneManager.GetSceneByName("Georama").GetRootGameObjects().First(e => e.name == "Env").transform.Find("Lights").gameObject;
		}
		else
		{
			lightContainer = SceneManager.GetSceneByName("MainMenu").GetRootGameObjects().First(e => e.name == "World_DesignOnly").transform.Find("Lights").gameObject;
		}

		GameObject lightingBlock = new("Lighting");
		lightingBlock.transform.SetParent(parent);
		RectTransform mainRect = lightingBlock.AddComponent<RectTransform>();
		mainRect.pivot = new(1, 1);
		mainRect.anchorMin = Vector2.zero;
		mainRect.anchorMax = Vector2.one;
		mainRect.anchoredPosition = new(0, 0);
		mainRect.sizeDelta = new(0, 0);
		VerticalLayoutGroup group = lightingBlock.AddComponent<VerticalLayoutGroup>();
		group.spacing = 30;
		group.padding = new(10, 10, 20, 20);
		group.childForceExpandHeight = false;

		UIUtils.CreateButton("Return to settings", lightingBlock.transform, font, fontMat, GotoMain, UICommons.FONT_SIZE_NORMAL);
		UIUtils.CreateLabel("Lights:", lightingBlock.transform, font, fontMat, UICommons.FONT_SIZE_NORMAL);
		UIUtils.CreateButton("Add light", lightingBlock.transform, font, fontMat, () =>
		{
			GameObject light = LightSystem.SpawnLight(lightContainer.transform);
			LightControl lightControl = lightSystem.CreateLightControl(light.GetComponent<Light>());
			if (isDiorama) lightControl.SetPosition(new(0, 10, 0));
			else lightControl.SetPosition(new(0, 10, 1000));
			lightControl.SetRotation(Quaternion.EulerAngles(new(90, 0, 0)));
			lightControl.SetDefaults();
			CreateLightContainer(lightsUIContainer.transform, lightControl, false);
		}, UICommons.FONT_SIZE_NORMAL);

		GameObject scrollView = UIUtils.CreateScrollView(lightingBlock.transform, true, false);
		scrollView.GetComponent<ScrollRect>().scrollSensitivity = 15;
		LayoutElement scrollLayout = scrollView.AddComponent<LayoutElement>();
		scrollLayout.flexibleWidth = 1;
		scrollLayout.flexibleHeight = 1;
		lightsUIContainer = scrollView.transform.GetChild(0).gameObject;

		// Populate the Lights list with whatever vanilla Light is active in the pattern.
		lightSystem.ClearLightList();
		ReloadUILights(lightContainer.transform, lightsUIContainer.transform);

		Utils.RescaleUI(lightingBlock);
		return lightingBlock;
	}
	private GameObject CreateLightContainer(Transform parent, LightControl light, bool isVanilla = false)
	{
		Plugin.LogDebug("CreateLightContainer In");
		GameObject container = new("Light");
		container.transform.SetParent(parent);
		RectTransform rectMain = container.AddComponent<RectTransform>();
		rectMain.pivot = new Vector2(0, 1);
		rectMain.anchorMin = new Vector2(0, 0);
		rectMain.anchorMax = new Vector2(0, 1);
		rectMain.anchoredPosition = Vector2.zero;
		Image image = container.AddComponent<Image>();
		image.color = new Color(.8f, .9f, 1, .15f);
		VerticalLayoutGroup layout = container.AddComponent<VerticalLayoutGroup>();
		layout.childControlHeight = true;
		layout.childForceExpandHeight = false;
		layout.spacing = 6;
		layout.padding = new RectOffset(10, 10, 10, 10);

		// Create controls
		// Active
		Plugin.LogDebug(light);
		UIUtils.CreateButton("Toggle", container.transform, font, fontMat, () => light.SetActive(!light.Active), UICommons.FONT_SIZE_NORMAL);

		// Color
		UIUtils.CreateLabel("Color:", container.transform, font, fontMat, UICommons.FONT_SIZE_NORMAL);
		UIUtils.CreateLabel("Hue", container.transform, font, fontMat, UICommons.FONT_SIZE_SUB);
		Slider lightHue = UIUtils.CreateSlider(0.01f, 360, container.transform, null).GetComponent<Slider>();
		UIUtils.CreateLabel("Saturation", container.transform, font, fontMat, UICommons.FONT_SIZE_SUB);
		Slider lightSat = UIUtils.CreateSlider(0.01f, 100, container.transform, null).GetComponent<Slider>();
		UIUtils.CreateLabel("Value", container.transform, font, fontMat, UICommons.FONT_SIZE_SUB);
		Slider lightVal = UIUtils.CreateSlider(0.01f, 100, container.transform, null).GetComponent<Slider>();
		if (isVanilla)
			UIUtils.CreateButton("Reset color", container.transform, font, fontMat, () => SetLightColor(light, Color.white, lightHue, lightSat, lightVal, true), UICommons.FONT_SIZE_NORMAL);

		// Position
		GameObject positionLabel = UIUtils.CreateLabel("Position", container.transform, font, fontMat, UICommons.FONT_SIZE_NORMAL);
		List<InputField> posArr = UIUtils.CreateVec3Input(container.transform, InputField.ContentType.DecimalNumber, 6);
		GameObject positionGroup = posArr[0].transform.parent.gameObject;

		// Rotation
		GameObject rotationLabel = UIUtils.CreateLabel("Rotation", container.transform, font, fontMat, UICommons.FONT_SIZE_NORMAL);
		GameObject rotationGroup = new("Rotation Group");
		rotationGroup.transform.SetParent(container.transform);
		RectTransform rect = rotationGroup.AddComponent<RectTransform>();
		rect.anchorMin = new(0, 0);
		rect.anchorMax = new(1, 0);
		LayoutElement rotLayout = rotationGroup.AddComponent<LayoutElement>();
		rotLayout.flexibleWidth = 1;
		VerticalLayoutGroup group = rotationGroup.AddComponent<VerticalLayoutGroup>();
		group.spacing = 10;
		group.padding = new(2, 2, 2, 2);
		Slider rotYaw = UIUtils.CreateSlider(-180, 180, rotationGroup.transform, null).GetComponent<Slider>();
		Slider rotPitch = UIUtils.CreateSlider(-180, 180, rotationGroup.transform, null).GetComponent<Slider>();

		// Light type
		UIUtils.CreateLabel("Light type", container.transform, font, fontMat, UICommons.FONT_SIZE_NORMAL);
		Dropdown dropdown = UIUtils.CreateDropdown(["Spot", "Point", "Directional"], container.transform, null, UICommons.FONT_SIZE_NORMAL).GetComponent<Dropdown>();

		// Amplitude (spotAngle), range and intensity
		GameObject amplitudeLabel = UIUtils.CreateLabel("Amplitude", container.transform, font, fontMat, UICommons.FONT_SIZE_SUB);
		Slider amplitude = UIUtils.CreateSlider(0, 180, container.transform, (value) => light.SetAmplitude(value)).GetComponent<Slider>();
		GameObject rangeLabel = UIUtils.CreateLabel("Range", container.transform, font, fontMat, UICommons.FONT_SIZE_SUB);
		Slider range = UIUtils.CreateSlider(0, 100, container.transform, (value) => light.SetRange(value)).GetComponent<Slider>();
		UIUtils.CreateLabel("Intensity", container.transform, font, fontMat, UICommons.FONT_SIZE_SUB);
		Slider intensity = UIUtils.CreateSlider(0, 100_000_000, container.transform, (value) => light.SetIntensity(value)).GetComponent<Slider>();

		// Shadows and volumetrics
		UIUtils.CreateButton("Toggle shadows", container.transform, font, fontMat, () => light.SetShadows(light.Shadows == LightShadows.None ? LightShadows.Hard : LightShadows.None), UICommons.FONT_SIZE_NORMAL);
		if (ModSettings.confVolumetrics.Value != ModSettings.DioramaOnlyEnum.off)
			UIUtils.CreateButton("Toggle volumetrics", container.transform, font, fontMat, () => light.SetVolumetrics(!light.Volumetrics), UICommons.FONT_SIZE_NORMAL);

		if (isVanilla)
			UIUtils.CreateButton("Reset parameters", container.transform, font, fontMat, () => ResetLightParams(light, dropdown, amplitude, range, intensity, posArr, rotYaw, rotPitch), UICommons.FONT_SIZE_NORMAL);
		if (!isVanilla)
			UIUtils.CreateButton("Delete", container.transform, font, fontMat, () => lightSystem?.DestroyLight(light), UICommons.FONT_SIZE_NORMAL);

		// UI events
		lightHue.onValueChanged.AddListener((Action<float>)((_) => OnLightColorChanged(light, lightHue, lightSat, lightVal)));
		lightSat.onValueChanged.AddListener((Action<float>)((_) => OnLightColorChanged(light, lightHue, lightSat, lightVal)));
		lightVal.onValueChanged.AddListener((Action<float>)((_) => OnLightColorChanged(light, lightHue, lightSat, lightVal)));
		foreach (var pos in posArr)
			pos.onValueChange.AddListener((Action<string>)((str) => SetLightPosition(light, posArr[0], posArr[1], posArr[2])));
		// foreach (var rot in rotArr)
		rotYaw.onValueChanged.AddListener((Action<float>)((str) => SetLightRotation(light, rotYaw, rotPitch)));
		rotPitch.onValueChanged.AddListener((Action<float>)((str) => SetLightRotation(light, rotYaw, rotPitch)));

		dropdown.onValueChanged.AddListener((Action<int>)((value) => OnLightTypeChanged(light, value, range, rangeLabel, amplitude, amplitudeLabel, positionGroup, positionLabel, rotationGroup, rotationLabel)));

		// Reset controls to default
		SetLightColor(light, Color.white, lightHue, lightSat, lightVal, true);
		ResetLightParams(light, dropdown, amplitude, range, intensity, posArr, rotYaw, rotPitch);
		OnLightTypeChanged(light, Utils.LightTypeToInt(light.Type), range, rangeLabel, amplitude, amplitudeLabel, positionGroup, positionLabel, rotationGroup, rotationLabel);
		light.UIContainer = container;

		Utils.RescaleUI(container);
		Plugin.LogDebug("CreateLightContainer Out");
		return container;
	}

	public void ReloadUILights(Transform lightContainer, Transform UIContainer) // Diorama (after the first call)
	{
		Plugin.LogDebug("ReloadUILights In");

		for (int i = 0; i < lightContainer.childCount; i++)
		{
			Plugin.LogDebug("ReloadUILights Loop In");
			if (isDiorama)
			{
				GameObject pattern = lightContainer.GetChild(i).gameObject;
				if (pattern.active)
				{
					for (int j = 0; j < pattern.transform.childCount; j++)
					{
						var lightObj = pattern.transform.GetChild(j).gameObject;
						if (lightObj.active == false) continue;
						if (lightObj.GetComponent<Light>())
						{
							LightControl lightControl = new(lightObj, true, null);
							lightSystem.lights.Add(lightControl);
							Plugin.LogDebug("ReloadUILights Light: " + lightControl);
							GameObject container = CreateLightContainer(UIContainer, lightControl, true);
							lightControl.UIContainer = container;
							Plugin.LogDebug("ReloadUILights Loop Out");
						}
					}
				}
			}
			else
			{
				if (i < 2) continue;
				var lightObj = lightContainer.transform.GetChild(i).gameObject;
				if (lightObj.GetComponent<Light>())
				{
					Plugin.LogDebug("ReloadUILights Loop In");
					LightControl lightControl = new(lightObj, true, null);
					lightSystem.lights.Add(lightControl);
					Plugin.LogDebug("ReloadUILights Light: " + lightControl);
					GameObject container = CreateLightContainer(UIContainer, lightControl, true);
					lightControl.UIContainer = container;
				}
			}
			Plugin.LogDebug("ReloadUILights Loop Out");
		}
		Plugin.LogDebug("ReloadUILights Out");
	}
	public void OnLightUpdate(int newPattern) // Diorama
	{
		Plugin.LogDebug("OnLightUpdate In");
		if (currentLightPattern == newPattern) return;
		currentLightPattern = newPattern;

		GameObject lightContainer = SceneManager.GetActiveScene().GetRootGameObjects().First(e => e.name == "Env").transform.Find("Lights").gameObject;

		lightSystem.ClearLightList();
		ReloadUILights(lightContainer.transform, lightsUIContainer.transform);
		Plugin.LogDebug("OnLightUpdate Out");
	}

	private void GotoMain()
	{
		mainMenu.active = true;
		environmentMenu.active = false;
		lightingMenu.active = false;
	}
	private void GotoEnvironment()
	{
		mainMenu.active = false;
		environmentMenu.active = true;
		lightingMenu.active = false;
	}
	private void GotoLighting()
	{
		mainMenu.active = false;
		environmentMenu.active = false;
		lightingMenu.active = true;
	}
	public void HideAll() // Diorama
	{
		GameObject canvas = SceneManager.GetActiveScene().GetRootGameObjects().First(item => item.name == "Canvas");
		canvas.active = !canvas.active;
		ToggleTerrain(canvas.active);
		ToggleGround(canvas.active);
		ToggleFrame(canvas.active);
		ToggleCaptions(canvas.active);
	}

	private void OnFogColorChanged(float _) // Diorama
	{
		Color color = Color.HSVToRGB(fogHue.value / 360, fogSat.value / 100, fogLum.value / 100);
		SetFogAlbedo(color);
	}
	private void OnBackColorChanged(float _) // Diorama
	{
		Color color = Color.HSVToRGB(backHue.value / 360, backSat.value / 100, backLum.value / 100);
		SetBackColor(color);
	}
	private void OnGndColorChanged(float _)
	{
		Color color = Color.HSVToRGB(gndHue.value / 360, gndSat.value / 100, gndLum.value / 100);
		SetGroundColor(color);
	}
	private void OnLightColorChanged(LightControl light, Slider hueSlider, Slider satSlider, Slider valSlider)
	{
		Plugin.LogDebug("OnLightColorChanged In");
		var color = Color.HSVToRGB(hueSlider.value / 360, satSlider.value / 100, valSlider.value / 100);
		SetLightColor(light, color, hueSlider, satSlider, valSlider);
		Plugin.LogDebug("OnLightColorChanged Out");
	}
	private void OnLightTypeChanged(LightControl light, int type, Slider range, GameObject rangeLabel, Slider amplitude, GameObject amplitudeLabel, GameObject positionGroup, GameObject positionLabel, GameObject rotationGroup, GameObject rotationLabel)
	{
		range.gameObject.active = rangeLabel.active = type == 0 || type == 1; // Not dir
		amplitude.gameObject.active = amplitudeLabel.active = type == 0; // Only spot
		positionGroup.active = positionLabel.active = type == 0 || type == 1; // Not dir
		rotationGroup.active = rotationLabel.active = type == 0 || type == 2; // Not point

		light.SetType(type);
	}

	public void SetFogAlbedo(Color color, bool reset = false, bool update = false) // Diorama
	{
		if (ModSettings.confVolumetrics.Value == ModSettings.DioramaOnlyEnum.off) return;
		if (ModSettings.confVolumetrics.Value == ModSettings.DioramaOnlyEnum.dioramaOnly && !isDiorama) return;

		if (reset)
			color = new Color(.255f, .255f, .255f, 1);

		Color.RGBToHSV(color, out float h, out float s, out float l);

		if (update || reset)
		{
			fogHue.SetValueWithoutNotify(h * 360);
			fogSat.SetValueWithoutNotify(s * 100);
			fogLum.SetValueWithoutNotify(l * 100);
		}

		fogHue.fillRect.GetComponent<Image>().color = Color.HSVToRGB(h, 1, 1);
		fogSat.fillRect.GetComponent<Image>().color = Color.HSVToRGB(h, s, 1);
		fogLum.fillRect.GetComponent<Image>().color = Color.HSVToRGB(0, 0, l);

		fog.albedo.value = color;
		Plugin.customConfig.dioramaFogColor = color;
	}
	public void SetBackColor(Color color, bool reset = false, bool update = false) // Diorama
	{
		if (reset)
			color = new Color(1, 1, 1, 1);

		Color.RGBToHSV(color, out float h, out float s, out float l);

		if (update || reset)
		{
			backHue.SetValueWithoutNotify(h * 360);
			backSat.SetValueWithoutNotify(s * 100);
			backLum.SetValueWithoutNotify(l * 100);
		}

		backHue.fillRect.GetComponent<Image>().color = Color.HSVToRGB(h, 1, 1);
		backSat.fillRect.GetComponent<Image>().color = Color.HSVToRGB(h, s, 1);
		backLum.fillRect.GetComponent<Image>().color = Color.HSVToRGB(0, 0, l);

		cameraData.backgroundColorHDR = color;
		Plugin.customConfig.dioramaBackgroundColor = color;
	}
	public void SetGroundColor(Color color, bool reset = false, bool update = false)
	{
		if (reset)
			color = new Color(.392f, .392f, .392f, 1);

		Color.RGBToHSV(color, out float h, out float s, out float l);
		if (update || reset)
		{
			gndHue.SetValueWithoutNotify(h * 360);
			gndSat.SetValueWithoutNotify(s * 100);
			gndLum.SetValueWithoutNotify(l * 100);
		}

		gndHue.fillRect.GetComponent<Image>().color = Color.HSVToRGB(h, 1, 1);
		gndSat.fillRect.GetComponent<Image>().color = Color.HSVToRGB(h, s, 1);
		gndLum.fillRect.GetComponent<Image>().color = Color.HSVToRGB(0, 0, l);

		groundMat.color = color;
		if (isDiorama)
		{
			Plugin.customConfig.dioramaGroundColor = color;
		}
		else
		{
			Plugin.customConfig.designerGroundColor = color;
		}
	}
	public void SetLightColor(LightControl light, Color color, Slider hueSlider, Slider satSlider, Slider valSlider, bool reset = false, bool update = false)
	{
		Plugin.LogDebug("SetLightColor In");
		if (reset)
			color = light.defaults["color"];

		Color.RGBToHSV(color, out float h, out float s, out float l);
		if (update || reset)
		{
			hueSlider.SetValueWithoutNotify(h * 360);
			satSlider.SetValueWithoutNotify(s * 100);
			valSlider.SetValueWithoutNotify(l * 100);
		}

		hueSlider.fillRect.GetComponent<Image>().color = Color.HSVToRGB(h, 1, 1);
		satSlider.fillRect.GetComponent<Image>().color = Color.HSVToRGB(h, s, 1);
		valSlider.fillRect.GetComponent<Image>().color = Color.HSVToRGB(0, 0, l);

		light.SetColor(color);
		Plugin.LogDebug("SetLightColor Out");
	}
	public void ResetLightParams(LightControl light, Dropdown dropdown, Slider amplitude, Slider range, Slider intensity, List<InputField> posArr, Slider rotYaw, Slider rotPitch)
	{
		int type = 0;
		switch (light.defaults["type"])
		{
			case LightType.Spot: type = 0; break;
			case LightType.Point: type = 1; break;
			case LightType.Directional: type = 2; break;
		}
		light.SetPosition(light.defaults["position"]);
		light.SetRotation(light.defaults["rotation"]);
		light.SetType(type);
		light.SetAmplitude(light.defaults["amplitude"]);
		light.SetRange(light.defaults["range"]);
		light.SetIntensity(light.defaults["intensity"]);
		light.SetVolumetrics(light.defaults["volumetrics"]);

		dropdown.value = type;
		amplitude.SetValueWithoutNotify(light.Amplitude);
		range.SetValueWithoutNotify(light.UIRange);
		intensity.SetValueWithoutNotify(light.UIIntensity);
		SetLightPosition(light, posArr[0], posArr[1], posArr[2], true);
		SetLightRotation(light, rotYaw, rotPitch, true);
	}
	public void SetLightPosition(LightControl light, InputField x, InputField y, InputField z, bool reset = false)
	{
		const float INPUT_MULTIPLIER = 1;
		int designer_offset_z = 0;
		if (!isDiorama) designer_offset_z = 1000;
		Vector3 vec;

		if (reset)
		{
			vec = light.defaults["position"];
			x.SetTextWithoutNotify((vec.x * INPUT_MULTIPLIER).ToString());
			y.SetTextWithoutNotify((vec.y * INPUT_MULTIPLIER).ToString());
			z.SetTextWithoutNotify(((vec.z - designer_offset_z) * INPUT_MULTIPLIER).ToString());
			light.SetPosition(vec);
			return;
		}

		float xPos = 0;
		float yPos = 10;
		float zPos = 0;
		try
		{
			xPos = float.Parse(x.text) / INPUT_MULTIPLIER;
		}
		catch (System.Exception) { }
		try
		{
			yPos = float.Parse(y.text) / INPUT_MULTIPLIER;
		}
		catch (System.Exception) { }
		try
		{
			zPos = float.Parse(z.text) / INPUT_MULTIPLIER;
		}
		catch (System.Exception) { }

		zPos += designer_offset_z;
		vec = new(xPos, yPos, zPos);

		light.SetPosition(vec);
	}
	public void SetLightRotation(LightControl light, Slider x, Slider y, bool reset = false)
	{
		Quaternion quad;
		if (reset)
		{
			quad = light.defaults["rotation"];
			x.SetValueWithoutNotify(Utils.NormalizeAngle(quad.eulerAngles.x - 90));
			y.SetValueWithoutNotify(Utils.NormalizeAngle(quad.eulerAngles.y));
			light.SetRotation(quad);
			return;
		}

		quad = Quaternion.Euler(x.value + 90, y.value, 0);
		light.SetRotation(quad);
	}

	// Diorama
	public void ToggleFog()
	{
		fog.active = !fog.active;
		if (isDiorama)
			Plugin.customConfig.dioramaFog = fog.active;
		else
			Plugin.customConfig.designerFog = fog.active;
	}
	public void ToggleTerrain()
	{
		terrainObj.active = !terrainObj.active;
		Plugin.customConfig.dioramaTerrain = terrainObj.active;
	}
	public void ToggleGround()
	{
		groundObj.active = !groundObj.active;
		Plugin.customConfig.dioramaGround = groundObj.active;
	}
	public void ToggleFrame()
	{
		frameObj.active = !frameObj.active;
		Plugin.customConfig.dioramaFrame = frameObj.active;
	}
	public void ToggleCaptions()
	{
		captionObj.active = !captionObj.active;
		Plugin.customConfig.dioramaCaptions = captionObj.active;
	}
	public void ToggleFog(bool toSet)
	{
		fog.active = toSet;
		if (isDiorama)
			Plugin.customConfig.dioramaFog = toSet;
		else
			Plugin.customConfig.designerFog = toSet;
	}
	public void ToggleTerrain(bool toSet)
	{
		terrainObj.active = toSet;
		Plugin.customConfig.dioramaTerrain = toSet;
	}
	public void ToggleGround(bool toSet)
	{
		groundObj.active = toSet;
		Plugin.customConfig.dioramaGround = toSet;
	}
	public void ToggleFrame(bool toSet)
	{
		frameObj.active = toSet;
		Plugin.customConfig.dioramaFrame = toSet;
	}
	public void ToggleCaptions(bool toSet)
	{
		captionObj.active = toSet;
		Plugin.customConfig.dioramaCaptions = toSet;
	}
}

public class LightSystem
{
	public List<LightControl> lights = [];

	public LightSystem() { }

	public static GameObject SpawnLight(Transform parent)
	{
		GameObject light = new("Light (Mod)");
		light.transform.SetParent(parent);
		light.AddComponent<Light>();
		light.AddComponent<HDAdditionalLightData>();
		return light;
	}
	public LightControl CreateLightControl(Light light)
	{
		Plugin.LogDebug("CreateLightControl In");
		LightControl lightControl = new(light.gameObject, false, new(0, 10, 0), Quaternion.Euler(Vector3.down), LightType.Spot, Color.white, 20, 20, 30, LightShadows.None, true, false);
		lights.Add(lightControl);
		Plugin.LogDebug("CreateLightControl Out");
		return lightControl;
	}
	public void ClearLightList()
	{
		foreach (LightControl light in lights)
			light.Destroy();

		lights.Clear();
	}
	public void DestroyLight(LightControl light)
	{
		lights.Remove(light);
		light.Destroy();
	}
}
public class LightControl
{
	private GameObject lightObject = null;
	public bool vanilla = false;
	public GameObject UIContainer = null;
	private Light Data;
	private HDAdditionalLightData HDData;
	public Dictionary<string, dynamic> defaults = new()
	{
		{"position", Vector3.zero},
		{"rotation", new Quaternion()},
		{"type", LightType.Spot},
		{"color", Color.white},
		{"amplitude", 20},
		{"range", 20},
		{"intensity", 25000},
		{"shadows", LightShadows.None},
		{"volumetrics", false},
	};

	public GameObject LightObject { get => lightObject; }
	public bool Active { get => lightObject ? lightObject.active : false; }
	public Vector3 Position { get => lightObject ? lightObject.transform.position : new(); }
	public Quaternion Rotation { get => lightObject ? lightObject.transform.rotation : new(); }
	public LightType Type { get => Data ? Data.type : LightType.Spot; }
	public Color Color { get => Data ? Data.color : Color.white; }
	public float Amplitude { get => Data ? Data.spotAngle : 0; }
	public float UIRange;
	public float UIIntensity;
	public float RealRange { get => Data ? Data.range : 0; }
	public float RealIntensity { get => Data ? Data.intensity : 0; }
	public LightShadows Shadows { get => Data ? Data.shadows : LightShadows.None; }
	public bool Volumetrics { get => HDData ? HDData.affectsVolumetric : false; }

	public LightControl(GameObject lightObject, bool vanilla, Vector3 position, Quaternion rotation, LightType type, Color color, float amplitude, float range, float intensity, LightShadows shadowType, bool hasHDData, bool volumetrics = false, GameObject UIContainer = null)
	{
		this.lightObject = lightObject;
		this.vanilla = vanilla;
		this.UIContainer = UIContainer;

		SetPosition(position);
		SetRotation(rotation);
		Data = lightObject.GetComponent<Light>();
		if (!Data)
			throw new Exception("The GameObject provided to this LightControl does not contain a Light component.");
		Data.type = type;
		Data.color = color;
		Data.spotAngle = amplitude;
		UIRange = range;
		SetRange(range);
		UIIntensity = intensity;
		SetIntensity(intensity);
		Data.shadows = shadowType;

		if (hasHDData)
		{
			HDData = lightObject.GetComponent<HDAdditionalLightData>();
			if (!HDData)
				throw new Exception("The GameObject provided to this LightControl does not contain a HDAdditionalLightData component, despite being marked as true during construction.");
			HDData.affectsVolumetric = volumetrics;
		}
		SetDefaults();
	}
	public LightControl(GameObject lightObject, bool vanilla = false, GameObject UIContainer = null)
	{
		this.lightObject = lightObject;
		this.UIContainer = UIContainer;
		this.vanilla = vanilla;

		Data = lightObject.GetComponent<Light>();
		if (!Data)
			throw new Exception("The GameObject provided to this LightControl does not contain a Light component.");

		HDData = lightObject.GetComponent<HDAdditionalLightData>();
		SetDefaults();
	}

	private bool ReacquireData()
	{
		if (!LightObject) return false;
		Data = LightObject.GetComponent<Light>();
		if (!Data) return false;
		return true;
	}
	private bool ReacquireHDData()
	{
		if (!LightObject) return false;
		HDData = LightObject.GetComponent<HDAdditionalLightData>();
		if (!HDData) return false;
		return true;
	}

	public bool SetActive(bool set)
	{
		if (!lightObject)
			return false;
		lightObject.active = set;
		return true;
	}
	public bool SetPosition(Vector3 vec)
	{
		if (!lightObject) return false;
		lightObject.transform.position = vec;
		return true;
	}
	public bool SetRotation(Quaternion quad)
	{
		if (!lightObject) return false;
		lightObject.transform.rotation = quad;
		return true;
	}
	public bool SetType(int type)
	{
		if (!Data)
			if (ReacquireData() == false) return false;
		switch (type)
		{
			case 1:
				if (Data.type == LightType.Point)
					return true;
				Data.type = LightType.Point;
				SetRange(UIRange);
				SetIntensity(UIIntensity);
				break;
			case 2:
				if (Data.type == LightType.Directional)
					return true;
				Data.type = LightType.Directional;
				SetRange(UIRange);
				SetIntensity(UIIntensity);
				break;
			case 0:
			default:
				if (Data.type == LightType.Spot)
					return true;
				Data.type = LightType.Spot;
				SetRange(UIRange);
				SetIntensity(UIIntensity);
				break;
		}

		return true;
	}
	public bool SetColor(Color set)
	{
		if (!Data)
			if (ReacquireData() == false) return false;
		Data.color = set;
		return true;
	}
	public bool SetAmplitude(float set)
	{
		if (!Data)
			if (ReacquireData() == false) return false;
		Data.spotAngle = set;
		return true;
	}
	public bool SetRange(float set)
	{
		if (!Data)
			if (ReacquireData() == false) return false;

		float realRange = 0;

		switch (Data.type)
		{
			case LightType.Spot:
				realRange = set / 2; break;
			case LightType.Point:
				realRange = set / 2; break;
			case LightType.Directional:
				realRange = set; break;
		}

		UIRange = set;
		Data.range = realRange;
		return true;
	}
	public bool SetIntensity(float set)
	{
		if (!Data)
			if (ReacquireData() == false) return false;

		float realIntensity = 0;
		switch (Data.type)
		{
			case LightType.Spot:
				realIntensity = set; break;
			case LightType.Point:
				realIntensity = set / 3; break;
			case LightType.Directional:
				realIntensity = set / 400; break;
		}

		UIIntensity = set;
		Data.intensity = realIntensity;
		return true;
	}
	public bool SetShadows(LightShadows set)
	{
		if (!Data)
			if (ReacquireData() == false) return false;
		Data.shadows = set;
		return true;
	}
	public bool SetVolumetrics(bool set)
	{
		if (!HDData)
			if (ReacquireHDData() == false) return false;
		HDData.affectsVolumetric = set;
		return true;
	}
	public void ReadDefaults()
	{
		lightObject.transform.position = defaults["position"];
		lightObject.transform.rotation = defaults["rotation"];
		Data.type = defaults["type"];
		Data.color = defaults["color"];
		Data.spotAngle = defaults["amplitude"];
		SetRange(defaults["range"]);
		SetIntensity(defaults["intensity"]);
		Data.shadows = defaults["shadows"];
		HDData?.affectsVolumetric = defaults["volumetrics"];
	}
	public void SetDefaults()
	{
		switch (Data.type)
		{
			case LightType.Spot:
				UIRange = Data.range * 2;
				UIIntensity = Data.intensity; break;
			case LightType.Point:
				UIRange = Data.range * 2;
				UIIntensity = Data.intensity * 3; break;
			case LightType.Directional:
				UIRange = Data.range;
				UIIntensity = Data.intensity * 400; break;
		}

		defaults["position"] = lightObject.transform.position;
		defaults["rotation"] = lightObject.transform.rotation;
		defaults["type"] = Data.type;
		defaults["color"] = Data.color;
		defaults["amplitude"] = Data.spotAngle;
		defaults["range"] = UIRange;
		defaults["intensity"] = UIIntensity;
		defaults["shadows"] = Data.shadows;
		defaults["volumetrics"] = HDData ? HDData.affectsVolumetric : false;
	}
	public void Destroy()
	{
		try
		{
			Plugin.LogDebug("Light.Destroy In");
			if (!vanilla) lightObject?.active = false;//lightObject?.Destroy();
			UIContainer?.name += " (Voided)";
			UIContainer?.active = false;
			Plugin.LogDebug("Light.Destroy Out");
		}
		catch (System.Exception ex)
		{
			Plugin.LogInfo("ERR: LightControl failed to destroy one of its properties.");
			Plugin.Log.LogError(ex);
		}
	}
}