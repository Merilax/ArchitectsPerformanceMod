using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArchPerformanceMod;

public static class UIUtils
{
	public static GameObject CreateScrollView(Transform parent, bool vertical, bool horizontal)
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
		scrollRect.scrollSensitivity = 10;
		RectMask2D mask2D = scrollView.AddComponent<RectMask2D>();

		// Content
		GameObject content = new("Content");
		content.transform.SetParent(scrollView.transform);
		RectTransform rectContent = content.AddComponent<RectTransform>();
		rectContent.pivot = new Vector2(0, 1);
		rectContent.anchorMin = new Vector2(0, 1);
		rectContent.anchorMax = new Vector2(1, 1);
		rect.sizeDelta = new Vector2(0, 0);
		rectContent.anchoredPosition = Vector2.zero;
		rectContent.offsetMax = new Vector2(-20, 0);
		VerticalLayoutGroup groupContent = content.AddComponent<VerticalLayoutGroup>();
		groupContent.childForceExpandHeight = false;
		groupContent.childForceExpandWidth = true;
		groupContent.childControlHeight = true;
		groupContent.childControlWidth = true;
		groupContent.childAlignment = TextAnchor.UpperLeft;
		// groupContent.padding = new RectOffset(20, 20, 80, 80);
		groupContent.spacing = 12;
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

		// Mask
		GameObject mask = new("Mask");
		mask.transform.SetParent(scrollView.transform);
		RectTransform maskRect = mask.AddComponent<RectTransform>();
		// maskRect.pivot = new Vector2(0, 1);
		maskRect.anchorMin = new Vector2(0, 0);
		maskRect.anchorMax = new Vector2(1, 1);
		maskRect.sizeDelta = new Vector2(0, 0);
		maskRect.anchoredPosition = Vector2.zero;
		// RectMask2D mask2D = mask.AddComponent<RectMask2D>();

