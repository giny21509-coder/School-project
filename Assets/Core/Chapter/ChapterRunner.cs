using System;
using JianQi.Chapters;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 每個章節場景放一個：讀出章節階段、只啟用該階段的物件群組，並把 Fungus／觸發器的請求
/// 轉成狀態機的 TryAdvance。規則在 ChapterStateMachine，這裡只負責接線與存檔。
/// Fungus 用法：Call Method → ChapterRunner.AdvanceTo(int) 或 AdvanceNext()。
/// </summary>
public class ChapterRunner : MonoBehaviour
{
    [Serializable]
    public class StageGroup
    {
        public ChapterStage stage;
        [Tooltip("只在這個階段啟用的物件（例如調查用的線索、浴室門、內心世界入口）")]
        public GameObject[] activeOnlyInStage = new GameObject[0];
    }

    [SerializeField] private ChapterDefinition chapter;
    [Tooltip("這個場景開始時，章節至少要在哪個階段（例如公寓場景至少是 Investigation）")]
    [SerializeField] private ChapterStage sceneEntryStage = ChapterStage.Commission;
    [SerializeField] private StageGroup[] stageGroups = new StageGroup[0];

    public UnityEvent<int> onStageChanged = new UnityEvent<int>();

    public ChapterDefinition Chapter => chapter;
    public ChapterStage Current => machine != null ? machine.Current : sceneEntryStage;

    private ChapterStateMachine machine;

    private void Awake()
    {
        machine = new ChapterStateMachine();
        if (chapter != null)
            machine.Restore(ChapterProgress.GetInt(chapter.chapterNumber, ChapterProgress.StateKey));
        machine.TryAdvance(sceneEntryStage); // 從前面的階段直接進到這個場景時補上
        machine.Changed += OnChanged;
        Save();
        ApplyGroups();
    }

    private void OnDestroy()
    {
        if (machine != null) machine.Changed -= OnChanged;
    }

    public void AdvanceTo(int stage) => machine.TryAdvance((ChapterStage)stage);
    public void AdvanceTo(ChapterStage stage) => machine.TryAdvance(stage);
    public void AdvanceNext() => machine.AdvanceNext();

    private void OnChanged(ChapterStage from, ChapterStage to)
    {
        Save();
        if (to == ChapterStage.Complete && chapter != null)
            ChapterProgress.SetFlag(chapter.chapterNumber, ChapterProgress.DoneKey);
        ApplyGroups();
        onStageChanged?.Invoke((int)to);
    }

    private void Save()
    {
        if (chapter != null)
            ChapterProgress.SetInt(chapter.chapterNumber, ChapterProgress.StateKey, (int)machine.Current);
    }

    private void ApplyGroups()
    {
        if (stageGroups == null) return;
        foreach (var g in stageGroups)
        {
            if (g == null || g.activeOnlyInStage == null) continue;
            bool on = g.stage == machine.Current;
            foreach (var go in g.activeOnlyInStage) if (go != null) go.SetActive(on);
        }
    }
}
