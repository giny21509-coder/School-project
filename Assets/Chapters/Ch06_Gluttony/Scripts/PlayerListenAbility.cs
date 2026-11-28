using MoreMountains.TopDownEngine;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 掛在玩家身上。按住傾聽鍵：
/// - 附近有未完成的回音 → 停止移動與攻擊，累積該回音的進度；受傷或放開 → 這一句重來。
/// - 失控中且附近沒有回音 → 「傾聽自己」：原地按住 N 秒，E −30（設計文件 §4.4）。
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerListenAbility : MonoBehaviour
{
    [SerializeField] private EmptinessMeter meter;
    [SerializeField] private Health health;

    public UnityEvent<float> onSelfListenProgress = new UnityEvent<float>(); // 0..1，給 UI
    public UnityEvent onSelfListenDone = new UnityEvent();

    public EchoListenTarget Current { get; private set; }
    public bool IsListening { get; private set; }

    private PlayerController controller;
    private float selfListenTimer;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        if (health == null) health = GetComponent<Health>();
    }

    private void OnEnable() { if (health != null) health.OnHit += HandleHit; }
    private void OnDisable()
    {
        if (health != null) health.OnHit -= HandleHit;
        StopListening();
    }

    private void Update()
    {
        if (Time.timeScale <= 0f) return;
        bool held = Ch06Input.ListenHeld;

        if (!held) { StopListening(); ResetSelfListen(); return; }

        if (!IsListening)
        {
            Current = FindNearestEcho();
            if (Current != null) StartListening();
        }

        if (IsListening && Current != null)
        {
            Current.Listen.Hold(Time.deltaTime);
            if (Current.IsComplete || Current.IsMuted) StopListening();
            return;
        }

        // 沒有回音可聽：失控時改為「傾聽自己」
        if (meter != null && meter.Model.CanSelfListen)
        {
            controller.StopControl();
            selfListenTimer += Time.deltaTime;
            float hold = meter.Config.selfListenHold;
            onSelfListenProgress?.Invoke(Mathf.Clamp01(selfListenTimer / hold));
            if (selfListenTimer >= hold)
            {
                meter.TrySelfListen();
                onSelfListenDone?.Invoke();
                ResetSelfListen();
            }
        }
    }

    private EchoListenTarget FindNearestEcho()
    {
        float radius = meter != null ? meter.Config.listenRadius : 1.6f;
        EchoListenTarget best = null;
        float bestSq = radius * radius;
        foreach (var echo in EchoListenTarget.Active)
        {
            if (echo == null || echo.IsComplete || echo.IsMuted) continue;
            float d = ((Vector2)echo.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (d <= bestSq) { bestSq = d; best = echo; }
        }
        return best;
    }

    private void StartListening()
    {
        IsListening = true;
        controller.StopControl();
        if (meter != null) meter.Listening = true;
        Current.BeginListening();
    }

    private void StopListening()
    {
        if (!IsListening) return;
        if (Current != null && !Current.IsComplete) Current.Listen.Interrupt();
        IsListening = false;
        Current = null;
        if (meter != null) meter.Listening = false;
        controller.StarControl();
    }

    private void ResetSelfListen()
    {
        if (selfListenTimer > 0f && !IsListening) controller.StarControl();
        selfListenTimer = 0f;
        onSelfListenProgress?.Invoke(0f);
    }

    private void HandleHit()
    {
        if (IsListening && Current != null) Current.Listen.Interrupt();
        if (selfListenTimer > 0f) ResetSelfListen();
    }
}
