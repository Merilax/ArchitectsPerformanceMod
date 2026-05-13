using System;
using System.Collections.Generic;

namespace PerformanceMod;

public class Localization
{
	enum Locales { ENGLISH, JAPANESE, CHINESE, CHINESE_SIMPLIFIED, UNKNOWN }

	public enum LocaleItems
	{
		setOff,
		setOn,
		setLow,
		setMedium,
		setHigh,
		none,
		reduced,
		full,
		FXAA,
		MSAA,
		TAA,
		setRaceOnly,
		settingsModButton,
		SET_GI_ENTRY,
		SET_SSR_ENTRY,
		SET_AO_ENTRY,
		SET_CHROMAABERRATION_ENTRY,
		SET_VIGNETTE_ENTRY,
		SET_SHADOWTONES_ENTRY,
		SET_DOCKLIGHTS_ENTRY,
		SET_AA_ENTRY,
		SET_MACHINEPARTICLES_ENTRY,
		SET_REDUCEOCEAN_ENTRY,
	}

	public delegate void LocaleChanged();
	public static event LocaleChanged OnLocaleChanged;
	// private static Locales currentLocale = Locales.ENGLISH;
	private readonly static Dictionary<LocaleItems, string> dict_en = new()
	{
		{LocaleItems.setOff, "Off"},
		{LocaleItems.setOn, "On"},
		{LocaleItems.setLow, "Low"},
		{LocaleItems.setMedium, "Medium"},
		{LocaleItems.setHigh, "High"},
		{LocaleItems.none, "None"},
		{LocaleItems.reduced, "Reduced"},
		{LocaleItems.full, "Full"},
		{LocaleItems.FXAA, "FXAA"},
		{LocaleItems.MSAA, "MSAA"},
		{LocaleItems.TAA, "TAA"},
		{LocaleItems.setRaceOnly, "Only in race"},
		{LocaleItems.settingsModButton, "Mod config"},
		{LocaleItems.SET_GI_ENTRY, "Global Illumination [!]"},
		{LocaleItems.SET_AO_ENTRY, "Ambient Occlusion"},
		{LocaleItems.SET_SSR_ENTRY, "Screen-Space Reflections"},
		{LocaleItems.SET_CHROMAABERRATION_ENTRY, "Chromatic Aberration"},
		{LocaleItems.SET_VIGNETTE_ENTRY, "Vignette"},
		{LocaleItems.SET_SHADOWTONES_ENTRY, "Shadow Tone-mapping"},
		{LocaleItems.SET_DOCKLIGHTS_ENTRY, "Dock light quality"},
		{LocaleItems.SET_AA_ENTRY, "Anti-Aliasing"},
		{LocaleItems.SET_MACHINEPARTICLES_ENTRY, "Machine particles"},
		{LocaleItems.SET_REDUCEOCEAN_ENTRY, "Reduce ocean quality"},
	};
	private readonly static Dictionary<LocaleItems, string> dict_jp = new()
	{
		{LocaleItems.setOff, "Off"},
		{LocaleItems.setOn, "On"},
		{LocaleItems.setLow, "Low"},
		{LocaleItems.setMedium, "Medium"},
		{LocaleItems.setHigh, "High"},
		{LocaleItems.none, "なし"},
		{LocaleItems.reduced, "少ない"},
		{LocaleItems.full, "完全な"},
		{LocaleItems.FXAA, "FXAA"},
		{LocaleItems.MSAA, "MSAA"},
		{LocaleItems.TAA, "TAA"},
		{LocaleItems.setRaceOnly, "レース中のみ"},
		{LocaleItems.settingsModButton, "MODの設定"},
		{LocaleItems.SET_GI_ENTRY, "グローバルイルミネーション [!]"},
		{LocaleItems.SET_AO_ENTRY, "アンビエントオクルージョン"},
		{LocaleItems.SET_SSR_ENTRY, "画面空間反射"},
		{LocaleItems.SET_CHROMAABERRATION_ENTRY, "色収差"},
		{LocaleItems.SET_VIGNETTE_ENTRY, "ビネット"},
		{LocaleItems.SET_SHADOWTONES_ENTRY, "シャドウトーンマッピング"},
		{LocaleItems.SET_DOCKLIGHTS_ENTRY, "ドックライトの品質"},
		{LocaleItems.SET_AA_ENTRY, "アンチエイリアシング"},
		{LocaleItems.SET_MACHINEPARTICLES_ENTRY, "機械の微粒子"},
		{LocaleItems.SET_REDUCEOCEAN_ENTRY, "Reduce ocean quality"},
	};
	private readonly static Dictionary<LocaleItems, string> dict_cn_t = new()
	{
		{LocaleItems.setOff, "Off"},
		{LocaleItems.setOn, "On"},
		{LocaleItems.setLow, "Low"},
		{LocaleItems.setMedium, "Medium"},
		{LocaleItems.setHigh, "High"},
		{LocaleItems.none, "無"},
		{LocaleItems.reduced, "降價"},
		{LocaleItems.full, "完整"},
		{LocaleItems.FXAA, "FXAA"},
		{LocaleItems.MSAA, "MSAA"},
		{LocaleItems.TAA, "TAA"},
		{LocaleItems.setRaceOnly, "僅限賽事"},
		{LocaleItems.settingsModButton, "模組設定"},
		{LocaleItems.SET_GI_ENTRY, "全局光照 [!]"},
		{LocaleItems.SET_AO_ENTRY, "環境光遮蔽"},
		{LocaleItems.SET_SSR_ENTRY, "螢幕空間反射"},
		{LocaleItems.SET_CHROMAABERRATION_ENTRY, "色差"},
		{LocaleItems.SET_VIGNETTE_ENTRY, "插圖"},
		{LocaleItems.SET_SHADOWTONES_ENTRY, "陰影色調映射"},
		{LocaleItems.SET_DOCKLIGHTS_ENTRY, "碼頭照明品質"},
		{LocaleItems.SET_AA_ENTRY, "抗鋸齒"},
		{LocaleItems.SET_MACHINEPARTICLES_ENTRY, "機械微粒"},
		{LocaleItems.SET_REDUCEOCEAN_ENTRY, "Reduce ocean quality"},
	};
	private readonly static Dictionary<LocaleItems, string> dict_cn_s = new()
	{
		{LocaleItems.setOff, "Off"},
		{LocaleItems.setOn, "On"},
		{LocaleItems.setLow, "Low"},
		{LocaleItems.setMedium, "Medium"},
		{LocaleItems.setHigh, "High"},
		{LocaleItems.none, "无"},
		{LocaleItems.reduced, "简化"},
		{LocaleItems.full, "完整"},
		{LocaleItems.FXAA, "FXAA"},
		{LocaleItems.MSAA, "MSAA"},
		{LocaleItems.TAA, "TAA"},
		{LocaleItems.setRaceOnly, "仅限比赛"},
		{LocaleItems.settingsModButton, "模组配置"},
		{LocaleItems.SET_GI_ENTRY, "全局光照 [!]"},
		{LocaleItems.SET_AO_ENTRY, "环境光遮蔽"},
		{LocaleItems.SET_SSR_ENTRY, "屏幕空间反射"},
		{LocaleItems.SET_CHROMAABERRATION_ENTRY, "色差"},
		{LocaleItems.SET_VIGNETTE_ENTRY, "插图"},
		{LocaleItems.SET_SHADOWTONES_ENTRY, "阴影色调映射"},
		{LocaleItems.SET_DOCKLIGHTS_ENTRY, "码头照明质量"},
		{LocaleItems.SET_AA_ENTRY, "抗锯齿"},
		{LocaleItems.SET_MACHINEPARTICLES_ENTRY, "机械颗粒"},
		{LocaleItems.SET_REDUCEOCEAN_ENTRY, "Reduce ocean quality"},
	};

	public static void SetLocale() // Config.LanguageType lang
	{
		OnLocaleChanged?.Invoke();
	}

	public static string GetText(LocaleItems item)
	{
		switch (Config.Language)
		{
			case Config.LanguageType.English:
				return dict_en[item];
			case Config.LanguageType.Japanese:
				return dict_jp[item];
			case Config.LanguageType.Chinese_t:
				return dict_cn_t[item];
			case Config.LanguageType.Chinese_s:
				return dict_cn_s[item];
			default:
				return "ERR: Unknown Locale";
		}
	}
}