using UnityEngine;

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance { get; private set; }

    [Header("Drag every ItemData asset here")]
    public ItemData[] allItems;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateDatabase()
    {
        if (Instance != null) return;
        ItemDatabase prefab = Resources.Load<ItemDatabase>("ItemDatabase");
        if (prefab == null)
        {
            Debug.LogError("ItemDatabase resource prefab is missing. Saved inventory cannot be restored.");
            return;
        }
        Instantiate(prefab);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public ItemData GetItemById(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
        {
            return null;
        }

        if (allItems == null) return null;
        for (int i = 0; i < allItems.Length; i++)
        {
            ItemData item = allItems[i];
            if (item != null && item.itemId == itemId)
            {
                return item;
            }
        }

        Debug.LogWarning("Item ID not found: " + itemId);
        return null;
    }
}
