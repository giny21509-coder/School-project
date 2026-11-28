using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(SpiritFollow))]
[DefaultExecutionOrder(100)]
public sealed class SpiritCameraFlash : MonoBehaviour
{
    [Header("Flash Input")]
    [Tooltip("Press to toggle photo mode. Left mouse button fires while photo mode is enabled.")]
    [SerializeField] private KeyCode flashKey = KeyCode.F;
    [SerializeField, Min(0.1f)] private float cooldown = 1.2f;
    [SerializeField, Min(0.1f)] private float range = 5f;
    [Tooltip("Fan angle when aiming close to the spirit.")]
    [SerializeField, Range(1f, 180f)] private float coneAngle = 70f;
    [Tooltip("Fan angle when the cursor reaches the maximum flash range.")]
    [SerializeField, Range(1f, 180f)] private float farAimAngle = 30f;
    [SerializeField] private LayerMask targetLayers = ~0;
    [SerializeField] private LayerMask obstacleLayers;
    [SerializeField] private Camera worldCamera;

    [Header("Photo Attack")]
    [Tooltip("Hold the left mouse button for this many seconds, then release to hit a mist shield.")]
    [SerializeField, Min(0.1f)] private float chargeDuration = 3f;
    [SerializeField, Min(0f)] private float photoDamage = 20f;
    [SerializeField, Min(0f)] private float photoHitInvincibility = 0.1f;

    [Header("Aim Preview")]
    [SerializeField] private bool showAimPreview = true;
    [SerializeField, Min(0f)] private float rangeExpandDuration = 0.35f;
    [SerializeField, Min(0f)] private float rangeRetractDuration = 0.25f;
    [Tooltip("Vertical scale of the ground attack area, for both the preview and hits.")]
    [SerializeField, Range(0.1f, 1f)] private float circleDepthScale = 0.36f;
    [Tooltip("Horizontal corridor floor sprites, including the recycled pieces.")]
    [SerializeField] private SpriteRenderer[] floorTiles;
    [Tooltip("Fraction of the sprite height occupied by the painted floor, measured from the bottom.")]
    [SerializeField, Range(0f, 1f)] private float floorHeightFraction = 1f;
    private readonly Vector4[] nearbyFloorRects = new Vector4[FlashGroundRange.MaxFloorRects];

    [Header("Flash Effect")]
    [SerializeField] private Texture2D flashTexture;
    [SerializeField, Min(0.02f)] private float flashDuration = 0.12f;
    [SerializeField] private AudioClip shutterSound;

    [Header("Flash On Audio")]
    [Tooltip("Played once when F switches photo mode on.")]
    [SerializeField] private AudioClip flashOnSound;
    [SerializeField, Range(0f, 1f)] private float flashOnVolume = 0.7f;
    [SerializeField] private UnityEngine.Audio.AudioMixerGroup flashAudioMixerGroup;
    public UnityEvent onFlashUsed = new UnityEvent();
    public UnityEvent<int> onTargetsFlashed = new UnityEvent<int>();

    public float CooldownRemaining => Mathf.Max(0f, nextFlashTime - Time.time);
    public bool IsPhotoMode => isPhotoMode;
    public bool CanFlash => isPhotoMode && CanUsePhotoMode;
    private bool CanUsePhotoMode => isActiveAndEnabled && spirit != null && spirit.isActiveAndEnabled && spirit.player != null
        && Time.timeScale > 0f
        && (playerController == null || playerController.canMove)
        && (playerHealth == null || playerHealth.CurrentHealth > 0f)
        && (CameraSystem.Instance == null || !CameraSystem.Instance.CameraModeOpen)
        && (CameraShootingEncounter.Instance == null || !CameraShootingEncounter.Instance.EncounterActive);

