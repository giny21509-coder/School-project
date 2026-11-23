using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// S2 顯形拍攝（設計文件 §3 S2）：在既有 CameraShootingEncounter 上加「進食／停頓」循環。
/// 只有停頓（啜泣）時拍攝才算命中，停頓時目標也不再移動。
/// 用法：放在場景裡，把 CameraShootingEncounter.onEncounterStarted 接到 Begin()、onEncounterCompleted 接到 End()。
/// </summary>
public class PauseWindowEncounter : MonoBehaviour
{
    [SerializeField] private GluttonyConfig config;
    [SerializeField] private CameraShootingEncounter encounter;

    [Header("表現：雜訊淡入淡出、抬頭動畫、啜泣聲（無障礙：畫面提示一定要有）")]
    public UnityEvent onEatingStarted = new UnityEvent();
    public UnityEvent onPauseStarted = new UnityEvent();

    public bool IsPaused { get; private set; }
    private bool running;
    private float phaseTimer;

    public void Begin()
    {
        if (encounter == null) encounter = CameraShootingEncounter.Instance;
        if (encounter == null || config == null) return;
        encounter.HitGate = () => IsPaused;
        running = true;
        EnterEating();
    }

    public void End()
    {
        running = false;
        IsPaused = false;
        if (encounter == null) return;
        encounter.HitGate = null;
        encounter.FreezeMovement = false;
    }

    private void OnDisable() => End();

    private void Update()
    {
        if (!running || Time.timeScale <= 0f) return;
        phaseTimer -= Time.deltaTime;
        if (phaseTimer > 0f) return;
        if (IsPaused) EnterEating(); else EnterPause();
    }

    private void EnterEating()
    {
        IsPaused = false;
        encounter.FreezeMovement = false;
        phaseTimer = Random.Range(config.eatingDurationMin, config.eatingDurationMax);
        onEatingStarted?.Invoke();
    }

    private void EnterPause()
    {
        IsPaused = true;
        encounter.FreezeMovement = true;
        phaseTimer = config.pauseHitWindow;
        onPauseStarted?.Invoke();
    }
}
