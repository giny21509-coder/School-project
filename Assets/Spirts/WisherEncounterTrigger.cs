using UnityEngine;
using UnityEngine.Events;

public class WisherEncounterTrigger : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private WisherPhotoTarget wisherTarget;
    [SerializeField] private bool triggerOnce = true;

    [Header("Input")]
    [SerializeField] private KeyCode interactKey = KeyCode.LeftControl;
    [SerializeField] private bool requireInteractKey = true;

    [Header("Prompt UI")]
    [SerializeField] private GameObject promptUi;
    [SerializeField] private bool hidePromptAfterStarted = true;

    [Header("Events")]
    public UnityEvent onPlayerEnterRange;
    public UnityEvent onPlayerExitRange;
    public UnityEvent onCannotStartWithoutCamera;

    private bool playerInRange;
    private bool triggered;

    private void Reset()
    {
        wisherTarget = GetComponent<WisherPhotoTarget>();
    }

    private void Start()
    {
        SetPromptVisible(false);
    }

    private void Update()
    {
        if (!requireInteractKey || !playerInRange)
        {
            return;
        }

        if (Input.GetKeyDown(interactKey))
        {
            TryStartEncounter();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        playerInRange = true;
        SetPromptVisible(true);
        onPlayerEnterRange?.Invoke();

        if (!requireInteractKey)
        {
            TryStartEncounter();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        playerInRange = false;
        SetPromptVisible(false);
        onPlayerExitRange?.Invoke();
    }

    private void TryStartEncounter()
    {
        if (triggered)
        {
            return;
        }

        if (CameraSystem.Instance == null || !CameraSystem.Instance.HasCamera)
        {
            onCannotStartWithoutCamera?.Invoke();
            return;
        }

        if (CameraShootingEncounter.Instance == null)
        {
            Debug.LogWarning("Scene has no CameraShootingEncounter.");
            return;
        }

        if (wisherTarget == null)
        {
            wisherTarget = GetComponent<WisherPhotoTarget>();
        }

        triggered = triggerOnce;
        if (hidePromptAfterStarted)
        {
            SetPromptVisible(false);
        }

        CameraShootingEncounter.Instance.StartEncounter(wisherTarget);
    }

    private void SetPromptVisible(bool visible)
    {
        if (promptUi != null)
        {
            promptUi.SetActive(visible);
        }
    }
}
