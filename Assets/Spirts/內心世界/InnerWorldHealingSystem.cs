using System;
using MoreMountains.TopDownEngine;
using UnityEngine;

/// <summary>Normal-monster rewards stored for later use, independent of CG collection slots.</summary>
[DisallowMultipleComponent]
public class InnerWorldHealingSystem : MonoBehaviour
{
    [Header("連接")]
    [SerializeField] private InnerWorldDistanceSpawner spawner;
    [SerializeField] private Health playerHealth;
    [SerializeField] private HealingItemPickup pickupPrefab;

    [Header("每輪擊敗幾隻一般怪掉落一個補血道具")]
    [SerializeField, Min(1)] private int minimumNormalKills = 3;
    [SerializeField, Min(1)] private int maximumNormalKills = 4;
    [SerializeField] private Vector3 dropOffset = new Vector3(0f, 0.35f, 0f);

    [Header("使用補血道具")]
    [SerializeField, Min(1)] private int healAmount = 2;
    [SerializeField] private KeyCode useKey = KeyCode.H;

    public event Action InventoryChanged;
    public int StoredCount => supplies != null ? supplies.StoredCount : 0;
    public int KillsSinceDrop => supplies != null ? supplies.KillsSinceDrop : 0;
    public int NextDropAt => supplies != null ? supplies.NextDropAt : 0;
    public int HealAmount => healAmount;
    public KeyCode UseKey => useKey;
    public Transform PlayerTransform => playerHealth != null ? playerHealth.transform : null;
    public bool PlayerAlive => playerHealth != null && playerHealth.isActiveAndEnabled
        && playerHealth.CurrentHealth > 0f;
    public bool IsFullHealth => PlayerAlive && playerHealth.CurrentHealth >= playerHealth.MaximumHealth;
    public bool CanUseItem => PlayerAlive && !IsFullHealth && StoredCount > 0 && Time.timeScale > 0f;

    private HealingSuppliesState supplies;
    private InnerWorldDistanceSpawner subscribedSpawner;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void OnEnable()
    {
        if (subscribedSpawner != null)
            subscribedSpawner.NormalMonsterDefeated -= HandleNormalDefeated;
        EnsureInitialized();
        if (spawner == null) spawner = GetComponent<InnerWorldDistanceSpawner>();
        if (spawner != null)
        {
            subscribedSpawner = spawner;
            subscribedSpawner.NormalMonsterDefeated += HandleNormalDefeated;
        }
    }

    private void OnDisable()
    {
        if (subscribedSpawner != null)
            subscribedSpawner.NormalMonsterDefeated -= HandleNormalDefeated;
        subscribedSpawner = null;
    }

    private void Update()
    {
        if (Input.GetKeyDown(useKey)) TryUseItem();
    }

    public bool TryStorePickup(int amount = 1)
    {
        EnsureInitialized();
        if (!PlayerAlive || !supplies.Store(amount)) return false;
        InventoryChanged?.Invoke();
        return true;
    }

    public bool TryUseItem()
    {
        if (!CanUseItem) return false;
        float previousHealth = playerHealth.CurrentHealth;
        playerHealth.ReceiveHealth(healAmount, gameObject);
        // Only consume an item after healing actually succeeds.
        if (playerHealth.CurrentHealth <= previousHealth) return false;
        supplies.TryConsume();
        InventoryChanged?.Invoke();
        return true;
    }

    private void HandleNormalDefeated(Vector3 deathPosition)
    {
        if (pickupPrefab == null)
        {
            Debug.LogWarning("補血系統：請指定 Healing Pickup Prefab。", this);
            return;
        }
        EnsureInitialized();
        if (!supplies.RegisterNormalDefeat()) return;
        HealingItemPickup pickup = Instantiate(pickupPrefab,
            deathPosition + dropOffset, Quaternion.identity, transform);
        pickup.Initialize(this);
        pickup.gameObject.SetActive(true);
    }

    private void EnsureInitialized()
    {
        if (supplies != null) return;
        supplies = new HealingSuppliesState(Mathf.Max(1, minimumNormalKills),
            Mathf.Max(Mathf.Max(1, minimumNormalKills), maximumNormalKills),
            (minimum, exclusiveMaximum) => UnityEngine.Random.Range(minimum, exclusiveMaximum));
    }

    private void OnValidate()
    {
        minimumNormalKills = Mathf.Clamp(minimumNormalKills, 1, 10000);
        maximumNormalKills = Mathf.Clamp(maximumNormalKills, minimumNormalKills, 10000);
        healAmount = Mathf.Max(1, healAmount);
    }
}
