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
    public static ConfigFile config;
    public static CycleConfigEntry<bool> _myToggle;

    public override void Load()
    {
        Console.InputEncoding = Encoding.UTF8;
        Console.OutputEncoding = Encoding.UTF8;

        // Plugin startup logic
        Log = BepInEx.Logging.Logger.CreateLogSource("ArchPerformanceMod");
        Log.LogInfo($"Initializing plugin...");

        config = Config;

        var harmony = Harmony.CreateAndPatchAll(typeof(PluginInitializer));
        harmony.PatchAll(typeof(QualityLevelPatch));
        harmony.PatchAll(typeof(GarageCameraPatch));
        harmony.PatchAll(typeof(ModSettings));
        harmony.PatchAll(typeof(ModPerformance));

        Log.LogInfo($"Plugin is ready.");
    }
}

public class PluginInitializer
{
    private static TMP_FontAsset mainFont;
    private static Material mainFontMaterial;
    private static bool once = false;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Scene_MainMenu), nameof(Scene_MainMenu.Start))]
    public static void AddModSignature(ref Scene_MainMenu __instance)
    {
        if (once) return;

        Transform introMenu = __instance.MainMenuRoot.transform.Find("StartMenu");
        Transform mainMenu = __instance.MainMenuRoot.transform.Find("MainMenu_All");

        mainFont = introMenu.GetChild(1).GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().font;
        mainFontMaterial = introMenu.GetChild(1).GetChild(0).GetChild(0).GetComponent<TextMeshProUGUI>().fontSharedMaterial;

        GameObject signatureObj = new();
        RectTransform rect = signatureObj.AddComponent<RectTransform>();
        TextMeshProUGUI signature = signatureObj.AddComponent<TextMeshProUGUI>();
        signatureObj.transform.SetParent(introMenu);
        signature.text = "Architect's Optimizations " + MyPluginInfo.PLUGIN_VERSION;
        signature.fontSize = 16f;
        signature.font = mainFont;
        signature.fontMaterial = mainFontMaterial;

        rect.pivot = Vector2.zero;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(1, 0);
        rect.offsetMax = new Vector2(0, 25);
        rect.offsetMin = new Vector2(25, 0);

        GameObject clone = GameObject.Instantiate(signatureObj);
        RectTransform cloneRect = clone.GetComponent<RectTransform>();
        clone.transform.SetParent(mainMenu);
        cloneRect.pivot = Vector2.zero;
        cloneRect.anchorMin = Vector2.zero;
        cloneRect.anchorMax = new Vector2(1, 0);
        cloneRect.offsetMax = new Vector2(0, 25);
        cloneRect.offsetMin = new Vector2(25, 0);

        once = true;
    }
}
