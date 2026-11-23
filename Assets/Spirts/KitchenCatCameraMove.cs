using UnityEngine;

/// <summary>接在原有貓咪鏡頭的啟用事件上，平滑移向既有特寫位置。</summary>
[DisallowMultipleComponent, RequireComponent(typeof(Camera))]
public sealed class KitchenCatCameraMove : MonoBehaviour
{
    [SerializeField] private Camera explorationCamera;
    [SerializeField, Min(0f)] private float moveDuration = 1f;

    private Camera focusCamera;
    private Vector3 focusPosition;
    private Quaternion focusRotation;
    private float focusSize;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private float startSize;
    private float elapsed;
    private bool moving;

    private void Awake()
    {
        focusCamera = GetComponent<Camera>();
        focusPosition = transform.position;
        focusRotation = transform.rotation;
        focusSize = focusCamera.orthographicSize;
    }

    private void OnEnable()
    {
        if (!Application.isPlaying || focusCamera == null) return;
        Camera source = explorationCamera != null ? explorationCamera : Camera.main;
        if (source == null || source == focusCamera || moveDuration <= 0f) return;
        startPosition = source.transform.position;
        startRotation = source.transform.rotation;
        startSize = source.orthographicSize;
        elapsed = 0f;
        moving = true;
        Apply(0f);
    }

    private void LateUpdate()
    {
        if (!moving) return;
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / Mathf.Max(0.001f, moveDuration));
        Apply(Mathf.SmoothStep(0f, 1f, t));
        if (t >= 1f) moving = false;
    }

    private void Apply(float t)
    {
        transform.SetPositionAndRotation(Vector3.Lerp(startPosition, focusPosition, t),
            Quaternion.Slerp(startRotation, focusRotation, t));
        focusCamera.orthographicSize = Mathf.Lerp(startSize, focusSize, t);
    }

    private void OnDisable()
    {
        moving = false;
        if (!Application.isPlaying || focusCamera == null) return;
        transform.SetPositionAndRotation(focusPosition, focusRotation);
        focusCamera.orthographicSize = focusSize;
    }
}
