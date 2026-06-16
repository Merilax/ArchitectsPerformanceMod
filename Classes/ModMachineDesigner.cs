using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ArchPerformanceMod;

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

public class DioramaEnvPatch
{
	const int FONT_SIZE_NORMAL = 20;
	const int FONT_SIZE_SUB = 16;
	private static bool allowInteraction = false;
	private static bool once = false;
	private static Fog fog;
	private static HDAdditionalCameraData cameraData;
	private static Material groundMat;
	private static List<LightControl> lights = [];
	private static int currentLightPattern;

	// UI
	private static TMP_FontAsset font;
	private static Material fontMat;
	private static GameObject modNav;
	private static GameObject mainMenu;
	private static GameObject environmentMenu;
	private static GameObject lightingMenu;
	private static GameObject lightsUIContainer;
	private static Slider fogHue;
	private static Slider fogSat;
	private static Slider fogLum;
	private static Slider backHue;
	private static Slider backSat;
	private static Slider backLum;
	private static Slider gndHue;
	private static Slider gndSat;
	private static Slider gndLum;

	[HarmonyPostfix]
	[HarmonyPatch(typeof(GeoramaSystem), nameof(GeoramaSystem.Start))]
	public static void PreInit(GeoramaSystem __instance)
	{
		if (!once) BridgedSceneManager.OnSceneLoadComplete.AddListener((Action)(() => ApplyConfiguration(__instance)));

		once = true;
	}

