using System.Linq;
using Crest;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using UnityEngine.VFX;

namespace PerformanceMod;

public class ModPerformance
{
	public static GameObject GetEnvironmentObj()
	{
		return SceneManager.GetSceneByName("MainMenu").GetRootGameObjects().First(item => item.name == "Enviroment");
	}
	public static GameObject GetPlayersObj()
	{
		return SceneManager.GetSceneByName("MainMenu").GetRootGameObjects().First(item => item.name == "Players");
	}

	public static void SetGlobalIllumination(ModSettings.GenericToggleEnum toSet)
	{
		bool input = toSet == ModSettings.GenericToggleEnum.on;

		GetEnvironmentObj().transform.Find("Vol").Find("PostProcess").GetComponent<Volume>().profile.components[12].active = input;

		GameObject SkyFogObj = GetEnvironmentObj().transform.Find("Vol").Find("Sky and Fog Volume").gameObject;
		VisualEnvironment visualEnv;
		SkyFogObj.GetComponent<Volume>().profile.TryGet(out visualEnv);
		visualEnv.skyAmbientMode.value = input ? SkyAmbientMode.Dynamic : SkyAmbientMode.Static;
	}

	public static void SetReflections(ModSettings.GenericRaceOnlyEnum toSet)
	{
		GameObject[] rootObjs = SceneManager.GetSceneByName("MainMenu").GetRootGameObjects();
		Transform postProcess = GetEnvironmentObj().transform.Find("Vol").Find("PostProcess");
		GameObject dockProbes = rootObjs.First(item => item.name == "World_PlayerDock").transform.Find("StageV3").Find("Stage_v3").Find("Probe").gameObject;
		ReflectionProbe designProbe = rootObjs.First(item => item.name == "World_DesignOnly").transform.Find("Reflection Probe").GetComponent<ReflectionProbe>();
		ReflectionProbe testTrackProbe = rootObjs.First(item => item.name == "World_TestPlay").transform.Find("Global Volume").GetComponent<ReflectionProbe>();

		dockProbes.active = toSet == ModSettings.GenericRaceOnlyEnum.on;
		designProbe.enabled = toSet == ModSettings.GenericRaceOnlyEnum.on || toSet == ModSettings.GenericRaceOnlyEnum.raceOnly;
		testTrackProbe.enabled = toSet == ModSettings.GenericRaceOnlyEnum.on || toSet == ModSettings.GenericRaceOnlyEnum.raceOnly;
		postProcess.GetComponent<Volume>().profile.components[3].active = toSet == ModSettings.GenericRaceOnlyEnum.on || toSet == ModSettings.GenericRaceOnlyEnum.raceOnly;
		postProcess.GetComponent<Volume>().profile.components[13].active = toSet == ModSettings.GenericRaceOnlyEnum.on || toSet == ModSettings.GenericRaceOnlyEnum.raceOnly; // Screen SSR flare

		if (SceneManager.GetActiveScene().name != "MainMenu") ApplyReflectionsInRace();
	}

	public static void SetChromaAberration(ModSettings.GenericToggleEnum toSet)
	{
		GetEnvironmentObj().transform.Find("Vol").Find("PostProcess").GetComponent<Volume>().profile.components[1].active = toSet == ModSettings.GenericToggleEnum.on;
	}

	public static void SetAmbientOcclusion(ModSettings.GenericToggleEnum toSet)
	{
		GetEnvironmentObj().transform.Find("Vol").Find("PostProcess").GetComponent<Volume>().profile.components[2].active = toSet == ModSettings.GenericToggleEnum.on;
	}

	public static void SetVignette(ModSettings.GenericToggleEnum toSet)
	{
		GetEnvironmentObj().transform.Find("Vol").Find("PostProcess").GetComponent<Volume>().profile.components[4].active = toSet == ModSettings.GenericToggleEnum.on;
	}

