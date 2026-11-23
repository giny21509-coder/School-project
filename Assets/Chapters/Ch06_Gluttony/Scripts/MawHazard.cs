using UnityEngine;

/// <summary>
/// 「嘴」（設計文件 §4.5）：固定在地面；E ≥ 門檻時把範圍內的玩家往牠吸。
/// 咬人的傷害請在 Prefab 上用 TopDownEngine 的 DamageOnTouch。
/// </summary>
public class MawHazard : MonoBehaviour
{
    [SerializeField] private EmptinessMeter meter;
    [SerializeField] private PlayerController player;
    [SerializeField] private float pullRange = 4f;

    private void Start()
    {
        if (meter == null) meter = FindAnyObjectByType<EmptinessMeter>();
        if (player == null) player = FindAnyObjectByType<PlayerController>();
    }

    private void FixedUpdate()
    {
        if (meter == null || player == null) return;
        float pull = meter.Config.MawPull(meter.Value);
        if (pull <= 0f) return;
        Vector2 to = (Vector2)transform.position - (Vector2)player.transform.position;
        if (to.sqrMagnitude > pullRange * pullRange) return;
        player.AddExternalVelocity(to.normalized * pull);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.6f, 0.2f, 0.4f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, pullRange);
    }
}
