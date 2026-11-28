using UnityEngine;

/// <summary>鏡頭四周的流動霧：每打倒一隻圖片怪，清楚的視野擴大並平滑退霧。</summary>
[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
[DefaultExecutionOrder(10000)]
public sealed class ScreenEdgeFog : MonoBehaviour
{
    [Header("連接（留空時自動尋找同場景的物件）")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private InnerWorldDistanceSpawner spawner;

    [Header("煙霧外觀")]
    [SerializeField, Range(0f, 2f)] private float density = 0.72f;
    [SerializeField, Range(0f, 1f)] private float clearRadius = 0.48f;
    [SerializeField, Range(0.05f, 0.6f)] private float edgeSoftness = 0.32f;
    [Tooltip("每次打倒圖片怪後，煙霧消退到下一階段所需的秒數。")]
    [SerializeField, Min(0f)] private float fadeDuration = 2.5f;
    [Tooltip("打完五隻圖片怪後的消霧進度；1 代表完全清晰。")]
    [SerializeField, Range(0f, 1f)] private float finalClearProgress = 1f;

    [Header("鏡頭覆蓋")]
    [SerializeField, Min(0.01f)] private float cameraDistance = 1f;
    [SerializeField, Range(1f, 1.2f)] private float overscan = 1.02f;

    [Header("編輯模式預覽（0：濃霧，1：完全清晰）")]
    [SerializeField, Range(0f, 1f)] private float previewProgress;

    public float ClearProgress => currentProgress;
    private SpriteRenderer fogRenderer;
    private MaterialPropertyBlock properties;
    private InnerWorldDistanceSpawner subscribedSpawner;
    private float currentProgress, fromProgress, targetProgress, fadeElapsed;

    private void OnEnable()
    {
        fogRenderer = GetComponent<SpriteRenderer>();
        properties = new MaterialPropertyBlock();
        if (Application.IsPlaying(gameObject)) BindSpawner();
        RefreshView();
    }

    private void BindSpawner()
    {
        if (spawner == null)
            foreach (var candidate in FindObjectsByType<InnerWorldDistanceSpawner>(FindObjectsSortMode.None))
                if (candidate.gameObject.scene == gameObject.scene) { spawner = candidate; break; }
        if (spawner == null) return;
        subscribedSpawner = spawner;
        subscribedSpawner.onImageDefeated.AddListener(SetDefeatedCount);
        // Late activation must show the current stage, rather than starting over in thick fog.
        currentProgress = targetProgress = fromProgress =
            Mathf.Clamp01((float)spawner.ImageDefeated / InnerWorldDistanceSpawner.TotalImages) * finalClearProgress;
    }

    private void Start()
    {
        if (Application.IsPlaying(gameObject) && subscribedSpawner == null) BindSpawner();
    }

    /// <summary>章節掛勾：直接指定消霧進度（0：濃霧，1：清晰），例如第六章由空虛值驅動。</summary>
    public void SetProgress(float progress01)
    {
        fromProgress = currentProgress;
        targetProgress = Mathf.Clamp01(progress01) * finalClearProgress;
        fadeElapsed = 0f;
        if (fadeDuration <= 0f) currentProgress = targetProgress;
    }

    public void SetDefeatedCount(int count)
    {
        fromProgress = currentProgress;
        targetProgress = Mathf.Clamp01((float)count / InnerWorldDistanceSpawner.TotalImages) * finalClearProgress;
        fadeElapsed = 0f;
        if (fadeDuration <= 0f) currentProgress = targetProgress;
    }

    private void LateUpdate()
    {
        if (Application.IsPlaying(gameObject))
        {
            fadeElapsed += Time.deltaTime;
            float t = fadeDuration <= 0f ? 1f : Mathf.Clamp01(fadeElapsed / fadeDuration);
            currentProgress = Mathf.Lerp(fromProgress, targetProgress, Mathf.SmoothStep(0f, 1f, t));
        }
        RefreshView();
    }

    public void RefreshView()
    {
        if (!gameObject.scene.IsValid()) return;
        if (fogRenderer == null) fogRenderer = GetComponent<SpriteRenderer>();
        if (fogRenderer == null) return;
        if (targetCamera == null) targetCamera = GetComponentInParent<Camera>();
        if (targetCamera == null && Camera.main != null && Camera.main.gameObject.scene == gameObject.scene)
            targetCamera = Camera.main;
        if (targetCamera != null && fogRenderer.sprite != null) FitCamera();
        if (properties == null) properties = new MaterialPropertyBlock();
        fogRenderer.GetPropertyBlock(properties);
        properties.SetFloat("_ClearProgress", Application.IsPlaying(gameObject) ? currentProgress : previewProgress);
        properties.SetFloat("_Density", density);
        properties.SetFloat("_ClearRadius", clearRadius);
        properties.SetFloat("_EdgeSoftness", edgeSoftness);
        fogRenderer.SetPropertyBlock(properties);
    }

    private void FitCamera()
    {
        float distance = Mathf.Clamp(cameraDistance, targetCamera.nearClipPlane + 0.01f,
            Mathf.Max(targetCamera.nearClipPlane + 0.01f, targetCamera.farClipPlane - 0.01f));
        Vector3 bottomLeft = targetCamera.ViewportToWorldPoint(new Vector3(0f, 0f, distance));
        Vector3 bottomRight = targetCamera.ViewportToWorldPoint(new Vector3(1f, 0f, distance));
        Vector3 topLeft = targetCamera.ViewportToWorldPoint(new Vector3(0f, 1f, distance));
        Vector3 center = targetCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, distance));
        transform.rotation = targetCamera.transform.rotation;
        Transform parent = transform.parent;
        float parentX = parent == null ? 1f : parent.TransformVector(transform.localRotation * Vector3.right).magnitude;
        float parentY = parent == null ? 1f : parent.TransformVector(transform.localRotation * Vector3.up).magnitude;
        Bounds sprite = fogRenderer.sprite.bounds;
        transform.localScale = new Vector3(
            Vector3.Distance(bottomLeft, bottomRight) * overscan / Mathf.Max(0.0001f, sprite.size.x * parentX),
            Vector3.Distance(bottomLeft, topLeft) * overscan / Mathf.Max(0.0001f, sprite.size.y * parentY), 1f);
        transform.position = center - transform.TransformVector(sprite.center);
    }

    private void OnDisable()
    {
        if (subscribedSpawner != null) subscribedSpawner.onImageDefeated.RemoveListener(SetDefeatedCount);
        subscribedSpawner = null;
        if (fogRenderer != null) fogRenderer.SetPropertyBlock(null);
    }

    private void OnValidate()
    {
        // Unity may call OnValidate while loading assets; apply on the next editor update.
        fadeDuration = Mathf.Max(0f, fadeDuration);
    }
}