	public static void SetShadowTones(ModSettings.GenericToggleEnum toSet)
	{
		GetEnvironmentObj().transform.Find("Vol").Find("PostProcess").GetComponent<Volume>().profile.components[6].active = toSet == ModSettings.GenericToggleEnum.on;
	}

	public static void SetDockLights(ModSettings.GenericToggleEnum toSet)
	{
		bool input = toSet == ModSettings.GenericToggleEnum.on;

		GameObject dockRootObj = SceneManager.GetSceneByName("MainMenu").GetRootGameObjects().First(item => item.name == "World_PlayerDock");

		GameObject dockMechLights = dockRootObj.transform.Find("StageV3").Find("Stage_v3").Find("Stage_v3_Base:front").Find("Stage_v3_Base:stage").gameObject;

		dockMechLights.transform.Find("SpotLight").GetChild(0).GetChild(0).Find("Spot Light").GetComponent<Light>().shadows = input ? LightShadows.Soft : LightShadows.None;
		dockMechLights.transform.Find("SpotLight (1)").GetChild(0).GetChild(0).Find("Spot Light").GetComponent<Light>().shadows = input ? LightShadows.Soft : LightShadows.None;
		dockMechLights.transform.Find("SpotLight (2)").GetChild(0).GetChild(0).Find("Spot Light").GetComponent<Light>().shadows = input ? LightShadows.Soft : LightShadows.None;

		// GameObject dockLights = dockRootObj.transform.Find("StageV3").Find("Stage_v3").Find("Lighting").gameObject;
		// dockLights.transform.Find("topSpots")
	}

	public static void SetAntialiasing(ModSettings.AntialiasingEnum toSet)
	{
		Camera camera = SceneManager.GetSceneByName("MainMenu").GetRootGameObjects().First(item => item.name == "Main Camera").GetComponent<Camera>();
		HDAdditionalCameraData cameraData = SceneManager.GetSceneByName("MainMenu").GetRootGameObjects().First(item => item.name == "Main Camera").GetComponent<HDAdditionalCameraData>();
		camera.allowMSAA = true;
		switch (toSet)
		{
			case ModSettings.AntialiasingEnum.FXAA:
				cameraData.antialiasing = HDAdditionalCameraData.AntialiasingMode.FastApproximateAntialiasing; break;
			case ModSettings.AntialiasingEnum.MSAA:
				cameraData.antialiasing = HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing; break;
			case ModSettings.AntialiasingEnum.TAA:
				cameraData.antialiasing = HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing; break;
			case ModSettings.AntialiasingEnum.off:
			default:
				cameraData.antialiasing = HDAdditionalCameraData.AntialiasingMode.None; break;
		}
	}

	public static void SetMachineParticles(ModSettings.GenericQuantityEnum toSet)
	{
		Transform playerVFX = GetPlayersObj()?.transform.Find("MyPlayer").Find("Effects");
		if (!playerVFX) return;

		SetSpecificMachineParticles(playerVFX, toSet, true);

		// Not really a machine particle but oh well.
		// GetEnvironmentObj().transform.Find("Vol").Find("PostProcess").GetComponent<Volume>().profile.components[2].active = toSet == ModSettings.GenericQuantityEnum.full || toSet == ModSettings.GenericQuantityEnum.reduced;

		ApplyParticlesInRace();
	}

	// public static void ReduceOceanQuality(ModSettings.GenericToggleEnum toSet)
	// {
	// 	if (toSet == ModSettings.GenericToggleEnum.off)
	// 	{

	// 	}
	// 	else
	// 	{

	// 		SettingsManager.SetOceanQuality();
	// 	}
	// }
	// [HarmonyPostfix]
	// [HarmonyPatch(typeof(StageInfo), nameof(StageInfo.SetupStage))]
	// public static void ApplyOceanQuality()
	// {
	// 	bool input = ModSettings.confReduceOcean.Value == ModSettings.GenericToggleEnum.off;