    private SpiritFollow spirit;
    private PlayerController playerController;
    private Collider2D playerBody;
    private MoreMountains.TopDownEngine.Health playerHealth;
    private readonly HashSet<Component> hitTargets = new HashSet<Component>();
    private float nextFlashTime;
    private float flashEndTime;
    private SpriteRenderer flashRenderer;
    private Sprite ownedSprite;
    private AudioSource audioSource;
    private SpiritFlashAimPreview aimPreview;
    private float rangeProgress;
    private bool isPhotoMode;
    private bool isAiming;
    private bool flashPending;
    private bool pendingShotIsCharged;
    private float? chargeStartedAt;
    private float ChargeProgress => chargeStartedAt.HasValue
        ? Mathf.Clamp01((Time.time - chargeStartedAt.Value) / Mathf.Max(0.1f, chargeDuration)) : 0f;
    private Vector2 lastAimPosition;

    public void Initialize()
    {
        spirit = GetComponent<SpiritFollow>();
        if (spirit.player != null)
        {
            playerController = spirit.player.GetComponent<PlayerController>();
            playerBody = spirit.player.GetComponent<Collider2D>();
            playerHealth = spirit.player.GetComponent<MoreMountains.TopDownEngine.Health>();
        }
        if (worldCamera == null) worldCamera = Camera.main;
    }

    private void Awake() { Initialize(); }

    private void Update()
    {
        ReadFlashInput(out bool togglePressed, out bool shootPressed, out bool shootReleased);
        bool hasAim = TryGetMouseAimPosition(out Vector2 aimPosition)
            && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject());
        ProcessAimInput(togglePressed, shootPressed, shootReleased, hasAim, aimPosition);
    }

    private void ProcessAimInput(bool togglePressed, bool shootPressed, bool shootReleased, bool hasAim, Vector2 aimPosition)
    {
        if (!CanUsePhotoMode)
        {
            CancelAim();
            return;
        }
        if (togglePressed)
        {
            if (isPhotoMode)
            {
                isPhotoMode = false;
                SuspendAim();
                return;
            }
            isPhotoMode = true;
            PlayFlashSound(flashOnSound, flashOnVolume);
        }
        if (!isPhotoMode) return;
        if (!hasAim)
        {
            SuspendAim();
            return;
        }
        if (flashPending) return;
        isAiming = true;
        lastAimPosition = aimPosition;
        spirit.SetFlashAim(true, aimPosition);
        if (shootPressed) chargeStartedAt = Time.time;
        if (!shootReleased || !chargeStartedAt.HasValue) return;

        bool fullyCharged = ChargeProgress >= 1f;
        chargeStartedAt = null;
        if (Time.time < nextFlashTime) return;
        pendingShotIsCharged = fullyCharged;
        flashPending = true;
        CompletePendingFlash();
    }

    private void CompletePendingFlash()
    {
        if (!flashPending) return;
        if (!CanFlash)
        {
            CancelAim();
            return;
        }
        if (!spirit.IsAtFlashAimPosition) return;
        flashPending = false;
        FirePhoto(lastAimPosition, pendingShotIsCharged);
    }

    private void UpdateAimPreview(float deltaTime)
    {
        if (!isPhotoMode && rangeProgress <= 0f) return;
        if (!showAimPreview || !CanUsePhotoMode || worldCamera == null)
        {
            HideAimPreview();
            return;
        }

        float target = isPhotoMode ? 1f : 0f;
        float duration = isPhotoMode ? rangeExpandDuration : rangeRetractDuration;
        rangeProgress = duration <= 0f ? target : Mathf.MoveTowards(rangeProgress, target, deltaTime / duration);
        if (rangeProgress <= 0f)
        {
            HideAimPreview();
            return;
        }
        if (aimPreview == null)
        {
            aimPreview = GetComponent<SpiritFlashAimPreview>();
            if (aimPreview == null) aimPreview = gameObject.AddComponent<SpiritFlashAimPreview>();
        }

        var area = GetGroundRange();
        float aimAngle = Mathf.Lerp(coneAngle, Mathf.Min(coneAngle, farAimAngle),
            area.GroundDistance(lastAimPosition) / area.Range);
        aimPreview.ShowGround(area, aimAngle, transform.position, lastAimPosition, worldCamera,
            obstacleLayers, GetComponent<SpriteRenderer>(), CooldownRemaining <= 0f,
            Time.time < flashEndTime, ChargeProgress, Mathf.SmoothStep(0f, 1f, rangeProgress), isAiming);
    }

    private bool TryGetMouseAimPosition(out Vector2 aimPosition)
    {
        aimPosition = default;
        if (worldCamera == null) worldCamera = Camera.main;
        if (worldCamera == null) return false;
        Vector2 mousePosition;
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null) return false;
        mousePosition = Mouse.current.position.ReadValue();
