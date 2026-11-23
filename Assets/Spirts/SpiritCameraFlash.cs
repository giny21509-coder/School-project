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
    [SerializeField, Range(1f, 180f)] private float coneAngle = 70f;
    [SerializeField] private LayerMask targetLayers = ~0;
    [SerializeField] private LayerMask obstacleLayers;
    [SerializeField] private Camera worldCamera;

    [Header("Photo Attack")]
    [SerializeField, Min(0f)] private float photoDamage = 20f;
    [SerializeField, Min(0f)] private float photoHitInvincibility = 0.1f;

    [Header("Aim Preview")]
    [SerializeField] private bool showAimPreview = true;

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
    private MoreMountains.TopDownEngine.Health playerHealth;
    private readonly HashSet<Component> hitTargets = new HashSet<Component>();
    private float nextFlashTime;
    private float flashEndTime;
    private SpriteRenderer flashRenderer;
    private Sprite ownedSprite;
    private AudioSource audioSource;
    private SpiritFlashAimPreview aimPreview;
    private bool isPhotoMode;
    private bool isAiming;
    private bool flashPending;
    private Vector2 lastAimPosition;

    public void Initialize()
    {
        spirit = GetComponent<SpiritFollow>();
        if (spirit.player != null)
        {
            playerController = spirit.player.GetComponent<PlayerController>();
            playerHealth = spirit.player.GetComponent<MoreMountains.TopDownEngine.Health>();
        }
        if (worldCamera == null) worldCamera = Camera.main;
    }

    private void Awake() { Initialize(); }

    private void Update()
    {
        ReadFlashInput(out bool togglePressed, out bool shootPressed);
        bool hasAim = TryGetMouseAimPosition(out Vector2 aimPosition)
            && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject());
        ProcessAimInput(togglePressed, shootPressed, hasAim, aimPosition);
    }

    private void ProcessAimInput(bool togglePressed, bool shootPressed, bool hasAim, Vector2 aimPosition)
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
                CancelAim();
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
        if (!shootPressed || Time.time < nextFlashTime) return;
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
        TryFlashAt(lastAimPosition);
    }

    private void UpdateAimPreview()
    {
        if (isAiming && showAimPreview && CanFlash)
        {
            if (aimPreview == null)
            {
                aimPreview = GetComponent<SpiritFlashAimPreview>();
                if (aimPreview == null) aimPreview = gameObject.AddComponent<SpiritFlashAimPreview>();
            }
            aimPreview.Show(transform.position, lastAimPosition, worldCamera, range, coneAngle,
                obstacleLayers, GetComponent<SpriteRenderer>(), CooldownRemaining <= 0f,
                false);
        }
        else HideAimPreview();
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
        if (aimPreview != null) aimPreview.Hide();
    }

    private void CancelAim()
    {
        isPhotoMode = false;
        SuspendAim();
    }

    private void SuspendAim()
    {
        isAiming = false;
        flashPending = false;
        if (spirit != null) spirit.SetFlashAim(false, Vector2.zero);
        HideAimPreview();
    }

    private void ReadFlashInput(out bool togglePressed, out bool shootPressed)
    {
        togglePressed = shootPressed = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && System.Enum.TryParse(flashKey.ToString(), true, out Key key))
            togglePressed = Keyboard.current[key].wasPressedThisFrame;
#if ENABLE_LEGACY_INPUT_MANAGER
        else togglePressed = Input.GetKeyDown(flashKey);
#endif
        if (Mouse.current != null) shootPressed = Mouse.current.leftButton.wasPressedThisFrame;
#if ENABLE_LEGACY_INPUT_MANAGER
        else shootPressed = Input.GetMouseButtonDown(0);
#endif
#elif ENABLE_LEGACY_INPUT_MANAGER
        togglePressed = Input.GetKeyDown(flashKey);
        shootPressed = Input.GetMouseButtonDown(0);
#endif
    }

    // All callers, including UI buttons, must obey mode and cooldown checks.
    public int TryFlashAt(Vector2 aimPosition)
    {
        if (!CanFlash || Time.time < nextFlashTime) return -1;
        Vector2 origin = transform.position;
        Vector2 direction = aimPosition - origin;
        if (direction.sqrMagnitude < 0.0001f) return -1;
        nextFlashTime = Time.time + Mathf.Max(0.1f, cooldown);
        hitTargets.Clear();
        int affectedTargets = 0;
        Physics2D.SyncTransforms();
        foreach (Collider2D hit in Physics2D.OverlapCircleAll(origin, range, targetLayers))
        {
            Component target = GetPhotoTarget(hit);
            if (target == null || hitTargets.Contains(target)) continue;
            Vector2 targetPosition = target.transform.position;
            Vector2 offset = targetPosition - origin;
            if (offset.magnitude > range || Vector2.Angle(direction, offset) > coneAngle * 0.5f) continue;
            if (Physics2D.Linecast(origin, targetPosition, obstacleLayers).collider != null) continue;
            hitTargets.Add(target);
            InnerWorldMistShield shield = target.GetComponentInParent<InnerWorldMistShield>();
            if (shield != null && shield.IsShielded)
            {
                // Breaking the mist and damaging the exposed enemy are separate photos.
                if (shield.ReceiveFlash()) affectedTargets++;
                continue;
            }
            if (DamagePhotoTarget(target, offset.normalized)) affectedTargets++;
        }
        PlayFlash(direction.normalized);
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
        UpdateAimPreview();
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
