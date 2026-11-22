using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CameraSystem : MonoBehaviour
{
    public static CameraSystem Instance { get; private set; }

    [Header("State")]
    [SerializeField] private bool hasCamera;
    [SerializeField] private bool cameraModeOpen;

    [Header("Input")]
    [SerializeField] private KeyCode toggleCameraKey = KeyCode.LeftControl;
    [SerializeField] private KeyCode shootKey = KeyCode.Mouse0;
    [SerializeField] private KeyCode flashKey = KeyCode.F;

    [Header("Photo Settings")]
    [SerializeField] private Camera worldCamera;
    [SerializeField] private LayerMask wisherLayer;
    [SerializeField] private float aimRadius = 0.35f;
    [SerializeField] private float photoCooldown = 0.6f;

    [Header("Camera Switching")]
    [SerializeField] private Camera normalCamera;
    [SerializeField] private Camera cameraModeCamera;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private bool stopPlayerMovementInCameraMode = true;
    [SerializeField] private bool moveCameraToPlayerWhenOpened = true;

    [Header("Text Scene Test")]
    [SerializeField] private bool autoCreateTextSceneCameraModeTest = true;
    [SerializeField] private bool unlockCameraInTextSceneTest = true;
    [SerializeField] private string testSceneName = "Text Scene";
    [SerializeField] private Vector3 testCameraModeCenter = new Vector3(100f, 0f, 0f);
    [SerializeField] private Sprite testBackgroundSprite;
    [SerializeField] private Vector2 testBackgroundWorldSize = new Vector2(16f, 9f);
    [SerializeField] private Color testBackgroundColor = new Color(0.2f, 0.27f, 0.38f, 1f);

    [Header("Camera View Movement")]
    [SerializeField] private bool allowCameraViewMovement = true;
    [SerializeField] private float cameraViewMoveSpeed = 4f;
    [SerializeField] private bool moveCameraViewWithMouse = false;
    [SerializeField] private Vector2 maxMouseViewOffset = new Vector2(5f, 3f);
    [SerializeField] private float mouseViewSmoothSpeed = 12f;
    [SerializeField] private bool useCameraMovementBounds;
    [SerializeField] private Vector2 cameraMinPosition;
    [SerializeField] private Vector2 cameraMaxPosition;

    [Header("Background Mouse Parallax")]
    [SerializeField] private Transform cameraModeBackgroundRoot;
    [SerializeField] private bool moveBackgroundWithMouse = true;
    [SerializeField] private bool invertBackgroundMouseMove = true;
    [SerializeField] private Vector2 maxBackgroundMouseOffset = new Vector2(0.8f, 0.45f);
    [SerializeField] private float backgroundMouseSmoothSpeed = 10f;

    [Header("UI")]
    [SerializeField] private GameObject cameraViewRoot;
    [SerializeField] private CameraReticle reticle;
    [SerializeField] private bool letCameraUiClicksPassThrough = true;

    [Header("Events")]
    public UnityEvent onCameraUnlocked;
    public UnityEvent onCameraOpened;
    public UnityEvent onCameraClosed;
    public UnityEvent onPhotoMissed;
    public UnityEvent onFlashUsed;

    private float nextPhotoTime;
    private Vector3 cameraViewAnchorPosition;
    private Vector3 keyboardCameraOffset;
    private Vector3 backgroundAnchorPosition;

    public bool HasCamera => hasCamera;
    public bool CameraModeOpen => cameraModeOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (worldCamera == null)
        {
            worldCamera = Camera.main;
        }

        PrepareTextSceneCameraModeTest();

        if (normalCamera == null)
        {
            normalCamera = worldCamera;
        }

        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
        }

        PrepareCameraUiRaycasts();
        SetCameraMode(false);
    }

    private void PrepareTextSceneCameraModeTest()
    {
        if (!autoCreateTextSceneCameraModeTest)
        {
            return;
        }

        if (SceneManager.GetActiveScene().name != testSceneName || cameraModeCamera != null)
        {
            return;
        }

        if (unlockCameraInTextSceneTest)
        {
            hasCamera = true;
        }

        GameObject worldRoot = new GameObject("CameraModeWorld_Test");
        worldRoot.transform.position = testCameraModeCenter;
        cameraModeBackgroundRoot = worldRoot.transform;

        GameObject background = new GameObject("CameraModeBackground_Test");
        background.transform.SetParent(worldRoot.transform);
        background.transform.localPosition = Vector3.zero;

        SpriteRenderer backgroundRenderer = background.AddComponent<SpriteRenderer>();
        backgroundRenderer.sprite = testBackgroundSprite != null
            ? testBackgroundSprite
            : CreateCheckerSprite(testBackgroundColor);
        backgroundRenderer.sortingOrder = -100;
        SetSpriteWorldSize(backgroundRenderer, testBackgroundWorldSize);

        GameObject testCameraObject = new GameObject("CameraModeCamera_Test");
        testCameraObject.transform.position = new Vector3(testCameraModeCenter.x, testCameraModeCenter.y, -10f);
        Camera createdCamera = testCameraObject.AddComponent<Camera>();
        createdCamera.orthographic = true;
        createdCamera.orthographicSize = 5f;
        createdCamera.backgroundColor = Color.black;
        createdCamera.clearFlags = CameraClearFlags.SolidColor;
        createdCamera.enabled = false;

        cameraModeCamera = createdCamera;
        moveCameraToPlayerWhenOpened = false;
        useCameraMovementBounds = true;
        cameraMinPosition = new Vector2(testCameraModeCenter.x - maxMouseViewOffset.x, testCameraModeCenter.y - maxMouseViewOffset.y);
        cameraMaxPosition = new Vector2(testCameraModeCenter.x + maxMouseViewOffset.x, testCameraModeCenter.y + maxMouseViewOffset.y);
    }

    private void SetSpriteWorldSize(SpriteRenderer spriteRenderer, Vector2 worldSize)
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            return;
        }

        Vector2 spriteSize = spriteRenderer.sprite.bounds.size;
        if (spriteSize.x <= 0f || spriteSize.y <= 0f)
        {
            return;
        }

        spriteRenderer.transform.localScale = new Vector3(
            worldSize.x / spriteSize.x,
            worldSize.y / spriteSize.y,
            1f);
    }

    private Sprite CreateCheckerSprite(Color baseColor)
    {
        const int width = 160;
        const int height = 90;
        const int cellSize = 10;

        Texture2D texture = new Texture2D(width, height);
        Color darkColor = baseColor * 0.75f;
        darkColor.a = 1f;
        Color lineColor = Color.white * 0.45f;
        lineColor.a = 1f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool isChecker = ((x / cellSize) + (y / cellSize)) % 2 == 0;
                bool isGridLine = x % cellSize == 0 || y % cellSize == 0;
                Color pixelColor = isChecker ? baseColor : darkColor;

                if (isGridLine)
                {
                    pixelColor = Color.Lerp(pixelColor, lineColor, 0.35f);
                }

                texture.SetPixel(x, y, pixelColor);
            }
        }

        texture.filterMode = FilterMode.Point;
        texture.Apply();

        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 10f);
    }

    private void Update()
    {
        if (!hasCamera)
        {
            return;
        }

        if (Input.GetKeyDown(toggleCameraKey))
        {
            SetCameraMode(!cameraModeOpen);
        }

        if (!cameraModeOpen)
        {
            return;
        }

        MoveCameraView();
        MoveCameraModeBackground();

        Vector2 aimWorldPosition = GetAimWorldPosition();

        if (reticle != null)
        {
            reticle.SetWorldPosition(aimWorldPosition);
        }

        if (Input.GetKeyDown(shootKey))
        {
            TryTakePhoto(aimWorldPosition);
        }

        if (Input.GetKeyDown(flashKey))
        {
            UseFlash(aimWorldPosition);
        }
    }

    public void UnlockCamera()
    {
        if (hasCamera)
        {
            return;
        }

        hasCamera = true;
        onCameraUnlocked?.Invoke();
    }

    public void SetCameraMode(bool open)
    {
        cameraModeOpen = hasCamera && open;

        if (cameraViewRoot != null)
        {
            cameraViewRoot.SetActive(cameraModeOpen);
        }

        SetActiveGameplayCamera(cameraModeOpen);
        SetPlayerMovement(!cameraModeOpen);

        if (cameraModeOpen)
        {
            onCameraOpened?.Invoke();
        }
        else
        {
            onCameraClosed?.Invoke();
        }
    }

    private void SetActiveGameplayCamera(bool useCameraModeCamera)
    {
        if (normalCamera == null && worldCamera != null)
        {
            normalCamera = worldCamera;
        }

        if (cameraModeCamera == null)
        {
            worldCamera = normalCamera;
            return;
        }

        if (normalCamera != null)
        {
            normalCamera.enabled = !useCameraModeCamera;
        }

        cameraModeCamera.enabled = useCameraModeCamera;
        worldCamera = useCameraModeCamera ? cameraModeCamera : normalCamera;

        if (reticle != null)
        {
            reticle.SetWorldCamera(worldCamera);
        }

        if (useCameraModeCamera && moveCameraToPlayerWhenOpened && playerController != null)
        {
            Vector3 cameraPosition = cameraModeCamera.transform.position;
            Vector3 playerPosition = playerController.transform.position;
            cameraModeCamera.transform.position = new Vector3(playerPosition.x, playerPosition.y, cameraPosition.z);
        }

        if (useCameraModeCamera)
        {
            cameraViewAnchorPosition = cameraModeCamera.transform.position;
            keyboardCameraOffset = Vector3.zero;

            if (cameraModeBackgroundRoot != null)
            {
                backgroundAnchorPosition = cameraModeBackgroundRoot.position;
            }
        }
    }

    private void SetPlayerMovement(bool canMove)
    {
        if (!stopPlayerMovementInCameraMode || playerController == null)
        {
            return;
        }

        playerController.canMove = canMove;
    }

    private void MoveCameraView()
    {
        if (cameraModeCamera == null)
        {
            return;
        }

        Vector3 targetPosition = cameraViewAnchorPosition;

        if (allowCameraViewMovement)
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            Vector3 movement = new Vector3(horizontal, vertical, 0f).normalized;
            keyboardCameraOffset += movement * cameraViewMoveSpeed * Time.deltaTime;
        }

        targetPosition += keyboardCameraOffset;

        if (moveCameraViewWithMouse)
        {
            targetPosition += GetMouseViewOffset();
        }

        if (useCameraMovementBounds)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, cameraMinPosition.x, cameraMaxPosition.x);
            targetPosition.y = Mathf.Clamp(targetPosition.y, cameraMinPosition.y, cameraMaxPosition.y);
        }

        cameraModeCamera.transform.position = Vector3.Lerp(
            cameraModeCamera.transform.position,
            targetPosition,
            mouseViewSmoothSpeed * Time.deltaTime);
    }

    private Vector3 GetMouseViewOffset()
    {
        if (Screen.width <= 0 || Screen.height <= 0)
        {
            return Vector3.zero;
        }

        float normalizedX = (Input.mousePosition.x / Screen.width - 0.5f) * 2f;
        float normalizedY = (Input.mousePosition.y / Screen.height - 0.5f) * 2f;

        normalizedX = Mathf.Clamp(normalizedX, -1f, 1f);
        normalizedY = Mathf.Clamp(normalizedY, -1f, 1f);

        return new Vector3(
            normalizedX * maxMouseViewOffset.x,
            normalizedY * maxMouseViewOffset.y,
            0f);
    }

    private void MoveCameraModeBackground()
    {
        if (!moveBackgroundWithMouse || cameraModeBackgroundRoot == null)
        {
            return;
        }

        Vector3 mouseOffset = GetMouseBackgroundOffset();
        Vector3 targetPosition = backgroundAnchorPosition + mouseOffset;

        cameraModeBackgroundRoot.position = Vector3.Lerp(
            cameraModeBackgroundRoot.position,
            targetPosition,
            backgroundMouseSmoothSpeed * Time.deltaTime);
    }

    private Vector3 GetMouseBackgroundOffset()
    {
        if (Screen.width <= 0 || Screen.height <= 0)
        {
            return Vector3.zero;
        }

        float normalizedX = (Input.mousePosition.x / Screen.width - 0.5f) * 2f;
        float normalizedY = (Input.mousePosition.y / Screen.height - 0.5f) * 2f;

        normalizedX = Mathf.Clamp(normalizedX, -1f, 1f);
        normalizedY = Mathf.Clamp(normalizedY, -1f, 1f);

        float direction = invertBackgroundMouseMove ? -1f : 1f;

        return new Vector3(
            normalizedX * maxBackgroundMouseOffset.x * direction,
            normalizedY * maxBackgroundMouseOffset.y * direction,
            0f);
    }

    private void PrepareCameraUiRaycasts()
    {
        if (!letCameraUiClicksPassThrough || cameraViewRoot == null)
        {
            return;
        }

        Graphic[] graphics = cameraViewRoot.GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in graphics)
        {
            graphic.raycastTarget = false;
        }
    }

    private void TryTakePhoto(Vector2 aimWorldPosition)
    {
        if (Time.time < nextPhotoTime)
        {
            return;
        }

        nextPhotoTime = Time.time + photoCooldown;

        Collider2D hit = Physics2D.OverlapCircle(aimWorldPosition, aimRadius, wisherLayer);
        if (hit == null || !hit.TryGetComponent(out WisherPhotoTarget target))
        {
            onPhotoMissed?.Invoke();
            return;
        }

        target.ReceivePhotoHit(aimWorldPosition);
    }

    private void UseFlash(Vector2 aimWorldPosition)
    {
        onFlashUsed?.Invoke();

        Collider2D hit = Physics2D.OverlapCircle(aimWorldPosition, aimRadius, wisherLayer);
        if (hit != null && hit.TryGetComponent(out WisherPhotoTarget target))
        {
            target.ReceiveFlashHit(aimWorldPosition);
        }
    }

    private Vector2 GetAimWorldPosition()
    {
        if (worldCamera == null)
        {
            return transform.position;
        }

        Vector3 mousePosition = Input.mousePosition;
        Vector3 worldPosition = worldCamera.ScreenToWorldPoint(mousePosition);
        worldPosition.z = 0f;
        return worldPosition;
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || !cameraModeOpen)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(GetAimWorldPosition(), aimRadius);
    }
}
