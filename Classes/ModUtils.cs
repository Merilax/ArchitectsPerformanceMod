using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Text.Json.Serialization;
using System;
using System.Text.Json;

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
    public static float NormalizeAngle(float angle)
    {
        return angle > 180f ? angle - 360f : angle;
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

    public static int LightTypeToInt(LightType type)
    {
        return type switch
        {
            LightType.Point => 1,
            LightType.Directional => 2,
            _ => 0,
        };
    }
    public static LightType IntToLightType(int type)
    {
        return type switch
        {
            1 => LightType.Point,
            2 => LightType.Directional,
            _ => LightType.Spot,
        };
    }


}

public class ColorConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string val = reader.GetString();
        string[] arr = val.Split(':');
        try
        {
            return new Color(float.Parse(arr[0]), float.Parse(arr[1]), float.Parse(arr[2]), float.Parse(arr[3]));
        }
        catch (System.Exception)
        {
            return Color.black;
        }
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        string val = $"{value.r}:{value.g}:{value.b}:{value.a}";
        writer.WriteStringValue(val);
    }
}

public enum NavDirEnum { UP, RIGHT, DONW, LEFT }