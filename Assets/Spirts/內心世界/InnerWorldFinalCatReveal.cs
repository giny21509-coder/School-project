using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>收集完成後，光點從上方落下聚集，漸漸凝成最後的貓咪。</summary>
[DisallowMultipleComponent]
public class InnerWorldFinalCatReveal : MonoBehaviour
{
    [Header("連接（請掛在保持啟用的生成器物件上）")]
    [SerializeField] private InnerWorldDistanceSpawner spawner;
    [Tooltip("拖入 Hierarchy 裡的貓咪物件，不是 Project 裡的 Prefab。")]
    [SerializeField] private GameObject catObject;

    [Header("出現位置")]
    [Tooltip("有指定時，以這個位置為準；請放在循環背景外。")]
    [SerializeField] private Transform appearancePoint;
    [SerializeField] private bool placeNearPlayer = true;
    [SerializeField] private Transform player;
    [Tooltip("世界座標偏移：X 正數在玩家右邊，Y 可調整貓咪腳底高度。")]
    [SerializeField] private Vector3 playerOffset = new Vector3(4f, 0f, 0f);

    [Header("光點落下與凝聚（不需要另外準備粒子 Prefab）")]
    [SerializeField, Range(5, 100)] private int lightCount = 32;
    [SerializeField, Min(0.1f)] private float descentDuration = 3f;
    [SerializeField, Min(0f)] private float gatherHoldDuration = 0.5f;
    [SerializeField, Min(0.1f)] private float materializeDuration = 1.5f;
    [SerializeField, Min(0.1f)] private float descentHeight = 4f;
    [SerializeField, Min(0.1f)] private float descentSpread = 3f;
    [SerializeField, Min(0.01f)] private float lightSize = 0.32f;
    [SerializeField] private Color lightColor = new Color(1f, 0.9f, 0.6f, 1f);
    [Tooltip("光點畫在貓咪上方的排序差值。若整組被場景擋住，調整貓咪的 Sorting Layer。")]
    [SerializeField] private int lightOrderOffset = 2;
    [Tooltip("光點獨立排序，避免跟隨 Characters 被上方的霧遮住。留空才沿用貓咪圖層。")]
    [SerializeField] private string lightSortingLayer = "Top";
    [SerializeField] private int lightSortingOrder = 100;

    [Header("演出時機與玩家控制")]
    [Tooltip("先讓最後一次撿取的延遲對話有機會開始。")]
    [SerializeField, Min(0f)] private float revealStartDelay = 0.4f;
    [SerializeField] private bool waitForDialogueToFinish = true;
    [SerializeField] private bool lockPlayerDuringReveal = true;
    [Header("執行狀態（Play 模式時查看）")]
    [SerializeField, TextArea(2, 4)] private string revealStatus = "等待收集完成";

    [Header("貓咪出現後（可留空，之後接叫聲或對話）")]
    public UnityEvent onCatAppeared = new UnityEvent();

    public bool HasAppeared { get; private set; }
    public bool IsRevealing { get; private set; }
    public string RevealStatus => revealStatus;
    private InnerWorldDistanceSpawner subscribedSpawner;
    private Coroutine revealRoutine;
    private GameObject lightRoot;
    private Texture2D lightTexture;
    private Sprite lightSprite;
    private Material lightMaterial;
    private SpriteRenderer[] catRenderers;
    private Color[] originalColors;
    private Collider2D[] catColliders;
    private bool[] originalColliderStates;
    private Rigidbody2D[] catBodies;
    private bool[] originalBodyStates;
    private bool catPrepared;
    private float catOpacity;
    private PlayerController lockedController;
    private Rigidbody2D lockedPlayerBody;
    private bool originalCanMove;
    private RigidbodyConstraints2D originalPlayerConstraints;
    private bool playerLocked;

    private class LightPoint
    {
        public SpriteRenderer renderer;
        public Vector3 start;
        public Vector3 target;
        public float delay;
        public float phase;
        public float size;
    }

