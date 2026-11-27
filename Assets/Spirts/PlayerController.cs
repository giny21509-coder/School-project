using Fungus;
using UnityEngine;
using MoreMountains.TopDownEngine;
//using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{

    private const string HorizontalAxis = "Horizontal";
    private const string VerticalAxis = "Vertical";
    private const string Player1VerticalAxis = "Player1_Vertical";

    [Header("Footstep Audio")]
    public bool enableFootsteps = true;
    [Tooltip("Enable for a recording containing several footsteps.")]
    public bool loopFootsteps = false;
    [Tooltip("Single steps, or a continuous recording when Loop Footsteps is enabled.")]
    public AudioClip[] footstepClips;
    [Range(0f, 1f)] public float footstepVolume = 0.45f;
    [Min(0.05f)] public float footstepInterval = 0.4f;
    public UnityEngine.Audio.AudioMixerGroup footstepMixerGroup;

    private AudioSource footstepSource;
    private Vector2 lastFootstepPosition;
    private bool hasFootstepPosition;
    private float nextFootstepTime;
    private int lastFootstepClipIndex = -1;

    public bool canMove = true;
    [Header("人物移動界線設置")]
    public float minY, maxY;
    [Header("設置")]
    public Rigidbody2D rb;
    public float moveSpeed;
    private Animator anim;
    Vector2 movement;


    [Header("人物移動縮放設定")]
    public float minScale = 0.7f; 
    public float maxScale = 1.3f;

    [Header("Depth Brightness")]
    [Tooltip("Darken the player at Max Y and brighten at Min Y. Enable per scene.")]
    public bool enableDepthBrightness = false;
    [Tooltip("Player body only. Defaults to the SpriteRenderer on this object.")]
    public SpriteRenderer depthSpriteRenderer;
    [Range(0f, 1f)] public float farBrightness = 0.45f;
    [Range(0f, 1f)] public float nearBrightness = 1f;

    private Color originalSpriteColor;
    private bool hasSpriteColor;
    private bool depthBrightnessApplied;

    [Header("Camera Companion")]
    public SpiritFollow spiritFollow;
    [HideInInspector] public Animator weaponAnimator;
    [HideInInspector] public GameObject attackArea;

    private void Awake()
    {
        anim= GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        if (depthSpriteRenderer == null) depthSpriteRenderer = GetComponent<SpriteRenderer>();
        if (depthSpriteRenderer != null)
        {
            originalSpriteColor = depthSpriteRenderer.color;
            hasSpriteColor = true;
        }
    }

    private void OnEnable()
    {
        DisableWeaponCombat();
        UpdateDepthBrightness();
    }

    private void DisableWeaponCombat()
    {
        if (attackArea != null && attackArea != gameObject) attackArea.SetActive(false);
        foreach (CharacterHandleWeapon handler in GetComponentsInChildren<CharacterHandleWeapon>(true))
        {
            handler.AbilityPermitted = false;
            handler.enabled = false;
        }
        foreach (DamageOnTouch damage in GetComponentsInChildren<DamageOnTouch>(true)) damage.enabled = false;
        foreach (Attack attack in GetComponentsInChildren<Attack>(true)) attack.enabled = false;
        foreach (Weapon weapon in GetComponentsInChildren<Weapon>(true))
        {
            weapon.enabled = false;
            if (weapon.gameObject != gameObject && weapon.GetComponent<SpiritFollow>() == null)
                weapon.gameObject.SetActive(false);
        }
    }



    // Update is called once per frame
    void Update()
    {
        if (!canMove || Time.timeScale <= 0f)
        {
            StopFootstepAudio();
            movement = Vector2.zero;
            return;
        }
        movement.x = Input.GetAxisRaw(HorizontalAxis);

        float vertical = Input.GetAxisRaw(VerticalAxis);
        float player1Vertical = Input.GetAxisRaw(Player1VerticalAxis);
        movement.y = Mathf.Abs(player1Vertical) > Mathf.Abs(vertical) ? player1Vertical : vertical;

        if(movement != Vector2.zero)
        {
            anim.SetFloat("moveX",movement.x);
            anim.SetFloat("moveY",movement.y);
            anim.SetBool("isWalk", true);
        }
        else
        {
            anim.SetBool("isWalk", false);
        }
    }
       
        
    private void FixedUpdate()
    {
        if (rb == null) return;
        UpdateFootstepAudio();
        if (!canMove)
        {
            movement = Vector2.zero;
            rb.linearVelocity = Vector2.zero;
            return;
        }
        Vector2 nextPosition = rb.position + movement.normalized * moveSpeed * Time.fixedDeltaTime;
      //nextPosition.y=Mathf.Clamp(nextPosition.y,minY,maxY);
      

        rb.MovePosition(nextPosition);

        float t = Mathf.InverseLerp(maxY, minY, rb.position.y);

        float currentScale = Mathf.Lerp(minScale, maxScale, t);

        float currentDirectionX = Mathf.Sign(transform.localScale.x);
        transform.localScale = new Vector3(currentScale * currentDirectionX, currentScale, 1f);
    }
    private void LateUpdate()
    {
        UpdateDepthBrightness();
    }

    private void UpdateDepthBrightness()
    {
        if (!enableDepthBrightness || Mathf.Approximately(minY, maxY))
        {
            RestoreDepthBrightness();
            return;
        }
        if (depthSpriteRenderer == null || !hasSpriteColor) return;

        // Use rendered world position so Rigidbody interpolation stays smooth.
        float depth = Mathf.InverseLerp(Mathf.Max(minY, maxY), Mathf.Min(minY, maxY), transform.position.y);
        float brightness = Mathf.Lerp(Mathf.Clamp01(farBrightness), Mathf.Clamp01(nearBrightness), depth);
        Color color = depthSpriteRenderer.color;
        color.r = originalSpriteColor.r * brightness;
        color.g = originalSpriteColor.g * brightness;
        color.b = originalSpriteColor.b * brightness;
        // Keep the current alpha so fading and visibility remain independent.
        depthSpriteRenderer.color = color;
        depthBrightnessApplied = true;
    }

    private void RestoreDepthBrightness()
    {
        if (!depthBrightnessApplied || depthSpriteRenderer == null || !hasSpriteColor) return;
        Color color = depthSpriteRenderer.color;
        color.r = originalSpriteColor.r;
        color.g = originalSpriteColor.g;
        color.b = originalSpriteColor.b;
        depthSpriteRenderer.color = color;
        depthBrightnessApplied = false;
    }

    private void UpdateFootstepAudio()
    {
        if (rb == null) return;

        Vector2 position = rb.position;
        Vector2 delta = position - lastFootstepPosition;
        bool hadPosition = hasFootstepPosition;
        lastFootstepPosition = position;
        hasFootstepPosition = true;

        // Sample completed physics movement, so pushing against a wall stays silent.
        float maxTravel = Mathf.Abs(moveSpeed) * Time.fixedDeltaTime * 2f + 0.1f;
        bool walking = hadPosition && enableFootsteps && canMove
            && Time.timeScale > 0f && movement.sqrMagnitude > 0.001f
            && delta.sqrMagnitude > 0.000001f && delta.magnitude <= maxTravel;

        if (!walking || footstepClips == null || footstepClips.Length == 0)
        {
            if (footstepSource != null) footstepSource.Stop();
            return;
        }

        if (loopFootsteps)
        {
            AudioClip loopClip = null;
            foreach (AudioClip clip in footstepClips)
            {
                if (clip == null) continue;
                loopClip = clip;
                break;
            }
            if (loopClip == null)
            {
                if (footstepSource != null) footstepSource.Stop();
                return;
            }

            EnsureFootstepSource();
            if (!footstepSource.loop || footstepSource.clip != loopClip)
            {
                footstepSource.Stop();
                footstepSource.loop = true;
                footstepSource.clip = loopClip;
            }
            footstepSource.pitch = 1f;
            if (!footstepSource.isPlaying) footstepSource.Play();
            return;
        }

        if (Time.time < nextFootstepTime) return;

        // Select a valid clip and avoid immediate repeats when alternatives exist.
        int start = Random.Range(0, footstepClips.Length);
        int selected = -1;
        for (int i = 0; i < footstepClips.Length; i++)
        {
            int index = (start + i) % footstepClips.Length;
            if (footstepClips[index] == null) continue;
            selected = index;
            if (index != lastFootstepClipIndex) break;
        }
        if (selected < 0) return;

        EnsureFootstepSource();
        footstepSource.loop = false;
        footstepSource.pitch = Random.Range(0.95f, 1.05f);
        footstepSource.clip = footstepClips[selected];
        footstepSource.Play();
        lastFootstepClipIndex = selected;
        nextFootstepTime = Time.time + Mathf.Max(0.05f, footstepInterval);
    }

    private void EnsureFootstepSource()
    {
        if (footstepSource == null)
        {
            // A dedicated source keeps footsteps separate from other character sounds.
            GameObject audioObject = new GameObject("FootstepAudio");
            audioObject.transform.SetParent(transform, false);
            footstepSource = audioObject.AddComponent<AudioSource>();
            footstepSource.playOnAwake = false;
            footstepSource.spatialBlend = 0f;
        }
        footstepSource.outputAudioMixerGroup = footstepMixerGroup;
        footstepSource.volume = Mathf.Clamp01(footstepVolume);
    }

    private void StopFootstepAudio()
    {
        hasFootstepPosition = false;
        if (footstepSource != null) footstepSource.Stop();
    }

    private void OnDisable()
    {
        StopFootstepAudio();
        RestoreDepthBrightness();
    }

    public void StopControl()
    {
        StopFootstepAudio();
        canMove = false;
        movement = Vector2.zero;
        if (rb != null) rb.linearVelocity = Vector2.zero;
        if (anim != null) anim.SetBool("isWalk", false);
    }
    public void StarControl()
    {
        canMove = true;
    }
    //rb.MovePosition(rb.position + movement*moveSpeed*Time.fixedDeltaTime);
}


