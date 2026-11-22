using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    public static event Action<ItemData, bool> ItemAdded;

    private static ItemData[] savedCollectSlots;
    private static ItemData[] savedBackpackSlots;
    private static ItemData[] savedQuestSlots;

    [Header("資料層 (Data)")]
    public ItemData[] collectSlots = new ItemData[8];
    public ItemData[] backpackSlots = new ItemData[8];
    public ItemData[] questSlots = new ItemData[8];

    [Header("介面層 (UI)")]
    public InventorySlot[] collectUiSlots = new InventorySlot[8];
    public InventorySlot[] backpackUiSlots = new InventorySlot[8];
    public InventorySlot[] questUiSlots = new InventorySlot[8];
    [SerializeField] private bool autoFindMissingUiSlots = true;

    [Header("目前顯示頁面")]
    public ItemPageType currentPage = ItemPageType.Backpack;

    private ItemData[] CurrentSlots
    {
        get { return GetSlotsByPage(currentPage); }
    }

    private InventorySlot[] CurrentUiSlots
    {
        get { return GetUiSlotsByPage(currentPage); }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            RestoreInventoryFromMemory();
            AutoFindMissingUiSlots();
            PrepareSlots();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static void ResetSession()
    {
        savedCollectSlots = null;
        savedBackpackSlots = null;
        savedQuestSlots = null;
        if (Instance != null) Instance.ClearInventory();
    }

    private void Start()
    {
        AutoFindMissingUiSlots();
        PrepareSlots();
        RefreshAllSlots();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SaveInventoryToMemory();
            Instance = null;
        }
    }

    public bool AddItem(ItemData itemToAdd)
    {
        if (itemToAdd == null)
        {
            return false;
        }

        ItemData[] targetSlots = GetSlotsByPage(itemToAdd.pageType);
        // CGs are unique assets; collecting the same CG again must not fill the page.
        bool alreadyOwned = Array.Exists(targetSlots, existing => existing != null
            && (existing == itemToAdd || (!string.IsNullOrEmpty(itemToAdd.itemId)
                && existing.itemId == itemToAdd.itemId)));
        if (itemToAdd.pageType == ItemPageType.Collect && alreadyOwned)
        {
            ItemAdded?.Invoke(itemToAdd, false);
            return true;
        }
        int emptyIndex = FindEmptySlotIndex(targetSlots);

        if (emptyIndex == -1)
        {
            Debug.Log(GetPageName(itemToAdd.pageType) + "已經滿了！");
            return false;
        }

        bool firstAcquisition = !Array.Exists(targetSlots, existing => existing != null
            && (existing == itemToAdd || (!string.IsNullOrEmpty(itemToAdd.itemId)
                && existing.itemId == itemToAdd.itemId)));
        targetSlots[emptyIndex] = itemToAdd;
        Debug.Log("成功放入" + GetPageName(itemToAdd.pageType) + "：" + itemToAdd.itemName);

        SaveInventoryToMemory();

        if (itemToAdd.pageType == currentPage)
        {
            RefreshAllSlots();
        }

        ItemAdded?.Invoke(itemToAdd, firstAcquisition);
        return true;
    }

    public ItemData GetItemAt(int index)
    {
        if (!IsValidSlotIndex(index))
        {
            return null;
        }

        return CurrentSlots[index];
    }

    public void SwapItems(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex)
        {
            RefreshAllSlots();
            return;
        }

        if (!IsValidSlotIndex(fromIndex) || !IsValidSlotIndex(toIndex))
        {
            return;
        }

        ItemData item = CurrentSlots[fromIndex];
        CurrentSlots[fromIndex] = CurrentSlots[toIndex];
        CurrentSlots[toIndex] = item;

        SaveInventoryToMemory();
        RefreshAllSlots();
    }

    public void ShowCollectPage()
    {
        ShowPage(ItemPageType.Collect);
    }

    public void ShowBackpackPage()
    {
        ShowPage(ItemPageType.Backpack);
    }

    public void ShowQuestPage()
    {
        ShowPage(ItemPageType.Quest);
    }

    public void ShowPageByIndex(int pageIndex)
    {
        if (pageIndex < 0 || pageIndex > 2)
        {
            return;
        }

        ShowPage((ItemPageType)pageIndex);
    }

    public void ShowPage(ItemPageType pageType)
    {
        currentPage = pageType;
        RefreshAllSlots();
    }

    public List<string> GetInventoryItemIds()
    {
        return GetBackpackItemIds();
    }

    public List<string> GetCollectItemIds()
    {
        return GetItemIdsByPage(ItemPageType.Collect);
    }

    public List<string> GetBackpackItemIds()
    {
        return GetItemIdsByPage(ItemPageType.Backpack);
    }

    public List<string> GetQuestItemIds()
    {
        return GetItemIdsByPage(ItemPageType.Quest);
    }

    public void LoadInventoryFromItemIds(List<string> itemIds, ItemDatabase itemDatabase)
    {
        ClearInventoryWithoutRefresh();

        if (itemIds == null || itemDatabase == null)
        {
            RefreshAllSlots();
            return;
        }

        for (int i = 0; i < itemIds.Count; i++)
        {
            if (string.IsNullOrEmpty(itemIds[i]))
            {
                continue;
            }

            ItemData item = itemDatabase.GetItemById(itemIds[i]);
            AddLoadedItemToOwnPage(item);
        }

        SaveInventoryToMemory();
        RefreshAllSlots();
    }

    public void LoadInventoryFromPageItemIds(
        List<string> collectItemIds,
        List<string> backpackItemIds,
        List<string> questItemIds,
        ItemDatabase itemDatabase)
    {
        ClearInventoryWithoutRefresh();

        if (itemDatabase == null)
        {
            RefreshAllSlots();
            return;
        }

        LoadPageSlots(collectSlots, collectItemIds, itemDatabase);
        LoadPageSlots(backpackSlots, backpackItemIds, itemDatabase);
        LoadPageSlots(questSlots, questItemIds, itemDatabase);

        SaveInventoryToMemory();
        RefreshAllSlots();
    }

    public void ClearInventory()
    {
        ClearInventoryWithoutRefresh();
        SaveInventoryToMemory();
        RefreshAllSlots();
    }

    private ItemData[] GetSlotsByPage(ItemPageType pageType)
    {
        switch (pageType)
        {
            case ItemPageType.Collect:
                return collectSlots;

            case ItemPageType.Quest:
                return questSlots;

            default:
                return backpackSlots;
        }
    }

    private InventorySlot[] GetUiSlotsByPage(ItemPageType pageType)
    {
        switch (pageType)
        {
            case ItemPageType.Collect:
                return collectUiSlots;

            case ItemPageType.Quest:
                return questUiSlots;

            default:
                return backpackUiSlots;
        }
    }

    private List<string> GetItemIdsByPage(ItemPageType pageType)
    {
        ItemData[] slots = GetSlotsByPage(pageType);
        List<string> itemIds = new List<string>();

        for (int i = 0; i < slots.Length; i++)
        {
            ItemData item = slots[i];
            itemIds.Add(item == null ? string.Empty : item.itemId);
        }

        return itemIds;
    }

    private void AddLoadedItemToOwnPage(ItemData item)
    {
        if (item == null)
        {
            return;
        }

        ItemData[] slots = GetSlotsByPage(item.pageType);
        int emptyIndex = FindEmptySlotIndex(slots);

        if (emptyIndex != -1)
        {
            slots[emptyIndex] = item;
        }
    }

    private void LoadPageSlots(ItemData[] slots, List<string> itemIds, ItemDatabase itemDatabase)
    {
        if (slots == null || itemIds == null)
        {
            return;
        }

        int count = Mathf.Min(slots.Length, itemIds.Count);
        for (int i = 0; i < count; i++)
        {
            if (string.IsNullOrEmpty(itemIds[i]))
            {
                continue;
            }

            slots[i] = itemDatabase.GetItemById(itemIds[i]);
        }
    }

    private int FindEmptySlotIndex(ItemData[] slots)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                return i;
            }
        }

        return -1;
    }

    private void ClearInventoryWithoutRefresh()
    {
        ClearSlots(collectSlots);
        ClearSlots(backpackSlots);
        ClearSlots(questSlots);
    }

    private void ClearSlots(ItemData[] slots)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] = null;
        }
    }

    private void RestoreInventoryFromMemory()
    {
        RestoreSlotsFromMemory(collectSlots, savedCollectSlots);
        RestoreSlotsFromMemory(backpackSlots, savedBackpackSlots);
        RestoreSlotsFromMemory(questSlots, savedQuestSlots);
    }

    private void RestoreSlotsFromMemory(ItemData[] slots, ItemData[] savedSlots)
    {
        if (slots == null || savedSlots == null)
        {
            return;
        }

        int count = Mathf.Min(slots.Length, savedSlots.Length);
        for (int i = 0; i < count; i++)
        {
            slots[i] = savedSlots[i];
        }
    }

    private void SaveInventoryToMemory()
    {
        savedCollectSlots = CopySlots(collectSlots);
        savedBackpackSlots = CopySlots(backpackSlots);
        savedQuestSlots = CopySlots(questSlots);
    }

    private ItemData[] CopySlots(ItemData[] slots)
    {
        if (slots == null)
        {
            return null;
        }

        ItemData[] copiedSlots = new ItemData[slots.Length];
        for (int i = 0; i < slots.Length; i++)
        {
            copiedSlots[i] = slots[i];
        }

        return copiedSlots;
    }

    private void RefreshAllSlots()
    {
        InventorySlot[] currentUiSlots = CurrentUiSlots;
        for (int i = 0; i < currentUiSlots.Length; i++)
        {
            if (currentUiSlots[i] != null)
            {
                ItemData item = i < CurrentSlots.Length ? CurrentSlots[i] : null;
                currentUiSlots[i].UpdateSlot(item);
            }
        }
    }

    private void PrepareSlots()
    {
        SetSlotIndexes(collectUiSlots);
        SetSlotIndexes(backpackUiSlots);
        SetSlotIndexes(questUiSlots);
    }

    private void AutoFindMissingUiSlots()
    {
        if (!autoFindMissingUiSlots)
        {
            return;
        }

        InventorySlot[] sceneSlots = FindObjectsOfType<InventorySlot>(true);

        if (!HasAssignedSlot(collectUiSlots))
        {
            InventorySlot[] foundCollectSlots = FindSlotsForPage(sceneSlots, ItemPageType.Collect);
            if (foundCollectSlots.Length > 0)
            {
                collectUiSlots = foundCollectSlots;
            }
        }

        if (!HasAssignedSlot(backpackUiSlots))
        {
            InventorySlot[] foundBackpackSlots = FindSlotsForPage(sceneSlots, ItemPageType.Backpack);
            if (foundBackpackSlots.Length > 0)
            {
                backpackUiSlots = foundBackpackSlots;
            }
        }

        if (!HasAssignedSlot(questUiSlots))
        {
            InventorySlot[] foundQuestSlots = FindSlotsForPage(sceneSlots, ItemPageType.Quest);
            if (foundQuestSlots.Length > 0)
            {
                questUiSlots = foundQuestSlots;
            }
        }
    }

    private bool HasAssignedSlot(InventorySlot[] slots)
    {
        if (slots == null)
        {
            return false;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    private InventorySlot[] FindSlotsForPage(InventorySlot[] sceneSlots, ItemPageType pageType)
    {
        List<InventorySlot> foundSlots = new List<InventorySlot>();

        for (int i = 0; i < sceneSlots.Length; i++)
        {
            InventorySlot slot = sceneSlots[i];
            if (slot == null || !slot.gameObject.scene.IsValid())
            {
                continue;
            }

            if (IsSlotUnderPage(slot.transform, pageType))
            {
                foundSlots.Add(slot);
            }
        }

        foundSlots.Sort(CompareSlotHierarchyOrder);
        return foundSlots.ToArray();
    }

    private bool IsSlotUnderPage(Transform slotTransform, ItemPageType pageType)
    {
        Transform current = slotTransform;
        while (current != null)
        {
            if (IsPageObjectName(current.name, pageType))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private bool IsPageObjectName(string objectName, ItemPageType pageType)
    {
        if (string.IsNullOrEmpty(objectName))
        {
            return false;
        }

        string lowerName = objectName.ToLowerInvariant();
        switch (pageType)
        {
            case ItemPageType.Collect:
                return objectName.Contains("收集")
                    || lowerName.Contains("collect")
                    || lowerName.Contains("collection");

            case ItemPageType.Quest:
                return objectName.Contains("委託")
                    || lowerName.Contains("quest");

            default:
                return objectName.Contains("背包")
                    || lowerName.Contains("backpack")
                    || lowerName.Contains("bag");
        }
    }

    private int CompareSlotHierarchyOrder(InventorySlot a, InventorySlot b)
    {
        return string.Compare(
            GetHierarchyOrderPath(a.transform),
            GetHierarchyOrderPath(b.transform),
            StringComparison.Ordinal);
    }

    private string GetHierarchyOrderPath(Transform transform)
    {
        string path = string.Empty;
        Transform current = transform;

        while (current != null)
        {
            path = current.GetSiblingIndex().ToString("D4") + "/" + path;
            current = current.parent;
        }

        return path;
    }

    private bool IsValidSlotIndex(int index)
    {
        return index >= 0 && index < CurrentSlots.Length;
    }

    private void SetSlotIndexes(InventorySlot[] slots)
    {
        if (slots == null)
        {
            return;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
            {
                slots[i].SetIndex(i);
            }
        }
    }

    private string GetPageName(ItemPageType pageType)
    {
        switch (pageType)
        {
            case ItemPageType.Collect:
                return "收集";

            case ItemPageType.Quest:
                return "委託單";

            default:
                return "背包";
        }
    }
}
