using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 一個 Zone 的入口與出口（設計文件 §3 S3、§6）。
/// - 玩家進入入口觸發區：設定是否自然上升、記錄進入時的 E（死亡時回到這個值）、啟用該 Zone 的生成器。
/// - 該 Zone 的回音全部聽完：打開出口的黑霧牆、存檔 ch6/zone。
/// </summary>
public class ZoneGate : MonoBehaviour
{
    [SerializeField] private ChapterDefinition chapter;
    [SerializeField] private EmptinessMeter meter;
    [Tooltip("0 = Zone A、1 = B、2 = C")]
    [SerializeField, Range(0, 2)] private int zoneIndex;
    [Tooltip("Zone B、C 會自然上升")]
    [SerializeField] private bool drifting;
    [SerializeField] private EchoListenTarget[] echoes = new EchoListenTarget[0];
    [SerializeField] private FillerSpawner spawner;
    [Tooltip("出口的黑霧牆（含阻擋用 Collider）")]
    [SerializeField] private GameObject exitMistWall;
    [SerializeField] private string playerTag = "Player";

    public UnityEvent onZoneEntered = new UnityEvent();
    public UnityEvent onZoneCleared = new UnityEvent();

    public int ZoneIndex => zoneIndex;
    public float EntryEmptiness { get; private set; }
    public bool Cleared { get; private set; }

    private void Start()
    {
        if (spawner != null) spawner.enabled = false;
        foreach (var e in echoes) if (e != null) e.onCompleted.AddListener(CheckCleared);
        CheckCleared(); // 讀檔：回音都已聽完時直接開門
    }

    private void OnDestroy()
    {
        foreach (var e in echoes) if (e != null) e.onCompleted.RemoveListener(CheckCleared);
    }

    // 入口觸發區：請在這個物件上放一個 Is Trigger 的 Collider2D
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (meter != null)
        {
            meter.Drifting = drifting && !Cleared;
            EntryEmptiness = meter.Value;
        }
        if (spawner != null) spawner.enabled = !Cleared;
        onZoneEntered?.Invoke();
    }

    /// <summary>玩家在這個 Zone 死亡、重試時呼叫（接 PlayerDeathUI 的重試）。</summary>
    public void RestartZone()
    {
        if (meter != null) meter.ResetTo(EntryEmptiness);
        if (spawner != null) spawner.DespawnAll();
    }

    private void CheckCleared()
    {
        if (Cleared) return;
        foreach (var e in echoes) if (e != null && !e.IsComplete) return;
        Cleared = true;
        if (exitMistWall != null) exitMistWall.SetActive(false);
        if (spawner != null) { spawner.DespawnAll(); spawner.enabled = false; }
        if (meter != null) meter.Drifting = false;
        if (chapter != null)
        {
            int saved = ChapterProgress.GetInt(chapter.chapterNumber, "zone");
            if (zoneIndex + 1 > saved) ChapterProgress.SetInt(chapter.chapterNumber, "zone", zoneIndex + 1);
        }
        onZoneCleared?.Invoke();
    }
}
