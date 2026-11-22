using UnityEngine;
using UnityEngine.EventSystems;
using Fungus;

public class TaikButton : MonoBehaviour
{
    public GameObject Butter;
    public Flowchart flowchart;
    public string blockName = "互動";
    public KeyCode interactkey = KeyCode.R;
    public bool isplayerInRange = false;

    [Header("Optional one-time interaction")]
    [SerializeField] private string interactionStateKey;
    [SerializeField] private GameObject[] objectsActiveAfterInteraction;
    [SerializeField] private GameObject[] objectsInactiveAfterInteraction;
    [SerializeField] private bool allowMouseClick;

    private bool HasCompletedInteraction => !string.IsNullOrEmpty(interactionStateKey)
        && GameProgressState.GetInt(interactionStateKey) == 1;

    private void Start()
    {
        if (HasCompletedInteraction) ApplyCompletedState();
    }

    private void Update()
    {
        // Other interaction points can share the same saved state.
        if (HasCompletedInteraction)
        {
            ApplyCompletedState();
            return;
        }

        if (isplayerInRange && Input.GetKeyDown(interactkey)) TryInteract();
    }

    private void OnMouseDown()
    {
        if (!allowMouseClick || (EventSystem.current != null
            && EventSystem.current.IsPointerOverGameObject())) return;
        TryInteract();
    }

    public void TryInteract()
    {
        if (!isplayerInRange || HasCompletedInteraction || flowchart == null
            || flowchart.HasExecutingBlocks() || flowchart.FindBlock(blockName) == null) return;

        flowchart.ExecuteBlock(blockName);
        if (!string.IsNullOrEmpty(interactionStateKey))
        {
            GameProgressState.SetInt(interactionStateKey, 1);
            ApplyCompletedState();
        }
    }

    private void ApplyCompletedState()
    {
        if (Butter != null) Butter.SetActive(false);
        isplayerInRange = false;
        SetObjectsActive(objectsActiveAfterInteraction, true);
        SetObjectsActive(objectsInactiveAfterInteraction, false);
        enabled = false;
    }

    private static void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null) return;
        foreach (GameObject target in objects)
            if (target != null) target.SetActive(active);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || HasCompletedInteraction) return;
        if (Butter != null) Butter.SetActive(true);
        isplayerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (Butter != null) Butter.SetActive(false);
        isplayerInRange = false;
    }
}