	// 	GameObject oceanRoot = SceneManager.GetActiveScene().GetRootGameObjects().First(e => e.name == "Ocean");
	// 	OceanRenderer oceanTier1 = oceanRoot.transform.Find("Ocean_Tier1").GetComponent<OceanRenderer>();
	// 	OceanRenderer oceanTier2 = oceanRoot.transform.Find("Ocean_Tier2").GetComponent<OceanRenderer>();
	// 	OceanRenderer oceanTier3 = oceanRoot.transform.Find("Ocean_Tier3").GetComponent<OceanRenderer>();
	// 	ShapeFFT waveShaper = oceanRoot.transform.Find("wave").GetComponent<ShapeFFT>();
		
	// 	oceanTier3._createDynamicWaveSim = input;
	// 	oceanTier3._createFoamSim = input;
	// 	oceanTier3._createSeaFloorDepthData = input;
	// 	oceanTier3._geometryDownSampleFactor = input ? 2 : 4;
	// 	oceanTier3._lodCount = input ? 6 : 1;
	// 	// oceanTier3._maxScale = input ? 256 : 256;
	// 	oceanTier3._minScale = input ? 8 : 64;

	// 	oceanTier2._createDynamicWaveSim = input;
	// 	oceanTier2._createFoamSim = input;
	// 	oceanTier2._createSeaFloorDepthData = input;
	// 	oceanTier2._geometryDownSampleFactor = input ? 4 : 8;
	// 	// oceanTier2._maxScale = input ? 256 : 256;
	// 	oceanTier2._lodCount = input ? 32 : 256;

	// 	oceanTier1._geometryDownSampleFactor = input ? 4 : 16;
	// 	oceanTier1._maxScale = input ? 256 : 512;
	// 	oceanTier1._lodCount = input ? 64 : 512;

	// 	waveShaper.enabled = input;
	// }

