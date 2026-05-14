using UnityEngine;
using System.Collections.Generic;

namespace ArchPerformanceMod;

public class Utils
{
    public static Dictionary<int, Vector2> screenResolutions = new()
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
}