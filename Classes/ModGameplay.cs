using System;
using HarmonyLib;
using UnityEngine;

namespace ArchPerformanceMod;

public class ModGameplay
{
	[HarmonyPostfix]
	[HarmonyPatch(typeof(StageInfo), nameof(StageInfo.SetupStage))]
	public static void ExtendCheckpointColliders()
	{
		if (NetworkManager.ins.curGameMode != NetworkManager.NetworkGameMode.onlineMatch && GameVariable.Match_GameMode == GameVariable.GameMode.main) // Offline check // && GameVariable.Match_GameMode == GameVariable.GameMode.main
		{
			try
			{
				var checkpoints = UnityEngine.Object.FindObjectsOfType<CheckPointData>();

				foreach (CheckPointData checkpoint in checkpoints)
				{
					BoxCollider box = checkpoint._transform.GetComponent<BoxCollider>();
					box?.size = new Vector3(box.size.x, box.size.y, 20);
				}

				Plugin.LogInfo("Patched checkpoint hitboxes (singleplayer)");
			}
			catch (Exception ex)
			{
				Plugin.Log.LogError(ex);
			}
		}
	}
}