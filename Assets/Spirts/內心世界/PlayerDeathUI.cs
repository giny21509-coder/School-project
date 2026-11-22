using MoreMountains.TopDownEngine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Scene UI stays alive when Health disables the player on death.</summary>
[DisallowMultipleComponent]
public class PlayerDeathUI : MonoBehaviour
{
    [Header("玩家死亡事件")]
    [SerializeField] private Health playerHealth;
    [SerializeField] private Behaviour[] disableOnDeath = new Behaviour[0];

    [Header("死亡畫面（控制器請放在畫面外層，保持啟用）")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainMenuButton;

    [Header("場景（需加入 Build Profiles 的 Scene List）")]
    [Tooltip("留空時重新載入此 UI 所在的場景。")]
    [SerializeField] private string retryScene = "";
    [SerializeField] private string mainMenuScene = "Menu";

    public bool IsShowing { get; private set; }
    public string RetryScene => string.IsNullOrWhiteSpace(retryScene) ? gameObject.scene.path : retryScene;
    public string MainMenuScene => mainMenuScene;

    private Health subscribedHealth;
    private bool[] previousEnabled;
    private float previousTimeScale;
    private bool ownsPause;
    private bool loading;

    private void OnEnable()
    {
        IsShowing = false;
        loading = false;
        if (deathPanel != null && deathPanel != gameObject) deathPanel.SetActive(false);
        if (playerHealth != null)
        {
            subscribedHealth = playerHealth;
            subscribedHealth.OnDeath += ShowDeathScreen;
        }
        if (retryButton != null) retryButton.onClick.AddListener(Retry);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(ReturnToMainMenu);
    }

    private void Start()
    {
        if (playerHealth == null)
            Debug.LogError("PlayerDeathUI：請指定玩家的 Health。", this);
        else if (playerHealth.CurrentHealth <= 0f) ShowDeathScreen();
    }

    private void LateUpdate()
    {
        // Other scene menus must not resume gameplay behind the death screen.
        if (ownsPause && !loading) Time.timeScale = 0f;
    }

    private void OnDisable()
    {
        if (subscribedHealth != null) subscribedHealth.OnDeath -= ShowDeathScreen;
        subscribedHealth = null;
        if (retryButton != null) retryButton.onClick.RemoveListener(Retry);
        if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(ReturnToMainMenu);
        ReleasePause();
        RestoreControls();
        IsShowing = false;
    }

    public void ShowDeathScreen()
    {
        if (IsShowing || loading) return;
        if (deathPanel == null || deathPanel == gameObject)
        {
            Debug.LogError("PlayerDeathUI：Death Panel 必須指定外層控制器底下的死亡畫面。", this);
            return;
        }
        IsShowing = true;
        previousTimeScale = Time.timeScale;
        ownsPause = true;
        previousEnabled = new bool[disableOnDeath.Length];
        for (int i = 0; i < disableOnDeath.Length; i++)
        {
            Behaviour target = disableOnDeath[i];
            if (target == null || target == this) continue;
            previousEnabled[i] = target.enabled;
            target.enabled = false;
        }
        deathPanel.SetActive(true);
        deathPanel.transform.SetAsLastSibling();
        Time.timeScale = 0f;
        if (EventSystem.current != null && retryButton != null)
            EventSystem.current.SetSelectedGameObject(retryButton.gameObject);
    }

    public void Retry() => LoadScene(RetryScene);
    public void ReturnToMainMenu() => LoadScene(mainMenuScene);

    private void LoadScene(string scene)
    {
        if (!IsShowing || loading) return;
        if (string.IsNullOrWhiteSpace(scene) || !Application.CanStreamedLevelBeLoaded(scene))
        {
            Debug.LogError("PlayerDeathUI：場景未加入 Build Profiles 的 Scene List：" + scene, this);
            return;
        }
        loading = true;
        SetButtonsInteractable(false);
        ownsPause = false;
        Time.timeScale = 1f;
        try { SceneManager.LoadScene(scene, LoadSceneMode.Single); }
        catch (System.Exception exception)
        {
            loading = false;
            ownsPause = true;
            Time.timeScale = 0f;
            SetButtonsInteractable(true);
            Debug.LogException(exception, this);
        }
    }

    private void ReleasePause()
    {
        if (!ownsPause) return;
        ownsPause = false;
        Time.timeScale = previousTimeScale;
    }

    private void RestoreControls()
    {
        if (previousEnabled == null) return;
        for (int i = 0; i < disableOnDeath.Length && i < previousEnabled.Length; i++)
            if (disableOnDeath[i] != null && disableOnDeath[i] != this)
                disableOnDeath[i].enabled = previousEnabled[i];
        previousEnabled = null;
    }

    private void SetButtonsInteractable(bool value)
    {
        if (retryButton != null) retryButton.interactable = value;
        if (mainMenuButton != null) mainMenuButton.interactable = value;
    }

#if UNITY_EDITOR
    [ContextMenu("預覽死亡畫面（編輯模式）")]
    private void PreviewInEditor()
    {
        if (Application.isPlaying || deathPanel == null || deathPanel == gameObject) return;
        UnityEditor.Undo.RecordObject(deathPanel, "Preview death screen");
        deathPanel.SetActive(true);
    }

    [ContextMenu("隱藏死亡畫面（編輯模式）")]
    private void HideInEditor()
    {
        if (Application.isPlaying || deathPanel == null || deathPanel == gameObject) return;
        UnityEditor.Undo.RecordObject(deathPanel, "Hide death screen");
        deathPanel.SetActive(false);
    }
#endif
}
