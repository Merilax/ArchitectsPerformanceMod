using HarmonyLib;

namespace BA3StandardMod;

public class GarageCameraPatch
{
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Scene_MainMenu), nameof(Scene_MainMenu.Designer_Design))]
    [HarmonyPatch(typeof(Scene_MainMenu), nameof(Scene_MainMenu.Diorama_OpenMachineDesign))]
    public static void IncreaseCameraConstraints(Scene_MainMenu __instance)
    {
        Plugin.Log.LogInfo("Increasing movement constraints for Design cameras...");
        // VC_Menu/Menu_FreeLookPivot
        var camera = __instance.MachineDesigner.freeCameraController;
        camera.maxDistance = 12;
        camera.maxHeight = 4.5f;
        camera.maxSquare = 3.5f;
    }
}