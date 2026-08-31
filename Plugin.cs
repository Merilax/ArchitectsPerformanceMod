using System;
using System.IO;
using System.Text;
using System.Text.Json;
using ArchEmperorLib;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;

namespace ArchPerformanceMod;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("BREAK ARTS III.exe")]
[BepInDependency("ArchEmperorLib", BepInDependency.DependencyFlags.HardDependency)]

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

		TryCopyOldPluginConfig();

		// Plugin startup logic
		Log = BepInEx.Logging.Logger.CreateLogSource("ArchPerformance");
		Log.LogInfo($"Loading plugin patches...");

		config = Config;

		string bundlePath = Path.Combine(Paths.PluginPath, "ArchPerformanceMod", "archperformance_assets");
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

		ModRegistry.Register(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION, RequirementScope.ClientOptional, VersionStrictness.None);

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
		catch (Exception ex)
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
		catch (Exception ex)
		{
			Log.LogError(ex);
		}
	}

	private static void TryCopyOldPluginConfig()
	{
		if (File.Exists(Path.Combine(Paths.ConfigPath, "ArchitectsPerformanceMod.cfg")))
		{
			if (File.Exists(Path.Combine(Paths.ConfigPath, "ArchPerformanceMod.cfg")))
				File.Delete(Path.Combine(Paths.ConfigPath, "ArchPerformanceMod.cfg"));
			File.Copy(Path.Combine(Paths.ConfigPath, "ArchitectsPerformanceMod.cfg"), Path.Combine(Paths.ConfigPath, "ArchPerformanceMod.cfg"));
			File.Delete(Path.Combine(Paths.ConfigPath, "ArchitectsPerformanceMod.cfg"));
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
	static bool once = false;
	[HarmonyPostfix]
	[HarmonyPatch(typeof(Scene_MainMenu), nameof(Scene_MainMenu.Start))]
	public static void InitializeMain(ref Scene_MainMenu __instance)
	{
		if (once) return; once = true;
		Cursor.lockState = CursorLockMode.Confined;

		Localization.RegisterTranslation();
		ModSettings.InitModConfig();

		ModManager.SetCredits(MyPluginInfo.PLUGIN_GUID, MakeCredits());
		ModManager.SetSettings(MyPluginInfo.PLUGIN_GUID, ModSettings.settingBlock);

		ModManager.OnModSettignsRefresh += guid => { if (guid == MyPluginInfo.PLUGIN_GUID) ModSettings.RefreshEntries(); };
		ModManager.OnModSettignsCancel += guid => { if (guid == MyPluginInfo.PLUGIN_GUID) ModSettings.RefreshEntries(); };
		ModManager.OnModSettignsApply += guid => { if (guid == MyPluginInfo.PLUGIN_GUID) ModSettings.OnSettingsApply(); };
	}

	private static CreditBlock MakeCredits()
	{
		CreditBlock creditData = new();
		if (Plugin.assets)
			creditData.AddLogo(Plugin.assets.LoadAsset<Sprite>("PerformanceModLogo"));
		creditData.AddText(CreditBlock.FONT_SIZE.TITLE, "ARCHITECT'S PERFORMANCE MOD");
		creditData.AddSeparator();
		creditData.AddText(CreditBlock.FONT_SIZE.ROLE, "DEVELOPER");
		creditData.AddText(CreditBlock.FONT_SIZE.NORMAL, "Merilax");
		creditData.AddSeparator();
		creditData.AddText(CreditBlock.FONT_SIZE.ROLE, "SPECIAL THANKS");
		creditData.AddText(CreditBlock.FONT_SIZE.NORMAL, "Ara. - For being there in my worst times.");
		creditData.AddText(CreditBlock.FONT_SIZE.NORMAL, "CloudCluster - For taking care of the BreakArts Builders Discord.");
		creditData.AddText(CreditBlock.FONT_SIZE.NORMAL, "The Break Arts community - For making this possible.");
		creditData.AddSeparator();
		creditData.AddText(CreditBlock.FONT_SIZE.ROLE, "TRANSLATION CONTRIBUTORS");
		creditData.AddText(CreditBlock.FONT_SIZE.ROLE, "Japanese");
		creditData.AddText(CreditBlock.FONT_SIZE.NORMAL, "oresi1034");
		creditData.AddText(CreditBlock.FONT_SIZE.NORMAL, "Kai5378");
		return creditData;
	}
}