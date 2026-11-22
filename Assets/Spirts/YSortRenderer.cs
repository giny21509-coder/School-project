using UnityEngine;

public class YSortRenderer : MonoBehaviour
{
    [SerializeField] private int offset = 0;
    [SerializeField] private int precision = 100;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        spriteRenderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * precision) + offset;
    }
}
