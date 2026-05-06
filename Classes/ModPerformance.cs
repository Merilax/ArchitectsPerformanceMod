using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

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

		// if (SceneManager.GetActiveScene().name != "MainMenu") ApplyInRace();
	}
	// This doesn't do anything beneficial. Volume Components in races seem to be done in a way that they actively cancel out the effects of the MainMenu components.
	// public static void SetGlobalIlluminationInRace()
	// SceneManager.GetActiveScene().GetRootGameObjects().First(item => item.name == "Global Volume").transform.GetComponent<Volume>().profile.components[10].active = input;

	public static void SetReflections(ModSettings.GenericRaceOnlyEnum toSet)
	{
		GameObject[] rootObjs = SceneManager.GetSceneByName("MainMenu").GetRootGameObjects();
		Transform postProcess = GetEnvironmentObj().transform.Find("Vol").Find("PostProcess");
		GameObject dockProbes = rootObjs.First(item => item.name == "World_PlayerDock").transform.Find("StageV3").Find("Stage_v3").Find("Probe").gameObject;
		ReflectionProbe designProbe = rootObjs.First(item => item.name == "World_DesignOnly").transform.Find("Reflection Probe").GetComponent<ReflectionProbe>();
		ReflectionProbe testTrackProbe = rootObjs.First(item => item.name == "World_TestPlay").transform.Find("Global Volume").GetComponent<ReflectionProbe>();

		switch (toSet)
		{
			case ModSettings.GenericRaceOnlyEnum.on:
				dockProbes.active = true;
				designProbe.enabled = true;
				testTrackProbe.enabled = true;
				postProcess.GetComponent<Volume>().profile.components[3].active = true;
				break;
			case ModSettings.GenericRaceOnlyEnum.raceOnly:
				dockProbes.active = false;
				designProbe.enabled = true;
				testTrackProbe.enabled = true;
				postProcess.GetComponent<Volume>().profile.components[3].active = true;
				break;
			case ModSettings.GenericRaceOnlyEnum.off:
				dockProbes.active = false;
				designProbe.enabled = false;
				testTrackProbe.enabled = false;
				postProcess.GetComponent<Volume>().profile.components[3].active = false;
				break;
		}

		if (SceneManager.GetActiveScene().name != "MainMenu") ApplyInRace();
	}

	public static void SetAmbientOcclusion(ModSettings.GenericToggleEnum toSet)
	{
		bool input = toSet == ModSettings.GenericToggleEnum.on;
		GetEnvironmentObj().transform.Find("Vol").Find("PostProcess").GetComponent<Volume>().profile.components[2].active = input;
	}

	public static void ReduceParticles(ModSettings.GenericToggleEnum toSet)
	{
		// WIP
		bool input = toSet == ModSettings.GenericToggleEnum.on;

		Transform myPlayerEffects = GetPlayersObj().transform.Find("MyPlayer").Find("Effects");
		myPlayerEffects.Find("WorldFragments").gameObject.active = input;
		myPlayerEffects.Find("Float_Around_Particle").gameObject.active = input;
		myPlayerEffects.Find("WaterEffects").gameObject.active = input;
		myPlayerEffects.Find("SandEffects").gameObject.active = input;

		GetEnvironmentObj().transform.Find("Vol").Find("PostProcess").GetComponent<Volume>().profile.components[2].active = input;
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

	public static void SetAntialiasing(ModSettings.GenericToggleEnum toSet)
	{
		// WIP
		bool input = toSet == ModSettings.GenericToggleEnum.on;

		HDAdditionalCameraData cameraData = SceneManager.GetSceneByName("MainMenu").GetRootGameObjects().First(item => item.name == "Main Camera").GetComponent<HDAdditionalCameraData>();
		cameraData.antialiasing = input ? HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing : HDAdditionalCameraData.AntialiasingMode.None;
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(StageInfo), nameof(StageInfo.SetupStage))]
	public static void ApplyInRace()
	{
		Scene scene = SceneManager.GetActiveScene();
		GameObject[] rootObjs = scene.GetRootGameObjects();

		switch (scene.name)
		{
			case "Grass_01":
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
}