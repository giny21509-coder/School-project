using UnityEngine;

/// <summary>Walking near a dropped item stores it; collecting never heals immediately.</summary>
[DisallowMultipleComponent]
public class HealingItemPickup : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float collectRadius = 1.2f;
    [SerializeField] private Transform visual;
    [SerializeField, Min(0f)] private float bobHeight = 0.08f;
    [SerializeField, Min(0f)] private float bobSpeed = 3f;
    [SerializeField, Min(0f)] private float collectDelay = 0.35f;

    private InnerWorldHealingSystem owner;
    private Vector3 initialVisualPosition;
    private float appearedAt;
    private bool collected;

    public void Initialize(InnerWorldHealingSystem system)
    {
        owner = system;
        appearedAt = Time.time;
        collected = false;
        if (visual != null) initialVisualPosition = visual.localPosition;
    }

    private void Update()
    {
        if (owner == null || collected || Time.timeScale <= 0f) return;
        if (visual != null)
            visual.localPosition = initialVisualPosition
                + Vector3.up * (Mathf.Sin((Time.time - appearedAt) * bobSpeed) * bobHeight);
        if (!owner.PlayerAlive || Time.time - appearedAt < collectDelay) return;
        Vector2 offset = owner.PlayerTransform.position - transform.position;
        if (offset.sqrMagnitude > collectRadius * collectRadius) return;
        if (!owner.TryStorePickup()) return;
        collected = true;
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.5f, 0.7f);
        Gizmos.DrawWireSphere(transform.position, collectRadius);
    }
}
