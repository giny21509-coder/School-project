using MoreMountains.TopDownEngine;
using UnityEngine;

/// <summary>
/// 雜訊怪（設計文件 §4.5）：走向最近一個還沒聽完的回音，碰到就把它蓋住；被擊敗後解除。
/// 生命值與受傷沿用 TopDownEngine Health；這裡只處理移動與「蓋住」。
/// </summary>
[RequireComponent(typeof(Health))]
public class NoiseFillerAI : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1.6f;
    [SerializeField] private float coverRadius = 0.8f;
    [SerializeField] private Rigidbody2D body;

    private Health health;
    private EchoListenTarget target;
    private bool covering;

    private void Awake()
    {
        health = GetComponent<Health>();
        if (body == null) body = GetComponent<Rigidbody2D>();
    }

    private void OnEnable() => health.OnDeath += Release;
    private void OnDisable()
    {
        health.OnDeath -= Release;
        Release();
    }

    private void FixedUpdate()
    {
        if (target == null || target.IsComplete) { Release(); target = FindTarget(); }
        if (target == null) return;

        Vector2 me = body != null ? body.position : (Vector2)transform.position;
        Vector2 to = (Vector2)target.transform.position - me;
        if (to.magnitude <= coverRadius)
        {
            if (!covering) { target.AddMuter(this); covering = true; }
            return;
        }
        Vector2 next = me + to.normalized * moveSpeed * Time.fixedDeltaTime;
        if (body != null) body.MovePosition(next); else transform.position = next;
    }

    private EchoListenTarget FindTarget()
    {
        EchoListenTarget best = null;
        float bestSq = float.MaxValue;
        foreach (var echo in EchoListenTarget.Active)
        {
            if (echo == null || echo.IsComplete) continue;
            float d = ((Vector2)echo.transform.position - (Vector2)transform.position).sqrMagnitude;
            if (d < bestSq) { bestSq = d; best = echo; }
        }
        return best;
    }

    private void Release()
    {
        if (covering && target != null) target.RemoveMuter(this);
        covering = false;
    }
}
