using System;
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
    public override void Load()
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;

        // Plugin startup logic
        Log = BepInEx.Logging.Logger.CreateLogSource("ArchPerform");
        Log.LogInfo($"Loading plugin patches...");

        config = Config;

        var harmony = Harmony.CreateAndPatchAll(typeof(PluginInitializer));
        harmony.PatchAll(typeof(Localization));
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
            Log.LogInfo("Saving mod configuration.");
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
            Log.LogInfo("Loading mod configuration.");
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

public class PluginInitializer
{
    public static TMP_FontAsset mainFont;
    public static Material mainFontMaterial;
    private static GameObject modSignature1;
    private static GameObject modSignature2;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Scene_MainMenu), nameof(Scene_MainMenu.Start))]
    public static void Initialize(ref Scene_MainMenu __instance)
    {
        Plugin.LogInfo($"Preparing mod...");



        Cursor.lockState = CursorLockMode.Confined;

        AddModSignature(ref __instance);

        Plugin.LogInfo($"Mod initialized.");
    }
    public static void AddModSignature(ref Scene_MainMenu __instance)
    {
        Transform introMenu = __instance.MainMenuRoot.transform.Find("StartMenu");
        Transform mainMenu = __instance.MainMenuRoot.transform.Find("MainMenu_All");

        mainFont = introMenu.GetChild(1).GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().font;
        mainFontMaterial = introMenu.GetChild(1).GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().fontSharedMaterial;

        GameObject signatureObj = new();
        RectTransform rect = signatureObj.AddComponent<RectTransform>();
        TextMeshProUGUI signature = signatureObj.AddComponent<TextMeshProUGUI>();
        signatureObj.transform.SetParent(introMenu);
        signature.name = "Mod Signature";
        signature.text = "Architect's Optimizations " + MyPluginInfo.PLUGIN_VERSION;
        signature.fontSize = 16f;
        signature.font = mainFont;
        signature.fontMaterial = mainFontMaterial;
        signature.transform.localScale = Vector3.one;

        rect.pivot = Vector2.zero;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(1, 0);
        rect.offsetMax = new Vector2(0, 25);
        rect.offsetMin = new Vector2(25, 0);

        GameObject clone = GameObject.Instantiate(signatureObj);
        RectTransform cloneRect = clone.GetComponent<RectTransform>();
        clone.transform.SetParent(mainMenu);
        clone.name = "Mod Signature";
        clone.transform.localScale = Vector3.one;

        cloneRect.pivot = Vector2.zero;
        cloneRect.anchorMin = Vector2.zero;
        cloneRect.anchorMax = new Vector2(1, 0);
        cloneRect.offsetMax = new Vector2(0, 25);
        cloneRect.offsetMin = new Vector2(25, 0);

        modSignature1 = signatureObj;
        modSignature2 = clone;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(SettingsManager), nameof(SettingsManager.SetResolution))]
    public static void RescaleUI()
    {
        Utils.RescaleUI(modSignature1);
        Utils.RescaleUI(modSignature2);
    }
}
