using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;

public class CameraShootingEncounter : MonoBehaviour
{
    public static CameraShootingEncounter Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject shootingViewRoot;
    [SerializeField] private RectTransform playArea;
    [SerializeField] private RectTransform reticle;
    [SerializeField] private RectTransform wisherTargetUi;
    [SerializeField] private TMP_Text remainingHitsText;
    [SerializeField] private string remainingHitsFormat = "剩餘拍攝:{0}";

    [Header("Shoot")]
    [SerializeField] private KeyCode shootKey = KeyCode.Mouse0;
    [SerializeField] private float hitRadius = 55f;
    [SerializeField] private int requiredHits = 3;
    [SerializeField] private float shootCooldown = 0.45f;

    [Header("Wisher Movement")]
    [SerializeField] private float moveSpeed = 450f;
    [SerializeField] private float changePositionInterval = 0.75f;
    [SerializeField] private Vector2 areaPadding = new Vector2(80f, 80f);

    [Header("Camera Mode")]
    [SerializeField] private bool closeCameraModeWhenEncounterEnds = true;

    [Header("Photo Hit Effect")]
    [SerializeField] private Image shutterFlashImage;
    [SerializeField] private float flashDuration = 0.12f;
    [SerializeField] private float flashMaxAlpha = 0.35f;
    [SerializeField] private float reticlePulseScale = 1.18f;
    [SerializeField] private float targetPulseScale = 1.08f;
    [SerializeField] private float pulseDuration = 0.12f;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip shutterSound;

    [Header("Completion Fade")]
    [SerializeField] private Image completionFadeImage;
    [SerializeField] private float fadeToBlackDuration = 0.6f;
    [SerializeField] private float holdBlackDuration = 0.25f;
    [SerializeField] private float fadeFromBlackDuration = 0.35f;
    [SerializeField] private bool fadeBackAfterEncounterEnds = true;

    [Header("Events")]
    public UnityEvent onEncounterStarted;
    public UnityEvent<int, int> onHitProgressChanged;
    public UnityEvent onShotMissed;
    public UnityEvent onEncounterCompleted;
    public UnityEvent onEncounterCancelled;

    private WisherPhotoTarget currentWisher;
    private Vector2 targetPosition;
    private int currentHits;
    private float nextShootTime;
    private float nextMoveChangeTime;
    private bool encounterActive;
    private Vector3 reticleOriginalScale = Vector3.one;
    private Vector3 targetOriginalScale = Vector3.one;
    private Coroutine hitEffectCoroutine;
    private Coroutine completionFadeCoroutine;

    public bool EncounterActive => encounterActive;

