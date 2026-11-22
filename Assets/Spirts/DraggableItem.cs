using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Image image;

    public InventorySlot SourceSlot { get; private set; }

    private Transform originalParent;
    private Vector3 originalPosition;
    private Canvas rootCanvas;

    private void Awake()
    {
        if (image == null)
        {
            image = GetComponent<Image>();
        }

        rootCanvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        SourceSlot = GetComponentInParent<InventorySlot>();
        if (SourceSlot == null || !SourceSlot.HasItem)
        {
            eventData.pointerDrag = null;
            return;
        }

        originalParent = transform.parent;
        originalPosition = transform.position;

        if (rootCanvas == null)
        {
            rootCanvas = GetComponentInParent<Canvas>();
        }

        transform.SetParent(rootCanvas != null ? rootCanvas.transform : transform.root);
        transform.SetAsLastSibling();

        if (image != null)
        {
            image.raycastTarget = false;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (SourceSlot == null)
        {
            return;
        }

        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (originalParent != null)
        {
            transform.SetParent(originalParent);
        }

        transform.position = originalPosition;

        if (image != null)
        {
            image.raycastTarget = true;
        }

        SourceSlot = null;
    }
}
