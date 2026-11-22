using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SaveLoadMenuUI : MonoBehaviour
{
    [Header("Menu")]
    [SerializeField] private GameObject menuRoot;
    [SerializeField] private KeyCode toggleKey = KeyCode.Escape;

    [Header("Load List")]
    [SerializeField] private Transform slotParent;
    [SerializeField] private SaveSlotButton slotButtonPrefab;
    [SerializeField] private Text emptyText;

    private readonly List<GameObject> spawnedSlots = new List<GameObject>();

    private void Start()
    {
        CloseMenu();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (menuRoot != null && menuRoot.activeSelf)
            {
                CloseMenu();
            }
            else
            {
                OpenMenu();
            }
        }
    }

    public void OpenMenu()
    {
        if (menuRoot != null)
        {
            menuRoot.SetActive(true);
        }

        RefreshSaveList();
        Time.timeScale = 0f;
    }

    public void CloseMenu()
    {
        if (menuRoot != null)
        {
            menuRoot.SetActive(false);
        }

        Time.timeScale = 1f;
    }

    public void RefreshSaveList()
    {
        ClearSlots();

        if (SaveManager.Instance == null || slotParent == null || slotButtonPrefab == null)
        {
            SetEmptyText(true);
            return;
        }

        List<GameSaveData> saves = SaveManager.Instance.GetAllSaves();
        SetEmptyText(saves.Count == 0);

        for (int i = 0; i < saves.Count; i++)
        {
            SaveSlotButton slot = Instantiate(slotButtonPrefab, slotParent);
            slot.Setup(saves[i]);
            spawnedSlots.Add(slot.gameObject);
        }
    }

    private void ClearSlots()
    {
        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            if (spawnedSlots[i] != null)
            {
                Destroy(spawnedSlots[i]);
            }
        }

        spawnedSlots.Clear();
    }

    private void SetEmptyText(bool show)
    {
        if (emptyText != null)
        {
            emptyText.gameObject.SetActive(show);
        }
    }
}
