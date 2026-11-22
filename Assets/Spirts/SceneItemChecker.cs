using UnityEngine;

public class SceneItemChecker : MonoBehaviour
{
    [Header("這個物件對應的開啟 ID")]
    [SerializeField] private string unlockId;

    [Header("要控制開關的物件，跨場景時把真正物品拖到這裡")]
    [SerializeField] private GameObject[] objectsToControl;

    [Header("設定")]
    [SerializeField] private bool hideWhenLocked = true;

    private void Start()
    {
        RefreshState();
    }

    public void RefreshState()
    {
        if (!hideWhenLocked)
        {
            return;
        }

        bool isUnlocked = GameDataManager.IsItemUnlocked(unlockId);

        if (objectsToControl == null || objectsToControl.Length == 0)
        {
            gameObject.SetActive(isUnlocked);
            return;
        }

        for (int i = 0; i < objectsToControl.Length; i++)
        {
            if (objectsToControl[i] != null)
            {
                objectsToControl[i].SetActive(isUnlocked);
            }
        }
    }
}
