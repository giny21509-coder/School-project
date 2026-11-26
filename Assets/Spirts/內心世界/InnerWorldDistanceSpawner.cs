using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>本次探索的距離生成器。圖片怪五個名額，物品怪一個名額。</summary>
public class InnerWorldDistanceSpawner : MonoBehaviour
{
    public const int TotalImages = 5;
    public enum MonsterKind { Normal, Image, Item }
    public enum SpawnAreaMode { HorizontalFloorBand, ExactColliderArea }

    [Serializable]
    public class WeightedPrefab
    {
        public GameObject prefab;
        [Min(0f)] public float weight = 1f;
    }

    private class Target
    {
        public GameObject prefab;
        public bool spawned;
        public bool defeated;
        public bool collected;
        public InnerWorldSpawnedMonster monster;
        public ItemPickup pickup;
    }

    [Header("玩家與鏡頭（生成器請放在不移動的空物件）")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private bool autoStart = true;
    [SerializeField] private bool moveRight = true;

    [Header("每前進多少距離，判定一個區段")]
    [SerializeField, Min(1f)] private float chunkSize = 10f;
    [SerializeField, Min(1f)] private float spawnAhead = 20f;
    [Tooltip("未指定 Spawn Areas 時，沿用此固定高度。")]
    [SerializeField] private float spawnY;
    [SerializeField, Min(0f)] private float minimumSeparation = 2f;
    [SerializeField, Min(1f)] private float despawnBuffer = 20f;
    [SerializeField, Min(0.1f)] private float patrolHalfWidth = 3f;

    [Header("地板生成範圍（場景中的 Collider 2D，建議勾 Is Trigger）")]
    [Tooltip("水平地板帶：用框框的世界 Y 上下限，X 隨區段無限延伸。精確區域：必須在 Collider 實際形狀內。")]
    [SerializeField] private SpawnAreaMode spawnAreaMode = SpawnAreaMode.HorizontalFloorBand;
    [Tooltip("水平地板帶請使用 Box Collider 2D。空陣列會沿用 Spawn Y。")]
    [SerializeField] private Collider2D[] spawnAreas = new Collider2D[0];

    [Header("一般怪：同時存活上限與每區段機率")]
    [SerializeField, Min(0)] private int maxNormal = 6;
    [SerializeField, Range(0f, 1f)] private float normalChance = 0.6f;
    [SerializeField] private WeightedPrefab[] normalPool = new WeightedPrefab[0];

    [Header("圖片怪：至少填五格；洗牌後取五格（可放相同 Prefab）")]
    [SerializeField] private GameObject[] imagePool = new GameObject[TotalImages];
    [SerializeField, Min(1)] private int imageMinGap = 3;
    [SerializeField, Range(0f, 1f)] private float imageBaseChance = 0.10f;
    [SerializeField, Min(0.001f)] private float imageStepChance = 0.08f;

    [Header("物品怪：只需收集一隻，同時最多一隻")]
    [SerializeField] private WeightedPrefab[] itemPool = new WeightedPrefab[0];
    [SerializeField, Range(0f, 1f)] private float itemChance = 0.25f;
    [SerializeField, Min(1)] private int itemCooldownMin = 2;
    [SerializeField, Min(1)] private int itemCooldownMax = 4;
    [Tooltip("連續幾次符合冷卻條件卻沒抽中後，下一次保證生成。")]
    [SerializeField, Min(1)] private int itemGuaranteeAttempts = 8;

    [Header("Mist Shield / Spirit Flash")]
    [SerializeField, Min(1)] private int imageShieldFlashes = 3;
    [SerializeField, Min(1)] private int itemShieldFlashes = 3;
    public int GetShieldFlashes(MonsterKind kind) => kind == MonsterKind.Item
        ? Mathf.Max(1, itemShieldFlashes) : Mathf.Max(1, imageShieldFlashes);

    [Header("接消霧、UI、開啟出口；不會自動離開場景")]
    public UnityEvent<int> onImageDefeated = new UnityEvent<int>();
    public UnityEvent<int> onImageCollected = new UnityEvent<int>();
    public UnityEvent onItemCollected = new UnityEvent();
    public UnityEvent onCompletionReady = new UnityEvent();

    // Normal kills carry a world position for consumable drops, separate from CG progress.
    public event Action<Vector3> NormalMonsterDefeated;

    public int ImageSpawned { get; private set; }
    public int ImageDefeated { get; private set; }
    public int ImageCollected { get; private set; }
    public bool ItemCollected => item != null && item.collected;
    public bool CompletionReady { get; private set; }
    public int AliveNormalCount
    {
        get
        {
            int count = 0;
            foreach (var m in monsters)
                if (m != null && m.Kind == MonsterKind.Normal && !m.IsDefeated) count++;
            return count;
        }
    }

    private readonly List<InnerWorldSpawnedMonster> monsters = new List<InnerWorldSpawnedMonster>();
    private Target[] images;
    private Target item;
    private bool initialized;
    private bool spawning;
    private bool hasFrontier;
    private float furthestX;
    private float nextChunkX;
    private int imageGap;
    private int itemCooldown;
    private int itemMisses;
    private float Direction => moveRight ? 1f : -1f;
    private string sessionId;

    private void Start()
    {
        if (autoStart) StartSpawning();
    }

    public void StartSpawning()
    {
        if (CompletionReady || !Initialize()) return;
        if (!hasFrontier)
        {
            furthestX = player.position.x * Direction;
            nextChunkX = furthestX + spawnAhead;
            hasFrontier = true;
        }
        spawning = true;
    }

    public void StopSpawning() { spawning = false; }

    private bool Initialize()
    {
        if (initialized) return true;
        if (player == null)
        {
            Debug.LogError("距離生成器：請指定 Player。", this);
            return false;
        }
        var pool = new List<GameObject>();
        if (imagePool != null)
            foreach (var prefab in imagePool) if (prefab != null) pool.Add(prefab);
        if (pool.Count < TotalImages || PickWeighted(itemPool) == null)
        {
            Debug.LogError("距離生成器：圖片池需至少五個有效格子，物品池需至少一個有效且權重大於零的 Prefab。", this);
            return false;
        }
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            var temp = pool[i]; pool[i] = pool[j]; pool[j] = temp;
        }
        images = new Target[TotalImages];
        for (int i = 0; i < TotalImages; i++) images[i] = new Target { prefab = pool[i] };
        item = new Target { prefab = PickWeighted(itemPool) };
        sessionId = Guid.NewGuid().ToString("N");
        if (worldCamera == null) worldCamera = Camera.main;
        PlayerController controller = player.GetComponent<PlayerController>();
        SpiritFollow spirit = controller != null ? controller.spiritFollow : null;
        if (spirit != null)
        {
            SpiritCameraFlash flash = spirit.GetComponent<SpiritCameraFlash>();
            if (flash == null) flash = spirit.gameObject.AddComponent<SpiritCameraFlash>();
            flash.Initialize();
        }
        else
        {
            Debug.LogWarning("Spirit flash: assign the player's Spirit Follow reference.", this);
        }
        initialized = true;
        return true;
    }

