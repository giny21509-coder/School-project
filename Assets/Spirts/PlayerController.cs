using Fungus;
using UnityEngine;
using System.Collections;
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

    [Header("Swing Audio")]
    public bool enableSwingAudio = true;
    public AudioClip swingClip;
    [Range(0f, 1f)] public float swingVolume = 0.7f;
    public UnityEngine.Audio.AudioMixerGroup swingMixerGroup;

    private AudioSource swingSource;

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

    [Header("攻擊設定")]
    public Animator weaponAnimator;
    public SpiritFollow spiritFollow;
    public GameObject attackArea;
    public string attackTriggerName = "MeleeAttackStart";
    public string[] comboAttackTriggerNames = { "MeleeAttack1", "MeleeAttack2", "MeleeAttack3" };
    public int attackMouseButton = 1;
    public bool triggerWeaponAnimatorAttacks = false;
    public float attackCooldown = 0.4f;
    public float attackAreaActiveTime = 0.2f;
    public float comboResetTime = 0.8f;

    [Header("Second Hit Knockback")]
    [Min(0f)] public float secondHitKnockbackForce = 30f;
    [Min(0f)] public float secondHitKnockbackDuration = 0.2f;
    [Min(0f)] public float attackHitInvincibilityDuration = 0.25f;

    [Header("Third Hit Heavy Slash")]
    [Min(1f)] public float thirdHitDamageMultiplier = 1.5f;
    [Min(0f)] public float thirdHitKnockbackForce = 8f;
    [Min(0f)] public float thirdHitKnockbackDuration = 0.12f;

    private float lastAttackTime = -999f;
    private float lastComboInputTime = -999f;
    private int comboStep;
    private Coroutine attackAreaCoroutine;
    private DamageOnTouch[] attackDamageAreas;
    private float[] baseMinDamage;
    private float[] baseMaxDamage;


    void Start()
    {
        anim= GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        if (attackArea != null)
        {
            attackArea.SetActive(false);
            attackDamageAreas = attackArea.GetComponentsInChildren<DamageOnTouch>(true);
            baseMinDamage = new float[attackDamageAreas.Length];
            baseMaxDamage = new float[attackDamageAreas.Length];
            for (int i = 0; i < attackDamageAreas.Length; i++)
            {
                DamageOnTouch damageArea = attackDamageAreas[i];
                baseMinDamage[i] = damageArea.MinDamageCaused;
                baseMaxDamage[i] = damageArea.MaxDamageCaused;
                if (damageArea.HitDamageableEvent == null)
                {
                    damageArea.HitDamageableEvent = new UnityEngine.Events.UnityEvent<Health>();
                }
                damageArea.HitDamageableEvent.AddListener(OnAttackHitDamageable);
            }
        }
    }

    private void OnDestroy()
    {
        if (attackDamageAreas == null) return;
        foreach (DamageOnTouch damageArea in attackDamageAreas)
        {
            if (damageArea != null)
            {
                damageArea.HitDamageableEvent?.RemoveListener(OnAttackHitDamageable);
            }
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
        ResetComboIfNeeded();

        if (Input.GetMouseButtonDown(attackMouseButton) && Time.time >= lastAttackTime + attackCooldown
            && (UnityEngine.EventSystems.EventSystem.current == null
                || !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()))
        {
            if (spiritFollow == null || !spiritFollow.IsWeaponMode)
            {
                return;
            }

            lastAttackTime = Time.time;
            AdvanceComboStep();
            PlaySwingAudio();

            string attackTrigger = GetCurrentAttackTriggerName();

            if (anim != null)
            {
                anim.SetTrigger(attackTrigger);
            }

            if (triggerWeaponAnimatorAttacks && weaponAnimator != null)
            {
                weaponAnimator.SetTrigger(attackTrigger);
            }
            if (attackArea != null)
            {
                if (attackAreaCoroutine != null)
                {
                    StopCoroutine(attackAreaCoroutine);
                }

                attackAreaCoroutine = StartCoroutine(EnableAttackAreaTemporarily());
            }
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
    private void PlaySwingAudio()
    {
        if (!enableSwingAudio || swingClip == null) return;

        if (swingSource == null)
        {
            GameObject audioObject = new GameObject("SwingAudio");
            audioObject.transform.SetParent(transform, false);
            swingSource = audioObject.AddComponent<AudioSource>();
            swingSource.playOnAwake = false;
            swingSource.spatialBlend = 0f;
        }

        swingSource.outputAudioMixerGroup = swingMixerGroup;
        swingSource.PlayOneShot(swingClip, Mathf.Clamp01(swingVolume));
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
        if (swingSource != null) swingSource.Stop();
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
    private void ResetComboIfNeeded()
    {
        if (comboStep > 0 && Time.time > lastComboInputTime + comboResetTime)
        {
            comboStep = 0;
        }
    }
    private void AdvanceComboStep()
    {
        comboStep++;

        int maxComboStep = comboAttackTriggerNames != null && comboAttackTriggerNames.Length > 0
            ? comboAttackTriggerNames.Length
            : 1;

        if (comboStep > maxComboStep)
        {
            comboStep = 1;
        }

        lastComboInputTime = Time.time;
    }
    private string GetCurrentAttackTriggerName()
    {
        if (comboAttackTriggerNames == null || comboAttackTriggerNames.Length == 0)
        {
            return attackTriggerName;
        }

        int triggerIndex = Mathf.Clamp(comboStep - 1, 0, comboAttackTriggerNames.Length - 1);
        return comboAttackTriggerNames[triggerIndex];
    }
    private float GetCurrentKnockbackForce()
    {
        if (comboStep == 2) return secondHitKnockbackForce;
        if (comboStep == 3) return thirdHitKnockbackForce;
        return 0f;
    }

    private void ConfigureAttackKnockback()
    {
        float knockbackForce = GetCurrentKnockbackForce();
        float damageMultiplier = comboStep == 3 ? thirdHitDamageMultiplier : 1f;

        for (int i = 0; i < attackDamageAreas.Length; i++)
        {
            DamageOnTouch damageArea = attackDamageAreas[i];
            if (damageArea == null) continue;

            damageArea.Owner = gameObject;
            damageArea.MinDamageCaused = baseMinDamage[i] * damageMultiplier;
            damageArea.MaxDamageCaused = baseMaxDamage[i] * damageMultiplier;
            damageArea.DamageCausedKnockbackType = knockbackForce > 0f
                ? DamageOnTouch.KnockbackStyles.AddForce
                : DamageOnTouch.KnockbackStyles.NoKnockback;
            // PolygonCollider2D is treated as 3D by this engine version; supply an explicit XY direction on hit.
            damageArea.DamageCausedKnockbackDirection = DamageOnTouch.KnockbackDirections.BasedOnScriptDirection;
            damageArea.DamageCausedKnockbackForce = Vector3.right * knockbackForce;
            // Keep hit immunity shorter than the combo interval so the next slash can connect.
            damageArea.InvincibilityDuration = Mathf.Clamp(attackHitInvincibilityDuration,
                0f, Mathf.Max(0f, attackCooldown - Time.fixedDeltaTime));
        }
    }

    private void OnAttackHitDamageable(Health target)
    {
        if (GetCurrentKnockbackForce() <= 0f || target == null
            || target.KnockbackForceMultiplier <= 0f || !target.CanGetKnockback(null))
        {
            return;
        }

        TopDownController2D controller = target.GetComponentInParent<TopDownController2D>();
        if (controller == null || !controller.isActiveAndEnabled) return;

        Vector3 direction = controller.transform.position - transform.position;
        direction.z = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = anim != null
                ? new Vector3(anim.GetFloat("moveX"), anim.GetFloat("moveY"), 0f)
                : Vector3.zero;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = transform.localScale.x < 0f ? Vector3.left : Vector3.right;
            }
        }
        foreach (DamageOnTouch damageArea in attackDamageAreas)
        {
            if (damageArea != null)
            {
                damageArea.SetKnockbackScriptDirection(direction.normalized);
            }
        }

        SecondHitKnockback2D knockback = controller.GetComponent<SecondHitKnockback2D>();
        if (knockback == null)
        {
            knockback = controller.gameObject.AddComponent<SecondHitKnockback2D>();
        }
        float duration = comboStep == 3 ? thirdHitKnockbackDuration : secondHitKnockbackDuration;
        knockback.Begin(duration);
    }

    private IEnumerator EnableAttackAreaTemporarily()
    {
        ConfigureAttackKnockback();
        attackArea.SetActive(true);
        yield return new WaitForSeconds(attackAreaActiveTime);
        attackArea.SetActive(false);
        attackAreaCoroutine = null;
    }


    //rb.MovePosition(rb.position + movement*moveSpeed*Time.fixedDeltaTime);
}


