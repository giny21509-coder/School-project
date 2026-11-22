using MoreMountains.TopDownEngine;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(TopDownController2D), typeof(Rigidbody2D))]
public sealed class SecondHitKnockback2D : MonoBehaviour
{
    private TopDownController2D controller;
    private Rigidbody2D body;
    private bool inProgress;
    private bool previousFreeMovement;
    private float endTime;

    private void Awake()
    {
        controller = GetComponent<TopDownController2D>();
        body = GetComponent<Rigidbody2D>();
    }

    public void Begin(float duration)
    {
        if (controller == null || body == null || duration <= 0f
            || body.bodyType != RigidbodyType2D.Dynamic)
        {
            return;
        }

        if (!inProgress)
        {
            previousFreeMovement = controller.FreeMovement;
        }

        // MovePosition overrides physics forces; let the native impact move the body briefly.
        controller.FreeMovement = false;
        body.linearVelocity = Vector2.zero;
        inProgress = true;
        endTime = Time.time + duration;
    }

    private void Update()
    {
        if (inProgress && Time.time >= endTime)
        {
            EndKnockback();
        }
    }

    private void OnDisable()
    {
        EndKnockback();
    }

    private void EndKnockback()
    {
        if (!inProgress) return;

        if (controller != null)
        {
            controller.FreeMovement = previousFreeMovement;
        }
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }
        inProgress = false;
    }
}
