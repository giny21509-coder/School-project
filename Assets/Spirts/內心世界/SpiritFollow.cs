using UnityEngine;
using UnityEngine.InputSystem;

public class SpiritFollow : MonoBehaviour
{
    public Transform player;
    public Transform weaponPoint;

    public Sprite spiritSprite;
    public Sprite swordSprite;

    public Vector3 followOffset = new Vector3(1.2f, 0.8f, 0f);
    public float followSmoothTime = 0.15f;

    [Header("Flash Aim Position")]
    [Min(0.1f)] public float flashAimForwardOffset = 1.2f;
    [Min(0.01f)] public float flashAimSmoothTime = 0.08f;

    public float floatHeight = 0.15f;
    public float floatSpeed = 3f;

    public Vector3 weaponLocalPosition = Vector3.zero;
    public Vector3 swordLocalRotation = new Vector3(0f, 0f, -45f);
    public Vector3 swordLocalScale = new Vector3(1f, 1f, 1f);

    public Vector3 spiritScale = new Vector3(1f, 1f, 1f);

    private bool isWeaponMode = false;
    public bool IsWeaponMode => isWeaponMode;
    private Vector3 velocity;
    private SpriteRenderer spriteRenderer;
    private bool flashAiming;
    private float flashAimSide = 1f;
    private Vector3 lastAimPlayerPosition;

    public bool IsAtFlashAimPosition => flashAiming && !isWeaponMode && player != null
        && (transform.position - GetFollowTargetPosition()).sqrMagnitude <= 0.0144f;

    public void SetFlashAim(bool active, Vector2 aimWorldPosition)
    {
        active = active && !isWeaponMode && player != null;
        if (active != flashAiming)
        {
            velocity = Vector3.zero;
            if (active) lastAimPlayerPosition = player.position;
        }
        flashAiming = active;
        if (!active) return;
        float horizontal = aimWorldPosition.x - player.position.x;
        if (Mathf.Abs(horizontal) > 0.02f)
        {
            float side = Mathf.Sign(horizontal);
            if (side != flashAimSide) velocity = Vector3.zero;
            flashAimSide = side;
        }
    }



    [Header("動畫設定")]
    public Animator spiritAnimator;
    public Animator playerAnimator;
    public string battleModeParameterName = "BattleMode";
    

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (playerAnimator == null && player != null)
        {
            playerAnimator = player.GetComponent<Animator>();
        }
    }

    private void Update()
    {
        if (PressedR())
        {
            ToggleMode();
        }
    }

    private void LateUpdate()
    {
        FollowPlayer(Time.deltaTime);
    }

    private Vector3 GetFollowTargetPosition()
    {
        Vector3 offset = followOffset;
        if (flashAiming) offset.x = flashAimSide * Mathf.Max(0.1f, flashAimForwardOffset);
        Vector3 floatOffset = Vector3.up * Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        return player.position + offset + floatOffset;
    }

    private void FollowPlayer(float deltaTime)
    {
        if (isWeaponMode)
            return;
        if (player == null)
            return;
        if (deltaTime <= 0f) return;
        if (flashAiming)
        {
            // Carry player movement separately so aiming can settle while walking.
            transform.position += player.position - lastAimPlayerPosition;
            lastAimPlayerPosition = player.position;
        }

        transform.position = Vector3.SmoothDamp(
            transform.position,
            GetFollowTargetPosition(),
            ref velocity,
            flashAiming ? flashAimSmoothTime : followSmoothTime,
            Mathf.Infinity,
            deltaTime
        );
    }
    private void ToggleMode()
    {
        SetFlashAim(false, Vector2.zero);
        isWeaponMode = !isWeaponMode;
        SetBattleModeAnimation(isWeaponMode);

        if (isWeaponMode)
        {
            BecomeWeapon();
        }
        else
        {
            BecomeSpirit();
        }
    }

    private void SetBattleModeAnimation(bool active)
    {
        if (spiritAnimator != null)
        {
            spiritAnimator.SetBool(battleModeParameterName, active);
        }

        if (playerAnimator != null)
        {
            playerAnimator.SetBool(battleModeParameterName, active);
        }
    }

    private void BecomeWeapon()
    {
        if (weaponPoint == null)
            return;

        transform.SetParent(weaponPoint);

        transform.localPosition = weaponLocalPosition;
        transform.localRotation = Quaternion.Euler(swordLocalRotation);
        transform.localScale = swordLocalScale;

        if (spriteRenderer != null && swordSprite != null)
        {
            spriteRenderer.sprite = swordSprite;
        }
    }

    private void BecomeSpirit()
    {
        transform.SetParent(null);

        transform.localScale = spiritScale;

        if (spriteRenderer != null && spiritSprite != null)
        {
            spriteRenderer.sprite = spiritSprite;
        }

        velocity = Vector3.zero;
    }
    private bool PressedR()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
            return Keyboard.current.rKey.wasPressedThisFrame;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.R);
#else
        return false;
#endif
    }

    private void OnDisable()
    {
        SetFlashAim(false, Vector2.zero);
    }
}

