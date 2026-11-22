using System.Collections.Generic;
using UnityEngine;

public class GameDataManager : MonoBehaviour
{
    private static readonly HashSet<string> unlockedItemIds = new HashSet<string>();

    // Keep old scenes working when unlockId is left empty.
    public static bool isItemUnlocked = false;

    public static void UnlockItem(string unlockId)
    {
        if (string.IsNullOrEmpty(unlockId))
        {
            isItemUnlocked = true;
            return;
        }

        unlockedItemIds.Add(unlockId);
        isItemUnlocked = true;
    }

    public static bool IsItemUnlocked(string unlockId)
    {
        if (string.IsNullOrEmpty(unlockId))
        {
            return isItemUnlocked;
        }

        return unlockedItemIds.Contains(unlockId);
    }

    public static List<string> GetUnlockedItemIds()
    {
        return new List<string>(unlockedItemIds);
    }

    public static void LoadUnlockedItemIds(List<string> unlockIds)
    {
        unlockedItemIds.Clear();
        isItemUnlocked = false;

        if (unlockIds == null)
        {
            return;
        }

        for (int i = 0; i < unlockIds.Count; i++)
        {
            if (!string.IsNullOrEmpty(unlockIds[i]))
            {
                unlockedItemIds.Add(unlockIds[i]);
            }
        }

        isItemUnlocked = unlockedItemIds.Count > 0;
    }
}