    /// <summary>Optional chapter hook: when set and returning false, an on-target shot counts as a miss (Ch06 pause window).</summary>
    public System.Func<bool> HitGate;
    /// <summary>Optional chapter hook: while true, the wisher target stops moving.</summary>
    [System.NonSerialized] public bool FreezeMovement;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        SaveOriginalScales();
        PrepareShutterFlash();
        PrepareCompletionFade();
        SetShootingView(false);
    }

    private void Update()
    {
        if (!encounterActive)
        {
            return;
        }

        UpdateReticle();
        UpdateWisherMovement();

        if (Input.GetKeyDown(shootKey))
        {
            TryShoot();
        }
    }

    public void StartEncounter(WisherPhotoTarget wisher)
    {
        if (encounterActive)
        {
            return;
        }

        currentWisher = wisher;
        currentHits = 0;
        encounterActive = true;
        PrepareCompletionFade();
        SaveOriginalScales();

        SetShootingView(true);
        PickNewTargetPosition();
        UpdateRemainingHitsText();
        onHitProgressChanged?.Invoke(currentHits, requiredHits);
        onEncounterStarted?.Invoke();
    }

    public void CancelEncounter()
    {
        if (!encounterActive)
        {
            return;
        }

        EndEncounter();
        onEncounterCancelled?.Invoke();
    }

    private void TryShoot()
    {
        if (Time.time < nextShootTime)
        {
            return;
        }

        nextShootTime = Time.time + shootCooldown;

        Vector2 mouseLocalPosition = GetMouseLocalPosition();
        float distance = Vector2.Distance(mouseLocalPosition, wisherTargetUi.anchoredPosition);

        if (distance > hitRadius || (HitGate != null && !HitGate()))
        {
            onShotMissed?.Invoke();
            return;
        }

        PlayPhotoHitEffect();
        currentHits++;
        UpdateRemainingHitsText();
        onHitProgressChanged?.Invoke(currentHits, requiredHits);

        if (currentHits >= requiredHits)
        {
            CompleteEncounter();
            return;
        }

        PickNewTargetPosition();
    }

    private void CompleteEncounter()
    {
        if (completionFadeCoroutine != null)
        {
            return;
        }

        completionFadeCoroutine = StartCoroutine(CompleteEncounterWithFadeRoutine());
    }

    private void EndEncounter()
    {
        encounterActive = false;
        currentWisher = null;
        SetShootingView(false);

        if (closeCameraModeWhenEncounterEnds &&
            CameraSystem.Instance != null &&
            CameraSystem.Instance.CameraModeOpen)
        {
            CameraSystem.Instance.SetCameraMode(false);
        }
    }

    private IEnumerator CompleteEncounterWithFadeRoutine()
    {
        yield return FadeCompletionImage(0f, 1f, fadeToBlackDuration);

        if (currentWisher != null)
        {
            currentWisher.ForceReveal();
        }

        yield return new WaitForSeconds(holdBlackDuration);

        EndEncounter();
        onEncounterCompleted?.Invoke();

        if (fadeBackAfterEncounterEnds)
        {
            yield return FadeCompletionImage(1f, 0f, fadeFromBlackDuration);
        }

        completionFadeCoroutine = null;
    }

    private void UpdateRemainingHitsText()
    {
        if (remainingHitsText == null)
        {
            return;
        }

        int remainingHits = Mathf.Max(requiredHits - currentHits, 0);
        remainingHitsText.text = string.Format(remainingHitsFormat, remainingHits);
    }

    private void SaveOriginalScales()
    {
        if (reticle != null)
        {
            reticleOriginalScale = reticle.localScale;
        }

        if (wisherTargetUi != null)
        {
            targetOriginalScale = wisherTargetUi.localScale;
        }
    }

    private void PrepareShutterFlash()
    {
        if (shutterFlashImage == null)
        {
            return;
        }

        Color color = shutterFlashImage.color;
        color.a = 0f;
        shutterFlashImage.color = color;
        shutterFlashImage.raycastTarget = false;
    }

    private void PrepareCompletionFade()
    {
        if (completionFadeImage == null)
        {
            return;
        }

        Color color = completionFadeImage.color;
        color.a = 0f;
        completionFadeImage.color = color;
        completionFadeImage.raycastTarget = false;
    }

    private IEnumerator FadeCompletionImage(float fromAlpha, float toAlpha, float duration)
    {
        if (completionFadeImage == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            SetCompletionFadeAlpha(toAlpha);
            yield break;
        }

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            SetCompletionFadeAlpha(Mathf.Lerp(fromAlpha, toAlpha, t));
            yield return null;
        }

        SetCompletionFadeAlpha(toAlpha);
    }

    private void SetCompletionFadeAlpha(float alpha)
    {
        if (completionFadeImage == null)
        {
            return;
        }

        Color color = completionFadeImage.color;
        color.a = alpha;
        completionFadeImage.color = color;
    }

    private void PlayPhotoHitEffect()
    {
        if (audioSource != null && shutterSound != null)
        {
            audioSource.PlayOneShot(shutterSound);
        }

        if (hitEffectCoroutine != null)
        {
            StopCoroutine(hitEffectCoroutine);
        }

        hitEffectCoroutine = StartCoroutine(PhotoHitEffectRoutine());
    }

    private IEnumerator PhotoHitEffectRoutine()
    {
        float timer = 0f;

        while (timer < pulseDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / pulseDuration);
            float pulse = Mathf.Sin(t * Mathf.PI);

            if (reticle != null)
            {
                reticle.localScale = reticleOriginalScale * Mathf.Lerp(1f, reticlePulseScale, pulse);
            }

            if (wisherTargetUi != null)
            {
                wisherTargetUi.localScale = targetOriginalScale * Mathf.Lerp(1f, targetPulseScale, pulse);
            }

            if (shutterFlashImage != null)
            {
                Color color = shutterFlashImage.color;
                color.a = Mathf.Lerp(0f, flashMaxAlpha, pulse);
                shutterFlashImage.color = color;
            }

            yield return null;
        }

        if (reticle != null)
        {
            reticle.localScale = reticleOriginalScale;
        }

        if (wisherTargetUi != null)
        {
            wisherTargetUi.localScale = targetOriginalScale;
        }

        if (shutterFlashImage != null)
        {
            Color color = shutterFlashImage.color;
            color.a = 0f;
            shutterFlashImage.color = color;
        }

        hitEffectCoroutine = null;
    }

    private void SetShootingView(bool show)
    {
        if (shootingViewRoot != null)
        {
            shootingViewRoot.SetActive(show);
        }
    }

    private void UpdateReticle()
    {
        if (reticle == null)
        {
            return;
        }

        RectTransform reticleParent = reticle.parent as RectTransform;
        if (reticleParent == null)
        {
            reticle.position = Input.mousePosition;
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            reticleParent,
            Input.mousePosition,
            GetCanvasCamera(reticleParent),
            out Vector2 localPosition);

        reticle.anchoredPosition = localPosition;
    }

    private void UpdateWisherMovement()
    {
        if (wisherTargetUi == null || FreezeMovement)
        {
            return;
        }

        if (Time.time >= nextMoveChangeTime)
        {
            PickNewTargetPosition();
        }

        wisherTargetUi.anchoredPosition = Vector2.MoveTowards(
            wisherTargetUi.anchoredPosition,
            targetPosition,
            moveSpeed * Time.deltaTime);
    }

    private void PickNewTargetPosition()
    {
        if (playArea == null || wisherTargetUi == null)
        {
            return;
        }

        Rect rect = playArea.rect;
        float minX = rect.xMin + areaPadding.x;
        float maxX = rect.xMax - areaPadding.x;
        float minY = rect.yMin + areaPadding.y;
        float maxY = rect.yMax - areaPadding.y;

        targetPosition = new Vector2(
            Random.Range(minX, maxX),
            Random.Range(minY, maxY));

        nextMoveChangeTime = Time.time + changePositionInterval;
    }

    private Vector2 GetMouseLocalPosition()
    {
        if (playArea == null)
        {
            return Vector2.zero;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            playArea,
            Input.mousePosition,
            GetCanvasCamera(playArea),
            out Vector2 localPosition);

        return localPosition;
    }

    private Camera GetCanvasCamera(RectTransform target)
    {
        if (target == null)
        {
            return null;
        }

        Canvas canvas = target.GetComponentInParent<Canvas>();
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            return null;
        }

        return canvas.worldCamera;
    }}
