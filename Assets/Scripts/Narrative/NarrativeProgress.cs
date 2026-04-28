using UnityEngine;

public static class NarrativeProgress
{
    const string KeyPrefix = "narrative.collected.";

    static string BuildKey(string entryId)
    {
        if (string.IsNullOrWhiteSpace(entryId)) return string.Empty;
        return KeyPrefix + entryId.Trim();
    }

    public static bool IsCollected(string entryId)
    {
        string key = BuildKey(entryId);
        if (string.IsNullOrEmpty(key)) return false;
        return PlayerPrefs.GetInt(key, 0) == 1;
    }

    public static void MarkCollected(string entryId)
    {
        string key = BuildKey(entryId);
        if (string.IsNullOrEmpty(key)) return;

        PlayerPrefs.SetInt(key, 1);
        PlayerPrefs.Save();
    }

    public static void ResetCollected(string entryId)
    {
        string key = BuildKey(entryId);
        if (string.IsNullOrEmpty(key)) return;

        PlayerPrefs.DeleteKey(key);
        PlayerPrefs.Save();
    }
}