#elif ENABLE_LEGACY_INPUT_MANAGER
        mousePosition = Input.mousePosition;
#else
        return false;
#endif
        if (!worldCamera.pixelRect.Contains(mousePosition)) return false;
        Ray ray = worldCamera.ScreenPointToRay(mousePosition);
        Plane plane = new Plane(Vector3.forward, transform.position);
        if (!plane.Raycast(ray, out float distance)) return false;
        aimPosition = ray.GetPoint(distance);
        return true;
    }

    private void HideAimPreview()
    {
        rangeProgress = 0f;
        if (aimPreview != null) aimPreview.Hide();
    }

    private void CancelAim()
    {
        isPhotoMode = false;
        SuspendAim();
        HideAimPreview();
    }

    private void SuspendAim()
    {
        isAiming = false;
        chargeStartedAt = null;
        flashPending = false;
        if (spirit != null) spirit.SetFlashAim(false, Vector2.zero);
    }

    private void ReadFlashInput(out bool togglePressed, out bool shootPressed, out bool shootReleased)
    {
        togglePressed = shootPressed = shootReleased = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && System.Enum.TryParse(flashKey.ToString(), true, out Key key))
            togglePressed = Keyboard.current[key].wasPressedThisFrame;
#if ENABLE_LEGACY_INPUT_MANAGER
        else togglePressed = Input.GetKeyDown(flashKey);
