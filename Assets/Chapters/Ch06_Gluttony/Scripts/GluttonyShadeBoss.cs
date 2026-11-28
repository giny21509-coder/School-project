using System.Collections;
using System.Collections.Generic;
using JianQi.Chapters;
using JianQi.Ch06;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 最終戰「暴食之影」（設計文件 §3 S4）：每回合 攻擊段 → 停頓選草稿。規則在 ShadeBossModel。
/// 攻擊招式（吞嚥、食物雨、污漬區）請做成子物件，用下面的事件開關；這裡只管節奏與存檔。
/// </summary>
public class GluttonyShadeBoss : MonoBehaviour
{
    [SerializeField] private ChapterDefinition chapter;
    [SerializeField] private ChapterRunner runner;
    [SerializeField] private EmptinessMeter meter;
    [SerializeField] private DraftCardMenu menu;
    [Tooltip("每回合的正確草稿 ID（設計文件：D1 老友、D2 前妻、D3 自己）")]
    [SerializeField] private string[] correctDraftPerRound = { "D1", "D2", "D3" };

    [Header("招式開關：攻擊段開始時 true、停頓時 false")]
    public UnityEvent<bool> onAttackActive = new UnityEvent<bool>();
    [Header("演出：回合台詞（例如「以前……大家都會等我。」）用 Fungus 接")]
    public UnityEvent<int> onPauseStarted = new UnityEvent<int>();   // 回合（0 開始）
    public UnityEvent onWrongChoice = new UnityEvent();              // 「……不是這個」
    public UnityEvent<int> onRoundCleared = new UnityEvent<int>();   // 縮小一階
    public UnityEvent<int> onFinished = new UnityEvent<int>();       // 參數：結局 CG 數

    public ShadeBossModel Model { get; private set; }
    private int Ch => chapter != null ? chapter.chapterNumber : 6;

    private void Start()
    {
        var unlocked = new List<string>();
        if (chapter != null && chapter.drafts != null)
            foreach (var d in chapter.drafts)
                if (d != null && ChapterProgress.GetFlag(Ch, ChapterProgress.DraftKey(d.draftId))) unlocked.Add(d.draftId);

        Model = new ShadeBossModel(correctDraftPerRound, unlocked);
        Model.Restore(ChapterProgress.GetInt(Ch, "boss/round"), ChapterProgress.GetInt(Ch, "boss/fallback"));
        Model.RoundCleared += HandleRoundCleared;
        Model.Finished += HandleFinished;

        if (Model.IsFinished) HandleFinished();
        else StartCoroutine(AttackThenPause());
    }

    private void OnDestroy()
    {
        if (Model == null) return;
        Model.RoundCleared -= HandleRoundCleared;
        Model.Finished -= HandleFinished;
    }

    /// <summary>玩家死亡重試：回到目前回合的攻擊段（已完成的回合保留）。</summary>
    public void RestartRound()
    {
        StopAllCoroutines();
        if (menu != null) menu.Hide();
        Model.Restore(Model.Round, Model.FallbackMask);
        if (!Model.IsFinished) StartCoroutine(AttackThenPause());
    }

    private IEnumerator AttackThenPause()
    {
        onAttackActive?.Invoke(true);
        float duration = meter != null ? meter.Config.BossAttackDuration(meter.Normalized) : 10f;
        yield return new WaitForSeconds(duration);
        onAttackActive?.Invoke(false);

        if (!Model.BeginChoosing()) yield break;
        onPauseStarted?.Invoke(Model.Round);
        if (menu != null) menu.Show(Model.GetOptions(), chapter, HandleChoice);
        else Debug.LogWarning("GluttonyShadeBoss：未指定 DraftCardMenu。", this);
    }

    private void HandleChoice(string optionId)
    {
        ChoiceResult result = Model.Choose(optionId);
        switch (result)
        {
            case ChoiceResult.Wrong:
                if (meter != null) meter.WrongCard();
                onWrongChoice?.Invoke();
                StartCoroutine(AttackThenPause());
                break;
            case ChoiceResult.Invalid:
                if (menu != null) menu.Show(Model.GetOptions(), chapter, HandleChoice);
                break;
            default:
                Save();
                if (!Model.IsFinished) StartCoroutine(AttackThenPause());
                break;
        }
    }

    private void HandleRoundCleared(int round) => onRoundCleared?.Invoke(round);

    private void HandleFinished()
    {
        Save();
        onFinished?.Invoke(Model.EndingCgCount);
        if (runner != null) runner.AdvanceTo(ChapterStage.Release);
    }

    private void Save()
    {
        ChapterProgress.SetInt(Ch, "boss/round", Model.Round);
        ChapterProgress.SetInt(Ch, "boss/fallback", Model.FallbackMask);
    }
}
