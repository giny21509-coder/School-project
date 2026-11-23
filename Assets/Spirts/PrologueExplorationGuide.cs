using Fungus;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>依目前故事進度顯示探索目標，不攔截玩家操作。</summary>
public sealed class PrologueExplorationGuide : MonoBehaviour
{
    private static PrologueExplorationGuide instance;
    private static int dialogueFrame = -1;
    private static bool dialogueRunning;
    private Font font;
    private GUIStyle style;
    private PlayerController player;
    private int sceneHandle = -1;
    private float nextPlayerSearch;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        dialogueFrame = -1;
        dialogueRunning = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<PrologueExplorationGuide>() != null) return;
        var root = new GameObject("Story exploration guide");
        DontDestroyOnLoad(root);
        root.AddComponent<PrologueExplorationGuide>();
        root.AddComponent<PrologueGuide>();
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
    }

    public static bool IsDialogueRunning
    {
        get
        {
            if (dialogueFrame == Time.frameCount) return dialogueRunning;
            dialogueFrame = Time.frameCount;
            dialogueRunning = false;
            foreach (var flow in Flowchart.CachedFlowcharts)
                if (flow != null && flow.isActiveAndEnabled && flow.HasExecutingBlocks())
                { dialogueRunning = true; break; }
            return dialogueRunning;
        }
    }

    private void Update()
    {
        Scene active = SceneManager.GetActiveScene();
        if (sceneHandle != active.handle)
        {
            sceneHandle = active.handle;
            player = null;
            nextPlayerSearch = 0f;
        }
        if (player != null || Time.unscaledTime < nextPlayerSearch) return;
        nextPlayerSearch = Time.unscaledTime + 0.5f;
        foreach (var candidate in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            if (candidate.gameObject.scene == active) { player = candidate; break; }
    }

    private string GetPrompt(string scene)
    {
        switch (scene)
        {
            case "office":
                bool hasCommission = InventoryManager.Instance != null && InventoryManager.Instance.questSlots != null
                    && System.Array.Exists(InventoryManager.Instance.questSlots, item => item != null && item.itemId == "Quest_min");
                if (hasCommission) return "已接下委託｜從左側門出發（E），前往住宅區。";
                if (GameDataManager.isItemUnlocked) return "查看桌上的委託單（R），再從左側門出發（E）。";
                return GameProgressState.GetInt("OfficeWindowOpened") == 1
                    ? "從右側門前往走廊（E），回房間尋找相機。"
                    : "沿桌邊繞行，靠近窗戶按 R 開窗。";
            case "corridor": return "右側是房間，左側是事務所；靠近門按 E。";
            case "room": return GameDataManager.isItemUnlocked
                ? "看完桌邊的筆記後，從門口返回事務所（E）。"
                : "查看右側桌上的相機（R）；看完筆記後返回事務所。";
            case "office Front": return "沿街向右，前往委託單上的住宅區。";
            case "Neighborhood": return "向右尋找鄰居，靠近後按 R 詢問。";
            case "Neighborhood  Cat Run": return "向右追蹤黑影；道路盡頭按 E 前往奶奶家。";
            case "Grandma's house In front": return "靠近右側大門，按 E 進屋。";
            case "Grandma's living room": return "調查屋內的生活痕跡；從右下方前往廚房（E）。";
            case "Grandma s kitchen":
                foreach (var target in FindObjectsByType<WisherPhotoTarget>(FindObjectsSortMode.None))
                    if (target.gameObject.scene == SceneManager.GetActiveScene() && target.FullyRevealed)
                        return "貓咪已顯形｜靠近貓咪按 R，繼續調查牠的記憶。";
                return "靠近黑影按左 Ctrl 使用相機，瞄準後按滑鼠左鍵拍攝。";
            default: return null;
        }
    }

    private void OnGUI()
    {
        if (player == null || !player.canMove || Time.timeScale <= 0f || IsDialogueRunning) return;
        if (CameraShootingEncounter.Instance != null && CameraShootingEncounter.Instance.EncounterActive) return;
        string prompt = GetPrompt(SceneManager.GetActiveScene().name);
        if (prompt != null) DrawPrompt(prompt, 900f);
    }

    public void DrawPrompt(string prompt, float maximumWidth)
    {
        if (style == null)
        {
            font = Font.CreateDynamicFontFromOSFont("Microsoft JhengHei", 20);
            style = new GUIStyle(GUI.skin.label) { font = font, wordWrap = true, alignment = TextAnchor.MiddleLeft };
            style.normal.textColor = new Color(1f, 0.94f, 0.82f);
        }
        style.fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height / 36f), 14, 22);
        Rect safe = Screen.safeArea;
        float width = Mathf.Max(1f, Mathf.Min(maximumWidth, safe.width - 32f));
        float inset = Mathf.Min(14f, width * 0.04f);
        float height = style.CalcHeight(new GUIContent(prompt), Mathf.Max(1f, width - inset * 2f)) + 20f;
        var rect = new Rect(safe.x + (safe.width - width) * 0.5f,
            Screen.height - safe.y - height - 16f, width, height);
        GUI.Box(rect, GUIContent.none);
        GUI.Label(new Rect(rect.x + inset, rect.y + 10f, Mathf.Max(1f, width - inset * 2f), height - 20f), prompt, style);
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        if (font != null) Destroy(font);
    }
}
