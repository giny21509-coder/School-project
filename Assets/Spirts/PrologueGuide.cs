using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>沿用老師的進度指引，操作文字配合目前的拍攝戰鬥。</summary>
public sealed class PrologueGuide : MonoBehaviour
{
    private InnerWorldDistanceSpawner spawner;
    private PlayerController player;
    private PrologueExplorationGuide display;
    private int sceneHandle = -1;
    private float nextSearch;

    private void Update()
    {
        Scene active = SceneManager.GetActiveScene();
        if (sceneHandle != active.handle)
        {
            sceneHandle = active.handle;
            spawner = null;
            player = null;
            nextSearch = 0f;
        }
        if (active.name != "Cat internal world" || (spawner != null && player != null)
            || Time.unscaledTime < nextSearch) return;
        nextSearch = Time.unscaledTime + 0.5f;
        foreach (var candidate in FindObjectsByType<InnerWorldDistanceSpawner>(FindObjectsSortMode.None))
            if (candidate.gameObject.scene == active) { spawner = candidate; break; }
        foreach (var candidate in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            if (candidate.gameObject.scene == active) { player = candidate; break; }
        display = GetComponent<PrologueExplorationGuide>();
    }

    private string GetPrompt()
    {
        if (spawner.CompletionReady) return "最後的等待｜靠近貓咪按 R 互動；Enter 繼續對話。";
        if (spawner.ImageCollected >= InnerWorldDistanceSpawner.TotalImages && !spawner.ItemCollected)
            return "找到空碗｜F 切換拍攝模式；瞄準後按左鍵破霧、攻擊，靠近掉落的空碗按 R 收集。";
        return "尋找記憶｜記憶 " + spawner.ImageCollected + "/5　退霧 " + spawner.ImageDefeated
            + "/5｜WASD 移動；F 開關相機；滑鼠瞄準、左鍵拍攝；R 收集。";
    }

    private void OnGUI()
    {
        if (SceneManager.GetActiveScene().name != "Cat internal world" || spawner == null || player == null
            || !player.canMove || display == null || Time.timeScale <= 0f
            || PrologueExplorationGuide.IsDialogueRunning) return;
        display.DrawPrompt(GetPrompt(), 1080f);
    }
}
