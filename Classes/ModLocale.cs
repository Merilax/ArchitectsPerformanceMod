using System;
using System.Collections.Generic;

namespace BA3StandardMod;

public class Localization
{
	enum Locales { en, jp, unknown }

	public enum LocaleItems
	{
		setOff,
		setOn,
		setLow,
		setMedium,
		setHigh,
		settingsModButton,
		settingsValueGlobalIllumination,
		settingsValueReflections,
		settingsValueAmbientOcclusion
	}

	public delegate void LocaleChanged();
	public static event LocaleChanged OnLocaleChanged;
	private static Locales currentLocale = Locales.en;
	private readonly static Dictionary<LocaleItems, string> dict_en = new()
	{
		{LocaleItems.setOff, "Off"},
		{LocaleItems.setOn, "On"},
		{LocaleItems.setLow, "Low"},
		{LocaleItems.setMedium, "Medium"},
		{LocaleItems.setHigh, "High"},
		{LocaleItems.settingsModButton, "Mod config"},
		{LocaleItems.settingsValueGlobalIllumination, "Global Illumination"},
		{LocaleItems.settingsValueReflections, "Screen-Space Reflections"},
		{LocaleItems.settingsValueAmbientOcclusion, "Ambient Occlusion"},
	};
	private readonly static Dictionary<LocaleItems, string> dict_jp = new()
	{
		{LocaleItems.setOff, "Off"},
		{LocaleItems.setOn, "On"},
		{LocaleItems.setLow, "Low"},
		{LocaleItems.setMedium, "Medium"},
		{LocaleItems.setHigh, "High"},
		{LocaleItems.settingsModButton, "MODの設定"},
		{LocaleItems.settingsValueGlobalIllumination, "グローバルイルミネーション"},
		{LocaleItems.settingsValueReflections, "画面空間反射"},
		{LocaleItems.settingsValueAmbientOcclusion, "アンビエントオクルージョン"},
	};

	public static void SetLocale(Config.LanguageType lang)
	{
		switch (lang)
		{
			case Config.LanguageType.English:
				currentLocale = Locales.en;
				break;
			case Config.LanguageType.Japanese:
				currentLocale = Locales.jp;
				break;
			default:
				currentLocale = Locales.unknown;
				break;
		}

		OnLocaleChanged?.Invoke();
	}

	public static string GetText(LocaleItems item)
	{
		switch (currentLocale)
		{
			case Locales.en:
				return dict_en[item];
			case Locales.jp:
				return dict_jp[item];
			case Locales.unknown:
				return "Unsupported Locale";
			default:
				return "ERR: No Locale set";
		}
	}
}