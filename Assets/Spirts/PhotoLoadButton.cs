using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PhotoLoadButton : MonoBehaviour, IPointerClickHandler
{
    [Header("Save Point")]
    [SerializeField] private string savePointId = "office_save_point";

    [Header("Photo Wall UI")]
    [SerializeField] private Image photoImage;
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private TMP_Text saveInfoText;
    [SerializeField] private bool dimPhotoWhenMissing = true;

    private bool hasSave;

    private void Awake()
    {
        if (photoImage == null)
        {
            photoImage = GetComponent<Image>();
        }
    }

    private void OnEnable()
    {
        RefreshPhotoState();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        LoadThisPhotoSave();
    }

    public void LoadThisPhotoSave()
    {
        RefreshPhotoState();

        if (!hasSave)
        {
            Debug.LogWarning("Save file not found for photo: " + savePointId);
            return;
        }

        Time.timeScale = 1f;
        SaveManager.Instance.LoadGame(savePointId);
    }

    public void RefreshPhotoState()
    {
        if (SaveManager.Instance == null)
        {
            hasSave = false;
            SetVisualState(null);
            return;
        }

        hasSave = SaveManager.Instance.TryReadSaveData(savePointId, out GameSaveData data);

        SetVisualState(data);
    }

    private void SetVisualState(GameSaveData data)
    {
        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(!hasSave);
        }

        if (saveInfoText != null)
        {
            saveInfoText.text = data == null ? "No Save" : data.displayName + "\n" + data.savedAt;
        }

        if (photoImage != null && dimPhotoWhenMissing)
        {
            Color color = photoImage.color;
            color.a = hasSave ? 1f : 0.35f;
            photoImage.color = color;
        }
    }
}
