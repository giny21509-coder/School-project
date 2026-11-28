using UnityEngine;

/// <summary>
/// 一片記憶碎片：順序固定（不洗牌），在第六章由「傾聽回音」取得。
/// CG 與背包顯示沿用既有的 ItemData，所以 CGCollectionProgressUI 可以直接使用 cgItem。
/// </summary>
[CreateAssetMenu(menuName = "JianQi/Chapter/Memory Fragment", fileName = "Fragment_")]
public class MemoryFragmentDefinition : ScriptableObject
{
    [Tooltip("1 開始的時間順序，例如：1 登山露營 → 2 婚後忙碌 → 3 爭吵後的沉默 → 4 妻子離開")]
    [Min(1)] public int order = 1;

    [Tooltip("取得後放進背包／相冊的 CG（沿用 ItemData，itemId 不可重複）")]
    public ItemData cgItem;

    [Tooltip("回音的台詞，每句約 1.5 秒；第六章預設 3 句")]
    [TextArea(1, 3)] public string[] echoLines = new string[3];
}
