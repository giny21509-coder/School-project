/// <summary>
/// 章節進度存取：一律加上 "ch{n}/" 前綴後寫入既有的 GameProgressState，
/// 存讀檔沿用 SaveManager（storyProgress）。取代全域布林（例如 GameDataManager.isItemUnlocked），
/// 讓七章的旗標不會互相污染。
/// </summary>
public static class ChapterProgress
{
    public static string Key(int chapter, string key) => $"ch{chapter}/{key}";

    public static int GetInt(int chapter, string key, int defaultValue = 0)
        => GameProgressState.GetInt(Key(chapter, key), defaultValue);

    public static void SetInt(int chapter, string key, int value)
        => GameProgressState.SetInt(Key(chapter, key), value);

    public static bool GetFlag(int chapter, string key)
        => GetInt(chapter, key) != 0;

    public static void SetFlag(int chapter, string key, bool value = true)
        => SetInt(chapter, key, value ? 1 : 0);

    // 常用 key（各章共用的命名）
    public const string StateKey = "state";
    public const string DoneKey = "done";
    public static string ClueKey(string clueId) => $"clue/{clueId}";
    public static string DraftKey(string draftId) => $"draft/{draftId}";
    public static string FragmentKey(int order) => $"echo/{order}";
}
