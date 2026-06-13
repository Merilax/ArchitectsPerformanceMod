using System;
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
	private static bool allowInteraction = false;
	private static bool once = false;
	private static Fog fog;
	private static HDAdditionalCameraData cameraData;
	private static Material groundMat;

	// UI
	private static GameObject modNav;
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

	private static void CreateUI()
	{
		GameObject canvas = SceneManager.GetActiveScene().GetRootGameObjects().First(item => item.name == "Canvas");
		TMP_FontAsset font = canvas.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().font;
		Material fontMat = canvas.transform.GetChild(0).GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().fontSharedMaterial;

		// Nav block
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

		ScrollRect scrollView = CreateScrollView(nav.transform, true, false).GetComponent<ScrollRect>();
		Transform scrollContent = scrollView.transform.GetChild(0);

		const int FONT_SIZE_HEADER = 20;
		const int FONT_SUB_SIZE = 16;

		CreateLabel("Fog Color", scrollContent, font, fontMat, FONT_SIZE_HEADER);
		if (ModSettings.confVolumetrics.Value != ModSettings.DioramaOnlyEnum.off)
		{
			CreateLabel("Hue", scrollContent, font, fontMat, FONT_SUB_SIZE);
			fogHue = CreateSlider(360, scrollContent, OnFogColorChanged).GetComponent<Slider>();
			CreateLabel("Saturation", scrollContent, font, fontMat, FONT_SUB_SIZE);
			fogSat = CreateSlider(100, scrollContent, OnFogColorChanged).GetComponent<Slider>();
			CreateLabel("Value", scrollContent, font, fontMat, FONT_SUB_SIZE);
			fogLum = CreateSlider(100, scrollContent, OnFogColorChanged).GetComponent<Slider>();
			CreateButton("Reset", scrollContent, font, fontMat, () => SetFogAlbedo(Color.white, true), FONT_SIZE_HEADER);
		}
		else
			CreateLabel("Volumetrics are disabled", scrollContent, font, fontMat, FONT_SUB_SIZE);

		CreateLabel("Background Color", scrollContent, font, fontMat, FONT_SIZE_HEADER);
		CreateLabel("Hue", scrollContent, font, fontMat, FONT_SUB_SIZE);
		backHue = CreateSlider(360, scrollContent, OnBackColorChanged).GetComponent<Slider>();
		CreateLabel("Saturation", scrollContent, font, fontMat, FONT_SUB_SIZE);
		backSat = CreateSlider(100, scrollContent, OnBackColorChanged).GetComponent<Slider>();
		CreateLabel("Value", scrollContent, font, fontMat, FONT_SUB_SIZE);
		backLum = CreateSlider(100, scrollContent, OnBackColorChanged).GetComponent<Slider>();
		CreateButton("Reset", scrollContent, font, fontMat, () => SetBackColor(Color.white, true), FONT_SIZE_HEADER);

		CreateLabel("Ground Color", scrollContent, font, fontMat, FONT_SIZE_HEADER);
		CreateLabel("Hue", scrollContent, font, fontMat, FONT_SUB_SIZE);
		gndHue = CreateSlider(360, scrollContent, OnGndColorChanged).GetComponent<Slider>();
		CreateLabel("Saturation", scrollContent, font, fontMat, FONT_SUB_SIZE);
		gndSat = CreateSlider(100, scrollContent, OnGndColorChanged).GetComponent<Slider>();
		CreateLabel("Value", scrollContent, font, fontMat, FONT_SUB_SIZE);
		gndLum = CreateSlider(100, scrollContent, OnGndColorChanged).GetComponent<Slider>();
		CreateButton("Reset", scrollContent, font, fontMat, () => SetGroundColor(Color.white, true), FONT_SIZE_HEADER);

		CreateLabel("Toggles:", scrollContent, font, fontMat, FONT_SIZE_HEADER);
		CreateButton("Ground", scrollContent, font, fontMat, ToggleGround, FONT_SUB_SIZE);
		CreateButton("Terrain", scrollContent, font, fontMat, ToggleTerrain, FONT_SUB_SIZE);
		CreateButton("Frame", scrollContent, font, fontMat, ToggleFrame, FONT_SUB_SIZE);
		CreateButton("Captions", scrollContent, font, fontMat, ToggleCaptions, FONT_SUB_SIZE);

		SetFogAlbedo(Color.white, true);
		SetBackColor(Color.white, true);
		SetGroundColor(Color.white, true);

		Utils.RescaleUI(nav);
		modNav = nav;
	}

	private static GameObject CreateScrollView(Transform parent, bool vertical, bool horizontal)
	{
		// Scroll View
		GameObject scrollView = new("ScrollView");
		scrollView.transform.SetParent(parent.transform);
		RectTransform rect = scrollView.AddComponent<RectTransform>();
		rect.pivot = new Vector2(0, 1);
		rect.anchorMin = new Vector2(0, 0);
		rect.anchorMax = new Vector2(1, 1);
		rect.sizeDelta = new Vector2(0, 0);
		rect.anchoredPosition = Vector2.zero;
		ScrollRect scrollRect = scrollView.AddComponent<ScrollRect>();
		scrollRect.movementType = ScrollRect.MovementType.Elastic;
		scrollRect.horizontal = horizontal;
		scrollRect.vertical = vertical;
		scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
		scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
		scrollRect.scrollSensitivity = 5;

		// Content
		GameObject content = new("Content");
		content.transform.SetParent(scrollView.transform);
		RectTransform rectContent = content.AddComponent<RectTransform>();
		rectContent.pivot = new Vector2(0, 1);
		rectContent.anchorMin = new Vector2(0, 1);
		rectContent.anchorMax = new Vector2(1, 1);
		// rect.sizeDelta = new Vector2(380, 1080);
		rectContent.anchoredPosition = Vector2.zero;
		rectContent.offsetMax = new Vector2(-20, 0);
		VerticalLayoutGroup groupContent = content.AddComponent<VerticalLayoutGroup>();
		groupContent.childForceExpandHeight = false;
		groupContent.childForceExpandWidth = true;
		groupContent.childControlHeight = true;
		groupContent.childControlWidth = true;
		groupContent.childAlignment = TextAnchor.UpperLeft;
		groupContent.padding = new RectOffset(20, 20, 80, 80);
		groupContent.spacing = 14;
		ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
		fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
		fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

		scrollRect.content = rectContent;

		if (vertical)
		{
			var scrollObj = CreateScrollbar(scrollView.transform, true);
			scrollRect.verticalScrollbar = scrollObj.GetComponent<Scrollbar>();
		}
		if (horizontal)
		{
			var scrollObj = CreateScrollbar(scrollView.transform, false);
			scrollRect.horizontalScrollbar = scrollObj.GetComponent<Scrollbar>();
		}

		return scrollView;
	}
	private static GameObject CreateScrollbar(Transform parent, bool isVertical)
	{
		// Scrollbar
		GameObject scrollbarObj = new("Scrollbar");
		scrollbarObj.transform.SetParent(parent.transform);
		RectTransform scrollbarRect = scrollbarObj.AddComponent<RectTransform>();
		if (isVertical)
		{
			scrollbarRect.pivot = new Vector2(1, 1);
			scrollbarRect.anchorMin = new Vector2(1, 0);
			scrollbarRect.anchorMax = new Vector2(1, 1);
		}
		else
		{
			scrollbarRect.pivot = new Vector2(0, 0);
			scrollbarRect.anchorMin = new Vector2(0, 0);
			scrollbarRect.anchorMax = new Vector2(1, 0);
		}
		scrollbarRect.sizeDelta = Vector2.zero;
		scrollbarRect.anchoredPosition = Vector2.zero;
		Scrollbar scrollbar = scrollbarObj.AddComponent<Scrollbar>();
		scrollbar.direction = isVertical ? Scrollbar.Direction.BottomToTop : scrollbar.direction = Scrollbar.Direction.RightToLeft;

		// Background bar
		GameObject background = new("Back Fill");
		background.transform.SetParent(scrollbarObj.transform);
		RectTransform backRect = background.AddComponent<RectTransform>();
		if (isVertical)
		{
			backRect.pivot = new Vector2(1, 1);
			backRect.anchorMin = new Vector2(1, 0);
			backRect.anchorMax = new Vector2(1, 1);
			backRect.sizeDelta = new Vector2(15, 0);
		}
		else
		{
			backRect.pivot = new Vector2(0, 0);
			backRect.anchorMin = new Vector2(0, 0);
			backRect.anchorMax = new Vector2(1, 0);
			backRect.sizeDelta = new Vector2(0, 15);
		}
		backRect.anchoredPosition = Vector2.zero;
		Image backgroundImage = background.AddComponent<Image>();
		backgroundImage.color = new Color(.5f, .5f, .5f);

		// Slider knob
		GameObject handle = new("Handle");
		handle.transform.SetParent(scrollbarObj.transform);
		RectTransform handleRect = handle.AddComponent<RectTransform>();
		handleRect.pivot = isVertical ? new Vector2(1, 1) : new Vector2(0, 0);
		handleRect.sizeDelta = isVertical ? new Vector2(15, 0) : new Vector2(0, 15);
		handleRect.anchoredPosition = Vector2.zero;
		Image handleImage = handle.AddComponent<Image>();
		handleImage.color = new Color(.9f, .9f, .9f);

		scrollbar.handleRect = handleRect;
		// scrollbar.size = .1f;

		return scrollbarObj;
	}
	private static GameObject CreateSlider(int maxValue, Transform parent, Action callable)
	{
		GameObject sliderObj = new("Slider");
		sliderObj.transform.SetParent(parent.transform);
		Slider slider = sliderObj.AddComponent<Slider>();
		slider.maxValue = maxValue;
		slider.minValue = 0.01f;
		slider.wholeNumbers = false;
		slider.onValueChanged.AddListener((Action<float>)(value => callable()));

		LayoutElement layout = sliderObj.AddComponent<LayoutElement>();
		layout.minHeight = 20;
		layout.preferredHeight = 20;
		layout.flexibleWidth = 1;
		layout.flexibleHeight = 0;

		// Background bar
		GameObject background = new("Back Fill");
		background.transform.SetParent(sliderObj.transform);
		RectTransform backRect = background.AddComponent<RectTransform>();
		backRect.pivot = new Vector2(.5f, .5f);
		backRect.anchorMin = new Vector2(0, .5f);
		backRect.anchorMax = new Vector2(1, .5f);
		backRect.sizeDelta = new Vector2(0, 20);
		backRect.anchoredPosition = Vector2.zero;
		Image backgroundImage = background.AddComponent<Image>();
		backgroundImage.color = new Color(.7f, .7f, .7f, .6f);

		// Filled progress bar
		GameObject fill = new("Fill");
		fill.transform.SetParent(slider.transform);
		RectTransform fillRect = fill.AddComponent<RectTransform>();
		fillRect.pivot = new Vector2(.5f, .5f);
		fillRect.sizeDelta = new Vector2(0, 0);
		fillRect.anchoredPosition = Vector2.zero;
		Image fillImage = fill.AddComponent<Image>();
		fillImage.color = new Color(.7f, .7f, .7f);

		slider.fillRect = fillRect;

		// Slider knob
		GameObject handle = new("Handle");
		handle.transform.SetParent(slider.transform);
		RectTransform handleRect = handle.AddComponent<RectTransform>();
		handleRect.pivot = new Vector2(.5f, .5f);
		handleRect.sizeDelta = new Vector2(20, 8);
		handleRect.anchoredPosition = Vector2.zero;
		Image handleImage = handle.AddComponent<Image>();
		// handleImage.color = new Color(.7f, .9f, 1);

		slider.handleRect = handleRect;

		// Transition FX
		slider.transition = Selectable.Transition.ColorTint;
		slider.targetGraphic = handleImage;
		ColorBlock colorBlock = new()
		{
			normalColor = new Color(.7f, .9f, 1),
			highlightedColor = new Color(.6f, .9f, 1, 1),
			pressedColor = new Color(1, 1, 1, .6f),
			selectedColor = new Color(.7f, .9f, 1),
			disabledColor = new Color(1, 1, 1, 0),
			colorMultiplier = 1,
			fadeDuration = .1f
		};
		slider.colors = colorBlock;

		return sliderObj;
	}
	private static GameObject CreateLabel(string text, Transform parent, TMP_FontAsset font, Material fontMat, int fontSize = -1)
	{
		GameObject hueLabel = new("Label: " + text);
		hueLabel.transform.SetParent(parent.transform);
		TextMeshProUGUI hueText = hueLabel.AddComponent<TextMeshProUGUI>();
		hueText.font = font;
		hueText.material = fontMat;
		hueText.text = text;
		if (fontSize != -1) hueText.fontSize = fontSize;

		return hueLabel;
	}
	private static GameObject CreateButton(string text, Transform parent, TMP_FontAsset font, Material fontMat, Action callable, int fontSize = -1)
	{
		GameObject buttonObj = new("Button: " + text);
		buttonObj.transform.SetParent(parent.transform);

		Button button = buttonObj.AddComponent<Button>();
		button.onClick.AddListener(callable);
		RectTransform btnRect = buttonObj.AddComponent<RectTransform>();
		btnRect.pivot = new Vector2(.5f, .5f);
		btnRect.anchorMin = new Vector2(0, .5f);
		btnRect.anchorMax = new Vector2(1, .5f);
		// btnRect.anchoredPosition = Vector2.zero;
		LayoutElement layout = buttonObj.AddComponent<LayoutElement>();
		layout.preferredHeight = 40;
		layout.flexibleWidth = 1;
		Image image = buttonObj.AddComponent<Image>();
		// image.color = new Color(.9f, .9f, .9f, 1);

		GameObject textObj = new("Text");
		textObj.transform.SetParent(buttonObj.transform);
		RectTransform textRect = textObj.AddComponent<RectTransform>();
		textRect.anchoredPosition = Vector2.zero;
		TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
		buttonText.autoSizeTextContainer = true;
		buttonText.font = font;
		buttonText.material = fontMat;
		buttonText.text = text;
		buttonText.color = new Color(.1f, .1f, .1f);
		if (fontSize != -1) buttonText.fontSize = fontSize;

		button.transition = Selectable.Transition.ColorTint;
		ColorBlock colorBlock = new()
		{
			normalColor = new Color(.9f, .9f, .9f),
			highlightedColor = new Color(.8f, .9f, 1),
			pressedColor = new Color(.2f, .8f, 1),
			selectedColor = new Color(.9f, .9f, .9f),
			disabledColor = new Color(.4f, .4f, .4f, .8f),
			colorMultiplier = 1,
			fadeDuration = .1f
		};
		button.colors = colorBlock;
		button.targetGraphic = image;

		return buttonObj;
	}

	private static void OnFogColorChanged()
	{
		SetFogAlbedo(Color.HSVToRGB(fogHue.value / 360, fogSat.value / 100, fogLum.value / 100));
	}
	private static void OnBackColorChanged()
	{
		SetBackColor(Color.HSVToRGB(backHue.value / 360, backSat.value / 100, backLum.value / 100));
	}
	private static void OnGndColorChanged()
	{
		SetGroundColor(Color.HSVToRGB(gndHue.value / 360, gndSat.value / 100, gndLum.value / 100));
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
}