    private void Awake()
    {
        if (spawner == null) spawner = GetComponent<InnerWorldDistanceSpawner>();
        if (IsSceneCat() && !transform.IsChildOf(catObject.transform))
            catObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (spawner == null) spawner = GetComponent<InnerWorldDistanceSpawner>();
        if (spawner == null)
        {
            Debug.LogWarning("最後貓咪：請指定 Spawner，或掛在距離生成器同一個物件上。", this);
            return;
        }
        subscribedSpawner = spawner;
        subscribedSpawner.onCompletionReady.AddListener(ShowCat);
        // 元件若在收集完成後才啟用，也能補顯示貓咪。
        if (subscribedSpawner.CompletionReady) ShowCat();
    }

    private void OnDisable()
    {
        if (subscribedSpawner != null)
            subscribedSpawner.onCompletionReady.RemoveListener(ShowCat);
        subscribedSpawner = null;
        if (revealRoutine != null) StopCoroutine(revealRoutine);
        revealRoutine = null;
        if (IsRevealing)
        {
            if (catObject != null) catObject.SetActive(false);
            RestoreCat();
            IsRevealing = false;
        }
        RestorePlayer();
        ClearLights();
    }

    public void ShowCat()
    {
        // 正式遊戲只接受收集完成事件；重複呼叫不會重播。
        if (!isActiveAndEnabled || HasAppeared || IsRevealing || spawner == null || !spawner.CompletionReady)
            return;
        BeginReveal();
    }

    private void BeginReveal()
    {
        if (!IsSceneCat() || transform.IsChildOf(catObject.transform))
        {
            Debug.LogWarning("最後貓咪：Cat Object 必須是 Hierarchy 裡獨立的貓咪物件，不能包含此控制元件。", this);
            return;
        }
        if (catObject.transform.parent != null && !catObject.transform.parent.gameObject.activeInHierarchy)
        {
            Debug.LogWarning("最後貓咪：貓咪的父物件未啟用，請移到保持啟用的場景根目錄。", this);
            return;
        }

        if (!PositionCat()) return;

        catRenderers = catObject.GetComponentsInChildren<SpriteRenderer>(true);
        SpriteRenderer mainRenderer = null;
        float largestArea = 0f;
        foreach (var candidate in catRenderers)
        {
            if (candidate.sprite == null || !candidate.enabled) continue;
            Vector2 size = candidate.sprite.bounds.size;
            Vector3 scale = candidate.transform.lossyScale;
            float area = Mathf.Abs(size.x * scale.x * size.y * scale.y);
            if (area > largestArea) { largestArea = area; mainRenderer = candidate; }
        }
        if (mainRenderer == null)
        {
            Debug.LogWarning("最後貓咪：需要至少一個有圖片的 Sprite Renderer，才能製作光點凝聚演出。", this);
            return;
        }
        if (!PrepareLightSprite()) return;

        catObject.SetActive(false);
        IsRevealing = true;
        revealStatus = "收集完成，準備光點演出";
        revealRoutine = StartCoroutine(Reveal(mainRenderer));
    }

