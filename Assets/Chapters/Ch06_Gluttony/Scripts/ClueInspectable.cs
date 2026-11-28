using Fungus;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// S1 現場調查的線索（設計文件 §3 S1）：靠近按互動鍵 → 記錄線索、解鎖對應草稿、播放 Fungus 調查文字。
/// 浴室門是否開啟由 InvestigationTracker 判斷。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ClueInspectable : MonoBehaviour
{
    [SerializeField] private ChapterDefinition chapter;
    [Tooltip("C1–C5")]
    [SerializeField] private string clueId = "C1";
    [SerializeField] private string playerTag = "Player";

    [Header("Fungus 調查文字")]
    [SerializeField] private Flowchart flowchart;
    [SerializeField] private string blockName;

    public UnityEvent onInspected = new UnityEvent();

    public string ClueId => clueId;
    private bool playerInside;

    private void Update()
    {
        if (playerInside && Time.timeScale > 0f && Ch06Input.InteractPressed) Inspect();
    }

    private void OnTriggerEnter2D(Collider2D other) { if (other.CompareTag(playerTag)) playerInside = true; }
    private void OnTriggerExit2D(Collider2D other) { if (other.CompareTag(playerTag)) playerInside = false; }

    public void Inspect()
    {
        if (chapter == null) return;
        int ch = chapter.chapterNumber;
        ChapterProgress.SetFlag(ch, ChapterProgress.ClueKey(clueId));
        if (chapter.drafts != null)
            foreach (var d in chapter.drafts)
                if (d != null && d.unlockedByClueId == clueId)
                    ChapterProgress.SetFlag(ch, ChapterProgress.DraftKey(d.draftId));

        DialoguePlayer.Play(flowchart, blockName, 0f, true, null);
        onInspected?.Invoke();
    }
}
