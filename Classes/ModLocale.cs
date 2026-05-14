using System.Collections.Generic;

namespace ArchPerformanceMod;

public class Localization
{
	enum Locales { ENGLISH, JAPANESE, CHINESE, CHINESE_SIMPLIFIED, UNKNOWN }

	public enum Items
	{
		setOff,
		setOn,
		setLow,
		setMedium,
		setHigh,
		none,
		reduced,
		full,
		def,
		optimized,
		aggresive,
		FXAA,
		MSAA,
		TAA,
		setRaceOnly,
		settingsModButton,
		SET_GI_ENTRY,
		SET_SSR_ENTRY,
		SET_SHADOWQUALITY_ENTRY,
		SET_AO_ENTRY,
		SET_CHROMAABERRATION_ENTRY,
		SET_VIGNETTE_ENTRY,
		SET_SHADOWTONES_ENTRY,
		SET_DOCKLIGHTS_ENTRY,
		SET_AA_ENTRY,
		SET_MACHINEPARTICLES_ENTRY,
	}

	public delegate void LocaleChanged();
	public static event LocaleChanged OnLocaleChanged;
	// private static Locales currentLocale = Locales.ENGLISH;
	private readonly static Dictionary<Items, string> dict_en = new()
	{
		{Items.setOff, "Off"},
		{Items.setOn, "On"},
		{Items.setLow, "Low"},
		{Items.setMedium, "Medium"},
		{Items.setHigh, "High"},
		{Items.none, "None"},
		{Items.reduced, "Reduced"},
		{Items.full, "Full"},
		{Items.def, "Default"},
		{Items.optimized, "Optimized"},
		{Items.aggresive, "Aggresive"},
		{Items.FXAA, "FXAA"},
		{Items.MSAA, "MSAA"},
		{Items.TAA, "TAA"},
		{Items.setRaceOnly, "Only in race"},
		{Items.settingsModButton, "Mod config"},
		{Items.SET_GI_ENTRY, "Global Illumination [!]"},
		{Items.SET_AO_ENTRY, "Ambient Occlusion"},
		{Items.SET_SSR_ENTRY, "Screen-Space Reflections"},
		{Items.SET_SHADOWQUALITY_ENTRY, "Shadow Quality"},
		{Items.SET_CHROMAABERRATION_ENTRY, "Chromatic Aberration"},
		{Items.SET_VIGNETTE_ENTRY, "Vignette"},
		{Items.SET_SHADOWTONES_ENTRY, "Shadow Tone-mapping"},
		{Items.SET_DOCKLIGHTS_ENTRY, "Dock light quality"},
		{Items.SET_AA_ENTRY, "Anti-Aliasing"},
		{Items.SET_MACHINEPARTICLES_ENTRY, "Machine particles"},
	};
	private readonly static Dictionary<Items, string> dict_jp = new()
	{
		{Items.setOff, "Off"},
		{Items.setOn, "On"},
		{Items.setLow, "Low"},
		{Items.setMedium, "Medium"},
		{Items.setHigh, "High"},
		{Items.none, "なし"},
		{Items.reduced, "少ない"},
		{Items.full, "完全な"},
		{Items.def, "デフォルト"},
		{Items.optimized, "最適化された"},
		{Items.aggresive, "攻撃的"},
		{Items.FXAA, "FXAA"},
		{Items.MSAA, "MSAA"},
		{Items.TAA, "TAA"},
		{Items.setRaceOnly, "レース中のみ"},
		{Items.settingsModButton, "MODの設定"},
		{Items.SET_GI_ENTRY, "グローバルイルミネーション [!]"},
		{Items.SET_AO_ENTRY, "アンビエントオクルージョン"},
		{Items.SET_SSR_ENTRY, "画面空間反射"},
		{Items.SET_SHADOWQUALITY_ENTRY, "シャドウの質"},
		{Items.SET_CHROMAABERRATION_ENTRY, "色収差"},
		{Items.SET_VIGNETTE_ENTRY, "ビネット"},
		{Items.SET_SHADOWTONES_ENTRY, "シャドウトーンマッピング"},
		{Items.SET_DOCKLIGHTS_ENTRY, "ドックライトの品質"},
		{Items.SET_AA_ENTRY, "アンチエイリアシング"},
		{Items.SET_MACHINEPARTICLES_ENTRY, "機械の微粒子"},
	};
	private readonly static Dictionary<Items, string> dict_cn_t = new()
	{
		{Items.setOff, "Off"},
		{Items.setOn, "On"},
		{Items.setLow, "Low"},
		{Items.setMedium, "Medium"},
		{Items.setHigh, "High"},
		{Items.none, "無"},
		{Items.reduced, "降價"},
		{Items.full, "完整"},
		{Items.def, "預設"},
		{Items.optimized, "已優化"},
		{Items.aggresive, "咄咄逼人"},
		{Items.FXAA, "FXAA"},
		{Items.MSAA, "MSAA"},
		{Items.TAA, "TAA"},
		{Items.setRaceOnly, "僅限賽事"},
		{Items.settingsModButton, "模組設定"},
		{Items.SET_GI_ENTRY, "全局光照 [!]"},
		{Items.SET_AO_ENTRY, "環境光遮蔽"},
		{Items.SET_SSR_ENTRY, "螢幕空間反射"},
		{Items.SET_SHADOWQUALITY_ENTRY, "陰影品質"},
		{Items.SET_CHROMAABERRATION_ENTRY, "色差"},
		{Items.SET_VIGNETTE_ENTRY, "插圖"},
		{Items.SET_SHADOWTONES_ENTRY, "陰影色調映射"},
		{Items.SET_DOCKLIGHTS_ENTRY, "碼頭照明品質"},
		{Items.SET_AA_ENTRY, "抗鋸齒"},
		{Items.SET_MACHINEPARTICLES_ENTRY, "機械微粒"},
	};
	private readonly static Dictionary<Items, string> dict_cn_s = new()
	{
		{Items.setOff, "Off"},
		{Items.setOn, "On"},
		{Items.setLow, "Low"},
		{Items.setMedium, "Medium"},
		{Items.setHigh, "High"},
		{Items.none, "无"},
		{Items.reduced, "简化"},
		{Items.full, "完整"},
		{Items.def, "默认"},
		{Items.optimized, "优化"},
		{Items.aggresive, "咄咄逼人"},
		{Items.FXAA, "FXAA"},
		{Items.MSAA, "MSAA"},
		{Items.TAA, "TAA"},
		{Items.setRaceOnly, "仅限比赛"},
		{Items.settingsModButton, "模组配置"},
		{Items.SET_GI_ENTRY, "全局光照 [!]"},
		{Items.SET_AO_ENTRY, "环境光遮蔽"},
		{Items.SET_SSR_ENTRY, "屏幕空间反射"},
		{Items.SET_SHADOWQUALITY_ENTRY, "阴影质量"},
		{Items.SET_CHROMAABERRATION_ENTRY, "色差"},
		{Items.SET_VIGNETTE_ENTRY, "插图"},
		{Items.SET_SHADOWTONES_ENTRY, "阴影色调映射"},
		{Items.SET_DOCKLIGHTS_ENTRY, "码头照明质量"},
		{Items.SET_AA_ENTRY, "抗锯齿"},
		{Items.SET_MACHINEPARTICLES_ENTRY, "机械颗粒"},
	};

	public static void SetLocale() // Config.LanguageType lang
	{
		OnLocaleChanged?.Invoke();
	}

	public static string GetText(Items item)
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