	[HarmonyPostfix]
	[HarmonyPatch(typeof(StageInfo), nameof(StageInfo.SetupStage))]
	public static void ApplyReflectionsInRace()
	{
		Scene scene = SceneManager.GetActiveScene();
		GameObject[] rootObjs = scene.GetRootGameObjects();
		// ReflectionProbe segmentProbe;

		switch (scene.name)
		{
			case "Grass_01":
				var track = rootObjs.First(item => item.name == "Circuit_01").transform;
				switch (ModSettings.confReflections.Value)
				{
					case ModSettings.GenericRaceOnlyEnum.on:
					case ModSettings.GenericRaceOnlyEnum.raceOnly:
						rootObjs.First(item => item.name == "Global Volume").transform.GetComponent<Volume>().profile.components[6].active = true;
						rootObjs.First(item => item.name == "Reflection Probe").transform.GetComponent<ReflectionProbe>().enabled = true;
						break;
					case ModSettings.GenericRaceOnlyEnum.off:
						rootObjs.First(item => item.name == "Global Volume").transform.GetComponent<Volume>().profile.components[6].active = false;
						rootObjs.First(item => item.name == "Reflection Probe").transform.GetComponent<ReflectionProbe>().enabled = false;
						// Track probes turn themselves on by proximity.
						// for (int i = 0; i < track.childCount; i++)
						// {
						// 	track.GetChild(i).TryGetComponent<ReflectionProbe>(out segmentProbe);
						// 	if (segmentProbe)
						// 		segmentProbe.enabled = false;
						// 	segmentProbe = null;
						// }
						break;
				}
				break;
			case "Grass_02":
				switch (ModSettings.confReflections.Value)
				{
					case ModSettings.GenericRaceOnlyEnum.on:
					case ModSettings.GenericRaceOnlyEnum.raceOnly:
						rootObjs.First(item => item.name == "Global Volume").transform.GetComponent<Volume>().profile.components[7].active = true;
						rootObjs.First(item => item.name == "Global Reflection Probe").transform.GetComponent<ReflectionProbe>().enabled = true;
						break;
					case ModSettings.GenericRaceOnlyEnum.off:
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
					case ModSettings.GenericRaceOnlyEnum.on:
					case ModSettings.GenericRaceOnlyEnum.raceOnly:
						rootObjs.First(item => item.name == "Lightings").transform.Find("Global Reflection Probe").GetComponent<ReflectionProbe>().enabled = true;
						rootObjs.First(item => item.name == "Probes").active = true;
						break;
					case ModSettings.GenericRaceOnlyEnum.off:
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
			active = ModSettings.confReflections.Value == ModSettings.GenericRaceOnlyEnum.on || ModSettings.confReflections.Value == ModSettings.GenericRaceOnlyEnum.raceOnly;
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(StageInfo), nameof(StageInfo.SetupStage))]
	// [HarmonyPatch(typeof(MachineDesignerMain), nameof(MachineDesignerMain.StartTestPlay))]
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

	// [HarmonyPostfix]
	// [HarmonyPatch(typeof(MachineDesignerMain), nameof(MachineDesignerMain.OpenDesigner))]
	// [HarmonyPatch(typeof(MachineDesignerMain), nameof(MachineDesignerMain.EndTestPlay))]
	// [HarmonyPatch(typeof(MachineDesignerMain), nameof(MachineDesignerMain.EndTestPlayByMenu))]
	// public static void ApplyParticlesInDesign(){}

	private static void SetSpecificMachineParticles(Transform playerVFX, ModSettings.GenericQuantityEnum toSet, bool isMainPlayer = false)
	{
		if (isMainPlayer)
		{
			GameObject worldFragments = playerVFX.Find("WorldFragments").gameObject;
			worldFragments.active = toSet == ModSettings.GenericQuantityEnum.full;
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

		warpVFX.active = toSet == ModSettings.GenericQuantityEnum.full;
		sandEffects.active = toSet == ModSettings.GenericQuantityEnum.full;
		waterEffects.active = toSet == ModSettings.GenericQuantityEnum.full;
		waterInteraction.active = toSet == ModSettings.GenericQuantityEnum.full;
		steamEffects.active = toSet == ModSettings.GenericQuantityEnum.full || toSet == ModSettings.GenericQuantityEnum.reduced;
		structureEffects.active = toSet == ModSettings.GenericQuantityEnum.full || toSet == ModSettings.GenericQuantityEnum.reduced;

		breakEffects.active = toSet == ModSettings.GenericQuantityEnum.full || toSet == ModSettings.GenericQuantityEnum.reduced;
		for (int i = 0; i < breakEffects.transform.childCount; i++)
			breakEffects.transform.GetChild(i).gameObject.active = toSet == ModSettings.GenericQuantityEnum.full;

		floatParticles.active = toSet == ModSettings.GenericQuantityEnum.full || toSet == ModSettings.GenericQuantityEnum.reduced;
		floatParticles.transform.GetChild(0).gameObject.active = toSet == ModSettings.GenericQuantityEnum.full;
		floatParticles.transform.GetChild(2).gameObject.active = toSet == ModSettings.GenericQuantityEnum.full;

		velocityParticles.active = toSet == ModSettings.GenericQuantityEnum.full || toSet == ModSettings.GenericQuantityEnum.reduced;
		velocityParticles.transform.GetChild(0).gameObject.active = toSet == ModSettings.GenericQuantityEnum.full;
		velocityParticles.transform.GetChild(3).gameObject.active = toSet == ModSettings.GenericQuantityEnum.full;
		velocityParticles.transform.GetChild(4).gameObject.active = toSet == ModSettings.GenericQuantityEnum.full;

		GameObject mechBody = playerVFX.parent.Find("ArchitectureComplex").gameObject;
		SetModuleParticles(mechBody.transform, toSet != ModSettings.GenericQuantityEnum.none);
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