#endif
        if (Mouse.current != null)
        {
            shootPressed = Mouse.current.leftButton.wasPressedThisFrame;
            shootReleased = Mouse.current.leftButton.wasReleasedThisFrame;
            return;
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        togglePressed = Input.GetKeyDown(flashKey);
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        shootPressed = Input.GetMouseButtonDown(0);
        shootReleased = Input.GetMouseButtonUp(0);
#endif
    }

    private FlashGroundRange GetGroundRange()
    {
        Vector2 feet = spirit.player.position;
        if (playerBody != null && playerBody.isActiveAndEnabled)
        {
            Bounds bounds = playerBody.bounds;
            feet = new Vector2(bounds.center.x, bounds.min.y);
        }
        if (floorTiles == null || floorTiles.Length == 0)
            return new FlashGroundRange(feet, circleDepthScale, range);

        int count = 0;
        foreach (SpriteRenderer tile in floorTiles)
        {
            if (tile == null || !tile.enabled || !tile.gameObject.activeInHierarchy || tile.sprite == null) continue;
            Bounds b = tile.bounds;
            if (b.max.x < feet.x - range || b.min.x > feet.x + range) continue;
            float top = Mathf.Lerp(b.min.y, b.max.y, floorHeightFraction);
            nearbyFloorRects[count++] = new Vector4(b.min.x, b.min.y, b.max.x, top);
            if (count == nearbyFloorRects.Length) break;
        }
        return new FlashGroundRange(feet, circleDepthScale, range, nearbyFloorRects, count);
    }

    // All callers, including UI buttons, must obey mode and cooldown checks.
    public int TryFlashAt(Vector2 aimPosition) => FirePhoto(aimPosition, false);

    private int FirePhoto(Vector2 aimPosition, bool fullyCharged)
    {
        if (!CanFlash || Time.time < nextFlashTime) return -1;
        Physics2D.SyncTransforms();
        FlashGroundRange area = GetGroundRange();
        nextFlashTime = Time.time + Mathf.Max(0.1f, cooldown);
        hitTargets.Clear();
        int affectedTargets = 0;
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(area.Origin, area.Range, targetLayers))
        {
            Component target = GetPhotoTarget(hit);
            if (target == null || hitTargets.Contains(target)) continue;
            Collider2D body = target.GetComponent<Collider2D>();
            Bounds bounds = body != null && body.isActiveAndEnabled ? body.bounds : hit.bounds;
            Vector2 feet = new Vector2(bounds.center.x, bounds.min.y);
            if (!area.Contains(feet)) continue;
            if (Physics2D.Linecast(area.Origin, feet, obstacleLayers).collider != null) continue;
            hitTargets.Add(target);
            InnerWorldMistShield shield = target.GetComponentInParent<InnerWorldMistShield>();
            if (shield != null && shield.IsShielded)
            {
                // Breaking the mist and damaging the exposed enemy are separate photos.
                if (fullyCharged && shield.ReceiveFlash()) affectedTargets++;
                continue;
            }
            if (DamagePhotoTarget(target, (feet - area.Origin).normalized)) affectedTargets++;
        }
        PlayFlash((aimPosition - (Vector2)transform.position).normalized);
        onFlashUsed.Invoke();
        onTargetsFlashed.Invoke(affectedTargets);
        return affectedTargets;
    }

    private Component GetPhotoTarget(Collider2D hit)
    {
        var health = hit.GetComponentInParent<MoreMountains.TopDownEngine.Health>();
        if (health != null && health.isActiveAndEnabled)
        {
            if (health.MasterHealth != null) health = health.MasterHealth;
            return health.isActiveAndEnabled && IsEnemy(health.transform) ? health : null;
        }
        var legacy = hit.GetComponentInParent<Chatacher>();
        return legacy != null && legacy.isActiveAndEnabled && IsEnemy(legacy.transform) ? legacy : null;
    }

    private bool IsEnemy(Transform target)
    {
        if (target.GetComponentInParent<PlayerController>() != null
            || target.GetComponentInParent<SpiritFollow>() != null
            || target.IsChildOf(spirit.player) || target == spirit.player) return false;
        return target.GetComponentInParent<InnerWorldSpawnedMonster>() != null
            || target.GetComponentInParent<Enemy>() != null
            || target.gameObject.layer == LayerMask.NameToLayer("Enemies")
            || target.CompareTag("Enemy");
    }

    private bool DamagePhotoTarget(Component target, Vector2 direction)
    {
        if (photoDamage <= 0f) return false;
        if (target is MoreMountains.TopDownEngine.Health health)
        {
            if (!health.CanTakeDamageThisFrame()) return false;
            float previous = health.CurrentHealth;
            float immunity = Mathf.Min(photoHitInvincibility, Mathf.Max(0f, cooldown - Time.fixedDeltaTime));
            health.Damage(photoDamage, spirit.player.gameObject, 0f, immunity, direction);
            return health == null || health.CurrentHealth < previous;
        }
        var legacy = (Chatacher)target;
        float previousHealth = legacy.currenHealthl;
        legacy.TakeDamage(photoDamage, spirit.player);
        return legacy == null || legacy.currenHealthl < previousHealth;
    }

    private void PlayFlash(Vector2 direction)
    {
        if (flashRenderer == null)
        {
            if (flashTexture == null) flashTexture = Resources.Load<Texture2D>("InnerWorldCombat/Flash");
            if (flashTexture != null)
            {
                ownedSprite = Sprite.Create(flashTexture,
                    new Rect(0f, 0f, flashTexture.width, flashTexture.height),
                    new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                var visual = new GameObject("SpiritFlashVisual");
                visual.transform.SetParent(transform, false);
                flashRenderer = visual.AddComponent<SpriteRenderer>();
                flashRenderer.sprite = ownedSprite;
                Material maskMaterial = Resources.Load<Material>("InnerWorldCombat/LuminanceMask");
                if (maskMaterial != null) flashRenderer.sharedMaterial = maskMaterial;
            }
        }
        if (flashRenderer != null)
        {
            SpriteRenderer source = GetComponent<SpriteRenderer>();
            flashRenderer.sortingLayerID = source != null ? source.sortingLayerID : 0;
            flashRenderer.sortingOrder = source != null ? source.sortingOrder + 5 : 5;
            flashRenderer.transform.position = transform.position + (Vector3)direction * 0.15f;
            Vector3 scale = transform.lossyScale;
            float size = Mathf.Max(0.001f, ownedSprite.bounds.size.x);
            flashRenderer.transform.localScale = new Vector3(
                2f / Mathf.Max(0.001f, size * Mathf.Abs(scale.x)),
                2f / Mathf.Max(0.001f, size * Mathf.Abs(scale.y)), 1f);
            flashRenderer.color = Color.white;
            flashRenderer.enabled = true;
            flashEndTime = Time.time + flashDuration;
        }
        PlayFlashSound(shutterSound, 1f);
    }

    private void PlayFlashSound(AudioClip sound, float volume)
    {
        if (sound == null || volume <= 0f) return;
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
        }
        audioSource.outputAudioMixerGroup = flashAudioMixerGroup;
        audioSource.PlayOneShot(sound, Mathf.Clamp01(volume));
    }

    private void LateUpdate()
    {
        CompletePendingFlash();
        UpdateAimPreview(Time.deltaTime);
        if (flashRenderer == null || !flashRenderer.enabled) return;
        float remaining = Mathf.Clamp01((flashEndTime - Time.time) / Mathf.Max(0.02f, flashDuration));
        flashRenderer.color = new Color(1f, 1f, 1f, remaining);
        if (remaining <= 0f) flashRenderer.enabled = false;
    }

    private void OnDisable()
    {
        CancelAim();
        if (flashRenderer != null) flashRenderer.enabled = false;
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused) CancelAim();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) CancelAim();
    }

    private void OnDestroy()
    {
        CancelAim();
        if (ownedSprite != null)
        {
            if (Application.isPlaying) Destroy(ownedSprite);
            else DestroyImmediate(ownedSprite);
        }
    }
}

