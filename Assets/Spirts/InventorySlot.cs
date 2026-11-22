using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour, IDropHandler
{
    public Image iconImage;
    public InventoryItemClickDetail clickDetail;

    [HideInInspector] public int slotIndex;

    public bool HasItem
    {
        get
        {
            return InventoryManager.Instance != null && InventoryManager.Instance.GetItemAt(slotIndex) != null;
        }
    }

    private void Awake()
    {
        if (iconImage == null)
        {
            iconImage = GetComponentInChildren<Image>();
        }

        if (clickDetail == null)
        {
            clickDetail = GetComponent<InventoryItemClickDetail>();
        }

        if (clickDetail == null)
        {
            clickDetail = gameObject.AddComponent<InventoryItemClickDetail>();
        }

        if (iconImage != null)
        {
            DraggableItem draggableItem = iconImage.GetComponent<DraggableItem>();
            if (draggableItem == null)
            {
                draggableItem = iconImage.gameObject.AddComponent<DraggableItem>();
            }

            draggableItem.image = iconImage;
        }
    }

    public void SetIndex(int index)
    {
        slotIndex = index;
    }

    public void UpdateSlot(ItemData itemData)
    {
        if (clickDetail != null)
        {
            if (itemData != null)
            {
                clickDetail.SetItemDataObject(itemData);
            }
            else
            {
                clickDetail.ClearItem();
            }
        }

        if (iconImage == null)
        {
            return;
        }

        if (itemData != null)
        {
            iconImage.sprite = itemData.itemIcon;
            iconImage.enabled = true;
            iconImage.raycastTarget = true;
        }
        else
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
            iconImage.raycastTarget = false;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
        {
            return;
        }

        DraggableItem draggableItem = eventData.pointerDrag.GetComponent<DraggableItem>();
        if (draggableItem == null || draggableItem.SourceSlot == null)
        {
            return;
        }

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.SwapItems(draggableItem.SourceSlot.slotIndex, slotIndex);
        }
    }
}
