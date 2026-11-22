using UnityEngine;

public class CameraReticle : MonoBehaviour
{
    [SerializeField] private Camera worldCamera;
    [SerializeField] private RectTransform uiReticle;
    [SerializeField] private Canvas canvas;
    [SerializeField] private bool followMouseDirectly = true;
    [SerializeField] private bool smoothFollow;
    [SerializeField] private float followSpeed = 20f;

    private void Awake()
    {
        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        if (uiReticle == null)
        {
            uiReticle = transform as RectTransform;
        }

        if (canvas == null)
        {
            canvas = GetComponentInParent<Canvas>();
        }
    }

    public void SetWorldPosition(Vector2 worldPosition)
    {
        if (uiReticle == null)
        {
            return;
        }

        Vector3 screenPosition = followMouseDirectly
            ? Input.mousePosition
            : worldCamera.WorldToScreenPoint(worldPosition);

        SetScreenPosition(screenPosition);
    }

    public void SetWorldCamera(Camera newWorldCamera)
    {
        worldCamera = newWorldCamera;
    }

    private void SetScreenPosition(Vector2 screenPosition)
    {
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            Vector3 targetPosition = screenPosition;
            uiReticle.position = smoothFollow
                ? Vector3.Lerp(uiReticle.position, targetPosition, followSpeed * Time.deltaTime)
                : targetPosition;
            return;
        }

        RectTransform parentRect = uiReticle.parent as RectTransform;
        if (parentRect == null)
        {
            return;
        }

        Camera canvasCamera = canvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            screenPosition,
            canvasCamera,
            out Vector2 localPosition);

        uiReticle.localPosition = smoothFollow
            ? Vector3.Lerp(uiReticle.localPosition, localPosition, followSpeed * Time.deltaTime)
            : localPosition;
    }
}
