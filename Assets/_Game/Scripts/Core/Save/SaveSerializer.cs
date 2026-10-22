using System;
using UnityEngine;

/// <summary>
/// JSON in and out for the save and settings files. Reading never throws: a damaged file
/// comes back as Corrupt so the caller can fall back to the backup or to defaults.
/// </summary>
public static class SaveSerializer
{
    /// <summary>A file without a version field reads as version 0 (older than any real format).</summary>
    [Serializable]
    private class VersionProbe
    {
        public int version;
    }

    public static string ToJson(object data) => JsonUtility.ToJson(data, true);

    /// <summary>
    /// Copies the file's fields onto <paramref name="target"/>, which should hold the defaults:
    /// any field the file doesn't mention keeps its default value.
    /// </summary>
    public static SaveParseResult TryOverwrite(string json, object target, int currentVersion, out int fileVersion)
    {
        fileVersion = 0;
        if (!LooksLikeObject(json)) return SaveParseResult.Corrupt;

        try
        {
            fileVersion = JsonUtility.FromJson<VersionProbe>(json).version;
            JsonUtility.FromJsonOverwrite(json, target);
        }
        catch (Exception)
        {
            // JsonUtility throws ArgumentException on malformed text; nothing a caller can fix.
            return SaveParseResult.Corrupt;
        }

        return fileVersion > currentVersion ? SaveParseResult.NewerVersion : SaveParseResult.Ok;
    }

    // JsonUtility happily "parses" "null", "[]" or an empty file into defaults, which would
    // silently replace a save with a fresh one. Only a JSON object can be one of our files.
    private static bool LooksLikeObject(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return false;

        string trimmed = json.Trim();
        return trimmed.Length >= 2 && trimmed[0] == '{' && trimmed[trimmed.Length - 1] == '}';
    }
}
