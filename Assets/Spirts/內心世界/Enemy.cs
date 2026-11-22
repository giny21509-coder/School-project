using UnityEngine;

public class Enemy : MonoBehaviour
{

    [Header("巡邏點")]
    public Transform pointA;
    public Transform pointB;

    [Header("玩家")]
    public Transform player;

    [Header("移動設定")]
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4f;
    public float stopDistance = 0.6f;

    [Header("視野設定")]
    public float viewDistance = 6f;
    public LayerMask obstacleLayer;

    [Header("受擊擊退")]
    public float knockbackForce = 8f;
    public float knockbackTime = 0.2f;

    private bool isKnockback;
    private float knockbackTimer;

    private Rigidbody2D rb;
    private Transform currentTarget;

    private Animator anim;
    private Collider2D enemyCollider;
    private Chatacher chatacher;
    private bool isDead;



    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        enemyCollider = GetComponent<Collider2D>();
        chatacher = GetComponent<Chatacher>();

        if (chatacher != null)
        {
            chatacher.OnDie.AddListener(EnemyDead);
        }
    }

    private void Start()
    {
        currentTarget = pointB;
    }

    private void FixedUpdate()
    {
        if (isDead)
        {
            return;
        }
        if (isKnockback)
        {
            knockbackTimer -= Time.fixedDeltaTime;

            if (knockbackTimer <= 0)
            {
                isKnockback = false;
            }

            return;
        }
        if (CanSeePlayer())
        {
            ChasePlayer();
        }
        else
        {
            Patrol();
        }
    }

    private void Patrol()
    {
        MoveTo(currentTarget.position, patrolSpeed);

        if (Vector2.Distance(transform.position, currentTarget.position) < 0.2f)
        {
            currentTarget = currentTarget == pointA ? pointB : pointA;
        }
    }

    private void ChasePlayer()
    {
        if (Vector2.Distance(transform.position, player.position) <= stopDistance)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        MoveTo(player.position, chaseSpeed);
    }

    private void MoveTo(Vector2 targetPosition, float speed)
    {
        Vector2 direction = (targetPosition - (Vector2)transform.position).normalized;
        rb.linearVelocity = direction * speed;

        // 翻面，可依你的美術方向調整
        if (direction.x > 0)
            transform.localScale = new Vector3(1, 1, 1);
        else if (direction.x < 0)
            transform.localScale = new Vector3(-1, 1, 1);
    }

    private bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector2 direction = player.position - transform.position;
        float distance = direction.magnitude;

        if (distance > viewDistance)
            return false;

        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            direction.normalized,
            distance,
            obstacleLayer
        );

        // 中間沒有牆壁，就代表看得到玩家
        return hit.collider == null;
    }
    public void TakeHit(Transform attacker)
    {
        isKnockback = true;
        knockbackTimer = knockbackTime;

        Vector2 direction = ((Vector2)transform.position - (Vector2)attacker.position).normalized;
        rb.linearVelocity = direction * knockbackForce;
    }
    public void EnemyDead()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        isKnockback = false;
        rb.linearVelocity = Vector2.zero;

        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }

        if (anim != null)
        {
            anim.SetBool("isDead", true);
        }

        Destroy(gameObject, 1.2f);
    }
}
