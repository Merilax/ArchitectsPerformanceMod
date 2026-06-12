using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.DebugUI;

namespace ArchPerformanceMod;

public class GarageCameraPatch
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(Scene_MainMenu), nameof(Scene_MainMenu.Designer_Design))]
	[HarmonyPatch(typeof(Scene_MainMenu), nameof(Scene_MainMenu.Diorama_OpenMachineDesign))]
	public static void IncreaseCameraConstraints(Scene_MainMenu __instance)
	{
		// VC_Menu/Menu_FreeLookPivot
		var camera = __instance.MachineDesigner.freeCameraController;
		camera.maxDistance = 12;
		camera.maxHeight = 4.5f;
		camera.maxSquare = 3.5f;
	}
}

public class DioramaEnvPatch
{
	private static bool allowInteraction = false;
	private static bool once = false;
	private static Fog fog;
	private static HDAdditionalCameraData cameraData;
	private static Material groundMat;

	[HarmonyPostfix]
	[HarmonyPatch(typeof(GeoramaSystem), nameof(GeoramaSystem.Start))]
	public static void PreInit(GeoramaSystem __instance)
	{
		if (!once) BridgedSceneManager.OnSceneLoadComplete.AddListener((Action)(() => ApplyConfiguration(__instance)));
		
		once = true;
	}

	public static void ApplyConfiguration(GeoramaSystem __instance)
	{
		if (SceneManager.GetActiveScene().name != "Georama") return;

		HDRPReflectionHelper.ApplyPipelineSupportFlagBatch( // true = disabled
			ModSettings.confReflections.Value != ModSettings.StrengthEnum.def, // SSR
			ModSettings.confAmbientOcclusion.Value == ModSettings.ToggleEnum.off, // Ambient Occlusion
			ModSettings.confVolumetrics.Value == ModSettings.DioramaOnlyEnum.off, // Volumetrics // Only disabled if off
			true, // Vol Clouds
			true, // Subsurface Scattering
			true, // Decals (already disabled by default)
			ModSettings.confMachineParticles.Value != ModSettings.QuantityEnum.full, // Distortion
			ModSettings.confReflections.Value != ModSettings.StrengthEnum.def, // SSR Transparency
			ModSettings.confReflections.Value != ModSettings.StrengthEnum.def, // Screen Space Lens Flare
			ModSettings.confReflections.Value != ModSettings.StrengthEnum.def  // Data Driven Lens Flare
		);

		Scene scene = SceneManager.GetActiveScene();

		cameraData = scene.GetRootGameObjects().First(item => item.name == "Camera").GetComponent<HDAdditionalCameraData>();

		scene.GetRootGameObjects().First(item => item.name == "Env").transform.Find("Vol").Find("Enviroments").GetComponent<Volume>().profile.TryGet(out Fog fogComp);
		fog = fogComp;

		groundMat = scene.GetRootGameObjects().First(item => item.name == "Ground").transform.GetChild(0).GetComponent<MeshRenderer>().material;

		if (ModSettings.confVolumetrics.Value == ModSettings.DioramaOnlyEnum.off)
		{
			fog.active = false;
			cameraData.backgroundColorHDR = new Color(.55f, .45f, .45f, 1);
		}

		// CreateUI();

		allowInteraction = true;
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(GeoramaSystem), nameof(GeoramaSystem.ReturnToMainMenu))]
	public static void OnDestroy()
	{
		Plugin.LogInfo("Return");
		allowInteraction = false;
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(GeoramaSystem), nameof(GeoramaSystem.Update))]
	public static void OnUpdate()
	{
		if (Keyboard.current.hKey.wasPressedThisFrame)
		{
			GameObject canvas = SceneManager.GetActiveScene().GetRootGameObjects().First(item => item.name == "Canvas");
			canvas.active = !canvas.active;
			ToggleTerrain(canvas.active);
			ToggleGround(canvas.active);
			ToggleFrame(canvas.active);
			ToggleCaptions(canvas.active);
		}
	}

	

	public static void ToggleTerrain(bool toSet)
	{
		SceneManager.GetActiveScene().GetRootGameObjects().First(e => e.name == "Ground").transform.Find("Terrains").gameObject.active = toSet;
	}
	public static void ToggleGround(bool toSet)
	{
		SceneManager.GetActiveScene().GetRootGameObjects().First(e => e.name == "Ground").gameObject.active = toSet;
	}
	public static void ToggleFrame(bool toSet)
	{
		SceneManager.GetActiveScene().GetRootGameObjects().First(e => e.name == "Frame").gameObject.active = toSet;
	}
	public static void ToggleCaptions(bool toSet)
	{
		SceneManager.GetActiveScene().GetRootGameObjects().First(e => e.name == "Caption").gameObject.active = toSet;
	}

	
}