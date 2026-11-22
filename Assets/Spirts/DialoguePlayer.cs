using System.Collections;
using Fungus;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

/// <summary>
/// 共用的 Fungus 對話播放工具。
/// 任何腳本（EnemyDeathReward、ItemPickup...）都可以呼叫 DialoguePlayer.Play(...)
/// 來播放對話，不需要各自實作 coroutine runner。
/// </summary>
public static class DialoguePlayer
{
    private class Runner : MonoBehaviour { }

    private static Runner instance;

    private static Runner Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject runnerObject = new GameObject("DialoguePlayerRunner");
                instance = runnerObject.AddComponent<Runner>();
                Object.DontDestroyOnLoad(runnerObject);
            }

            return instance;
        }
    }

    public static void Play(
        Flowchart flowchart,
        string blockName,
        float delay,
        bool waitUntilFlowchartIsFree,
        UnityEvent onDialogueStarted)
    {
        if (string.IsNullOrWhiteSpace(blockName))
        {
            Debug.LogWarning("DialoguePlayer: Block Name 沒有設定。");
            return;
        }

        // 動態 Prefab 不能儲存場景引用，可用唯一的 Block 名稱找到場景對話。
        if (flowchart == null)
        {
            flowchart = FindSceneFlowchart(blockName);
            if (flowchart == null) return;
        }

        Instance.StartCoroutine(PlayRoutine(flowchart, blockName, delay, waitUntilFlowchartIsFree, onDialogueStarted));
    }

    private static Flowchart FindSceneFlowchart(string blockName)
    {
        Flowchart match = null;
        var activeScene = SceneManager.GetActiveScene();
        foreach (var candidate in Flowchart.CachedFlowcharts)
        {
            if (candidate == null || !candidate.isActiveAndEnabled ||
                candidate.gameObject.scene != activeScene || !candidate.HasBlock(blockName))
            {
                continue;
            }

            if (match != null)
            {
                Debug.LogWarning("DialoguePlayer: 場景中有多個 Flowchart 包含同名 Block：" + blockName + "，請改用唯一名稱或指定 Flowchart。");
                return null;
            }
            match = candidate;
        }

        if (match == null)
        {
            Debug.LogWarning("DialoguePlayer: 目前場景找不到啟用中且包含此 Block 的 Flowchart：" + blockName);
        }
        return match;
    }

    private static IEnumerator PlayRoutine(
        Flowchart flowchart,
        string blockName,
        float delay,
        bool waitUntilFlowchartIsFree,
        UnityEvent onDialogueStarted)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        if (waitUntilFlowchartIsFree)
        {
            while (flowchart != null && flowchart.HasExecutingBlocks())
            {
                yield return null;
            }
        }

        if (flowchart == null || !flowchart.HasBlock(blockName))
        {
            Debug.LogWarning("DialoguePlayer: Fungus block was not found: " + blockName);
            yield break;
        }

        flowchart.ExecuteBlock(blockName);
        onDialogueStarted?.Invoke();
    }
}
