using MoreMountains.TopDownEngine;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class InnerWorldMistShield : MonoBehaviour
{
    [Header("Mist Shield")]
    [SerializeField, Min(1)] private int flashesToBreak = 2;
    [SerializeField] private Texture2D mistTexture;
    [SerializeField] private GameObject mistVisual;

    public UnityEvent<int, int> onFlashHit = new UnityEvent<int, int>();
    public UnityEvent onShieldBroken = new UnityEvent();

    public bool IsShielded => initialized && shielded && isActiveAndEnabled;
    public int FlashHits { get; private set; }
    public int FlashesToBreak => flashesToBreak;

    private Health[] healthComponents;
    private bool[] originalImmunity;
    private Chatacher legacyHealth;
    private SpriteRenderer bodyRenderer;
    private SpriteRenderer mistRenderer;
    private Sprite ownedSprite;
    private bool initialized;
    private bool shielded;

    public void Initialize(int requiredFlashes)
    {
        if (!initialized)
        {
            healthComponents = GetComponentsInChildren<Health>(true);
            originalImmunity = new bool[healthComponents.Length];
            for (int i = 0; i < healthComponents.Length; i++)
                originalImmunity[i] = healthComponents[i].ImmuneToDamage;
            legacyHealth = GetComponent<Chatacher>();
            bodyRenderer = GetComponentInChildren<SpriteRenderer>();
            initialized = true;
        }
        flashesToBreak = Mathf.Max(1, requiredFlashes);
        FlashHits = 0;
        PrepareVisual();
        SetShielded(true);
    }

    private void Start()
    {
        if (!initialized) Initialize(flashesToBreak);
    }

    private bool IsAlive()
    {
        if (healthComponents != null)
            foreach (Health health in healthComponents)
                if (health != null && health.isActiveAndEnabled)
                    return health.CurrentHealth > 0f || health.InitialHealth == 0f;
        return legacyHealth == null || !legacyHealth.isDead;
    }

    public bool ReceiveFlash()
    {
        if (!IsShielded || !IsAlive()) return false;
        FlashHits++;
        if (FlashHits >= flashesToBreak)
        {
            SetShielded(false);
            onShieldBroken.Invoke();
        }
        onFlashHit.Invoke(FlashHits, flashesToBreak);
        return true;
    }

    private void SetShielded(bool active)
    {
        shielded = active;
        ApplyImmunity(active && isActiveAndEnabled);
        if (mistVisual != null) mistVisual.SetActive(active && isActiveAndEnabled);
    }

    private void ApplyImmunity(bool active)
    {
        if (healthComponents == null) return;
        // Keep shield immunity separate from TopDown's timed hit invulnerability.
        for (int i = 0; i < healthComponents.Length; i++)
            if (healthComponents[i] != null)
                healthComponents[i].ImmuneToDamage = active || originalImmunity[i];
    }

    private void OnEnable()
    {
        if (initialized) SetShielded(shielded);
    }

    private void OnDisable()
    {
        ApplyImmunity(false);
        if (mistVisual != null) mistVisual.SetActive(false);
    }

    private void PrepareVisual()
    {
        if (mistVisual != null) return;
        if (mistTexture == null) mistTexture = Resources.Load<Texture2D>("InnerWorldCombat/Mist");
        if (mistTexture == null || bodyRenderer == null) return;
        ownedSprite = Sprite.Create(mistTexture,
            new Rect(0f, 0f, mistTexture.width, mistTexture.height),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        mistVisual = new GameObject("MistShieldVisual");
        mistVisual.transform.SetParent(transform, false);
        mistRenderer = mistVisual.AddComponent<SpriteRenderer>();
        mistRenderer.sprite = ownedSprite;
        Material maskMaterial = Resources.Load<Material>("InnerWorldCombat/LuminanceMask");
        if (maskMaterial != null) mistRenderer.sharedMaterial = maskMaterial;
    }

    private void LateUpdate()
    {
        if (!IsShielded || mistRenderer == null || bodyRenderer == null) return;
        Bounds bounds = bodyRenderer.bounds;
        mistVisual.transform.position = bounds.center;
        Vector3 size = ownedSprite.bounds.size;
        Vector3 parentScale = transform.lossyScale;
        float pulse = 1.35f + Mathf.Sin(Time.time * 2f) * 0.04f;
        mistVisual.transform.localScale = new Vector3(
            bounds.size.x * pulse / Mathf.Max(0.001f, size.x * Mathf.Abs(parentScale.x)),
            bounds.size.y * pulse / Mathf.Max(0.001f, size.y * Mathf.Abs(parentScale.y)), 1f);
        mistRenderer.sortingLayerID = bodyRenderer.sortingLayerID;
        mistRenderer.sortingOrder = bodyRenderer.sortingOrder + 1;
        float progress = (float)FlashHits / flashesToBreak;
        mistRenderer.color = new Color(0.06f, 0.05f, 0.09f, Mathf.Lerp(0.85f, 0.35f, progress));
    }

    private void OnDestroy()
    {
        ApplyImmunity(false);
        if (ownedSprite != null)
        {
            if (Application.isPlaying) Destroy(ownedSprite);
            else DestroyImmediate(ownedSprite);
        }
    }
}