		return scrollView;
	}
	public static GameObject CreateScrollbar(Transform parent, bool isVertical)
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
	public static GameObject CreateSlider(float minValue, float maxValue, Transform parent, Action<float> callable = null)
	{
		Plugin.LogDebug("CreateSlider In");
		GameObject sliderObj = new("Slider");
		sliderObj.transform.SetParent(parent.transform);
		Slider slider = sliderObj.AddComponent<Slider>();
		slider.maxValue = maxValue;
		slider.minValue = minValue;
		slider.wholeNumbers = false;
		if (callable != null)
			slider.onValueChanged.AddListener((Action<float>)(value => callable(value)));

		LayoutElement layout = sliderObj.AddComponent<LayoutElement>();
		layout.minHeight = 8;
		layout.preferredHeight = 16;
		layout.flexibleWidth = 1;
		layout.flexibleHeight = 0;

		// Background bar
		GameObject background = new("Back Fill");
		background.transform.SetParent(sliderObj.transform);
		RectTransform backRect = background.AddComponent<RectTransform>();
		backRect.pivot = new Vector2(.5f, .5f);
		backRect.anchorMin = new Vector2(0, .5f);
		backRect.anchorMax = new Vector2(1, .5f);
		backRect.sizeDelta = new Vector2(0, 14);
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
		handleRect.sizeDelta = new Vector2(16, 8);
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

		Plugin.LogDebug("CreateSlider Out");
		return sliderObj;
	}
	public static GameObject CreateLabel(Localization.Items text, Transform parent, TMP_FontAsset font, Material fontMat, int fontSize = -1)
	{
		GameObject label = new("Label: " + text);
		label.transform.SetParent(parent.transform);
		TextMeshProUGUI textMesh = label.AddComponent<TextMeshProUGUI>();
		textMesh.font = font;
		textMesh.material = fontMat;
		textMesh.text = Localization.GetText(text);
		Localization.OnLocaleChanged += () => textMesh.text = Localization.GetText(text);
		if (fontSize != -1) textMesh.fontSize = fontSize;

		Plugin.LogDebug("CreateLabel Out");
		return label;
	}
	public static GameObject CreateButton(Localization.Items text, Transform parent, TMP_FontAsset font, Material fontMat, Action callable, int fontSize = -1, int preferredHeight = 34)
	{
		Plugin.LogDebug("CreateButton In");
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
		layout.preferredHeight = preferredHeight;
		layout.flexibleWidth = 1;
		Image image = buttonObj.AddComponent<Image>();
		image.color = new Color(.9f, .9f, .9f, 1);

		GameObject textObj = new("Text");
		textObj.transform.SetParent(buttonObj.transform);
		RectTransform textRect = textObj.AddComponent<RectTransform>();
		textRect.anchorMin = new(0, 0);
		textRect.anchorMax = new(1, 1);
		textRect.anchoredPosition = Vector2.zero;
		textRect.sizeDelta = new(0, 0);
		TextMeshProUGUI buttonText = textObj.AddComponent<TextMeshProUGUI>();
		// buttonText.autoSizeTextContainer = true;
		buttonText.font = font;
		buttonText.material = fontMat;
		buttonText.text = Localization.GetText(text);
		Localization.OnLocaleChanged += () => buttonText.text = Localization.GetText(text);
		buttonText.color = new Color(.1f, .1f, .1f);
		buttonText.alignment = TextAlignmentOptions.Center;
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

		Plugin.LogDebug("CreateButton Out");
		return buttonObj;
	}
	public static GameObject CreateDropdown(List<Localization.Items> items, Transform parent, Action<int> callable, int fontSize = -1)
	{
		if (fontSize == -1) fontSize = 20;

		GameObject dropObj = new("Dropdown");
		dropObj.transform.SetParent(parent.transform);
		RectTransform btnRect = dropObj.AddComponent<RectTransform>();
		btnRect.pivot = new Vector2(.5f, .5f);
		btnRect.anchorMin = new Vector2(0, .5f);
		btnRect.anchorMax = new Vector2(1, .5f);
		// btnRect.anchoredPosition = Vector2.zero;
		LayoutElement layout = dropObj.AddComponent<LayoutElement>();
		layout.preferredHeight = 34;
		layout.flexibleWidth = 1;
		Image image = dropObj.AddComponent<Image>();
		image.color = new Color(.9f, .9f, .9f, 1);
		Dropdown dropdown = dropObj.AddComponent<Dropdown>();
		if (callable != null)
			dropdown.onValueChanged.AddListener(callable);
		dropdown.options = new();
		for (int i = 0; i < items.Count; i++)
		{
			dropdown.options.Add(new Dropdown.OptionData(Localization.GetText(items[i])));
			Localization.OnLocaleChanged += () => dropdown.options[i].text = Localization.GetText(items[i]);
		}

		dropdown.transition = Selectable.Transition.ColorTint;
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
		dropdown.colors = colorBlock;
		dropdown.targetGraphic = image;

		// Caption
		GameObject caption = new("Caption");
		caption.transform.SetParent(dropdown.transform);
		RectTransform captionRect = caption.AddComponent<RectTransform>();
		captionRect.pivot = new Vector2(.5f, .5f);
		captionRect.anchorMin = new Vector2(0, 0);
		captionRect.anchorMax = new Vector2(1, 1);
		captionRect.sizeDelta = Vector2.zero;
		captionRect.anchoredPosition = Vector2.zero;
		Image captionImage = caption.AddComponent<Image>();
		captionImage.color = new Color(.9f, .9f, .9f, 1);
		LayoutElement captionLayout = caption.AddComponent<LayoutElement>();
		captionLayout.preferredHeight = 34;
		captionLayout.flexibleWidth = 1;

		// Caption text
		GameObject captionLabel = new("Label");
		captionLabel.transform.SetParent(caption.transform);
		RectTransform textCaptionRect = captionLabel.AddComponent<RectTransform>();
		textCaptionRect.anchoredPosition = Vector2.zero;
		Text captionText = captionLabel.AddComponent<Text>();
		captionText.alignByGeometry = true;
		captionText.font = Font.CreateDynamicFontFromOSFont("Arial", 20);
		// captionText.material = fontMat;
		captionText.color = new Color(.1f, .1f, .1f);
		captionText.alignment = TextAnchor.MiddleCenter;
		if (fontSize != -1) captionText.fontSize = fontSize;

		// Template
		GameObject template = new("Template");
		template.transform.SetParent(dropdown.transform);
		RectTransform templateRect = template.AddComponent<RectTransform>();
		templateRect.sizeDelta = Vector2.zero;
		templateRect.anchorMin = new Vector2(0, 0);
		templateRect.anchorMax = new Vector2(1, 0);
		templateRect.anchoredPosition = Vector2.zero;

		// Template ScrollView
		GameObject viewport = CreateScrollView(template.transform, false, false);
		viewport.GetComponent<RectMask2D>().enabled = false;
		RectTransform viewRect = viewport.GetComponent<RectTransform>();
		viewRect.sizeDelta = Vector2.zero;
		viewRect.anchorMin = new Vector2(0, 1);
		viewRect.anchorMax = new Vector2(1, 1);
		viewRect.anchoredPosition = Vector2.zero;

		GameObject content = viewport.transform.GetChild(0).gameObject;
		VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
		contentLayout.padding = new RectOffset(0, 0, 0, 0);
		contentLayout.spacing = 0;
		contentLayout.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

		// Item
		GameObject item = new("Item");
		item.transform.SetParent(content.transform);
		RectTransform itemRect = item.AddComponent<RectTransform>();
		itemRect.pivot = new Vector2(0, 0);
		itemRect.anchorMin = new Vector2(0, 0);
		itemRect.anchorMax = new Vector2(0, 1);
		itemRect.anchoredPosition = Vector2.zero;
		Image itemImage = item.AddComponent<Image>();
		itemImage.color = new Color(.9f, .9f, .9f, 1);
		LayoutElement itemLayout = item.AddComponent<LayoutElement>();
		itemLayout.preferredHeight = 30;
		itemLayout.flexibleWidth = 1;
		Toggle toggle = item.AddComponent<Toggle>();
		toggle.enabled = true;

		// Item text
		GameObject itemLabel = new("Label");
		itemLabel.transform.SetParent(item.transform);
		RectTransform itemTextRect = itemLabel.AddComponent<RectTransform>();
		itemTextRect.anchoredPosition = Vector2.zero;
		Text itemText = itemLabel.AddComponent<Text>();
		captionText.alignByGeometry = true;
		itemText.font = Font.CreateDynamicFontFromOSFont("Arial", 20);
		// itemText.material = fontMat;
		itemText.color = new Color(.1f, .1f, .1f);
		itemText.alignment = TextAnchor.MiddleCenter;
		itemText.text = "Sample text";
		if (fontSize != -1) itemText.fontSize = fontSize;

		dropdown.captionText = captionText;
		dropdown.captionImage = image;
		// dropdown.itemImage = itemImageComp;
		dropdown.itemText = itemText;
		dropdown.template = templateRect;

		template.active = false;
		return dropObj;
	}

	public static List<InputField> CreateVec2Input(Transform parent, InputField.ContentType contentType, int charLimit)
	{
		return CreateArrayInput(parent, 2, contentType, charLimit);
	}
	public static List<InputField> CreateVec3Input(Transform parent, InputField.ContentType contentType, int charLimit)
	{
		return CreateArrayInput(parent, 3, contentType, charLimit);
	}
	public static List<InputField> CreateArrayInput(Transform parent, int count, InputField.ContentType contentType, int charLimit)
	{
		GameObject container = new("Vec3 Input");
		container.transform.SetParent(parent);
		RectTransform rect = container.AddComponent<RectTransform>();
		// rect.anchoredPosition = new(0,0);
		rect.anchorMin = new(0, 0);
		rect.anchorMax = new(1, 0);
		// rect.sizeDelta = new();
		LayoutElement layout = container.AddComponent<LayoutElement>();
		layout.preferredHeight = 40;
		layout.flexibleWidth = 1;
		HorizontalLayoutGroup group = container.AddComponent<HorizontalLayoutGroup>();
		group.spacing = 3;
		group.padding = new(2, 2, 2, 2);

		List<InputField> list = [];
		for (int i = 0; i < count; i++)
		{
			GameObject input = new("Input");
			input.transform.SetParent(container.transform);
			Image image = input.AddComponent<Image>();
			image.color = new(0, 0, 0, .4f);
			LayoutElement inputLayout = input.AddComponent<LayoutElement>();
			inputLayout.flexibleHeight = 1;
			InputField field = input.AddComponent<InputField>();
			field.contentType = contentType;
			field.lineType = InputField.LineType.SingleLine;
			field.characterLimit = charLimit;

			GameObject textObj = new("Text");
			textObj.transform.SetParent(input.transform);
			RectTransform rectText = textObj.AddComponent<RectTransform>();
			rectText.anchorMin = new(0, 0);
			rectText.anchorMax = new(1, 1);
			rectText.anchoredPosition = new(0, 0);
			rectText.sizeDelta = new(-20, 0);
			Text text = textObj.AddComponent<Text>();
			text.font = Font.CreateDynamicFontFromOSFont("Arial", 20);
			text.fontSize = 20;
			text.alignByGeometry = true;
			text.alignment = TextAnchor.MiddleLeft;

			field.textComponent = text;
			list.Add(field);
		}

		return list;
	}
}