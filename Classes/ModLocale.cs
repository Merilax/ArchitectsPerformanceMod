using System.Collections.Generic;
using HarmonyLib;

namespace ArchPerformanceMod;

public class Localization
{
	enum Locales { ENGLISH, JAPANESE, CHINESE, CHINESE_SIMPLIFIED, UNKNOWN }

	public enum Items
	{
		OFF, ON,
		LOW, MEDIUM, HIGH,
		NONE, REDUCED, FULL,
		DEF, OPTIMIZED, AGGRESIVE,
		FXAA, MSAA, TAA,
		RACE_ONLY, DIORAMA_ONLY,
		MOD_BUTTON,
		SET_PRESET, SET_GI_ENTRY, SET_SSR_ENTRY, SET_SHADOWQUALITY_ENTRY, SET_AO_ENTRY, SET_CHROMAABERRATION_ENTRY, SET_VIGNETTE_ENTRY,
		SET_SHADOWTONES_ENTRY, SET_DOCKLIGHTS_ENTRY, SET_AA_ENTRY, SET_MACHINEPARTICLES_ENTRY, SET_VOLUMETRICS, SET_CAMERACLIP,
		PRESET_VANILLA, PRESET_OPTIMIZED, PRESET_OVERDRIVE, PRESET_CUSTOM,
		ENVIRONMENT, LIGHTING,
		VOLUMETRICS_DISABLED,
		RETURN, RETURN_TO_SETTINGS,
		COLOR, FOG_COLOR, BACKGROUND_COLOR, GROUND_COLOR, 
		HUE, SATURATION, VALUE,
		RESET, RESET_PARAMS,
		TOGGLES, TOGGLE, TOGGLE_FOG, TOGGLE_GROUND, TOGGLE_TERRAIN, TOGGLE_FRAME, TOGGLE_CAPTIONS, TOGGLE_SHADOWS, TOGGLE_VOLUMETRICS,
		LIGHTS, ADD_LIGHT, DELETE,
		POSITION, ROTATION, LIGHT_TYPE, AMPLITUDE, RANGE, INTENSITY,
		POINT, SPOT, DIRECTIONAL,
	}

