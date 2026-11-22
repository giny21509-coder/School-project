using UnityEngine;
using UnityEngine.Events;

public class SavePoint : MonoBehaviour
{
    [Header("Save Point")]
    [SerializeField] private string savePointId = "office_save_point";
    [SerializeField] private string displayName = "Office";
    [SerializeField] private Transform spawnPoint;

    [Header("Auto Save")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool saveImmediatelyOnEnter = true;
    [SerializeField] private bool saveOnlyOncePerEnter = true;
    [SerializeField] private float repeatSaveCooldown = 2f;

    [Header("Hint UI")]
    [SerializeField] private GameObject interactHint;

    [Header("Events")]
    public UnityEvent onPlayerEntered;
    public UnityEvent onPlayerExited;
    public UnityEvent onSavedHere;

    private bool playerInside;
    private bool savedThisEnter;
    private float nextSaveTime;

    private void Awake()
    {
        SetHint(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        playerInside = true;
        savedThisEnter = false;
        SetHint(true);
        onPlayerEntered?.Invoke();

        if (saveImmediatelyOnEnter)
        {
            SaveHere();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        playerInside = false;
        savedThisEnter = false;
        SetHint(false);
        onPlayerExited?.Invoke();
    }

    public void SaveHere()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("SaveManager was not found.");
            return;
        }

        if (saveOnlyOncePerEnter && savedThisEnter)
        {
            return;
        }

        if (Time.time < nextSaveTime)
        {
            return;
        }

        Vector3 savePosition = spawnPoint != null ? spawnPoint.position : transform.position;
        SaveManager.Instance.SaveAtPoint(savePointId, displayName, savePosition);
        savedThisEnter = true;
        nextSaveTime = Time.time + repeatSaveCooldown;
        onSavedHere?.Invoke();
    }

    private void SetHint(bool show)
    {
        if (interactHint != null)
        {
            interactHint.SetActive(show);
        }
    }
}
