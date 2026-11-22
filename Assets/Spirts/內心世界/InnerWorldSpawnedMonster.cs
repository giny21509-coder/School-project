using UnityEngine;
using UnityEngine.Events;
using MoreMountains.TopDownEngine;

/// <summary>生成器自動加入；死亡、收集、離場是三個獨立回報。</summary>
public class InnerWorldSpawnedMonster : MonoBehaviour
{
    public InnerWorldDistanceSpawner.MonsterKind Kind { get; private set; }
    public int TargetId { get; private set; } = -1;
    public bool IsDefeated { get; private set; }
    public string CollectionSaveId => owner != null ? owner.GetCollectionSaveId(Kind, TargetId) : "";

    private InnerWorldDistanceSpawner owner;
    private Chatacher health;
    private Health topDownHealth;
    private GameObject patrolAnchors;

    public void Initialize(InnerWorldDistanceSpawner spawner, InnerWorldDistanceSpawner.MonsterKind kind,
        int id, Transform player, float patrolHalfWidth)
    {
        owner = spawner;
        Kind = kind;
        TargetId = id;
        InnerWorldMistShield shield = GetComponent<InnerWorldMistShield>();
        if (kind != InnerWorldDistanceSpawner.MonsterKind.Normal)
        {
            if (shield == null) shield = gameObject.AddComponent<InnerWorldMistShield>();
            shield.enabled = true;
            shield.Initialize(spawner.GetShieldFlashes(kind));
        }
        else if (shield != null)
        {
            shield.enabled = false;
        }
        // 擊敗進度與 CG 掉落使用同一套血量；優先使用啟用的 TopDown Health。
        topDownHealth = GetComponent<Health>();
        if (topDownHealth != null && topDownHealth.isActiveAndEnabled)
        {
            topDownHealth.OnDeath += NotifyDefeated;
        }
        else
        {
            topDownHealth = null;
            health = GetComponent<Chatacher>();
            if (health != null && health.isActiveAndEnabled)
            {
                if (health.OnDie == null) health.OnDie = new UnityEvent();
                health.OnDie.AddListener(NotifyDefeated);
            }
            else
            {
                health = null;
            }
        }
        var enemy = GetComponent<Enemy>();
        if (enemy == null) return;
        enemy.player = player;
        // 動態生成的 Prefab 不能引用場景巡邏點；缺少時在世界座標建立。
        if (enemy.pointA == null || enemy.pointB == null)
        {
            patrolAnchors = new GameObject(name + "_PatrolAnchors");
            patrolAnchors.transform.SetParent(owner.transform, false);
            if (enemy.pointA == null) enemy.pointA = CreateAnchor("A", -patrolHalfWidth);
            if (enemy.pointB == null) enemy.pointB = CreateAnchor("B", patrolHalfWidth);
        }
    }

    private Transform CreateAnchor(string label, float offset)
    {
        var anchor = new GameObject(label).transform;
        anchor.SetParent(patrolAnchors.transform, false);
        anchor.position = transform.position + Vector3.right * offset;
        return anchor;
    }

    public void NotifyDefeated()
    {
        if (IsDefeated) return;
        IsDefeated = true;
        if (owner != null)
        {
            if (Kind == InnerWorldDistanceSpawner.MonsterKind.Normal)
                owner.ReportNormalDefeated(transform.position);
            else
                owner.ReportDefeated(Kind, TargetId);
        }
    }

    // 僅在獎勵確實收集成功後呼叫；不能把 OnDie 直接當成收集。
    public void Collect()
    {
        if (owner != null) owner.CollectTarget(Kind, TargetId);
    }

    public void BindPickup(ItemPickup pickup)
    {
        if (owner != null) owner.BindPickup(Kind, TargetId, pickup);
    }

    private void OnDestroy()
    {
        if (health != null && health.OnDie != null) health.OnDie.RemoveListener(NotifyDefeated);
        if (topDownHealth != null) topDownHealth.OnDeath -= NotifyDefeated;
        if (patrolAnchors != null) Destroy(patrolAnchors);
        if (owner != null) owner.Release(this);
    }
}
