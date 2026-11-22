using Unity.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTeleporter : MonoBehaviour
{
    public  GameObject interactHintUI;
    public KeyCode teleportKey = KeyCode.E;
    public string targetSceneName;

    private bool playerInside = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (interactHintUI != null) interactHintUI.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.timeScale <= 0f) return;
        if (playerInside && Input.GetKeyDown(teleportKey))
        {
            if (string.IsNullOrWhiteSpace(targetSceneName) || !Application.CanStreamedLevelBeLoaded(targetSceneName))
            {
                Debug.LogWarning("SceneTeleporter has no available destination: " + targetSceneName, this);
                return;
            }
            SceneManager.LoadScene(targetSceneName);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInside = true;
            if (interactHintUI != null) interactHintUI.SetActive(true);
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInside = false;
            if (interactHintUI != null) interactHintUI.SetActive(false);
        }
    }
}
