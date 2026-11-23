using UnityEngine;

/// <summary>
/// 一章的資料：七章共用同一個結構，數量與內容都放在資料裡，不寫死在程式碼。
/// 取代目前寫死在程式中的「5 張記憶」「場景名稱」「貓咪專用流程」。
/// </summary>
[CreateAssetMenu(menuName = "JianQi/Chapter/Chapter Definition", fileName = "Ch0_")]
public class ChapterDefinition : ScriptableObject
{
    [Header("基本")]
    [Tooltip("章號：序章 0、傲慢 1 … 暴食 6、色慾 7。進度 key 會是 ch{章號}/...")]
    [Min(0)] public int chapterNumber;
    public string displayName;

    [Header("場景（名稱不要有空白）")]
    public string[] sceneNames = new string[0];

    [Header("記憶碎片（依時間順序排列）")]
    public MemoryFragmentDefinition[] fragments = new MemoryFragmentDefinition[0];

    [Header("未送出的訊息（第六章：最終戰的話語卡）")]
    public DraftMessageDefinition[] drafts = new DraftMessageDefinition[0];

    [Header("執念物與獎勵")]
    public ItemData obsessionItem;
    public ItemData memento;
    public ItemData albumEntry;

    [Header("Fungus")]
    [Tooltip("釋懷演出的 Block 名稱")]
    public string releaseBlockName;

    public int FragmentCount => fragments != null ? fragments.Length : 0;

    public DraftMessageDefinition FindDraft(string draftId)
    {
        if (drafts == null) return null;
        foreach (var d in drafts) if (d != null && d.draftId == draftId) return d;
        return null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 碎片順序必須是 1..N 且不重複，否則跳出提示（不自動修正，避免覆蓋設計者的資料）。
        if (fragments == null) return;
        var seen = new bool[fragments.Length + 1];
        foreach (var f in fragments)
        {
            if (f == null) continue;
            if (f.order < 1 || f.order > fragments.Length || seen[f.order])
            {
                Debug.LogWarning($"{name}: 記憶碎片順序需為 1..{fragments.Length} 且不重複（{f.name} = {f.order}）", this);
                return;
            }
            seen[f.order] = true;
        }
    }
#endif
}
