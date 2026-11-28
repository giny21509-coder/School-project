using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// S1：調查到指定數量的線索後開啟浴室門（設計文件：5 條線索中滿 3 條）。
/// </summary>
public class InvestigationTracker : MonoBehaviour
{
    [SerializeField] private ChapterDefinition chapter;
    [SerializeField] private ClueInspectable[] clues = new ClueInspectable[0];
    [SerializeField, Min(1)] private int requiredClues = 3;

    public UnityEvent onThresholdReached = new UnityEvent();
    public UnityEvent<int, int> onProgress = new UnityEvent<int, int>(); // (已調查, 需要)

    private bool reached;

    private void Start()
    {
        foreach (var c in clues) if (c != null) c.onInspected.AddListener(Refresh);
        Refresh();
    }

    private void OnDestroy()
    {
        foreach (var c in clues) if (c != null) c.onInspected.RemoveListener(Refresh);
    }

    public int FoundCount()
    {
        if (chapter == null) return 0;
        int n = 0;
        foreach (var c in clues)
            if (c != null && ChapterProgress.GetFlag(chapter.chapterNumber, ChapterProgress.ClueKey(c.ClueId))) n++;
        return n;
    }

    private void Refresh()
    {
        int found = FoundCount();
        onProgress?.Invoke(found, requiredClues);
        if (!reached && found >= requiredClues)
        {
            reached = true;
            onThresholdReached?.Invoke();
        }
    }
}