    private IEnumerator Reveal(SpriteRenderer mainRenderer)
    {
        float delay = 0f;
        while (delay < Mathf.Max(0f, revealStartDelay))
        {
            delay += Time.deltaTime;
            yield return null;
        }
        // ItemPickup 先通知收集完成，才排入對話，因此先等一小段時間。
        // 只等目前場景的對話，不要求五張 CG 都已閱讀才讓貓咪出現。
        float dialogueQuietTime = 0f;
        while (waitForDialogueToFinish && dialogueQuietTime < 0.25f)
        {
            if (HasRunningDialogue())
            {
                dialogueQuietTime = 0f;
                revealStatus = "收集完成，等待目前對話結束";
            }
            else dialogueQuietTime += Time.deltaTime;
            if (catObject == null) { CancelReveal(); yield break; }
            yield return null;
        }
        if (catObject == null || mainRenderer == null || !PositionCat())
        {
            CancelReveal();
            yield break;
        }
        LockPlayer();
        PrepareCat();
        revealStatus = "光點飄落中";
        Debug.Log("最後貓咪：開始光點落下演出。", this);
        lightRoot = new GameObject("FinalCat_GatheringLights");
        lightRoot.transform.SetParent(transform, false);
        LightPoint[] points = CreateLights(mainRenderer);
        float elapsed = 0f;
        float descent = Mathf.Max(0.1f, descentDuration);
        while (elapsed < descent)
        {
            if (catObject == null) { CancelReveal(); yield break; }
            elapsed += Time.deltaTime;
            foreach (var point in points)
            {
                float t = Mathf.Clamp01((elapsed - point.delay) / (descent - point.delay));
                float eased = Mathf.SmoothStep(0f, 1f, t);
                float sway = Mathf.Sin(t * Mathf.PI) * 0.4f;
                Vector3 position = Vector3.Lerp(point.start, point.target, eased);
                position.x += Mathf.Sin(t * Mathf.PI * 3f + point.phase) * sway;
                position.y += Mathf.Cos(t * Mathf.PI * 3f + point.phase) * sway * 0.3f;
                point.renderer.transform.position = position;
                float alpha = Mathf.SmoothStep(0f, 1f, t * 5f);
                SetLightColor(point, alpha);
            }
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < Mathf.Max(0f, gatherHoldDuration))
        {
            if (catObject == null) { CancelReveal(); yield break; }
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (catObject == null) { CancelReveal(); yield break; }

        // 先設為全透明再啟用；淡入期間不開放碰撞或觸發互動。
        catOpacity = 0f;
        revealStatus = "光點凝成貓咪中";
        ApplyCatOpacity();
        catObject.SetActive(true);
        SuppressCatPhysics();
        elapsed = 0f;
        float fade = Mathf.Max(0.1f, materializeDuration);
        while (elapsed < fade)
        {
            if (catObject == null) { CancelReveal(); yield break; }
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / fade));
            catOpacity = t;
            ApplyCatOpacity();
            foreach (var point in points)
            {
                SetLightColor(point, 1f - t);
                point.renderer.transform.localScale = Vector3.one * point.size * (1f - t * 0.7f);
            }
            yield return null;
        }