    private void Update()
    {
        if (!initialized || player == null) return;
        CleanupBehindCamera();
        if (!spawning) return;
        furthestX = Mathf.Max(furthestX, player.position.x * Direction);
        float edge = furthestX + spawnAhead;
        // 傳送或單幀移動過遠時跳過已在玩家後方的空區段，不在背後補出整群怪。
        if (nextChunkX + chunkSize < furthestX) nextChunkX = edge;
        int budget = 4;
        while (edge >= nextChunkX && budget-- > 0 && spawning)
        {
            float x0 = nextChunkX;
            nextChunkX += chunkSize;
            TrySpawnImage(x0, nextChunkX);
            TrySpawnNormal(x0, nextChunkX);
            TrySpawnItem(x0, nextChunkX);
        }
    }

    private void TrySpawnNormal(float x0, float x1)
    {
        if (AliveNormalCount >= maxNormal || UnityEngine.Random.value >= normalChance) return;
        Spawn(PickWeighted(normalPool), MonsterKind.Normal, -1, x0, x1);
    }

    private void TrySpawnImage(float x0, float x1)
    {
        imageGap = Mathf.Min(imageGap + 1, 1000000);
        int index = -1;
        // 先補漏掉的同一名額，再發新名額。重新出場不增加 ImageSpawned。
        for (int i = 0; i < TotalImages; i++)
            if (images[i].spawned && IsWaiting(images[i])) { index = i; break; }
        if (index < 0 && ImageSpawned < TotalImages) index = ImageSpawned;
        if (index < 0 || imageGap < imageMinGap) return;
        float chance = Mathf.Clamp01(imageBaseChance + (imageGap - imageMinGap) * imageStepChance);
        if (chance < 1f && UnityEngine.Random.value >= chance) return;
        var target = images[index];
        var instance = Spawn(target.prefab, MonsterKind.Image, index, x0, x1);
        if (instance == null) return;
        target.monster = instance;
        if (!target.spawned) { target.spawned = true; ImageSpawned++; }
        imageGap = 0;
    }

