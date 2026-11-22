using UnityEngine;
using UnityEngine.UI;

public class SaveSlotButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Text titleText;
    [SerializeField] private Text detailText;

    private string saveId;

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }
    }

    public void Setup(GameSaveData data)
    {
        saveId = data.saveId;

        if (titleText != null)
        {
            titleText.text = data.displayName;
        }

        if (detailText != null)
        {
            detailText.text = data.sceneName + "  " + data.savedAt;
        }

        if (button != null)
        {
            button.onClick.RemoveListener(LoadThisSave);
            button.onClick.AddListener(LoadThisSave);
        }
    }

    public void LoadThisSave()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.LoadGame(saveId);
        }
    }
}
