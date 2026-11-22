using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryItemDetailPanel : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private bool hideWhenEmpty = true;
    [SerializeField] private Button closeButton;

    [Header("UI References")]
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text useDescriptionText;
    [SerializeField] private TMP_Text itemInfoText;
    [SerializeField] private Image itemPhotoImage;

    [Header("Empty State")]
    [SerializeField] private string emptyName = "";
    [SerializeField] private string emptyDescription = "";
    [SerializeField] private string emptyInfo = "";
    [SerializeField] private Sprite emptyPhoto;

    private bool initialized;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Hide);
        }

        Clear();
    }

    private void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Hide);
        }
    }

    public void ShowItem(string itemName, string useDescription, Sprite itemPhoto)
    {
        ShowItem(itemName, useDescription, "", itemPhoto);
    }

    public void ShowItem(string itemName, string useDescription, string itemInfo, Sprite itemPhoto)
    {
        Initialize();

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        SetText(itemNameText, itemName);
        SetText(useDescriptionText, useDescription);
        SetText(itemInfoText, itemInfo);
        SetPhoto(itemPhoto);
    }

    public void Clear()
    {
        SetText(itemNameText, emptyName);
        SetText(useDescriptionText, emptyDescription);
        SetText(itemInfoText, emptyInfo);
        SetPhoto(emptyPhoto);

        if (hideWhenEmpty && panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    public void Hide()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    private void SetText(TMP_Text targetText, string value)
    {
        if (targetText == null)
        {
            return;
        }

        targetText.text = value ?? "";
    }

    private void SetPhoto(Sprite photo)
    {
        if (itemPhotoImage == null)
        {
            return;
        }

        itemPhotoImage.sprite = photo;
        itemPhotoImage.enabled = photo != null;
    }
}
