using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using UnityEngine.VFX;

namespace ArchPerformanceMod;

public class ModPerformance
{
	public static GameObject GetEnvironmentObj()
	{
		Scene diorama = SceneManager.GetSceneByName("Georama");
		if (diorama.IsValid())
		{
			return diorama.GetRootGameObjects().First(item => item.name == "Env");
		}
		else return SceneManager.GetSceneByName("MainMenu").GetRootGameObjects().First(item => item.name == "Enviroment");
	}
	public static Volume GetPostProcessVolume()
	{
		return GetEnvironmentObj().transform.Find("Vol").Find("PostProcess").GetComponent<Volume>();
	}
	public static GameObject GetPlayersObj()
	{
		return SceneManager.GetSceneByName("MainMenu").GetRootGameObjects().First(item => item.name == "Players");
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(GeoramaSystem), nameof(GeoramaSystem.Start))]
	public static void ApplyConfigInDiorama()
	{
		SetGlobalIllumination(Config.UseGI);
		// SetGlobalIllumination(ModSettings.confGlobalIllumination.Value);
		// SetReflections(ModSettings.confReflections.Value);
		SetChromaAberration(ModSettings.confChromaAberration.Value);
		SetVignette(ModSettings.confVignette.Value);
		SetShadowTones(ModSettings.confShadowTones.Value);
		// SetDockLights(ModSettings.confDockLights.Value);
		SetAntialiasing(ModSettings.confAntialiasing.Value);
		SetShadowQuality(ModSettings.confShadowQuality.Value);
		// SetMachineParticles(ModSettings.confMachineParticles.Value);
		// SetExtendCameraRenderDistance(ModSettings.confCameraClipPlane.Value);

		HDRPReflectionHelper.ApplyPipelineSupportFlagBatch( // true = disabled
			ModSettings.confReflections.Value != ModSettings.StrengthEnum.def, // SSR
			ModSettings.confAmbientOcclusion.Value == ModSettings.ToggleEnum.off, // Ambient Occlusion
			ModSettings.confVolumetrics.Value == ModSettings.DioramaOnlyEnum.off, // Volumetrics // Disabled if not on
			true, // Vol Clouds
			true, // Subsurface Scattering
			true, // Decals (already disabled by default)
			ModSettings.confMachineParticles.Value != ModSettings.QuantityEnum.full, // Distortion
			ModSettings.confReflections.Value != ModSettings.StrengthEnum.def, // SSR Transparency
			ModSettings.confReflections.Value != ModSettings.StrengthEnum.def, // Screen Space Lens Flare
			ModSettings.confReflections.Value != ModSettings.StrengthEnum.def  // Data Driven Lens Flare
		);
	}

	public static void SetGlobalIllumination(bool toSet)
	{
		var scene = SceneManager.GetSceneByName("Georama");
		if (scene.IsValid())
		{
			GetPostProcessVolume().profile.TryGet(out GlobalIllumination ilum);
			ilum?.active = toSet;

			GetEnvironmentObj().transform.Find("Vol").Find("Enviroments").GetComponent<Volume>().profile.TryGet(out VisualEnvironment visualEnv);
			visualEnv?.skyAmbientMode.value = toSet ? SkyAmbientMode.Dynamic : SkyAmbientMode.Static;
		}
	}

	public static void SetReflections(ModSettings.StrengthEnum toSet)
	{
		if (SceneManager.GetActiveScene().name == "Georama") return;

		GameObject[] rootObjs = SceneManager.GetSceneByName("MainMenu").GetRootGameObjects();
		Transform postProcess = GetEnvironmentObj().transform.Find("Vol").Find("PostProcess");
		GameObject dockProbes = rootObjs.First(item => item.name == "World_PlayerDock").transform.Find("StageV3").Find("Stage_v3").Find("Probe").gameObject;
		ReflectionProbe designProbe = rootObjs.First(item => item.name == "World_DesignOnly").transform.Find("Reflection Probe").GetComponent<ReflectionProbe>();
		ReflectionProbe testTrackProbe = rootObjs.First(item => item.name == "World_TestPlay").transform.Find("Global Volume").GetComponent<ReflectionProbe>();

		dockProbes.active = toSet != ModSettings.StrengthEnum.aggresive;
		designProbe.enabled = toSet != ModSettings.StrengthEnum.aggresive;
		testTrackProbe.enabled = toSet != ModSettings.StrengthEnum.aggresive;
		// postProcess.GetComponent<Volume>().profile.components[3].active = toSet == ModSettings.GenericStrengthEnum.on || toSet == ModSettings.GenericStrengthEnum.raceOnly;
		// postProcess.GetComponent<Volume>().profile.components[13].active = toSet == ModSettings.GenericStrengthEnum.on || toSet == ModSettings.GenericStrengthEnum.raceOnly; // Screen SSR flare

		if (SceneManager.GetActiveScene().name != "MainMenu") ApplyReflectionsInRace();
	}

	public static void SetChromaAberration(ModSettings.ToggleEnum toSet)
	{
		GetPostProcessVolume().profile.TryGet(out ChromaticAberration chroma);
		chroma?.active = toSet == ModSettings.ToggleEnum.on;
	}

	public static void SetVignette(ModSettings.ToggleEnum toSet)
	{
		GetEnvironmentObj().transform.Find("Vol").Find("PostProcess").GetComponent<Volume>().profile.TryGet(out Vignette vignette);
		vignette?.active = toSet == ModSettings.ToggleEnum.on;
	}

	public static void SetShadowTones(ModSettings.ToggleEnum toSet)
	{
		GetEnvironmentObj().transform.Find("Vol").Find("PostProcess").GetComponent<Volume>().profile.TryGet(out Tonemapping tonemapping);
		tonemapping?.active = toSet == ModSettings.ToggleEnum.on;
	}

	public static void SetDockLights(ModSettings.ToggleEnum toSet)
	{
		if (SceneManager.GetActiveScene().name == "Georama") return;

		bool input = toSet == ModSettings.ToggleEnum.on;

		GameObject dockRootObj = SceneManager.GetSceneByName("MainMenu").GetRootGameObjects().First(item => item.name == "World_PlayerDock");

		GameObject dockMechLights = dockRootObj.transform.Find("StageV3").Find("Stage_v3").Find("Stage_v3_Base:front").Find("Stage_v3_Base:stage").gameObject;

		dockMechLights.transform.Find("SpotLight").GetChild(0).GetChild(0).Find("Spot Light").GetComponent<Light>().shadows = input ? LightShadows.Soft : LightShadows.None;
		dockMechLights.transform.Find("SpotLight (1)").GetChild(0).GetChild(0).Find("Spot Light").GetComponent<Light>().shadows = input ? LightShadows.Soft : LightShadows.None;
		dockMechLights.transform.Find("SpotLight (2)").GetChild(0).GetChild(0).Find("Spot Light").GetComponent<Light>().shadows = input ? LightShadows.Soft : LightShadows.None;
	}

	public static void SetAntialiasing(ModSettings.AntialiasingEnum toSet)
	{
		Camera camera = null;
		HDAdditionalCameraData cameraData = null;
		Scene diorama = SceneManager.GetSceneByName("Georama");
		if (diorama.IsValid())
		{
			var cameraObj = diorama.GetRootGameObjects().First(item => item.name == "Camera");
			camera = cameraObj.GetComponent<Camera>();
			cameraData = cameraObj.GetComponent<HDAdditionalCameraData>();
		}
		else
		{
			var cameraObj = SceneManager.GetSceneByName("MainMenu").GetRootGameObjects().First(item => item.name == "Main Camera");
			camera = cameraObj.GetComponent<Camera>();
			cameraData = cameraObj.GetComponent<HDAdditionalCameraData>();
		}

		switch (toSet)
		{
			case ModSettings.AntialiasingEnum.FXAA:
				cameraData.antialiasing = HDAdditionalCameraData.AntialiasingMode.FastApproximateAntialiasing; break;
			case ModSettings.AntialiasingEnum.MSAA:
				camera.allowMSAA = true;
				cameraData.antialiasing = HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing; break;
			case ModSettings.AntialiasingEnum.TAA:
				cameraData.antialiasing = HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing; break;
			case ModSettings.AntialiasingEnum.off:
			default:
				cameraData.antialiasing = HDAdditionalCameraData.AntialiasingMode.None; break;
		}
	}

	public static void SetShadowQuality(ModSettings.StrengthEnum toSet)
	{
		switch (toSet)
		{
			case ModSettings.StrengthEnum.def:
				HDRPReflectionHelper.ApplyShadowInitParams(256, 4096, 2048, 1);
				break;
			case ModSettings.StrengthEnum.optimized:
				HDRPReflectionHelper.ApplyShadowInitParams(64, 1024, 1024, 1);
				break;
			case ModSettings.StrengthEnum.aggresive:
				HDRPReflectionHelper.ApplyShadowInitParams(32, 512, 512, 1);
				break;
		}
	}

	public static void SetMachineParticles(ModSettings.QuantityEnum toSet)
	{
		if (SceneManager.GetActiveScene().name == "Georama") return;

		Transform playerVFX = GetPlayersObj()?.transform.Find("MyPlayer").Find("Effects");
		if (!playerVFX) return;

		SetSpecificMachineParticles(playerVFX, toSet, true);

		ApplyParticlesInRace();
	}

	public static void SetExtendCameraRenderDistance(ModSettings.ToggleEnum toSet)
	{
		Camera camera = null;
		// HDAdditionalCameraData cameraData = null;
		Scene diorama = SceneManager.GetSceneByName("Georama");
		if (diorama.IsValid())
		{
			var cameraObj = diorama.GetRootGameObjects().First(item => item.name == "Camera");
			camera = cameraObj.GetComponent<Camera>();
			// cameraData = cameraObj.GetComponent<HDAdditionalCameraData>();
		}
		else
		{
			var cameraObj = SceneManager.GetSceneByName("MainMenu").GetRootGameObjects().First(item => item.name == "Main Camera");
			camera = cameraObj.GetComponent<Camera>();
			// cameraData = cameraObj.GetComponent<HDAdditionalCameraData>();
		}

		camera.farClipPlane = toSet == ModSettings.ToggleEnum.off ? 2000 : 20000; 
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(StageInfo), nameof(StageInfo.SetupStage))]
	public static void ApplyReflectionsInRace()
	{
		Scene scene = SceneManager.GetActiveScene();
		GameObject[] rootObjs = scene.GetRootGameObjects();

		switch (scene.name)
		{
			case "Grass_01":
				var track = rootObjs.First(item => item.name == "Circuit_01").transform;
				switch (ModSettings.confReflections.Value)
				{
					case ModSettings.StrengthEnum.def:
					case ModSettings.StrengthEnum.optimized:
						rootObjs.First(item => item.name == "Global Volume").transform.GetComponent<Volume>().profile.components[6].active = true;
						rootObjs.First(item => item.name == "Reflection Probe").transform.GetComponent<ReflectionProbe>().enabled = true;
						break;
					case ModSettings.StrengthEnum.aggresive:
						rootObjs.First(item => item.name == "Global Volume").transform.GetComponent<Volume>().profile.components[6].active = false;
						rootObjs.First(item => item.name == "Reflection Probe").transform.GetComponent<ReflectionProbe>().enabled = false;
						break;
				}
				break;
			case "Grass_02":
				switch (ModSettings.confReflections.Value)
				{
					case ModSettings.StrengthEnum.def:
					case ModSettings.StrengthEnum.optimized:
						rootObjs.First(item => item.name == "Global Volume").transform.GetComponent<Volume>().profile.components[7].active = true;
						rootObjs.First(item => item.name == "Global Reflection Probe").transform.GetComponent<ReflectionProbe>().enabled = true;
						break;
					case ModSettings.StrengthEnum.aggresive:
						rootObjs.First(item => item.name == "Global Volume").transform.GetComponent<Volume>().profile.components[7].active = false;
						rootObjs.First(item => item.name == "Global Reflection Probe").transform.GetComponent<ReflectionProbe>().enabled = false;
						break;
				}
				break;

			case "Canyon_01":
			case "Canyon_02":
			case "Cyber_01":
			case "Cyber_02":
			case "Sea_01":
			case "Sea_02":
			case "Sky_01":
			case "Moon_01":
				switch (ModSettings.confReflections.Value)
				{
					case ModSettings.StrengthEnum.def:
					case ModSettings.StrengthEnum.optimized:
						rootObjs.First(item => item.name == "Lightings").transform.Find("Global Reflection Probe").GetComponent<ReflectionProbe>().enabled = true;
						rootObjs.First(item => item.name == "Probes").active = true;
						break;
					case ModSettings.StrengthEnum.aggresive:
						rootObjs.First(item => item.name == "Lightings").transform.Find("Global Reflection Probe").GetComponent<ReflectionProbe>().enabled = false;
						rootObjs.First(item => item.name == "Probes").active = false;
						break;
				}
				break;
		}
	}

	[HarmonyPrefix]
	[HarmonyPatch(typeof(ProbeManager), nameof(ProbeManager.SafeSetActive))]
	// Toggles race track probes as needed.
	public static void ToggleTrackProbes(ref ProbeManager.ProbeEntry e, ref bool active)
	{
		if (SceneManager.GetActiveScene().name != "MainMenu" && SceneManager.GetActiveScene().name != "Settings")
			active = ModSettings.confReflections.Value != ModSettings.StrengthEnum.aggresive;
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(StageInfo), nameof(StageInfo.SetupStage))]
	public static void ApplyParticlesInRace()
	{
		Transform players = GetPlayersObj().transform;

		for (int i = 0; i < players.childCount; i++)
		{
			Transform player = players.GetChild(i);
			if (player.name.Contains("Debug")) continue;
			if (!player.gameObject.active) continue;

			Transform playerVFX = player.Find("Effects");

			var toSet = ModSettings.confMachineParticles.Value;
			bool myPlayer = false;

			if (player.name.Contains("MyPlayer"))
				myPlayer = true;
			SetSpecificMachineParticles(playerVFX, toSet, myPlayer);
		}
	}

	private static void SetSpecificMachineParticles(Transform playerVFX, ModSettings.QuantityEnum toSet, bool isMainPlayer = false)
	{
		if (isMainPlayer)
		{
			GameObject worldFragments = playerVFX.Find("WorldFragments").gameObject;
			worldFragments.active = toSet == ModSettings.QuantityEnum.full;
		}
		GameObject warpVFX = playerVFX.Find("WarpVFX").gameObject;
		GameObject sandEffects = playerVFX.Find("SandEffects").gameObject;
		GameObject waterEffects = playerVFX.Find("WaterEffects").gameObject;
		GameObject waterInteraction = playerVFX.Find("WaterInteraction").gameObject;
		GameObject breakEffects = playerVFX.Find("Break").gameObject;
		GameObject steamEffects = playerVFX.Find("SteamVFX").gameObject;
		GameObject structureEffects = playerVFX.Find("StructureEffects").gameObject;
		GameObject floatParticles = playerVFX.Find("Float_Around_Particle").gameObject;
		GameObject velocityParticles = playerVFX.Find("VelocityParticle").gameObject;

		warpVFX.active = toSet == ModSettings.QuantityEnum.full;
		sandEffects.active = toSet == ModSettings.QuantityEnum.full;
		waterEffects.active = toSet == ModSettings.QuantityEnum.full;
		waterInteraction.active = toSet == ModSettings.QuantityEnum.full;
		steamEffects.active = toSet == ModSettings.QuantityEnum.full || toSet == ModSettings.QuantityEnum.reduced;
		structureEffects.active = toSet == ModSettings.QuantityEnum.full || toSet == ModSettings.QuantityEnum.reduced;

		breakEffects.active = toSet == ModSettings.QuantityEnum.full || toSet == ModSettings.QuantityEnum.reduced;
		for (int i = 0; i < breakEffects.transform.childCount; i++)
			breakEffects.transform.GetChild(i).gameObject.active = toSet == ModSettings.QuantityEnum.full;

		floatParticles.active = toSet == ModSettings.QuantityEnum.full || toSet == ModSettings.QuantityEnum.reduced;
		floatParticles.transform.GetChild(0).gameObject.active = toSet == ModSettings.QuantityEnum.full;
		floatParticles.transform.GetChild(2).gameObject.active = toSet == ModSettings.QuantityEnum.full;

		velocityParticles.active = toSet == ModSettings.QuantityEnum.full || toSet == ModSettings.QuantityEnum.reduced;
		velocityParticles.transform.GetChild(0).gameObject.active = toSet == ModSettings.QuantityEnum.full;
		velocityParticles.transform.GetChild(3).gameObject.active = toSet == ModSettings.QuantityEnum.full;
		velocityParticles.transform.GetChild(4).gameObject.active = toSet == ModSettings.QuantityEnum.full;

		GameObject mechBody = playerVFX.parent.Find("ArchitectureComplex").gameObject;
		SetModuleParticles(mechBody.transform, toSet != ModSettings.QuantityEnum.none);
	}

	private static void SetModuleParticles(Transform xform, bool toSet)
	{
		if (xform.name.Contains("ConnectorMarker") || xform.name.Contains("Col"))
			return;

		if (xform.name.Contains("EmitterEffects"))
		{
			for (int i = 0; i < xform.childCount; i++)
			{
				xform.GetChild(i).GetComponent<VisualEffect>()?.enabled = toSet;
			}
			return;
		}
		if (xform.name.Contains("BoostEffects_Round") || xform.name.Contains("BoostEffects_Detonator"))
		{
			xform.GetComponent<VisualEffect>()?.enabled = toSet;
			return;
		}

		for (int i = 0; i < xform.childCount; i++)
		{
			SetModuleParticles(xform.GetChild(i), toSet);
		}
	}
}
