using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    // [Header("----Esc 菜單-----")]
    // [SerializeField] Button resumeButter;
    // [SerializeField] Button menuButter;
    //[SerializeField] Slider volume;
    public void QuitGame()
    {
        Debug.Log("遊戲正在退出...");

        // 執行離開遊戲
        Application.Quit();
    }

    public void ChangeScene(string sceneName)
    {

        if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("Scene is not available in the build: " + sceneName, this);
            return;
        }
        if (SceneManager.GetActiveScene().name == "Menu" && sceneName != "Menu")
            SaveManager.Instance?.StartNewGame();
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    void Start()
    {
        
    }

    
    void Update()
    {
        
    }
}
