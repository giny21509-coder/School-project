using UnityEngine;

/// <summary>
/// 一則「未送出的訊息」：現場調查時由線索解鎖，最終戰時作為話語卡使用。
/// </summary>
[CreateAssetMenu(menuName = "JianQi/Chapter/Draft Message", fileName = "Draft_")]
public class DraftMessageDefinition : ScriptableObject
{
    [Tooltip("存檔與比對用 ID，例如 D1、D2、D3")]
    public string draftId = "D1";

    [Tooltip("收件人顯示名稱，例如「登山老友」「前妻」「自己」")]
    public string recipient;

    [TextArea(2, 4)] public string text;

    [Tooltip("哪一條線索會解鎖這則草稿，例如 C2")]
    public string unlockedByClueId;
}
