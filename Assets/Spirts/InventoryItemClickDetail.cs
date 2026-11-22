using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryItemClickDetail : MonoBehaviour, IPointerClickHandler
{
    [Header("Detail Panel")]
    [SerializeField] private InventoryItemDetailPanel detailPanel;

    [Header("Manual Item Detail")]
    [SerializeField] private string itemName;
    [TextArea(2, 5)]
    [SerializeField] private string useDescription;
    [TextArea(2, 6)]
    [SerializeField] private string itemInfo;
    [SerializeField] private Sprite itemPhoto;

    [Header("Optional Data Source")]
    [SerializeField] private UnityEngine.Object itemDataObject;
    [SerializeField] private bool readFromItemDataObject = true;
    [SerializeField] private Image slotIconImage;
    [SerializeField] private bool useSlotIconWhenPhotoMissing = true;

    [Header("Click Settings")]
    [SerializeField] private bool onlyLeftClick = true;
    [SerializeField] private bool ignoreEmptyItem = true;

    public UnityEvent onSelected;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (onlyLeftClick && eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        Select();
    }

    public void Select()
    {
        if (detailPanel == null)
        {
            detailPanel = FindDetailPanelForThisPage();
        }

        if (detailPanel == null)
        {
            Debug.LogWarning("InventoryItemClickDetail needs an InventoryItemDetailPanel reference.", this);
            return;
        }

        ItemDetail detail = BuildDetail();
        if (ignoreEmptyItem && detail.IsEmpty)
        {
            detailPanel.Clear();
            return;
        }

        detailPanel.ShowItem(detail.Name, detail.UseDescription, detail.Info, detail.Photo);
        onSelected?.Invoke();
    }

    public void SetManualDetail(string newItemName, string newUseDescription, Sprite newItemPhoto)
    {
        SetManualDetail(newItemName, newUseDescription, "", newItemPhoto);
    }

    public void SetManualDetail(string newItemName, string newUseDescription, string newItemInfo, Sprite newItemPhoto)
    {
        itemName = newItemName;
        useDescription = newUseDescription;
        itemInfo = newItemInfo;
        itemPhoto = newItemPhoto;
    }

    public void SetItemDataObject(UnityEngine.Object newItemDataObject)
    {
        itemDataObject = newItemDataObject;
    }

    public void ClearItem()
    {
        itemName = "";
        useDescription = "";
        itemInfo = "";
        itemPhoto = null;
        itemDataObject = null;
    }

    private InventoryItemDetailPanel FindDetailPanelForThisPage()
    {
        // Start near the slot so a sibling panel on this page wins.
        Transform current = transform;
        while (current != null)
        {
            InventoryItemDetailPanel[] panels = current.GetComponentsInChildren<InventoryItemDetailPanel>(true);
            if (panels.Length == 1)
            {
                return panels[0];
            }

            // Multiple panels mean we have reached a container of different pages.
            if (panels.Length > 1)
            {
                return null;
            }

            current = current.parent;
        }

        // Keep older scenes with one separate detail panel working.
        InventoryItemDetailPanel[] scenePanels = FindObjectsByType<InventoryItemDetailPanel>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        return scenePanels.Length == 1 ? scenePanels[0] : null;
    }

    private ItemDetail BuildDetail()
    {
        string resolvedName = itemName;
        string resolvedDescription = useDescription;
        string resolvedInfo = itemInfo;
        Sprite resolvedPhoto = itemPhoto;

        if (readFromItemDataObject && itemDataObject != null)
        {
            resolvedName = ReadString(itemDataObject, resolvedName,
                "itemName", "displayName", "title", "name");
            resolvedDescription = ReadString(itemDataObject, resolvedDescription,
                "useDescription", "itemUseDescription", "useText");
            resolvedInfo = ReadString(itemDataObject, resolvedInfo,
                "itemInfo", "itemInformation", "information", "info", "detail", "detailText", "flavorText", "storyText", "clueText", "description", "itemDescription");
            resolvedPhoto = ReadSprite(itemDataObject, resolvedPhoto,
                "itemPhoto", "previewSprite", "itemImage", "itemIcon", "icon", "sprite");
        }

        if (resolvedPhoto == null && useSlotIconWhenPhotoMissing && slotIconImage != null)
        {
            resolvedPhoto = slotIconImage.sprite;
        }

        return new ItemDetail(resolvedName, resolvedDescription, resolvedInfo, resolvedPhoto);
    }

    private string ReadString(UnityEngine.Object source, string fallback, params string[] memberNames)
    {
        object value = ReadMember(source, memberNames);
        return value is string text && !string.IsNullOrWhiteSpace(text) ? text : fallback;
    }

    private Sprite ReadSprite(UnityEngine.Object source, Sprite fallback, params string[] memberNames)
    {
        object value = ReadMember(source, memberNames);
        return value as Sprite ?? fallback;
    }

    private object ReadMember(UnityEngine.Object source, params string[] memberNames)
    {
        Type type = source.GetType();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        for (int i = 0; i < memberNames.Length; i++)
        {
            FieldInfo field = type.GetField(memberNames[i], flags);
            if (field != null)
            {
                return field.GetValue(source);
            }

            PropertyInfo property = type.GetProperty(memberNames[i], flags);
            if (property != null && property.GetIndexParameters().Length == 0)
            {
                try
                {
                    return property.GetValue(source);
                }
                catch
                {
                    return null;
                }
            }
        }

        return null;
    }

    private struct ItemDetail
    {
        public ItemDetail(string name, string useDescription, string info, Sprite photo)
        {
            Name = name;
            UseDescription = useDescription;
            Info = info;
            Photo = photo;
        }

        public string Name { get; }
        public string UseDescription { get; }
        public string Info { get; }
        public Sprite Photo { get; }

        public bool IsEmpty
        {
            get
            {
                return string.IsNullOrWhiteSpace(Name)
                    && string.IsNullOrWhiteSpace(UseDescription)
                    && string.IsNullOrWhiteSpace(Info)
                    && Photo == null;
            }
        }
    }
}
