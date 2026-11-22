using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StoryProgressValue
{
    public string key;
    public int value;
}

// Story and room state belong to the current game/save slot, not global preferences.
public static class GameProgressState
{
    private static readonly Dictionary<string, int> values = new Dictionary<string, int>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitializeSession() { Clear(); }

    public static int GetInt(string key, int defaultValue = 0)
    {
        return !string.IsNullOrEmpty(key) && values.TryGetValue(key, out int value) ? value : defaultValue;
    }

    public static void SetInt(string key, int value)
    {
        if (!string.IsNullOrEmpty(key)) values[key] = value;
    }

    public static void DeleteKey(string key)
    {
        if (!string.IsNullOrEmpty(key)) values.Remove(key);
    }

    public static void Clear() { values.Clear(); }

    public static List<StoryProgressValue> Capture()
    {
        var result = new List<StoryProgressValue>();
        foreach (var pair in values) result.Add(new StoryProgressValue { key = pair.Key, value = pair.Value });
        return result;
    }

    public static void Restore(List<StoryProgressValue> savedValues)
    {
        Clear();
        if (savedValues == null) return;
        foreach (var entry in savedValues)
            if (entry != null && !string.IsNullOrEmpty(entry.key)) values[entry.key] = entry.value;
    }

    public static void ClearEvents()
    {
        foreach (var key in new List<string>(values.Keys))
            if (key.StartsWith("event/", StringComparison.Ordinal)) values.Remove(key);
    }
}
