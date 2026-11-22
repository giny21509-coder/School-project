using UnityEngine;

public enum ItemPageType
{
    Collect,
    Backpack,
    Quest
}

[CreateAssetMenu(fileName = "New Item",
    menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    [Header("存檔用 ID，請每個道具都填不同值")]
    public string itemId;

    public string itemName;
    public Sprite itemIcon;
    public Sprite itemPhoto;

    [Header("這個道具要放在哪一頁")]
    public ItemPageType pageType = ItemPageType.Backpack;

    [Header("物品說明")]
    [TextArea(2, 4)]
    public string useDescription;

    [TextArea(3, 8)]
    public string itemInfo;

    [TextArea]
    public string description;
}