	private readonly static Dictionary<Items, string> englishDict = new()
	{
		{Items.OFF, "Off"},
		{Items.ON, "On"},
		{Items.LOW, "Low"},
		{Items.MEDIUM, "Medium"},
		{Items.HIGH, "High"},

		{Items.NONE, "None"},
		{Items.REDUCED, "Reduced"},
		{Items.FULL, "Full"},

		{Items.DEF, "Default"},
		{Items.OPTIMIZED, "Optimized"},
		{Items.AGGRESIVE, "Minimum"},

		{Items.FXAA, "FXAA"},
		{Items.MSAA, "MSAA"},
		{Items.TAA, "TAA"},

		{Items.RACE_ONLY, "Only in race"},
		{Items.DIORAMA_ONLY, "Diorama only"},

		{Items.MOD_BUTTON, "Mod config"},

		{Items.SET_PRESET, "Preset"},
		{Items.SET_GI_ENTRY, "Global Illumination [!]"},
		{Items.SET_SSR_ENTRY, "Screen-Space Reflections"},
		{Items.SET_SHADOWQUALITY_ENTRY, "Shadow Quality"},
		{Items.SET_VOLUMETRICS, "Volumetrics"},
		{Items.SET_AO_ENTRY, "Ambient Occlusion"},
		{Items.SET_CHROMAABERRATION_ENTRY, "Chromatic Aberration"},
		{Items.SET_VIGNETTE_ENTRY, "Vignette"},
		{Items.SET_SHADOWTONES_ENTRY, "Shadow Tone-mapping"},
		{Items.SET_DOCKLIGHTS_ENTRY, "Dock light quality"},
		{Items.SET_AA_ENTRY, "Anti-Aliasing"},
		{Items.SET_MACHINEPARTICLES_ENTRY, "Machine particles"},
		{Items.SET_CAMERACLIP, "Extend camera render distance"},
		
		{Items.PRESET_VANILLA, "Vanilla"},
		{Items.PRESET_OPTIMIZED, "Optimized"},
		{Items.PRESET_OVERDRIVE, "Minimal"},
		{Items.PRESET_CUSTOM, "Custom"},

		{Items.ENVIRONMENT, "Environment"},
		{Items.LIGHTING, "Lighting"},

		{Items.RETURN, "Return"},
		{Items.RETURN_TO_SETTINGS, "Return to settings"},
		{Items.VOLUMETRICS_DISABLED, "Volumetrics are disabled"},

		{Items.COLOR, "Color:"},
		{Items.FOG_COLOR, "Fog color:"},
		{Items.BACKGROUND_COLOR, "Background color:"},
		{Items.GROUND_COLOR, "Ground color:"},

		{Items.HUE, "Hue"},
		{Items.SATURATION, "Saturation"},
		{Items.VALUE, "Value"},

		{Items.RESET, "Reset"},
		{Items.RESET_PARAMS, "Reset parameters"},

		{Items.TOGGLES, "Toggles:"},
		{Items.TOGGLE, "Toggle"},
		{Items.TOGGLE_FOG, "Fog"},
		{Items.TOGGLE_GROUND, "Ground"},
		{Items.TOGGLE_TERRAIN, "Terrain"},
		{Items.TOGGLE_FRAME, "Frame"},
		{Items.TOGGLE_CAPTIONS, "Captions"},
		{Items.TOGGLE_SHADOWS, "Toggle shadows"},
		{Items.TOGGLE_VOLUMETRICS, "Toggle volumetrics"},

		{Items.LIGHTS, "Lights:"},
		{Items.ADD_LIGHT, "Add light"},
		{Items.DELETE, "Delete"},

		{Items.POSITION, "Position"},
		{Items.ROTATION, "Rotation"},
		{Items.LIGHT_TYPE, "Light type"},
		{Items.AMPLITUDE, "Amplitude"},
		{Items.RANGE, "Range"},
		{Items.INTENSITY, "Intensity"},

		{Items.POINT, "Point"},
		{Items.SPOT, "Spotlight"},
		{Items.DIRECTIONAL, "Directional"},
	};
	private readonly static Dictionary<Items, string> japaneseDict = new()
	{
		{Items.OFF, "Off"},
		{Items.ON, "On"},
		{Items.LOW, "Low"},
		{Items.MEDIUM, "Medium"},
		{Items.HIGH, "High"},

		{Items.NONE, "なし"},
		{Items.REDUCED, "少ない"},
		{Items.FULL, "最大"},

		{Items.DEF, "デフォルト"},
		{Items.OPTIMIZED, "最適化"},
		{Items.AGGRESIVE, "最低限"},

		{Items.FXAA, "FXAA"},
		{Items.MSAA, "MSAA"},
		{Items.TAA, "TAA"},

		{Items.RACE_ONLY, "レース中のみ"},
		{Items.DIORAMA_ONLY, "ジオラマのみ"},

		{Items.MOD_BUTTON, "MODの設定"},

		{Items.SET_PRESET, "プリセット"},
		{Items.SET_GI_ENTRY, "グローバルイルミネーション [!]"},
		{Items.SET_SSR_ENTRY, "画面空間反射 [SSR]"},
		{Items.SET_SHADOWQUALITY_ENTRY, "影の品質"},
		{Items.SET_AO_ENTRY, "アンビエントオクルージョン"},
		{Items.SET_CHROMAABERRATION_ENTRY, "色収差"},
		{Items.SET_VIGNETTE_ENTRY, "ビネット"},
		{Items.SET_SHADOWTONES_ENTRY, "シャドウトーンマッピング"},
		{Items.SET_DOCKLIGHTS_ENTRY, "ドックライトの品質"},
		{Items.SET_AA_ENTRY, "アンチエイリアス"},
		{Items.SET_MACHINEPARTICLES_ENTRY, "機体のパーティクル"},

		{Items.PRESET_VANILLA, "デフォルト"},
		{Items.PRESET_OPTIMIZED, "最適化"},
		{Items.PRESET_OVERDRIVE, "ミニマル"},
		{Items.PRESET_CUSTOM, "カスタム"},

		{Items.ENVIRONMENT, "環境設定"},
		{Items.LIGHTING, "照明設定"},

		{Items.RETURN, "Return"},
		{Items.RETURN_TO_SETTINGS, "設定画面に戻る"},
		{Items.VOLUMETRICS_DISABLED, "ボリュームメトリクス無効"},

		{Items.COLOR, "カラー"},
		{Items.FOG_COLOR, "フォグカラー"},
		{Items.BACKGROUND_COLOR, "バックグラウンドカラー"},
		{Items.GROUND_COLOR, "グランドカラー"},

		{Items.HUE, "色相"},
		{Items.SATURATION, "飽和度"},
		{Items.VALUE, "値"},

		{Items.RESET, "初期化"},
		{Items.RESET_PARAMS, "パラメーターを初期化"},

		{Items.TOGGLES, "有効・無効"},
		{Items.TOGGLE, "有効・無効"},
		{Items.TOGGLE_FOG, "霧"},
		{Items.TOGGLE_GROUND, "地面"},
		{Items.TOGGLE_TERRAIN, "地形"},
		{Items.TOGGLE_FRAME, "フレーム"},
		{Items.TOGGLE_CAPTIONS, "キャプション"},
		{Items.TOGGLE_SHADOWS, "影の表示を切り替える"},

		{Items.LIGHTS, "Lights:"},
		{Items.ADD_LIGHT, "照明を追加する"},
		{Items.DELETE, "削除"},

		{Items.POSITION, "役職"},
		{Items.ROTATION, "回転"},
		{Items.LIGHT_TYPE, "ライトタイプ"},
		{Items.AMPLITUDE, "振幅"},
		{Items.RANGE, "範囲"},
		{Items.INTENSITY, "強度"},
	};
	private readonly static Dictionary<Items, string> simplifiedChineseDict = new()
	{
		{Items.OFF, "Off"},
		{Items.ON, "On"},
		{Items.LOW, "Low"},
		{Items.MEDIUM, "Medium"},
		{Items.HIGH, "High"},

		{Items.NONE, "无"},
		{Items.REDUCED, "简化"},
		{Items.FULL, "完整"},

		{Items.DEF, "默认"},
		{Items.OPTIMIZED, "优化"},
		{Items.AGGRESIVE, "最小"},

		{Items.FXAA, "FXAA"},
		{Items.MSAA, "MSAA"},
		{Items.TAA, "TAA"},

		{Items.RACE_ONLY, "仅限比赛"},

		{Items.MOD_BUTTON, "模组配置"},

		{Items.SET_PRESET, "预设"},
		{Items.SET_GI_ENTRY, "全局光照 [!]"},
		{Items.SET_SSR_ENTRY, "屏幕空间反射"},
		{Items.SET_SHADOWQUALITY_ENTRY, "阴影质量"},
		{Items.SET_AO_ENTRY, "环境光遮蔽"},
		{Items.SET_CHROMAABERRATION_ENTRY, "色差"},
		{Items.SET_VIGNETTE_ENTRY, "插图"},
		{Items.SET_SHADOWTONES_ENTRY, "阴影色调映射"},
		{Items.SET_DOCKLIGHTS_ENTRY, "码头照明质量"},
		{Items.SET_AA_ENTRY, "抗锯齿"},
		{Items.SET_MACHINEPARTICLES_ENTRY, "机械颗粒"},

		{Items.PRESET_VANILLA, "默认"},
		{Items.PRESET_OPTIMIZED, "优化"},
		{Items.PRESET_OVERDRIVE, "极简"},
		{Items.PRESET_CUSTOM, "自定义"},
	};
	private readonly static Dictionary<Items, string> traditionalChineseDict = new()
	{
		{Items.OFF, "Off"},
		{Items.ON, "On"},
		{Items.LOW, "Low"},
		{Items.MEDIUM, "Medium"},
		{Items.HIGH, "High"},

		{Items.NONE, "無"},
		{Items.REDUCED, "降價"},
		{Items.FULL, "完整"},

		{Items.DEF, "預設"},
		{Items.OPTIMIZED, "已優化"},
		{Items.AGGRESIVE, "最低"},

		{Items.FXAA, "FXAA"},
		{Items.MSAA, "MSAA"},
		{Items.TAA, "TAA"},

		{Items.RACE_ONLY, "僅限賽事"},

		{Items.MOD_BUTTON, "模組設定"},

		{Items.SET_PRESET, "預設"},
		{Items.SET_GI_ENTRY, "全局光照 [!]"},
		{Items.SET_SSR_ENTRY, "螢幕空間反射"},
		{Items.SET_SHADOWQUALITY_ENTRY, "陰影品質"},
		{Items.SET_AO_ENTRY, "環境光遮蔽"},
		{Items.SET_CHROMAABERRATION_ENTRY, "色差"},
		{Items.SET_VIGNETTE_ENTRY, "插圖"},
		{Items.SET_SHADOWTONES_ENTRY, "陰影色調映射"},
		{Items.SET_DOCKLIGHTS_ENTRY, "碼頭照明品質"},
		{Items.SET_AA_ENTRY, "抗鋸齒"},
		{Items.SET_MACHINEPARTICLES_ENTRY, "機械微粒"},

		{Items.PRESET_VANILLA, "預設"},
		{Items.PRESET_OPTIMIZED, "已優化"},
		{Items.PRESET_OVERDRIVE, "極簡"},
		{Items.PRESET_CUSTOM, "自訂"},
	};

	public delegate void LocaleChanged();
	public static event LocaleChanged OnLocaleChanged;

	[HarmonyPostfix]
	[HarmonyPatch(typeof(TranslationList), nameof(TranslationList.ChangeLanguage))]
	public static void SetLocale() // Config.LanguageType lang
	{
		OnLocaleChanged?.Invoke();
	}

	public static string GetText(Items item)
	{
		return GetTextLocale(item, Config.Language);
	}

	public static string GetTextLocale(Items item, Config.LanguageType locale)
	{
		Dictionary<Items, string> dict = locale switch
		{
			Config.LanguageType.Japanese => japaneseDict,
			Config.LanguageType.Chinese_t => traditionalChineseDict,
			Config.LanguageType.Chinese_s => simplifiedChineseDict,
			_ => englishDict,
		};

		bool ok = dict.TryGetValue(item, out string str);
		// Return found item.
		if (ok) return str;
		// Else, try again in English.
		if (locale != Config.LanguageType.English)
			return GetTextLocale(item, Config.LanguageType.English);
		// Else, since the default is English and nothing was found, return an error.
		return "ERR: No Text";
	}
}