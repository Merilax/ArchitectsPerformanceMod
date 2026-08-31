using UnityEngine;
using System.Text.Json.Serialization;
using System;
using System.Text.Json;

namespace ArchPerformanceMod;

public class Utils
{
    public static void RescaleUI(GameObject obj)
    {
        obj?.transform.localScale = Vector3.one;
    }
    public static float NormalizeAngle(float angle)
    {
        return angle > 180f ? angle - 360f : angle;
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
        catch (Exception)
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