	public static void ApplyConfiguration(GeoramaSystem __instance)
	{
		if (SceneManager.GetActiveScene().name != "Georama") return;

		HDRPReflectionHelper.ApplyPipelineSupportFlagBatch( // true = disabled
			ModSettings.confReflections.Value != ModSettings.StrengthEnum.def, // SSR
			ModSettings.confAmbientOcclusion.Value == ModSettings.ToggleEnum.off, // Ambient Occlusion
			ModSettings.confVolumetrics.Value == ModSettings.DioramaOnlyEnum.off, // Volumetrics // Only disabled if off
			true, // Vol Clouds
			true, // Subsurface Scattering
			true, // Decals (already disabled by default)
			ModSettings.confMachineParticles.Value != ModSettings.QuantityEnum.full, // Distortion
			ModSettings.confReflections.Value != ModSettings.StrengthEnum.def, // SSR Transparency
			ModSettings.confReflections.Value != ModSettings.StrengthEnum.def, // Screen Space Lens Flare
			ModSettings.confReflections.Value != ModSettings.StrengthEnum.def  // Data Driven Lens Flare
		);

		Scene scene = SceneManager.GetActiveScene();

		cameraData = scene.GetRootGameObjects().First(item => item.name == "Camera").GetComponent<HDAdditionalCameraData>();

		scene.GetRootGameObjects().First(item => item.name == "Env").transform.Find("Vol").Find("Enviroments").GetComponent<Volume>().profile.TryGet(out Fog fogComp);
		fog = fogComp;

		groundMat = scene.GetRootGameObjects().First(item => item.name == "Ground").transform.GetChild(0).GetComponent<MeshRenderer>().material;

		if (ModSettings.confVolumetrics.Value == ModSettings.DioramaOnlyEnum.off)
		{
			fog.active = false;
			cameraData.backgroundColorHDR = new Color(.55f, .45f, .45f, 1);
		}

		CreateUI();

		currentLightPattern = __instance.currentLightPattern;

		// GameObject handles = scene.GetRootGameObjects().First(item => item.name == "TransformHandle");

		allowInteraction = true;
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(GeoramaSystem), nameof(GeoramaSystem.ReturnToMainMenu))]
	public static void OnDestroy()
	{
		allowInteraction = false;
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(GeoramaSystem), nameof(GeoramaSystem.Update))]
	public static void OnUpdate()
	{
		if (Keyboard.current.hKey.wasPressedThisFrame && allowInteraction)
		{
			GameObject canvas = SceneManager.GetActiveScene().GetRootGameObjects().First(item => item.name == "Canvas");
			canvas.active = !canvas.active;
			ToggleTerrain(canvas.active);
			ToggleGround(canvas.active);
			ToggleFrame(canvas.active);
			ToggleCaptions(canvas.active);
		}
		if (Keyboard.current.tabKey.wasPressedThisFrame && allowInteraction)
		{
			modNav.active = !modNav.active;
		}
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(GeoramaSystem), nameof(GeoramaSystem.Button_ChangeLight))]
	public static void OnLightUpdate(GeoramaSystem __instance)
	{
		Plugin.LogDebug("OnLightUpdate In");
		if (currentLightPattern == __instance.currentLightPattern) return;
		currentLightPattern = __instance.currentLightPattern;

		GameObject lightContainer = SceneManager.GetActiveScene().GetRootGameObjects().First(e => e.name == "Env").transform.Find("Lights").gameObject;

		ClearLightList();
		ReloadUILights(lightContainer.transform, lightsUIContainer.transform, font, fontMat);
		Plugin.LogDebug("OnLightUpdate Out");
	}
	private static void CreateUI()
	{
		GameObject canvas = SceneManager.GetActiveScene().GetRootGameObjects().First(item => item.name == "Canvas");
		font = canvas.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().font;
		fontMat = canvas.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().fontSharedMaterial;

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
		image.color = new Color(.1f, .1f, .1f, .6f);

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

		GameObject environmentButton = UIUtils.CreateButton("Environment", mainMenu.transform, font, fontMat, GotoEnvironment, FONT_SIZE_NORMAL);
		GameObject lightsButton = UIUtils.CreateButton("Lighting", mainMenu.transform, font, fontMat, GotoLighting, FONT_SIZE_NORMAL);

		lightingMenu = CreateLightingUIBlock(nav.transform, font, fontMat);
		lightingMenu.active = false;
		environmentMenu = CreateEnviromentUIBlock(nav.transform, font, fontMat);
		environmentMenu.active = false;

		// After UI
		SetFogAlbedo(Color.white, true);
		SetBackColor(Color.white, true);
		SetGroundColor(Color.white, true);

		Utils.RescaleUI(nav);
		modNav = nav;
	}

	private static GameObject CreateEnviromentUIBlock(Transform parent, TMP_FontAsset font, Material fontMat)
	{
		GameObject scrollView = UIUtils.CreateScrollView(parent.transform, true, false);
		scrollView.transform.GetChild(0).GetComponent<VerticalLayoutGroup>().padding = new(20, 20, 80, 80);
		Transform scrollContent = scrollView.transform.GetChild(0);

		UIUtils.CreateButton("Return to settings", scrollContent.transform, font, fontMat, GotoMain, FONT_SIZE_NORMAL);

		UIUtils.CreateLabel("Fog Color", scrollContent, font, fontMat, FONT_SIZE_NORMAL);
		if (ModSettings.confVolumetrics.Value != ModSettings.DioramaOnlyEnum.off)
		{
			UIUtils.CreateLabel("Hue", scrollContent, font, fontMat, FONT_SIZE_SUB);
			fogHue = UIUtils.CreateSlider(0.01f, 360, scrollContent, OnFogColorChanged).GetComponent<Slider>();
			UIUtils.CreateLabel("Saturation", scrollContent, font, fontMat, FONT_SIZE_SUB);
			fogSat = UIUtils.CreateSlider(0.01f, 100, scrollContent, OnFogColorChanged).GetComponent<Slider>();
			UIUtils.CreateLabel("Value", scrollContent, font, fontMat, FONT_SIZE_SUB);
			fogLum = UIUtils.CreateSlider(0.01f, 100, scrollContent, OnFogColorChanged).GetComponent<Slider>();
			UIUtils.CreateButton("Reset", scrollContent, font, fontMat, () => SetFogAlbedo(Color.white, true), FONT_SIZE_NORMAL);
		}
		else
			UIUtils.CreateLabel("Volumetrics are disabled", scrollContent, font, fontMat, FONT_SIZE_SUB);

		UIUtils.CreateLabel("Background Color", scrollContent, font, fontMat, FONT_SIZE_NORMAL);
		UIUtils.CreateLabel("Hue", scrollContent, font, fontMat, FONT_SIZE_SUB);
		backHue = UIUtils.CreateSlider(0.01f, 360, scrollContent, OnBackColorChanged).GetComponent<Slider>();
		UIUtils.CreateLabel("Saturation", scrollContent, font, fontMat, FONT_SIZE_SUB);
		backSat = UIUtils.CreateSlider(0.01f, 100, scrollContent, OnBackColorChanged).GetComponent<Slider>();
		UIUtils.CreateLabel("Value", scrollContent, font, fontMat, FONT_SIZE_SUB);
		backLum = UIUtils.CreateSlider(0.01f, 100, scrollContent, OnBackColorChanged).GetComponent<Slider>();
		UIUtils.CreateButton("Reset", scrollContent, font, fontMat, () => SetBackColor(Color.white, true), FONT_SIZE_NORMAL);

		UIUtils.CreateLabel("Ground Color", scrollContent, font, fontMat, FONT_SIZE_NORMAL);
		UIUtils.CreateLabel("Hue", scrollContent, font, fontMat, FONT_SIZE_SUB);
		gndHue = UIUtils.CreateSlider(0, 360, scrollContent, OnGndColorChanged).GetComponent<Slider>();
		UIUtils.CreateLabel("Saturation", scrollContent, font, fontMat, FONT_SIZE_SUB);
		gndSat = UIUtils.CreateSlider(0, 100, scrollContent, OnGndColorChanged).GetComponent<Slider>();
		UIUtils.CreateLabel("Value", scrollContent, font, fontMat, FONT_SIZE_SUB);
		gndLum = UIUtils.CreateSlider(0, 100, scrollContent, OnGndColorChanged).GetComponent<Slider>();
		UIUtils.CreateButton("Reset", scrollContent, font, fontMat, () => SetGroundColor(Color.white, true), FONT_SIZE_NORMAL);

		UIUtils.CreateLabel("Toggles:", scrollContent, font, fontMat, FONT_SIZE_NORMAL);
		UIUtils.CreateButton("Ground", scrollContent, font, fontMat, ToggleGround, FONT_SIZE_NORMAL);
		UIUtils.CreateButton("Terrain", scrollContent, font, fontMat, ToggleTerrain, FONT_SIZE_NORMAL);
		UIUtils.CreateButton("Frame", scrollContent, font, fontMat, ToggleFrame, FONT_SIZE_NORMAL);
		UIUtils.CreateButton("Captions", scrollContent, font, fontMat, ToggleCaptions, FONT_SIZE_NORMAL);

		return scrollView;
	}
	private static GameObject CreateLightingUIBlock(Transform parent, TMP_FontAsset font, Material fontMat)
	{
		GameObject lightContainer = SceneManager.GetActiveScene().GetRootGameObjects().First(e => e.name == "Env").transform.Find("Lights").gameObject;

		GameObject lightingBlock = new("Lighting");
		lightingBlock.transform.SetParent(parent);
		RectTransform mainRect = lightingBlock.AddComponent<RectTransform>();
		mainRect.pivot = new(1, 1);
		mainRect.anchorMin = Vector2.zero;
		mainRect.anchorMax = Vector2.one;
		mainRect.anchoredPosition = new(0, 0);
		mainRect.sizeDelta = new(0, 0);
		VerticalLayoutGroup group = lightingBlock.AddComponent<VerticalLayoutGroup>();
		group.spacing = 10;
		group.padding = new(10, 10, 40, 40);
		group.childForceExpandHeight = false;

		UIUtils.CreateButton("Return to settings", lightingBlock.transform, font, fontMat, GotoMain, FONT_SIZE_NORMAL);
		// TODO: Add "Create new light" button.
		UIUtils.CreateLabel("Lights:", lightingBlock.transform, font, fontMat, FONT_SIZE_NORMAL);
		UIUtils.CreateButton("Add light", lightingBlock.transform, font, fontMat, () =>
		{
			GameObject light = SpawnLight(lightContainer.transform);
			LightControl lightControl = CreateLightControl(light.GetComponent<Light>());
			CreateLightContainer(lightsUIContainer.transform, lightControl, font, fontMat, false);
		}, FONT_SIZE_NORMAL);

		GameObject scrollView = UIUtils.CreateScrollView(lightingBlock.transform, true, false);
		scrollView.GetComponent<ScrollRect>().scrollSensitivity = 15;
		LayoutElement scrollLayout = scrollView.AddComponent<LayoutElement>();
		scrollLayout.flexibleWidth = 1;
		scrollLayout.flexibleHeight = 1;
		lightsUIContainer = scrollView.transform.GetChild(0).gameObject;

		// Populate the Lights list with whatever vanilla Light is active in the pattern.
		ClearLightList();
		ReloadUILights(lightContainer.transform, lightsUIContainer.transform, font, fontMat);

		return lightingBlock;
	}
	private static void ClearLightList()
	{
		foreach (LightControl light in lights)
			light.Destroy();

		lights.Clear();
	}
	private static void ReloadUILights(Transform lightContainer, Transform UIContainer, TMP_FontAsset font, Material fontMat)
	{
		Plugin.LogDebug("ReloadUILights In");
		for (int i = 0; i < lightContainer.transform.childCount; i++)
		{
			var pattern = lightContainer.transform.GetChild(i).gameObject;
			Plugin.LogInfo(pattern.active);
			if (pattern.active)
			{
				for (int j = 0; j < pattern.transform.childCount; j++)
				{
					var lightObj = pattern.transform.GetChild(j).gameObject;
					if (lightObj.active == false) continue;
					if (lightObj.GetComponent<Light>())
					{
						Plugin.LogDebug("ReloadUILights Loop In");
						LightControl lightControl = new(lightObj, true, null);
						lights.Add(lightControl);
						Plugin.LogDebug("ReloadUILights Light: " + lightControl);
						GameObject container = CreateLightContainer(UIContainer, lightControl, font, fontMat, true);
						lightControl.UIContainer = container;
						Plugin.LogDebug("ReloadUILights Loop Out");
					}
				}
			}
		}
		Plugin.LogDebug("ReloadUILights Out");
	}
	private static GameObject SpawnLight(Transform parent)
	{
		GameObject light = new("Light (Mod)");
		light.transform.SetParent(parent);
		light.AddComponent<Light>();
		light.AddComponent<HDAdditionalLightData>();
		return light;
	}
	private static LightControl CreateLightControl(Light light)
	{
		Plugin.LogDebug("CreateLightControl In");
		LightControl lightControl = new(light.gameObject, false, new(0, 10, 0), Quaternion.Euler(Vector3.down), LightType.Spot, Color.white, 20, 20, 30, LightShadows.None, true, false);
		lights.Add(lightControl);
		Plugin.LogDebug("CreateLightControl Out");
		return lightControl;
	}
	private static GameObject CreateLightContainer(Transform parent, LightControl light, TMP_FontAsset font, Material fontMat, bool isVanilla = false)
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
		layout.spacing = 10;
		layout.padding = new RectOffset(14, 14, 10, 10);

		// Create controls
		// Active
		Plugin.LogDebug(light);
		UIUtils.CreateButton("Toggle", container.transform, font, fontMat, () => light.SetActive(!light.Active), FONT_SIZE_NORMAL);

		// Color
		UIUtils.CreateLabel("Color:", container.transform, font, fontMat, FONT_SIZE_NORMAL);
		UIUtils.CreateLabel("Hue", container.transform, font, fontMat, FONT_SIZE_SUB);
		Slider lightHue = UIUtils.CreateSlider(0.01f, 360, container.transform, null).GetComponent<Slider>();
		UIUtils.CreateLabel("Saturation", container.transform, font, fontMat, FONT_SIZE_SUB);
		Slider lightSat = UIUtils.CreateSlider(0.01f, 100, container.transform, null).GetComponent<Slider>();
		UIUtils.CreateLabel("Value", container.transform, font, fontMat, FONT_SIZE_SUB);
		Slider lightVal = UIUtils.CreateSlider(0.01f, 100, container.transform, null).GetComponent<Slider>();
		if (isVanilla)
			UIUtils.CreateButton("Reset color", container.transform, font, fontMat, () => SetLightColor(light, Color.white, lightHue, lightSat, lightVal, true), FONT_SIZE_NORMAL);

		// Position and rotation
		GameObject positionLabel = UIUtils.CreateLabel("Position", container.transform, font, fontMat, FONT_SIZE_NORMAL);
		List<InputField> posArr = UIUtils.CreateVec3Input(container.transform);
		GameObject positionGroup = posArr[0].transform.parent.gameObject;

		GameObject rotationLabel = UIUtils.CreateLabel("Rotation", container.transform, font, fontMat, FONT_SIZE_NORMAL);
		List<InputField> rotArr = UIUtils.CreateVec2Input(container.transform);
		GameObject rotationGroup = rotArr[0].transform.parent.gameObject;

		// Light type
		UIUtils.CreateLabel("Light type", container.transform, font, fontMat, FONT_SIZE_NORMAL);
		Dropdown dropdown = UIUtils.CreateDropdown(["Spot", "Point", "Directional"], container.transform, font, fontMat, null, FONT_SIZE_NORMAL).GetComponent<Dropdown>();

		// Amplitude (spotAngle), range and intensity
		GameObject amplitudeLabel = UIUtils.CreateLabel("Amplitude", container.transform, font, fontMat, FONT_SIZE_SUB);
		Slider amplitude = UIUtils.CreateSlider(0, 180, container.transform, (value) => light.SetAmplitude(value)).GetComponent<Slider>();
		GameObject rangeLabel = UIUtils.CreateLabel("Range", container.transform, font, fontMat, FONT_SIZE_SUB);
		Slider range = UIUtils.CreateSlider(0, 100, container.transform, (value) => light.SetRange(value)).GetComponent<Slider>();
		UIUtils.CreateLabel("Intensity", container.transform, font, fontMat, FONT_SIZE_SUB);
		Slider intensity = UIUtils.CreateSlider(0, 100_000_000, container.transform, (value) => light.SetIntensity(value)).GetComponent<Slider>();

		// Shadows and volumetrics
		UIUtils.CreateButton("Toggle shadows", container.transform, font, fontMat, () => light.SetShadows(light.Shadows == LightShadows.None ? LightShadows.Hard : LightShadows.None), FONT_SIZE_NORMAL);
		if (ModSettings.confVolumetrics.Value != ModSettings.DioramaOnlyEnum.off)
			UIUtils.CreateButton("Toggle volumetrics", container.transform, font, fontMat, () => light.SetVolumetrics(!light.Volumetrics), FONT_SIZE_NORMAL);

		if (isVanilla)
			UIUtils.CreateButton("Reset parameters", container.transform, font, fontMat, () => ResetLightParams(light, dropdown, amplitude, range, intensity, posArr, rotArr), FONT_SIZE_NORMAL);
		if (!isVanilla)
			UIUtils.CreateButton("Delete", container.transform, font, fontMat, () => DestroyLight(light), FONT_SIZE_NORMAL);

		// UI events
		lightHue.onValueChanged.AddListener((Action<float>)((_) => OnLightColorChanged(light, lightHue, lightSat, lightVal)));
		lightSat.onValueChanged.AddListener((Action<float>)((_) => OnLightColorChanged(light, lightHue, lightSat, lightVal)));
		lightVal.onValueChanged.AddListener((Action<float>)((_) => OnLightColorChanged(light, lightHue, lightSat, lightVal)));
		foreach (var pos in posArr)
			pos.onValueChange.AddListener((Action<string>)((str) => SetLightPosition(light, posArr[0], posArr[1], posArr[2])));
		foreach (var rot in rotArr)
			rot.onValueChange.AddListener((Action<string>)((str) => SetLightRotation(light, rotArr[0], rotArr[1])));
		dropdown.onValueChanged.AddListener((Action<int>)((value) => OnLightTypeChanged(light, value, range, rangeLabel, amplitude, amplitudeLabel, positionGroup, positionLabel, rotationGroup, rotationLabel)));

		// Reset controls to default
		SetLightColor(light, Color.white, lightHue, lightSat, lightVal, true);
		ResetLightParams(light, dropdown, amplitude, range, intensity, posArr, rotArr);
		OnLightTypeChanged(light, Utils.LightTypeToInt(light.Type), range, rangeLabel, amplitude, amplitudeLabel, positionGroup, positionLabel, rotationGroup, rotationLabel);
		light.UIContainer = container;

		Plugin.LogDebug("CreateLightContainer Out");
		return container;
	}

	private static void GotoMain()
	{
		mainMenu.active = true;
		environmentMenu.active = false;
		lightingMenu.active = false;
	}
	private static void GotoEnvironment()
	{
		mainMenu.active = false;
		environmentMenu.active = true;
		lightingMenu.active = false;
	}
	private static void GotoLighting()
	{
		mainMenu.active = false;
		environmentMenu.active = false;
		lightingMenu.active = true;
	}

	private static void OnFogColorChanged(float _)
	{
		SetFogAlbedo(Color.HSVToRGB(fogHue.value / 360, fogSat.value / 100, fogLum.value / 100));
	}
	private static void OnBackColorChanged(float _)
	{
		SetBackColor(Color.HSVToRGB(backHue.value / 360, backSat.value / 100, backLum.value / 100));
	}
	private static void OnGndColorChanged(float _)
	{
		SetGroundColor(Color.HSVToRGB(gndHue.value / 360, gndSat.value / 100, gndLum.value / 100));
	}
	private static void OnLightColorChanged(LightControl light, Slider hueSlider, Slider satSlider, Slider valSlider)
	{
		Plugin.LogDebug("OnLightColorChanged In");
		var color = Color.HSVToRGB(hueSlider.value / 360, satSlider.value / 100, valSlider.value / 100);
		SetLightColor(light, color, hueSlider, satSlider, valSlider);
		Plugin.LogDebug("OnLightColorChanged Out");
	}
	private static void OnLightTypeChanged(LightControl light, int type, Slider range, GameObject rangeLabel, Slider amplitude, GameObject amplitudeLabel, GameObject positionGroup, GameObject positionLabel, GameObject rotationGroup, GameObject rotationLabel)
	{
		range.gameObject.active = rangeLabel.active = type == 0 || type == 1; // Not dir
		amplitude.gameObject.active = amplitudeLabel.active = type == 0; // Only spot
		positionGroup.active = positionLabel.active = type == 0 || type == 1; // Not dir
		rotationGroup.active = rotationLabel.active = type == 0 || type == 2; // Not point

		light.SetType(type);
	}

	public static void SetFogAlbedo(Color color, bool reset = false)
	{
		if (ModSettings.confVolumetrics.Value == ModSettings.DioramaOnlyEnum.off) return;

		if (reset)
			color = new Color(.255f, .255f, .255f, 1);

		Color.RGBToHSV(color, out float h, out float s, out float l);

		if (reset)
		{
			fogHue.SetValueWithoutNotify(h * 360);
			fogSat.SetValueWithoutNotify(s * 100);
			fogLum.SetValueWithoutNotify(l * 100);
		}

		fogHue.fillRect.GetComponent<Image>().color = Color.HSVToRGB(h, 1, 1);
		fogSat.fillRect.GetComponent<Image>().color = Color.HSVToRGB(h, s, 1);
		fogLum.fillRect.GetComponent<Image>().color = Color.HSVToRGB(0, 0, l);

		fog.albedo.value = color;
	}
	public static void SetBackColor(Color color, bool reset = false)
	{
		if (reset)
			color = new Color(1, 1, 1, 1);

		Color.RGBToHSV(color, out float h, out float s, out float l);

		if (reset)
		{
			backHue.SetValueWithoutNotify(h * 360);
			backSat.SetValueWithoutNotify(s * 100);
			backLum.SetValueWithoutNotify(l * 100);
		}

		backHue.fillRect.GetComponent<Image>().color = Color.HSVToRGB(h, 1, 1);
		backSat.fillRect.GetComponent<Image>().color = Color.HSVToRGB(h, s, 1);
		backLum.fillRect.GetComponent<Image>().color = Color.HSVToRGB(0, 0, l);

		cameraData.backgroundColorHDR = color;
	}
	public static void SetGroundColor(Color color, bool reset = false)
	{
		if (reset)
			color = new Color(.392f, .392f, .392f, 1);

		Color.RGBToHSV(color, out float h, out float s, out float l);
		if (reset)
		{
			gndHue.SetValueWithoutNotify(h * 360);
			gndSat.SetValueWithoutNotify(s * 100);
			gndLum.SetValueWithoutNotify(l * 100);
		}

		gndHue.fillRect.GetComponent<Image>().color = Color.HSVToRGB(h, 1, 1);
		gndSat.fillRect.GetComponent<Image>().color = Color.HSVToRGB(h, s, 1);
		gndLum.fillRect.GetComponent<Image>().color = Color.HSVToRGB(0, 0, l);

		groundMat.color = color;
	}
	public static void SetLightColor(LightControl light, Color color, Slider hueSlider, Slider satSlider, Slider valSlider, bool reset = false)
	{
		Plugin.LogDebug("SetLightColor In");
		if (reset)
			color = light.defaults["color"];

		Color.RGBToHSV(color, out float h, out float s, out float l);
		if (reset)
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
	public static void ResetLightParams(LightControl light, Dropdown dropdown, Slider amplitude, Slider range, Slider intensity, List<InputField> posArr, List<InputField> rotArr)
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
		for (int i = 0; i < posArr.Count; i++)
			posArr[i].SetTextWithoutNotify(light.Position[i].ToString());
		for (int i = 0; i < rotArr.Count; i++)
			rotArr[i].SetTextWithoutNotify(light.Rotation[i].ToString());
	}
	public static void SetLightPosition(LightControl light, InputField x, InputField y, InputField z, bool reset = false)
	{
		Vector3 vec;
		if (reset)
		{
			vec = light.defaults["position"];
			x.SetTextWithoutNotify(((int)vec.x).ToString());
			y.SetTextWithoutNotify(((int)vec.y).ToString());
			z.SetTextWithoutNotify(((int)vec.z).ToString());
			light.SetPosition(vec);
			return;
		}

		int xPos = 0;
		int yPos = 0;
		int zPos = 0;
		try
		{
			xPos = int.Parse(x.text);
		}
		catch (System.Exception) { }
		try
		{
			yPos = int.Parse(y.text);
		}
		catch (System.Exception) { }
		try
		{
			zPos = int.Parse(z.text);
		}
		catch (System.Exception) { }

		vec = new(xPos, yPos, zPos);
		light.SetPosition(vec);
	}
	public static void SetLightRotation(LightControl light, InputField x, InputField y, bool reset = false)
	{
		Quaternion quad;
		if (reset)
		{
			quad = light.defaults["rotation"];
			// x.SetTextWithoutNotify((quad * Vector3.forward).x.ToString());
			// y.SetTextWithoutNotify((quad * Vector3.forward).y.ToString());
			x.SetTextWithoutNotify(((int)quad.eulerAngles.x).ToString());
			y.SetTextWithoutNotify(((int)quad.eulerAngles.y).ToString());
			light.SetRotation(quad);
			return;
		}

		int xRot = 0;
		int yRot = 0;
		try
		{
			xRot = int.Parse(x.text);
		}
		catch (System.Exception) { }
		try
		{
			yRot = int.Parse(y.text);
		}
		catch (System.Exception) { }

		quad = Quaternion.Euler(xRot, yRot, 0);
		light.SetRotation(quad);
	}

	public static void ToggleTerrain()
	{
		var terrains = SceneManager.GetActiveScene().GetRootGameObjects().First(e => e.name == "Ground").transform.Find("Terrains").gameObject;
		terrains.active = !terrains.active;
	}
	public static void ToggleGround()
	{
		var ground = SceneManager.GetActiveScene().GetRootGameObjects().First(e => e.name == "Ground").gameObject;
		ground.active = !ground.active;
	}
	public static void ToggleFrame()
	{
		var frame = SceneManager.GetActiveScene().GetRootGameObjects().First(e => e.name == "Frame").gameObject;
		frame.active = !frame.active;
	}
	public static void ToggleCaptions()
	{
		var captions = SceneManager.GetActiveScene().GetRootGameObjects().First(e => e.name == "Caption").gameObject;
		captions.active = !captions.active;
	}
	public static void ToggleTerrain(bool toSet)
	{
		var terrains = SceneManager.GetActiveScene().GetRootGameObjects().First(e => e.name == "Ground").transform.Find("Terrains").gameObject.active = toSet;
	}
	public static void ToggleGround(bool toSet)
	{
		var ground = SceneManager.GetActiveScene().GetRootGameObjects().First(e => e.name == "Ground").gameObject.active = toSet;
	}
	public static void ToggleFrame(bool toSet)
	{
		var frame = SceneManager.GetActiveScene().GetRootGameObjects().First(e => e.name == "Frame").gameObject.active = toSet;
	}
	public static void ToggleCaptions(bool toSet)
	{
		var captions = SceneManager.GetActiveScene().GetRootGameObjects().First(e => e.name == "Caption").gameObject.active = toSet;
	}

	private static void DestroyLight(LightControl light)
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

		lightObject.transform.position = position;
		lightObject.transform.rotation = rotation;
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