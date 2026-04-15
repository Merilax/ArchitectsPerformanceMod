using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace BA3StandardMod;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("BREAK ARTS III.exe")]

public class Plugin : BasePlugin
{
    internal static new ManualLogSource Log;
    
    public static ConfigFile config;

    public static CycleConfigEntry<bool> _myToggle;
    
    public override void Load()
    {
        // Plugin startup logic
        Log = BepInEx.Logging.Logger.CreateLogSource("Standard MOD");
        Log.LogInfo($"Initializing plugin {MyPluginInfo.PLUGIN_GUID}...");
        
        config = Config;

        var harmony = Harmony.CreateAndPatchAll(typeof(PluginInitializer));
        harmony.PatchAll(typeof(GarageCameraPatch));
        harmony.PatchAll(typeof(ModSettings));

        Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is ready.");
    }
}

public class PluginInitializer
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Scene_MainMenu), nameof(Scene_MainMenu.Start))]
    [HarmonyPatch(typeof(TranslationList), nameof(TranslationList.ChangeLanguage))]
    public static void ApplyModdedLocalization()
    {
        Localization.SetLocale(Config.Language);
    }
}
