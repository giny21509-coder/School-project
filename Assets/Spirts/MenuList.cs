using UnityEngine;
using UnityEngine.UI;

public class MenuList : MonoBehaviour
{
    [Header("選單設定")]
    public GameObject menuList;
    public GameObject escMenu;
    public GameObject settingsMenu;
    public GameObject[] objectsHiddenInSettings;
    [SerializeField] private bool menuKey = true;

    [Header("按鈕設定")]
    public Button settingsButton;
    public Button settingsBackButton;
    public Button resumeButton;

    [Header("背包設定")]
    public GameObject inventorUI;
    [SerializeField] private bool inventoryKey = true;

    void Awake()
    {
        AutoFindMissingReferences();
        AddButtonListeners();
    }

    void Start()
    {
        ShowEscMenu();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (menuKey)
            {
                OpenMenu();
            }
            else if (settingsMenu != null && settingsMenu.activeSelf)
            {
                ShowEscMenu();
            }
            else
            {
                CloseMenu();
            }
        }

        if (inventoryKey)
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (inventorUI != null)
                {
                    inventorUI.SetActive(true);
                }

                inventoryKey = false;
                RefreshPauseState();
            }
        }
        else if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (inventorUI != null)
            {
                inventorUI.SetActive(false);
            }

            inventoryKey = true;
            RefreshPauseState();
        }
    }

    private void RefreshPauseState()
    {
        Time.timeScale = (!menuKey || !inventoryKey) ? 0f : 1f;
    }

    void OnDestroy()
    {
        RemoveButtonListeners();
    }

    public void OpenMenu()
    {
        if (menuList != null)
        {
            menuList.SetActive(true);
        }

        ShowEscMenu();
        menuKey = false;
        RefreshPauseState();
    }

    public void CloseMenu()
    {
        if (menuList != null)
        {
            menuList.SetActive(false);
        }

        menuKey = true;
        RefreshPauseState();
    }

    public void ShowSettings()
    {
        if (menuList != null)
        {
            menuList.SetActive(true);
        }

        SetObjectsHiddenInSettings(false);

        if (escMenu != null && !IsChildOf(settingsMenu, escMenu))
        {
            escMenu.SetActive(false);
        }

        if (settingsMenu != null)
        {
            settingsMenu.SetActive(true);
        }
        else
        {
            Debug.LogWarning("MenuList: settingsMenu 沒有指定，所以設置介面打不開。請把 設置介面 拖到 settingsMenu 欄位。");
        }

        menuKey = false;
        RefreshPauseState();
    }

    public void ShowEscMenu()
    {
        if (escMenu != null)
        {
            escMenu.SetActive(true);
        }

        SetObjectsHiddenInSettings(true);

        if (settingsMenu != null)
        {
            settingsMenu.SetActive(false);
        }
    }

    private void AddButtonListeners()
    {
        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(ShowSettings);
            settingsButton.onClick.AddListener(ShowSettings);
        }

        if (settingsBackButton != null)
        {
            settingsBackButton.onClick.RemoveListener(ShowEscMenu);
            settingsBackButton.onClick.AddListener(ShowEscMenu);
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(CloseMenu);
            resumeButton.onClick.AddListener(CloseMenu);
        }
    }

    private void RemoveButtonListeners()
    {
        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(ShowSettings);
        }

        if (settingsBackButton != null)
        {
            settingsBackButton.onClick.RemoveListener(ShowEscMenu);
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(CloseMenu);
        }
    }

    private void AutoFindMissingReferences()
    {
        if (menuList == null)
        {
            Transform esc = FindChildRecursive(transform.root, "ESC");
            if (esc != null)
            {
                menuList = esc.gameObject;
            }
        }

        if (escMenu == null && menuList != null)
        {
            Transform back = FindChildRecursive(menuList.transform, "Back");
            if (back != null)
            {
                escMenu = back.gameObject;
            }
        }

        if (settingsMenu == null && menuList != null)
        {
            Transform settings = FindChildRecursive(menuList.transform, "設置介面");
            if (settings != null)
            {
                settingsMenu = settings.gameObject;
            }
        }

        if (settingsButton == null && escMenu != null)
        {
            Transform settings = FindChildRecursive(escMenu.transform, "設置");
            if (settings != null)
            {
                settingsButton = settings.GetComponent<Button>();
            }
        }

        if (settingsBackButton == null && settingsMenu != null)
        {
            settingsBackButton = settingsMenu.GetComponentInChildren<Button>(true);
        }

        if (resumeButton == null && escMenu != null)
        {
            Transform resume = FindChildRecursive(escMenu.transform, "返回遊戲");
            if (resume != null)
            {
                resumeButton = resume.GetComponent<Button>();
            }
        }
    }

    private void SetObjectsHiddenInSettings(bool show)
    {
        if (objectsHiddenInSettings == null)
        {
            return;
        }

        for (int i = 0; i < objectsHiddenInSettings.Length; i++)
        {
            if (objectsHiddenInSettings[i] != null)
            {
                objectsHiddenInSettings[i].SetActive(show);
            }
        }
    }

    private bool IsChildOf(GameObject child, GameObject parent)
    {
        if (child == null || parent == null)
        {
            return false;
        }

        return child.transform.IsChildOf(parent.transform);
    }

    private Transform FindChildRecursive(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }

            Transform found = FindChildRecursive(child, childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}