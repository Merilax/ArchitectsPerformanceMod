// Thanks a lot to Josiah Siegel for making this work of art.
// https://github.com/JosiahSiegel/aska-mod-performance-booster

using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine.Rendering;

namespace ArchPerformanceMod;

/// <summary>
/// Static helper for all HDRP reflection operations.
///
/// CRITICAL: These methods MUST NOT live on the MonoBehaviour class.
/// IL2CPP's ClassInjector processes all instance methods on MonoBehaviour
/// subclasses and chokes on System.Object / System.Type parameters.
/// Static methods on non-MonoBehaviour classes are invisible to ClassInjector.
///
/// This class handles:
///   - HDRP Asset reflection (finding properties on RenderPipelineSettings)
///   - Pipeline support flag writes (the PRIMARY optimization mechanism)
///   - HDRP Asset staleness detection (quality level changes)
/// </summary>
internal static class HDRPReflectionHelper
{
    // ------------------------------------------------------------------
    //  HDRP Asset reflection cache
    // ------------------------------------------------------------------
    internal static bool HdrpReflectionCached;
    internal static object HdrpAssetRef;
    internal static Type HdrpAssetType;

    internal static object RenderPipelineSettingsRef;
    internal static Type RenderPipelineSettingsType;

    internal static PropertyInfo PropCurrentPlatformRenderPipelineSettings;

    // Pipeline support flag properties (the PRIMARY optimization)
    internal static PropertyInfo PropSupportSSR;
    internal static PropertyInfo PropSupportSSAO;
    internal static PropertyInfo PropSupportVolumetrics;
    internal static PropertyInfo PropSupportVolumetricClouds;
    internal static PropertyInfo PropSupportSubsurfaceScattering;
    internal static PropertyInfo PropSupportDecals;
    internal static PropertyInfo PropSupportDistortion;
    internal static PropertyInfo PropSupportSSRTransparent;
    internal static PropertyInfo PropSupportDataDrivenLensFlare;
    internal static PropertyInfo PropSupportScreenSpaceLensFlare;

    // ------------------------------------------------------------------
    //  HDRP Shadow Init Parameters (hdShadowInitParams on RenderPipelineSettings)
    // ------------------------------------------------------------------
    internal static object HdShadowInitParamsRef;
    internal static Type HdShadowInitParamsType;
    internal static PropertyInfo PropHdShadowInitParams;

    // Writable shadow properties on HDShadowInitParameters
    internal static PropertyInfo PropMaxShadowRequests;
    internal static PropertyInfo PropMaxDirectionalShadowMapResolution;
    internal static PropertyInfo PropMaxPunctualShadowMapResolution;
    internal static PropertyInfo PropMaxAreaShadowMapResolution;
    internal static PropertyInfo PropAreaShadowFilteringQuality;

    // Track which HDRP Asset the reflection cache was built for.
    private static int _cachedHdrpAssetInstanceId = -1;

    // ------------------------------------------------------------------
    //  Debug logging helper
    // ------------------------------------------------------------------
    private static bool Debug => true;

    private static void DebugLog(string msg)
    {
        if (Debug)
            Plugin.LogDebug($"[Debug] {msg}");
    }

    // ==================================================================
    //  Cache HDRP reflection
    // ==================================================================

