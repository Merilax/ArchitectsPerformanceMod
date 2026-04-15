using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

namespace BA3StandardMod;

public class ModPerformance
{
	public static GameObject GetEnvironmentObj()
	{
		return SceneManager.GetSceneByName("MainMenu").GetRootGameObjects().First(item => item.name == "Enviroment");
	}
	public static void SetGlobalIllumination(ModSettings.ValueEnum toSet)
	{
		bool input = toSet == ModSettings.ValueEnum.on;
		GetEnvironmentObj().transform.Find("Vol").Find("PostProcess").GetComponent<UnityEngine.Rendering.Volume>().profile.components[12].active = input;

		GameObject SkyFogObj = GetEnvironmentObj().transform.Find("Vol").Find("Sky and Fog Volume").gameObject;
		VisualEnvironment visualEnv;
		SkyFogObj.GetComponent<UnityEngine.Rendering.Volume>().profile.TryGet(out visualEnv);
		visualEnv.skyAmbientMode.value = input ? SkyAmbientMode.Dynamic : SkyAmbientMode.Static;
	}

	public static void SetReflections(ModSettings.ValueEnum toSet)
	{
		bool input = toSet == ModSettings.ValueEnum.on;
		GetEnvironmentObj().transform.Find("Vol").Find("PostProcess").GetComponent<UnityEngine.Rendering.Volume>().profile.components[3].active = input;
	}

	public static void SetAmbientOcclusion(ModSettings.ValueEnum toSet)
	{
		bool input = toSet == ModSettings.ValueEnum.on;
		GetEnvironmentObj().transform.Find("Vol").Find("PostProcess").GetComponent<UnityEngine.Rendering.Volume>().profile.components[2].active = input;
	}
}