// The floor projection and the hit test use the same ground-space radius.
internal readonly struct FlashGroundRange
{
    // Match the floor rectangle capacity in SpotlightBeam.shader.
    public const int MaxFloorRects = 16;
    public readonly Vector4[] FloorRects;
    public readonly int FloorRectCount;
    public readonly Vector2 Origin;
    public readonly float DepthScale;
    public readonly float Range;

    public FlashGroundRange(Vector2 origin, float depthScale, float range, Vector4[] floorRects = null, int floorRectCount = 0)
    {
        FloorRects = floorRects;
        FloorRectCount = floorRectCount;
        Origin = origin;
        DepthScale = Mathf.Clamp(depthScale, 0.1f, 1f);
        Range = Mathf.Max(0.1f, range);
    }

    public Vector2 ProjectOffset(Vector2 offset) => new Vector2(offset.x, offset.y * DepthScale);

    public float GroundDistance(Vector2 point)
    {
        Vector2 offset = point - Origin;
        offset.y /= DepthScale;
        return offset.magnitude;
    }

    public bool Contains(Vector2 feet)
    {
        if (GroundDistance(feet) > Range) return false;
        if (FloorRects == null) return true;
        for (int i = 0; i < FloorRectCount; i++)
        {
            Vector4 r = FloorRects[i];
            if (feet.x >= r.x && feet.x <= r.z && feet.y >= r.y && feet.y <= r.w) return true;
        }
        return false;
    }
}