    internal static void CacheHDRPReflection()
    {
        if (HdrpReflectionCached) return;
        HdrpReflectionCached = true;

        try
        {
            var pipelineAsset = GraphicsSettings.currentRenderPipeline;
            if (pipelineAsset == null)
            {
                Plugin.Log.LogWarning(
                    "GraphicsSettings.currentRenderPipeline is null -- HDRP settings unavailable.");
                return;
            }

            HdrpAssetRef = pipelineAsset;

            // IL2CPP interop: resolve the concrete HDRenderPipelineAsset type
            Type t = pipelineAsset.GetType();
            Type concreteType = ResolveIl2CppConcreteType(pipelineAsset);
            if (concreteType != null && concreteType != t)
            {
                DebugLog(
                    $"IL2CPP type resolution: GetType()={t.Name}, " +
                    $"concrete IL2CPP type={concreteType.Name}. Using concrete type.");
                t = concreteType;

                var castAsset = RuntimeCastToConcreteType(pipelineAsset, concreteType);
                if (castAsset != null && castAsset != (object)pipelineAsset)
                {
                    HdrpAssetRef = castAsset;
                    DebugLog(
                        $"HDRP Asset re-wrapped: managed type now {castAsset.GetType().Name}");
                }
            }

            HdrpAssetType = t;

            try
            {
                _cachedHdrpAssetInstanceId = pipelineAsset.GetInstanceID();
                DebugLog(
                    $"HDRP Asset cached: '{pipelineAsset.name}' (instanceID={_cachedHdrpAssetInstanceId})");
            }
            catch
            {
                _cachedHdrpAssetInstanceId = -1;
            }

            // Get currentPlatformRenderPipelineSettings
            PropCurrentPlatformRenderPipelineSettings = FindProp(t, "currentPlatformRenderPipelineSettings");
            if (PropCurrentPlatformRenderPipelineSettings != null)
            {
                try
                {
                    RenderPipelineSettingsRef = PropCurrentPlatformRenderPipelineSettings.GetValue(HdrpAssetRef);
                    if (RenderPipelineSettingsRef != null)
                    {
                        RenderPipelineSettingsType = RenderPipelineSettingsRef.GetType();
                        DebugLog(
                            $"RenderPipelineSettings resolved: type={RenderPipelineSettingsType.FullName}");
                        CacheSupportFlagProperties();
                        CacheShadowInitProperties();
                    }
                    else
                    {
                        Plugin.Log.LogWarning(
                            "currentPlatformRenderPipelineSettings returned null.");
                    }
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning(
                        $"Failed to read currentPlatformRenderPipelineSettings: {ex.Message}");
                }
            }
            else
            {
                Plugin.Log.LogWarning(
                    "currentPlatformRenderPipelineSettings property not found on " + t.FullName);
                TryFindSettingsViaInternalField(t);
            }

            LogReflectionSummary();
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Failed to cache HDRP reflection: {ex.Message}");
        }
    }

    private static void CacheSupportFlagProperties()
    {
        if (RenderPipelineSettingsType == null || RenderPipelineSettingsRef == null) return;

        Type st = RenderPipelineSettingsType;

        PropSupportSSR = FindProp(st, "supportSSR");
        PropSupportSSAO = FindProp(st, "supportSSAO");
        PropSupportVolumetrics = FindProp(st, "supportVolumetrics");
        PropSupportVolumetricClouds = FindProp(st, "supportVolumetricClouds");
        PropSupportSubsurfaceScattering = FindProp(st, "supportSubsurfaceScattering");
        PropSupportDecals = FindProp(st, "supportDecals");
        PropSupportDistortion = FindProp(st, "supportDistortion");
        PropSupportSSRTransparent = FindProp(st, "supportSSRTransparent");
        PropSupportDataDrivenLensFlare = FindProp(st, "supportDataDrivenLensFlare");
        PropSupportScreenSpaceLensFlare = FindProp(st, "supportScreenSpaceLensFlare");
    }

    /// <summary>
    /// Resolve the hdShadowInitParams nested struct on RenderPipelineSettings
    /// and cache PropertyInfo handles for writable shadow properties.
    /// </summary>
    private static void CacheShadowInitProperties()
    {
        if (RenderPipelineSettingsType == null || RenderPipelineSettingsRef == null) return;

        try
        {
            // Locate hdShadowInitParams property on RenderPipelineSettings
            PropHdShadowInitParams = FindProp(RenderPipelineSettingsType, "hdShadowInitParams");
            if (PropHdShadowInitParams == null)
            {
                DebugLog("hdShadowInitParams property not found on RenderPipelineSettings.");
                return;
            }

            HdShadowInitParamsRef = PropHdShadowInitParams.GetValue(RenderPipelineSettingsRef);
            if (HdShadowInitParamsRef == null)
            {
                DebugLog("hdShadowInitParams returned null.");
                return;
            }

            HdShadowInitParamsType = HdShadowInitParamsRef.GetType();

            // Cache individual shadow properties
            PropMaxShadowRequests = FindProp(HdShadowInitParamsType, "maxShadowRequests");
            PropMaxDirectionalShadowMapResolution = FindProp(HdShadowInitParamsType, "maxDirectionalShadowMapResolution");
            PropMaxPunctualShadowMapResolution = FindProp(HdShadowInitParamsType, "maxPunctualShadowMapResolution");
            PropMaxAreaShadowMapResolution = FindProp(HdShadowInitParamsType, "maxAreaShadowMapResolution");
            PropAreaShadowFilteringQuality = FindProp(HdShadowInitParamsType, "areaShadowFilteringQuality");

            // Log discovery results
            string maxReqVal = PropMaxShadowRequests != null ? SafeGetInt(PropMaxShadowRequests, HdShadowInitParamsRef).ToString() : "N/A";
            string maxDirVal = PropMaxDirectionalShadowMapResolution != null ? SafeGetInt(PropMaxDirectionalShadowMapResolution, HdShadowInitParamsRef).ToString() : "N/A";
            string maxPuncVal = PropMaxPunctualShadowMapResolution != null ? SafeGetInt(PropMaxPunctualShadowMapResolution, HdShadowInitParamsRef).ToString() : "N/A";
            string maxAreaVal = PropMaxAreaShadowMapResolution != null ? SafeGetInt(PropMaxAreaShadowMapResolution, HdShadowInitParamsRef).ToString() : "N/A";
            string areaFilterVal = PropAreaShadowFilteringQuality != null ? SafeGetEnum(PropAreaShadowFilteringQuality, HdShadowInitParamsRef) : "N/A";

            DebugLog(
                $"HDShadowInitParams cached: maxShadowRequests={maxReqVal}, " +
                $"maxDirShadowRes={maxDirVal}, maxPuncShadowRes={maxPuncVal}, " +
                $"maxAreaShadowRes={maxAreaVal}, areaFilterQuality={areaFilterVal}");
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Failed to cache shadow init properties: {ex.Message}");
        }
    }