    private void TrySpawnItem(float x0, float x1)
    {
        if (itemCooldown > 0) itemCooldown--;
        if (itemCooldown > 0 || !IsWaiting(item)) return;
        itemMisses++;
        if (itemMisses < itemGuaranteeAttempts && UnityEngine.Random.value >= itemChance) return;
        var instance = Spawn(item.prefab, MonsterKind.Item, 0, x0, x1);
        if (instance == null) return;
        item.monster = instance;
        item.spawned = true;
        itemMisses = 0;
        itemCooldown = UnityEngine.Random.Range(itemCooldownMin, itemCooldownMax + 1);
    }

    private static bool IsWaiting(Target target)
    {
        return !target.collected && target.monster == null && target.pickup == null;
    }

    private InnerWorldSpawnedMonster Spawn(GameObject prefab, MonsterKind kind, int id, float x0, float x1)
    {
        if (prefab == null) return null;
        // 地板若由循環場景搬動，取位置前先更新 Collider 的世界座標。
        if (spawnAreas != null && spawnAreas.Length > 0) Physics2D.SyncTransforms();
        Vector3 position = Vector3.zero;
        bool found = false;
        for (int attempt = 0; attempt < 24; attempt++)
        {
            if (!TryPickSpawnPosition(x0, x1, out position)) continue;
            bool crowded = false;
            foreach (var m in monsters)
                if (m != null && Vector2.Distance(m.transform.position, position) < minimumSeparation)
                { crowded = true; break; }
            if (!crowded) { found = true; break; }
        }
        if (!found) return null;
        GameObject obj = Instantiate(prefab, position, prefab.transform.rotation);
        var member = obj.GetComponent<InnerWorldSpawnedMonster>();
        if (member == null) member = obj.AddComponent<InnerWorldSpawnedMonster>();
        member.Initialize(this, kind, id, player, patrolHalfWidth);
        monsters.Add(member);
        obj.SetActive(true);
        return member;
    }

    private bool TryPickSpawnPosition(float x0, float x1, out Vector3 position)
    {
        position = Vector3.zero;
        if (spawnAreas == null || spawnAreas.Length == 0)
        {
            position = new Vector3(UnityEngine.Random.Range(x0, x1) * Direction, spawnY, transform.position.z);
            return true;
        }
        float minX = Mathf.Min(x0 * Direction, x1 * Direction);
        float maxX = Mathf.Max(x0 * Direction, x1 * Direction);
        // 依區段與範圍的交集面積抽選，窄區域也能正常生成。
        float totalWeight = 0f;
        foreach (var area in spawnAreas) totalWeight += AreaWeight(area, minX, maxX);
        if (totalWeight <= 0f) return false;
        float roll = UnityEngine.Random.value * totalWeight;
        Collider2D selected = null;
        foreach (var area in spawnAreas)
        {
            float weight = AreaWeight(area, minX, maxX);
            if (weight <= 0f) continue;
            selected = area;
            roll -= weight;
            if (roll < 0f) break;
        }
        if (selected == null) return false;
        Bounds bounds = selected.bounds;
        if (spawnAreaMode == SpawnAreaMode.ExactColliderArea)
        {
            minX = Mathf.Max(minX, bounds.min.x);
            maxX = Mathf.Min(maxX, bounds.max.x);
        }
        position = new Vector3(UnityEngine.Random.Range(minX, maxX),
            UnityEngine.Random.Range(bounds.min.y, bounds.max.y), transform.position.z);
        // 水平地板帶明確使用世界 Y 上下限；精確模式另外檢查旋轉與多邊形形狀。
        return spawnAreaMode == SpawnAreaMode.HorizontalFloorBand || selected.OverlapPoint(position);
    }

    private float AreaWeight(Collider2D area, float minX, float maxX)
    {
        if (area == null || !area.enabled || !area.gameObject.activeInHierarchy) return 0f;
        if (spawnAreaMode == SpawnAreaMode.HorizontalFloorBand && !(area is BoxCollider2D)) return 0f;
        Bounds bounds = area.bounds;
        float width = spawnAreaMode == SpawnAreaMode.HorizontalFloorBand
            ? maxX - minX : Mathf.Min(maxX, bounds.max.x) - Mathf.Max(minX, bounds.min.x);
        return Mathf.Max(0f, width) * Mathf.Max(0f, bounds.size.y);
    }

    private static GameObject PickWeighted(WeightedPrefab[] pool)
    {
        if (pool == null) return null;
        float sum = 0f;
        foreach (var entry in pool)
            if (entry != null && entry.prefab != null && entry.weight > 0f) sum += entry.weight;
        if (sum <= 0f) return null;
        float roll = UnityEngine.Random.value * sum;
        GameObject fallback = null;
        foreach (var entry in pool)
        {
            if (entry == null || entry.prefab == null || entry.weight <= 0f) continue;
            fallback = entry.prefab;
            roll -= entry.weight;
            if (roll < 0f) return entry.prefab;
        }
        return fallback;
    }

