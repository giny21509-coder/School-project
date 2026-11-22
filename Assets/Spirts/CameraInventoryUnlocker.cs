using UnityEngine;

public class CameraInventoryUnlocker : MonoBehaviour
{
    [Header("Camera Item")]
    [SerializeField] private string cameraItemId = "Camera_01";
    [SerializeField] private string cameraItemName = "相機";

    [Header("Check Settings")]
    [SerializeField] private bool checkOnStart = true;
    [SerializeField] private bool keepCheckingUntilUnlocked = true;

    private bool unlocked;

    private void Start()
    {
        if (checkOnStart)
        {
            TryUnlockCameraFromInventory();
        }
    }

    private void Update()
    {
        if (!keepCheckingUntilUnlocked || unlocked)
        {
            return;
        }

        TryUnlockCameraFromInventory();
    }

    public void TryUnlockCameraFromInventory()
    {
        if (InventoryManager.Instance == null || CameraSystem.Instance == null)
        {
            return;
        }

        if (!InventoryHasCamera())
        {
            return;
        }

        CameraSystem.Instance.UnlockCamera();
        unlocked = true;
    }

    private bool InventoryHasCamera()
    {
        return SlotsHaveCamera(InventoryManager.Instance.backpackSlots)
            || SlotsHaveCamera(InventoryManager.Instance.questSlots)
            || SlotsHaveCamera(InventoryManager.Instance.collectSlots);
    }

    private bool SlotsHaveCamera(ItemData[] slots)
    {
        if (slots == null)
        {
            return false;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            ItemData item = slots[i];
            if (item == null)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(cameraItemId) && item.itemId == cameraItemId)
            {
                return true;
            }

            if (!string.IsNullOrEmpty(cameraItemName) && item.itemName == cameraItemName)
            {
                return true;
            }
        }

        return false;
    }
}