    private static void TryFindSettingsViaInternalField(Type assetType)
    {
        try
        {
            FieldInfo settingsField = null;
            Type walkType = assetType;
            while (walkType != null && walkType != typeof(object))
            {
                settingsField = walkType.GetField("m_RenderPipelineSettings",
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (settingsField != null) break;
                walkType = walkType.BaseType;
            }

            if (settingsField != null && HdrpAssetRef != null)
            {
                RenderPipelineSettingsRef = settingsField.GetValue(HdrpAssetRef);
                if (RenderPipelineSettingsRef != null)
                {
                    RenderPipelineSettingsType = RenderPipelineSettingsRef.GetType();
                    DebugLog(
                        $"RenderPipelineSettings resolved via m_RenderPipelineSettings field: " +
                        $"type={RenderPipelineSettingsType.FullName}");
                    CacheSupportFlagProperties();
                    CacheShadowInitProperties();
                }
            }
            else
            {
                Plugin.Log.LogWarning(
                    "Neither currentPlatformRenderPipelineSettings property nor " +
                    "m_RenderPipelineSettings field found on HDRP Asset.");
            }
        }
        catch (Exception ex)
        {
            DebugLog($"Fallback settings field search failed: {ex.Message}");
        }
    }

    private static void LogReflectionSummary()
    {
        string assetName = "unknown";
        try { assetName = ((UnityEngine.Object)HdrpAssetRef)?.name ?? "unknown"; }
        catch { }

        string ssaoVal = PropSupportSSAO != null ? SafeGetBool(PropSupportSSAO, RenderPipelineSettingsRef).ToString() : "N/A";
        string volVal = PropSupportVolumetrics != null ? SafeGetBool(PropSupportVolumetrics, RenderPipelineSettingsRef).ToString() : "N/A";
        string cloudsVal = PropSupportVolumetricClouds != null ? SafeGetBool(PropSupportVolumetricClouds, RenderPipelineSettingsRef).ToString() : "N/A";
        string sssVal = PropSupportSubsurfaceScattering != null ? SafeGetBool(PropSupportSubsurfaceScattering, RenderPipelineSettingsRef).ToString() : "N/A";
        string decalsVal = PropSupportDecals != null ? SafeGetBool(PropSupportDecals, RenderPipelineSettingsRef).ToString() : "N/A";

        DebugLog(
            $"HDRP Asset: {assetName} ({HdrpAssetType?.Name}). " +
            $"RenderPipelineSettings: {(RenderPipelineSettingsRef != null ? "resolved" : "NOT FOUND")}. " +
            $"Key flags: supportSSAO={ssaoVal}, " +
            $"supportVolumetrics={volVal}, " +
            $"supportVolumetricClouds={cloudsVal}, " +
            $"supportSubsurfaceScattering={sssVal}, " +
            $"supportDecals={decalsVal}");
    }

    // ==================================================================
    //  Pipeline support flag writes (PRIMARY optimization mechanism)
    // ==================================================================

    internal static int WriteSupportFlagBatch(
        List<(PropertyInfo prop, string name, bool value)> flags)
    {
        if (RenderPipelineSettingsRef == null) return 0;

        int changed = 0;
        foreach (var (prop, name, value) in flags)
        {
            if (prop == null) continue;
            try
            {
                bool current = SafeGetBool(prop, RenderPipelineSettingsRef);
                if (current == value) continue;

                prop.SetValue(RenderPipelineSettingsRef, value);
                changed++;

                DebugLog(
                    $"HDRP Asset support flag: {name} = {current} -> {value}");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning(
                    $"Could not write support flag {name}: {ex.Message}");
            }
        }

        if (changed > 0)
        {
            WriteSettingsBackToAsset();
            DebugLog(
                $"HDRP Asset support flags: {changed} flag(s) changed, wrote settings back to asset.");
        }

        return changed;
    }

    /// <summary>
    /// Convenience method callable from the MonoBehaviour.
    /// Each bool parameter means "the user wants to DISABLE this feature".
    /// </summary>
    internal static void ApplyPipelineSupportFlagBatch(
        bool disableSSR,
        bool disableSSAO,
        bool disableVolumetrics,
        bool disableVolumetricClouds,
        bool disableSubsurfaceScattering,
        bool disableDecals,
        bool disableDistortion,
        bool disableSSRTransparent,
        bool disableScreenSpaceLensFlare,
        bool disableDataDrivenLensFlare)
    {
        if (RenderPipelineSettingsRef == null) return;

        if (!disableSSR && !disableSSAO && !disableVolumetrics &&
            !disableVolumetricClouds && !disableSubsurfaceScattering &&
            !disableDecals && !disableDistortion && !disableSSRTransparent &&
            !disableScreenSpaceLensFlare && !disableDataDrivenLensFlare)
        {
            DebugLog("Pipeline support flags: no flags requested for disable, skipping.");
            return;
        }

        var flags = new List<(PropertyInfo prop, string name, bool value)>();

        // if (disableSSR) flags.Add((PropSupportSSR, "supportSSR", false));
        // if (disableSSAO) flags.Add((PropSupportSSAO, "supportSSAO", false));
        // if (disableVolumetrics) flags.Add((PropSupportVolumetrics, "supportVolumetrics", false));
        // if (disableVolumetricClouds) flags.Add((PropSupportVolumetricClouds, "supportVolumetricClouds", false));
        // if (disableSubsurfaceScattering) flags.Add((PropSupportSubsurfaceScattering, "supportSubsurfaceScattering", false));
        // if (disableDecals) flags.Add((PropSupportDecals, "supportDecals", false));
        // if (disableDistortion) flags.Add((PropSupportDistortion, "supportDistortion", false));
        // if (disableSSRTransparent) flags.Add((PropSupportSSRTransparent, "supportSSRTransparent", false));
        // if (disableScreenSpaceLensFlare) flags.Add((PropSupportScreenSpaceLensFlare, "supportScreenSpaceLensFlare", false));
        // if (disableDataDrivenLensFlare) flags.Add((PropSupportDataDrivenLensFlare, "supportDataDrivenLensFlare", false));

        flags.Add((PropSupportSSR, "supportSSR", !disableSSR));
        flags.Add((PropSupportSSAO, "supportSSAO", !disableSSAO));
        flags.Add((PropSupportVolumetrics, "supportVolumetrics", !disableVolumetrics));
        flags.Add((PropSupportVolumetricClouds, "supportVolumetricClouds", !disableVolumetricClouds));
        flags.Add((PropSupportSubsurfaceScattering, "supportSubsurfaceScattering", !disableSubsurfaceScattering));
        flags.Add((PropSupportDecals, "supportDecals", !disableDecals));
        flags.Add((PropSupportDistortion, "supportDistortion", !disableDistortion));
        flags.Add((PropSupportSSRTransparent, "supportSSRTransparent", !disableSSRTransparent));
        flags.Add((PropSupportScreenSpaceLensFlare, "supportScreenSpaceLensFlare", !disableScreenSpaceLensFlare));
        flags.Add((PropSupportDataDrivenLensFlare, "supportDataDrivenLensFlare", disableDataDrivenLensFlare));

        int changed = WriteSupportFlagBatch(flags);

        if (changed == 0)
        {
            DebugLog("Pipeline support flags: all requested flags already at target values.");
        }
    }

    /// <summary>
    /// Write the modified RenderPipelineSettings back to the HDRP Asset.
    /// Safety net: the NativeFieldInfoPtr setters likely already write through.
    /// </summary>
    internal static void WriteSettingsBackToAsset()
    {
        if (HdrpAssetRef == null || RenderPipelineSettingsRef == null) return;

        try
        {
            if (PropCurrentPlatformRenderPipelineSettings != null &&
                PropCurrentPlatformRenderPipelineSettings.GetSetMethod(true) != null)
            {
                PropCurrentPlatformRenderPipelineSettings.SetValue(HdrpAssetRef, RenderPipelineSettingsRef);
                return;
            }

            if (HdrpAssetType != null)
            {
                Type walkType = HdrpAssetType;
                while (walkType != null && walkType != typeof(object))
                {
                    var field = walkType.GetField("m_RenderPipelineSettings",
                        BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    if (field != null)
                    {
                        field.SetValue(HdrpAssetRef, RenderPipelineSettingsRef);
                        return;
                    }
                    walkType = walkType.BaseType;
                }
            }
        }
        catch (Exception ex)
        {
            DebugLog($"Could not write settings back to HDRP Asset: {ex.Message}");
        }
    }

    // ==================================================================
    //  Shadow init parameter writes
    // ==================================================================

    /// <summary>
    /// Apply shadow init parameter overrides to the HDRP Asset.
    /// Each parameter uses a sentinel value to indicate "don't override":
    ///   int  0 = don't override
    ///   int -1 = don't override (for areaShadowFilteringQuality)
    /// </summary>
    internal static void ApplyShadowInitParams(
        int maxShadowRequests,
        int maxDirectionalShadowMapResolution,
        int maxAreaShadowMapResolution,
        int areaShadowFilteringQuality)
    {
        if (HdShadowInitParamsRef == null || HdShadowInitParamsType == null)
        {
            DebugLog("Shadow init params: not cached, skipping.");
            return;
        }

        bool anyChanged = false;

        if (maxShadowRequests > 0)
            anyChanged |= WriteShadowInt(PropMaxShadowRequests, "maxShadowRequests", maxShadowRequests);

        if (maxDirectionalShadowMapResolution > 0)
            anyChanged |= WriteShadowInt(PropMaxDirectionalShadowMapResolution, "maxDirectionalShadowMapResolution", maxDirectionalShadowMapResolution);

        if (maxAreaShadowMapResolution > 0)
            anyChanged |= WriteShadowInt(PropMaxAreaShadowMapResolution, "maxAreaShadowMapResolution", maxAreaShadowMapResolution);

        if (areaShadowFilteringQuality >= 0)
            anyChanged |= WriteShadowEnum(PropAreaShadowFilteringQuality, "areaShadowFilteringQuality", areaShadowFilteringQuality);

        if (anyChanged)
        {
            WriteShadowInitParamsBack();
            WriteSettingsBackToAsset();
            DebugLog("Shadow init params: wrote changes back to HDRP Asset.");
        }
        else
        {
            DebugLog("Shadow init params: all values already at target.");
        }
    }

    /// <summary>
    /// Write an int property on HdShadowInitParamsRef. Returns true if the value changed.
    /// Uses Math.Min to ensure we never INCREASE the value beyond what the game already
    /// has -- if the user's quality preset already uses a lower (cheaper) value, we
    /// keep it rather than overriding with our higher target.
    /// </summary>
    private static bool WriteShadowInt(PropertyInfo prop, string name, int targetValue)
    {
        if (prop == null)
        {
            DebugLog($"Shadow param {name}: property not found, skipping.");
            return false;
        }

        try
        {
            int current = SafeGetInt(prop, HdShadowInitParamsRef);

            // Never increase: use the lesser of current and target.
            // If the game's quality preset already has a lower value (e.g. Low preset
            // with maxShadowRequests=32), writing our target of 48 would INCREASE cost.
            // int effective = Math.Min(current, targetValue);
            int effective = targetValue; // [!] MOD OVERRIDE BEHAVIOUR

            if (current == effective)
            {
                DebugLog($"Shadow param {name}: already {current} (<= target {targetValue}), no change.");
                return false;
            }

            prop.SetValue(HdShadowInitParamsRef, effective);
            DebugLog($"Shadow param: {name} = {current} -> {effective} (target was {targetValue})");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Could not write shadow param {name}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Write an enum property on HdShadowInitParamsRef using its int value.
    /// HDRP shadow filtering quality enums map to int values:
    ///   HDAreaShadowFilteringQuality: Low=0, Medium=1, High=2
    /// Returns true if the value changed.
    ///
    /// Uses Math.Min on the underlying int values to ensure we never INCREASE
    /// quality beyond what the game already has. For quality enums where
    /// lower int = lower quality (Low=0, Medium=1, High=2), Min picks the
    /// cheaper option.
    ///
    /// NOTE: IL2CPP enum wrappers may not convert correctly via Convert.ToInt32().
    /// We use ToString() comparison as a fallback to detect the current value,
    /// and always write if the string representation doesn't match the target.
    /// </summary>
    private static bool WriteShadowEnum(PropertyInfo prop, string name, int targetValue)
    {
        if (prop == null)
        {
            DebugLog($"Shadow param {name}: property not found, skipping.");
            return false;
        }

        try
        {
            object currentObj = prop.GetValue(HdShadowInitParamsRef);
            string currentStr = currentObj?.ToString() ?? "null";

            // Try to get the current int value for Min comparison.
            // For quality enums (Low=0, Medium=1, High=2), lower = cheaper.
            int currentInt = targetValue; // fallback: no Min applied
            try
            {
                currentInt = Convert.ToInt32(currentObj);
            }
            catch
            {
                // IL2CPP enum wrapper didn't convert -- fall through to string compare
            }

            // Never increase: use the lesser of current and target.
            int effectiveInt = Math.Min(currentInt, targetValue);

            object effectiveEnum = Enum.ToObject(prop.PropertyType, effectiveInt);
            string effectiveStr = effectiveEnum?.ToString() ?? "null";

            if (string.Equals(currentStr, effectiveStr, StringComparison.OrdinalIgnoreCase))
            {
                DebugLog($"Shadow param {name}: already {currentStr} (<= target {targetValue}), no change.");
                return false;
            }

            prop.SetValue(HdShadowInitParamsRef, effectiveEnum);
            DebugLog($"Shadow param: {name} = {currentStr} -> {effectiveStr} (target was {targetValue})");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"Could not write shadow param {name}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Write the modified HDShadowInitParameters back to RenderPipelineSettings.
    /// The NativeFieldInfoPtr setters on IL2CPP structs may already write through,
    /// but this is a safety net (same pattern as WriteSettingsBackToAsset).
    /// </summary>
    private static void WriteShadowInitParamsBack()
    {
        if (PropHdShadowInitParams == null || RenderPipelineSettingsRef == null ||
            HdShadowInitParamsRef == null)
            return;

        try
        {
            if (PropHdShadowInitParams.GetSetMethod(true) != null)
            {
                PropHdShadowInitParams.SetValue(RenderPipelineSettingsRef, HdShadowInitParamsRef);
                DebugLog("Wrote HDShadowInitParams back via property setter.");
                return;
            }

            // Fallback: try field write
            if (RenderPipelineSettingsType != null)
            {
                var field = RenderPipelineSettingsType.GetField("hdShadowInitParams",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(RenderPipelineSettingsRef, HdShadowInitParamsRef);
                    DebugLog("Wrote HDShadowInitParams back via field.");
                }
            }
        }
        catch (Exception ex)
        {
            DebugLog($"Could not write HDShadowInitParams back: {ex.Message}");
        }
    }

    // ==================================================================
    //  HDRP Asset staleness detection
    // ==================================================================

    internal static bool CheckHdrpAssetStale()
    {
        if (!HdrpReflectionCached || _cachedHdrpAssetInstanceId == -1)
            return false;

        try
        {
            var currentAsset = GraphicsSettings.currentRenderPipeline;
            if (currentAsset == null) return false;

            int currentId = currentAsset.GetInstanceID();

            if (currentId == _cachedHdrpAssetInstanceId)
            {
                DebugLog($"[AssetStaleCheck] Cache valid (instanceID={currentId}).");
                return false;
            }

            string currentName = "unknown";
            try { currentName = currentAsset.name; } catch { }

            DebugLog(
                $"[AssetStaleCheck] HDRP Asset CHANGED! " +
                $"Cached instanceID={_cachedHdrpAssetInstanceId}, " +
                $"Current='{currentName}' (instanceID={currentId}). " +
                "Invalidating cache.");

            InvalidateCache();
            return true;
        }
        catch (Exception ex)
        {
            DebugLog($"CheckHdrpAssetStale failed: {ex.Message}");
            return false;
        }
    }

    // ==================================================================
    //  Invalidate / clear
    // ==================================================================

    internal static void InvalidateCache()
    {
        HdrpReflectionCached = false;
        HdrpAssetRef = null;
        HdrpAssetType = null;
        _cachedHdrpAssetInstanceId = -1;
        RenderPipelineSettingsRef = null;
        RenderPipelineSettingsType = null;
        PropCurrentPlatformRenderPipelineSettings = null;

        PropSupportSSR = null;
        PropSupportSSAO = null;
        PropSupportVolumetrics = null;
        PropSupportVolumetricClouds = null;
        PropSupportSubsurfaceScattering = null;
        PropSupportDecals = null;
        PropSupportDistortion = null;
        PropSupportSSRTransparent = null;
        PropSupportDataDrivenLensFlare = null;
        PropSupportScreenSpaceLensFlare = null;

        HdShadowInitParamsRef = null;
        HdShadowInitParamsType = null;
        PropHdShadowInitParams = null;
        PropMaxShadowRequests = null;
        PropMaxDirectionalShadowMapResolution = null;
        PropMaxPunctualShadowMapResolution = null;
        PropMaxAreaShadowMapResolution = null;
        PropAreaShadowFilteringQuality = null;

        DebugLog("HDRP reflection cache invalidated.");
    }

    // ==================================================================
    //  Shadow property discovery (debug-only)
    // ==================================================================

    /// <summary>
    /// Enumerates all shadow-related properties on RenderPipelineSettings
    /// and any nested shadow init parameter structs. Run once with
    /// DebugLogging=true to discover exact IL2CPP property names for
    /// shadow distance, cascade count, atlas resolution, etc.
    /// </summary>
    internal static void DumpShadowProperties()
    {
        if (RenderPipelineSettingsRef == null || RenderPipelineSettingsType == null)
        {
            DebugLog("[ShadowDiscover] RenderPipelineSettings not available.");
            return;
        }

        DebugLog("[ShadowDiscover] === RenderPipelineSettings shadow-related properties ===");

        try
        {
            foreach (var prop in RenderPipelineSettingsType.GetProperties(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (prop.Name.IndexOf("shadow", StringComparison.OrdinalIgnoreCase) < 0 &&
                    prop.Name.IndexOf("Shadow", StringComparison.Ordinal) < 0)
                    continue;

                string val = "?";
                try { val = prop.GetValue(RenderPipelineSettingsRef)?.ToString() ?? "null"; }
                catch (Exception ex) { val = $"<error: {ex.Message}>"; }

                DebugLog(
                    $"[ShadowDiscover] {RenderPipelineSettingsType.Name}.{prop.Name} " +
                    $"({prop.PropertyType.Name}) = {val}");

                // If the property returns a struct/object, enumerate its shadow-related props too
                if (!prop.PropertyType.IsPrimitive && prop.PropertyType != typeof(string) &&
                    prop.PropertyType != typeof(bool))
                {
                    try
                    {
                        object nested = prop.GetValue(RenderPipelineSettingsRef);
                        if (nested != null)
                            DumpNestedShadowProps(nested, prop.Name);
                    }
                    catch { }
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning($"[ShadowDiscover] Error enumerating properties: {ex.Message}");
        }

        DebugLog("[ShadowDiscover] === End shadow property dump ===");
    }

    private static void DumpNestedShadowProps(object obj, string parentName)
    {
        if (obj == null) return;
        Type t = obj.GetType();

        try
        {
            foreach (var prop in t.GetProperties(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                string val = "?";
                try { val = prop.GetValue(obj)?.ToString() ?? "null"; }
                catch (Exception ex) { val = $"<error: {ex.Message}>"; }

                DebugLog(
                    $"[ShadowDiscover]   {parentName}.{prop.Name} " +
                    $"({prop.PropertyType.Name}) = {val}");
            }
        }
        catch { }
    }

    // ==================================================================
    //  Reflection helpers (all static, safe for IL2CPP)
    // ==================================================================

    internal static PropertyInfo FindProp(Type type, string name)
    {
        if (type == null) return null;
        try
        {
            Type current = type;
            while (current != null && current != typeof(object))
            {
                var prop = current.GetProperty(name,
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.DeclaredOnly);
                if (prop != null) return prop;
                current = current.BaseType;
            }

            var fallback = type.GetProperty(name,
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            return fallback;
        }
        catch { }
        return null;
    }

    internal static Type ResolveIl2CppConcreteType(Il2CppSystem.Object il2cppObj)
    {
        try
        {
            var il2cppType = il2cppObj.GetIl2CppType();
            string fullName = il2cppType.FullName;

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var managedType = asm.GetType(fullName);
                    if (managedType != null)
                        return managedType;
                }
                catch { }
            }

            if (fullName.Contains('.'))
            {
                string shortName = fullName.Substring(fullName.LastIndexOf('.') + 1);
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        foreach (var t in asm.GetTypes())
                        {
                            if (t.Name == shortName && t.IsClass)
                                return t;
                        }
                    }
                    catch { }
                }
            }
        }
        catch { }

        return null;
    }

    internal static object RuntimeCastToConcreteType(Il2CppObjectBase il2cppObj, Type concreteType)
    {
        if (il2cppObj == null || concreteType == null) return il2cppObj;
        if (concreteType.IsInstanceOfType(il2cppObj)) return il2cppObj;

        try
        {
            IntPtr pointer = il2cppObj.Pointer;
            if (pointer == IntPtr.Zero) return il2cppObj;

            var intPtrCtor = concreteType.GetConstructor(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null, new[] { typeof(IntPtr) }, null);

            if (intPtrCtor == null) return il2cppObj;

            object castObj = intPtrCtor.Invoke(new object[] { pointer });
            DebugLog($"RuntimeCast: {il2cppObj.GetType().Name} -> {concreteType.Name}");
            return castObj;
        }
        catch (Exception ex)
        {
            Plugin.Log.LogWarning(
                $"RuntimeCast failed ({il2cppObj.GetType().Name} -> {concreteType.Name}): {ex.Message}");
            return il2cppObj;
        }
    }

    internal static bool SafeGetBool(PropertyInfo prop, object target)
    {
        if (prop == null) return false;
        try
        {
            var val = prop.GetValue(target);
            if (val is bool b) return b;
            return Convert.ToBoolean(val);
        }
        catch { return false; }
    }

    internal static int SafeGetInt(PropertyInfo prop, object target)
    {
        if (prop == null) return 0;
        try
        {
            var val = prop.GetValue(target);
            if (val is int i) return i;
            return Convert.ToInt32(val);
        }
        catch { return 0; }
    }

    internal static string SafeGetEnum(PropertyInfo prop, object target)
    {
        if (prop == null) return "N/A";
        try
        {
            var val = prop.GetValue(target);
            return val?.ToString() ?? "null";
        }
        catch { return "error"; }
    }
}

/// <summary>
/// Harmony postfix on QualitySettings.SetQualityLevel to detect when Aska's
/// settings menu (or any code) changes the active quality level.
///
/// In HDRP, changing the quality level can swap the HDRP Asset (each quality
/// level can reference a different pipeline asset). This means:
///   1. Our cached HDRP Asset reflection becomes stale (wrong object).
///   2. Our pipeline support flag writes need re-application.
///
/// This patch sets a flag that PerformanceBehaviour reads on its next Update
/// to invalidate caches and trigger an immediate full reapplication. We do NOT
/// call ApplyAllSettings directly from the patch because Harmony postfixes
/// run on the caller's thread and the quality level change may not have fully
/// propagated yet (HDRP Asset swap, pipeline rebuild, etc.).
///
/// Uses string-based type resolution + TargetMethod for IL2CPP interop
/// compatibility. We target the two-parameter overload (int, bool) which is
/// what the single-parameter overload delegates to internally.
/// </summary>
[HarmonyPatch]
internal static class QualityLevelPatch
{
    /// <summary>
    /// Set to true by the postfix. PerformanceBehaviour reads and clears this
    /// flag each Update. Volatile because Harmony postfix and Update may run
    /// on the same thread but we want the write to be immediately visible.
    /// </summary>
    internal static volatile bool QualityLevelChanged;

    /// <summary>
    /// The quality level that was set, for logging.
    /// </summary>
    internal static int LastSetLevel;

    /// <summary>
    /// Resolve the target method at patch time using string-based type lookup.
    /// This is the IL2CPP-safe way to specify both the type and overload.
    /// </summary>
    [HarmonyTargetMethod]
    public static MethodBase TargetMethod()
    {
        // Resolve the IL2CPP-interop type by its original CLR name
        Type qualitySettingsType = AccessTools.TypeByName("UnityEngine.QualitySettings");
        if (qualitySettingsType == null)
        {
            Plugin.Log?.LogWarning(
                "[QualityLevelPatch] Could not resolve UnityEngine.QualitySettings type.");
            return null;
        }

        // Target the two-parameter overload: SetQualityLevel(int index, bool applyExpensiveChanges)
        // The single-parameter overload delegates to this one.
        // NOTE: IL2CPP may rename parameters vs the original C# source. The actual
        // interop signature uses "index" (not "qualityLevel" or "level").
        MethodInfo method = AccessTools.Method(qualitySettingsType, "SetQualityLevel",
            new[] { typeof(int), typeof(bool) });

        if (method == null)
        {
            // Fallback: try any SetQualityLevel overload
            method = AccessTools.Method(qualitySettingsType, "SetQualityLevel",
                new[] { typeof(int) });
        }

        if (method == null)
        {
            Plugin.Log?.LogWarning(
                "[QualityLevelPatch] Could not resolve SetQualityLevel method.");
        }

        return method;
    }

    /// <summary>
    /// Postfix that fires after SetQualityLevel completes.
    /// IMPORTANT: Parameter name must EXACTLY match the IL2CPP method signature.
    /// The IL2CPP interop assembly exposes the parameter as "index", not
    /// "qualityLevel" (which is the name in the original C# source). HarmonyX
    /// injects postfix parameters by name, so a mismatch causes:
    ///   "Parameter 'qualityLevel' not found in method SetQualityLevel(int index, ...)"
    /// </summary>
    [HarmonyPostfix]
    public static void Postfix(int index)
    {
        try
        {
            QualityLevelChanged = true;
            LastSetLevel = index;

            Plugin.LogInfo(
                $"[QualityLevelPatch] Game changed quality level to {index}. " +
                "Will invalidate caches and reapply optimizations on next frame.");
        }
        catch { }
    }
}