    private float BackEdge()
    {
        if (worldCamera != null && worldCamera.orthographic)
            return worldCamera.transform.position.x * Direction - worldCamera.orthographicSize * worldCamera.aspect - despawnBuffer;
        return player.position.x * Direction - despawnBuffer;
    }

    private void CleanupBehindCamera()
    {
        float back = BackEdge();
        for (int i = monsters.Count - 1; i >= 0; i--)
        {
            var monster = monsters[i];
            if (monster == null) { monsters.RemoveAt(i); continue; }
            if (monster.transform.position.x * Direction >= back) continue;
            // 先停用，避免同一幀仍能攻擊；Destroy 後由名額狀態決定是否補出。
            monster.gameObject.SetActive(false);
            Destroy(monster.gameObject);
            monsters.RemoveAt(i);
        }
        foreach (var target in images) CleanupPickup(target, back);
        CleanupPickup(item, back);
    }

    private void CleanupPickup(Target target, float back)
    {
        if (target.pickup == null || target.pickup.transform.position.x * Direction >= back) return;
        target.pickup.gameObject.SetActive(false);
        Destroy(target.pickup.gameObject);
        target.pickup = null;
    }

    private Target GetTarget(MonsterKind kind, int id)
    {
        if (!initialized) return null;
        if (kind == MonsterKind.Item && id == 0) return item;
        return kind == MonsterKind.Image && id >= 0 && id < TotalImages ? images[id] : null;
    }

    public string GetCollectionSaveId(MonsterKind kind, int id)
    {
        // 此生成器管理單次探索；避免既有 ItemPickup 的存檔 ID 把新探索的掉落刪掉。
        return gameObject.scene.name + "_inner_" + sessionId + "_" + kind + "_" + id;
    }

    public void ReportNormalDefeated(Vector3 deathPosition)
    {
        NormalMonsterDefeated?.Invoke(deathPosition);
    }

    public void ReportDefeated(MonsterKind kind, int id)
    {
        var target = GetTarget(kind, id);
        if (target == null || target.defeated) return;
        target.defeated = true;
        if (kind == MonsterKind.Image) onImageDefeated.Invoke(++ImageDefeated);
    }

    public void CollectTarget(MonsterKind kind, int id)
    {
        var target = GetTarget(kind, id);
        if (target == null || !target.spawned || target.collected) return;
        target.collected = true;
        target.pickup = null;
        if (kind == MonsterKind.Image) onImageCollected.Invoke(++ImageCollected);
        else onItemCollected.Invoke();
        if (!CompletionReady && ImageCollected == TotalImages && ItemCollected)
        {
            CompletionReady = true;
            spawning = false;
            onCompletionReady.Invoke();
        }
    }

    public void BindPickup(MonsterKind kind, int id, ItemPickup pickup)
    {
        var target = GetTarget(kind, id);
        if (target == null || pickup == null || target.collected) return;
        target.pickup = pickup;
        pickup.pickupId = GetCollectionSaveId(kind, id);
        if (pickup.onPickedUp == null) pickup.onPickedUp = new UnityEvent();
        // 閉包只保留生成器與 ID；怪物屍體銷毀後，撿取事件仍可回報。
        pickup.onPickedUp.AddListener(() => { if (this != null) CollectTarget(kind, id); });
    }

    public void Release(InnerWorldSpawnedMonster monster)
    {
        monsters.Remove(monster);
        var target = GetTarget(monster.Kind, monster.TargetId);
        if (target != null && target.monster == monster) target.monster = null;
    }

    private void OnDestroy()
    {
        // 複製清單，避免物件銷毀回呼改動正在列舉的集合。
        foreach (var monster in monsters.ToArray())
            if (monster != null) Destroy(monster.gameObject);
        if (images != null)
            foreach (var target in images) if (target.pickup != null) Destroy(target.pickup.gameObject);
        if (item != null && item.pickup != null) Destroy(item.pickup.gameObject);
    }

    private void OnValidate()
    {
        chunkSize = Mathf.Max(1f, chunkSize);
        spawnAhead = Mathf.Max(1f, spawnAhead);
        imageMinGap = Mathf.Max(1, imageMinGap);
        imageStepChance = Mathf.Max(0.001f, imageStepChance);
        itemCooldownMin = Mathf.Max(1, itemCooldownMin);
        itemCooldownMax = Mathf.Max(itemCooldownMin, itemCooldownMax);
        itemGuaranteeAttempts = Mathf.Max(1, itemGuaranteeAttempts);
    }
}