        RestoreCat();
        RestorePlayer();
        ClearLights();
        IsRevealing = false;
        HasAppeared = true;
        revealStatus = "貓咪已出現";
        revealRoutine = null;
        // 只在貓咪完整成形後，才發出對話、互動或音效的後續事件。
        Debug.Log("最後貓咪：光點凝聚完成，貓咪已成形。", this);
        onCatAppeared.Invoke();
    }

    private LightPoint[] CreateLights(SpriteRenderer mainRenderer)
    {
        var points = new LightPoint[Mathf.Clamp(lightCount, 5, 100)];
        Vector2[] vertices = mainRenderer.sprite.vertices;
        ushort[] triangles = mainRenderer.sprite.triangles;
        float[] areas = new float[triangles.Length / 3];
        float totalArea = 0f;
        for (int i = 0; i < areas.Length; i++)
        {
            Vector2 a = vertices[triangles[i * 3]];
            Vector2 b = vertices[triangles[i * 3 + 1]];
            Vector2 c = vertices[triangles[i * 3 + 2]];
            totalArea += Mathf.Abs((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x)) * 0.5f;
            areas[i] = totalArea;
        }
        Vector3 center = mainRenderer.transform.TransformPoint(mainRenderer.sprite.bounds.center);
        for (int i = 0; i < points.Length; i++)
        {
            Vector2 local = Vector2.zero;
            if (totalArea > 0f)
            {
                float roll = Random.value * totalArea;
                int triangle = 0;
                while (triangle < areas.Length - 1 && roll > areas[triangle]) triangle++;
                Vector2 a = vertices[triangles[triangle * 3]];
                Vector2 b = vertices[triangles[triangle * 3 + 1]];
                Vector2 c = vertices[triangles[triangle * 3 + 2]];
                float u = Mathf.Sqrt(Random.value);
                float v = Random.value;
                local = (1f - u) * a + u * (1f - v) * b + u * v * c;
            }
            if (mainRenderer.flipX) local.x = -local.x;
            if (mainRenderer.flipY) local.y = -local.y;
            Vector3 target = mainRenderer.transform.TransformPoint(local);
            var lightObject = new GameObject("MemoryLight_" + (i + 1));
            lightObject.layer = catObject.layer;
            lightObject.transform.SetParent(lightRoot.transform, false);
            var renderer = lightObject.AddComponent<SpriteRenderer>();
            renderer.sprite = lightSprite;
            renderer.sharedMaterial = lightMaterial;
            renderer.sortingLayerID = mainRenderer.sortingLayerID;
            renderer.sortingOrder = mainRenderer.sortingOrder + lightOrderOffset;
            if (!string.IsNullOrEmpty(lightSortingLayer))
                foreach (var layer in SortingLayer.layers)
                    if (layer.name == lightSortingLayer)
                    {
                        renderer.sortingLayerID = layer.id;
                        renderer.sortingOrder = lightSortingOrder;
                        break;
                    }
            var point = new LightPoint
            {
                renderer = renderer,
                start = center + new Vector3(Random.Range(-descentSpread, descentSpread),
                    descentHeight + Random.Range(0f, descentHeight * 0.3f), 0f),
                target = target,
                delay = Random.Range(0f, Mathf.Max(0.1f, descentDuration) * 0.2f),
                phase = Random.Range(0f, Mathf.PI * 2f),
                size = Mathf.Max(0.01f, lightSize) * Random.Range(0.7f, 1.3f)
            };
            lightObject.transform.position = point.start;
            lightObject.transform.localScale = Vector3.one * point.size;
            SetLightColor(point, 0f);
            points[i] = point;
        }
        return points;
    }

    private bool PrepareLightSprite()
    {
        if (lightSprite != null) return true;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Debug.LogWarning("最後貓咪：找不到 Sprites/Default Shader，無法顯示光點。", this);
            return false;
        }
        const int size = 32;
        lightTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        lightTexture.name = "FinalCat_SoftLight";
        lightTexture.filterMode = FilterMode.Bilinear;
        lightTexture.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float distance = new Vector2((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f).magnitude;
                float alpha = Mathf.Clamp01(1f - distance);
                pixels[y * size + x] = new Color(1f, 1f, 1f, alpha * alpha);
            }
        lightTexture.SetPixels(pixels);
        lightTexture.Apply(false, true);
        // 光點使用完整四邊形，避免 Tight Mesh 裁掉柔邊光暈。
        lightSprite = Sprite.Create(lightTexture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect);
        lightMaterial = new Material(shader);
        return true;
    }

    private void SetLightColor(LightPoint point, float alpha)
    {
        Color color = lightColor;
        color.a *= alpha;
        point.renderer.color = color;
    }

    private void PrepareCat()
    {
        originalColors = new Color[catRenderers.Length];
        for (int i = 0; i < catRenderers.Length; i++) originalColors[i] = catRenderers[i].color;
        catColliders = catObject.GetComponentsInChildren<Collider2D>(true);
        originalColliderStates = new bool[catColliders.Length];
        for (int i = 0; i < catColliders.Length; i++) originalColliderStates[i] = catColliders[i].enabled;
        catBodies = catObject.GetComponentsInChildren<Rigidbody2D>(true);
        originalBodyStates = new bool[catBodies.Length];
        for (int i = 0; i < catBodies.Length; i++) originalBodyStates[i] = catBodies[i].simulated;
        catPrepared = true;
        catOpacity = 0f;
        ApplyCatOpacity();
        SuppressCatPhysics();
    }

    private void SuppressCatPhysics()
    {
        foreach (var collider in catColliders) if (collider != null) collider.enabled = false;
        foreach (var body in catBodies) if (body != null) body.simulated = false;
    }

    private void LateUpdate()
    {
        // 避免對話結束時把 canMove 改回 true，讓玩家把鏡頭帶離演出位置。
        if (playerLocked && lockedController != null) lockedController.canMove = false;
        if (!IsRevealing && !HasAppeared && spawner != null)
            revealStatus = "圖片 " + spawner.ImageCollected + "/5；飼料碗：" +
                (spawner.ItemCollected ? "已收集" : "未收集");
        // 動畫若修改 SpriteRenderer 顏色，仍維持凝形的淡入進度。
        if (IsRevealing && catPrepared) ApplyCatOpacity();
    }

    private void ApplyCatOpacity()
    {
        for (int i = 0; i < catRenderers.Length; i++)
        {
            if (catRenderers[i] == null) continue;
            Color color = catRenderers[i].color;
            color.a = originalColors[i].a * catOpacity;
            catRenderers[i].color = color;
        }
    }

    private void RestoreCat()
    {
        if (!catPrepared) return;
        for (int i = 0; i < catRenderers.Length; i++)
            if (catRenderers[i] != null) catRenderers[i].color = originalColors[i];
        for (int i = 0; i < catColliders.Length; i++)
            if (catColliders[i] != null) catColliders[i].enabled = originalColliderStates[i];
        for (int i = 0; i < catBodies.Length; i++)
            if (catBodies[i] != null) catBodies[i].simulated = originalBodyStates[i];
        catPrepared = false;
    }

    private void ClearLights()
    {
        if (lightRoot != null) { lightRoot.SetActive(false); Destroy(lightRoot); }
        lightRoot = null;
    }

    private void CancelReveal()
    {
        RestoreCat();
        RestorePlayer();
        ClearLights();
        IsRevealing = false;
        revealRoutine = null;
        revealStatus = "演出已中止，請查看 Console";
    }

    private bool PositionCat()
    {
        if (appearancePoint != null) catObject.transform.position = appearancePoint.position;
        else if (placeNearPlayer)
        {
            if (player == null)
            {
                Debug.LogWarning("最後貓咪：請指定 Player，或指定 Appearance Point。", this);
                return false;
            }
            catObject.transform.position = player.position + playerOffset;
        }
        return true;
    }

    private bool HasRunningDialogue()
    {
        foreach (var flowchart in Fungus.Flowchart.CachedFlowcharts)
            if (flowchart != null && flowchart.isActiveAndEnabled &&
                flowchart.gameObject.scene == gameObject.scene && flowchart.HasExecutingBlocks())
                return true;
        return false;
    }

    private void LockPlayer()
    {
        if (!lockPlayerDuringReveal || player == null || playerLocked) return;
        lockedController = player.GetComponent<PlayerController>();
        lockedPlayerBody = player.GetComponent<Rigidbody2D>();
        if (lockedController != null)
        {
            originalCanMove = lockedController.canMove;
            lockedController.canMove = false;
        }
        if (lockedPlayerBody != null)
        {
            originalPlayerConstraints = lockedPlayerBody.constraints;
            lockedPlayerBody.linearVelocity = Vector2.zero;
            lockedPlayerBody.constraints = RigidbodyConstraints2D.FreezeAll;
        }
        var animator = player.GetComponent<Animator>();
        if (animator != null) animator.SetBool("isWalk", false);
        playerLocked = true;
    }

    private void RestorePlayer()
    {
        if (!playerLocked) return;
        if (lockedController != null) lockedController.canMove = originalCanMove;
        if (lockedPlayerBody != null) lockedPlayerBody.constraints = originalPlayerConstraints;
        playerLocked = false;
    }

    private void OnDestroy()
    {
        ClearLights();
        if (lightSprite != null) Destroy(lightSprite);
        if (lightTexture != null) Destroy(lightTexture);
        if (lightMaterial != null) Destroy(lightMaterial);
    }

    [ContextMenu("播放光點演出（僅 Play 模式）")]
    private void PreviewReveal()
    {
        if (!Application.isPlaying || !isActiveAndEnabled || IsRevealing || HasAppeared) return;
        BeginReveal();
    }

    private bool IsSceneCat()
    {
        return catObject != null && catObject.scene.IsValid() && catObject.scene == gameObject.scene;
    }
}
