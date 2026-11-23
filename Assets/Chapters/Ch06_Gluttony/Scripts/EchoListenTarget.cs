using System.Collections.Generic;
using JianQi.Ch06;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 回音（沒有臉的朋友／前妻殘影）：按住傾聽 3 句 → 取得記憶碎片、E −20（設計文件 §4.3）。
/// 雜訊怪碰到時會被「蓋住」，擊敗雜訊怪後恢復。
/// </summary>
public class EchoListenTarget : MonoBehaviour
{
    public static readonly List<EchoListenTarget> Active = new List<EchoListenTarget>();

    [SerializeField] private ChapterDefinition chapter;
    [SerializeField] private MemoryFragmentDefinition fragment;

    [Header("事件：接 UI 字幕、黑霧變淡、音效")]
    public UnityEvent<string> onLineShown = new UnityEvent<string>();
    public UnityEvent<bool> onMutedChanged = new UnityEvent<bool>();
    public UnityEvent onCompleted = new UnityEvent();

    public ListenModel Listen { get; private set; }
    public bool IsComplete => Listen != null && Listen.IsComplete;
    public bool IsMuted => Listen != null && Listen.Muted;
    public MemoryFragmentDefinition Fragment => fragment;

    private EmptinessMeter meter;
    private readonly HashSet<Object> muters = new HashSet<Object>();

    private void Awake()
    {
        meter = FindAnyObjectByType<EmptinessMeter>();
        float lineDuration = meter != null ? meter.Config.lineDuration : 1.5f;
        int lines = fragment != null && fragment.echoLines != null && fragment.echoLines.Length > 0
            ? fragment.echoLines.Length : 3;
        Listen = new ListenModel(lines, lineDuration);
        Listen.LineCompleted += ShowNextLine;
        Listen.Completed += HandleCompleted;

        if (chapter != null && fragment != null &&
            ChapterProgress.GetFlag(chapter.chapterNumber, ChapterProgress.FragmentKey(fragment.order)))
            Listen.RestoreCompleted(); // 讀檔：已聽完
    }

    private void OnEnable() => Active.Add(this);
    private void OnDisable() => Active.Remove(this);

    private void OnDestroy()
    {
        if (Listen == null) return;
        Listen.LineCompleted -= ShowNextLine;
        Listen.Completed -= HandleCompleted;
    }

    /// <summary>傾聽開始時顯示第一句（或目前這一句）。</summary>
    public void BeginListening()
    {
        if (IsComplete) return;
        string line = LineAt(Listen.CompletedLines);
        if (line != null) onLineShown?.Invoke(line);
    }

    public void AddMuter(Object source)
    {
        if (source == null || IsComplete) return;
        bool wasMuted = muters.Count > 0;
        muters.Add(source);
        if (!wasMuted) { Listen.SetMuted(true); onMutedChanged?.Invoke(true); }
    }

    public void RemoveMuter(Object source)
    {
        if (!muters.Remove(source) || muters.Count > 0) return;
        Listen.SetMuted(false);
        onMutedChanged?.Invoke(false);
    }

    private void ShowNextLine(int finishedIndex)
    {
        string next = LineAt(finishedIndex + 1);
        if (next != null) onLineShown?.Invoke(next);
    }

    private string LineAt(int index) =>
        fragment != null && fragment.echoLines != null && index >= 0 && index < fragment.echoLines.Length
            ? fragment.echoLines[index] : null;

    private void HandleCompleted()
    {
        if (meter != null) meter.CompleteEcho();
        if (chapter != null && fragment != null)
            ChapterProgress.SetFlag(chapter.chapterNumber, ChapterProgress.FragmentKey(fragment.order));
        if (fragment != null && fragment.cgItem != null && InventoryManager.Instance != null)
            InventoryManager.Instance.AddItem(fragment.cgItem);
        onCompleted?.Invoke();
    }
}
