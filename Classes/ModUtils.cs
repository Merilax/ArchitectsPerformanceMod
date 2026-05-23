using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

namespace ArchPerformanceMod;

public class Utils
{
    public static readonly Dictionary<int, Vector2> screenResolutions = new()
    {
        {4, new Vector2(1920, 1200)},
        {5, new Vector2(1920, 1080)},
        {6, new Vector2(1600, 900)},
        {7, new Vector2(1440, 810)},
        {8, new Vector2(1366, 768)},
        {9, new Vector2(1280, 800)},
        {10, new Vector2(1280, 720)},
        {11, new Vector2(1024, 576)},
        {12, new Vector2(960, 540)},
    };
    public static void RescaleUI(GameObject obj)
    {
        obj?.transform.localScale = Vector3.one;
    }

    public static void SetUINavigation(Selectable input, NavDirEnum direction, Selectable target)
    {
        Navigation nav = input.navigation;
        switch (direction)
        {
            case NavDirEnum.UP: nav.selectOnUp = target; break;
            case NavDirEnum.RIGHT: nav.selectOnRight = target; break;
            case NavDirEnum.DONW: nav.selectOnDown = target; break;
            case NavDirEnum.LEFT: nav.selectOnLeft = target; break;
        }
        input.navigation = nav;
    }
}

public enum NavDirEnum { UP, RIGHT, DONW, LEFT }