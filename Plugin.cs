using System;
using System.Text;
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
    private static readonly bool verboseLogging = true;
    public static ConfigFile config;
    public override void Load()
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;

        // Plugin startup logic
        Log = BepInEx.Logging.Logger.CreateLogSource("ArchPerform");
        Log.LogInfo($"Loading plugin patches...");

        config = Config;

        var harmony = Harmony.CreateAndPatchAll(typeof(PluginInitializer));
        harmony.PatchAll(typeof(QualityLevelPatch));
        harmony.PatchAll(typeof(GarageCameraPatch));
        harmony.PatchAll(typeof(ModSettings));
        harmony.PatchAll(typeof(ModPerformance));
        harmony.PatchAll(typeof(ModGameplay));
        harmony.PatchAll(typeof(DioramaEnvPatch));
        // harmony.PatchAll(typeof(TestPatch));

        Log.LogInfo($"Done.");
    }

    public static void LogInfo(object data) => Log.LogInfo(data);
    public static void LogDebug(object data)
    {
        if (verboseLogging) Log.LogInfo(data);
    }
}

public class PluginInitializer
{
    private static TMP_FontAsset mainFont;
    private static Material mainFontMaterial;
    private static GameObject modSignature1;
    private static GameObject modSignature2;
    private static bool init = false;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Scene_MainMenu), nameof(Scene_MainMenu.Start))]
    public static void Initialize(ref Scene_MainMenu __instance)
    {
        // if (init) return;
        Plugin.LogInfo($"Preparing mod...");

        Cursor.lockState = CursorLockMode.Confined;

        AddModSignature(ref __instance);

        init = true;
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
