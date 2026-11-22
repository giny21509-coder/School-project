using UnityEngine;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerBoundary : MonoBehaviour
{
    public float minY = -4f;
    public float maxY = 4f;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (rb == null)
        {
            return;
        }

        Vector2 pos = rb.position;
        float clampedY = Mathf.Clamp(pos.y, minY, maxY);

        if (Mathf.Approximately(pos.y, clampedY))
        {
            return;
        }

        pos.y = clampedY;
        rb.MovePosition(pos);
    }
}
