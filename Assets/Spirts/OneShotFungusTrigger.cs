using UnityEngine;
using Fungus;

public class OneShotFungusTrigger : MonoBehaviour
{
    [Header("Trigger")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool requireInteractKey = false;
    [SerializeField] private KeyCode interactKey = KeyCode.R;

    [Header("Fungus")]
    [SerializeField] private Flowchart flowchart;
    [SerializeField] private string blockName;

    [Header("UI")]
    [SerializeField] private GameObject promptUi;

    [Header("After Trigger")]
    [SerializeField] private GameObject objectToDisableAfterTrigger;
    [SerializeField] private bool disableFungusEventHandlers = true;
    [SerializeField] private bool hidePromptAfterTrigger = true;

    private bool playerInRange;
    private bool hasTriggered;

    private void Start()
    {
        SetPromptVisible(false);
    }

    private void Update()
    {
        if (!requireInteractKey || !playerInRange || hasTriggered)
        {
            return;
        }

        if (Input.GetKeyDown(interactKey))
        {
            TriggerDialogue();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        playerInRange = true;

        if (requireInteractKey)
        {
            SetPromptVisible(true);
            return;
        }

        TriggerDialogue();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        playerInRange = false;
        SetPromptVisible(false);
    }

    private void TriggerDialogue()
    {
        if (hasTriggered)
        {
            return;
        }

        if (flowchart == null)
        {
            Debug.LogWarning("OneShotFungusTrigger: Flowchart is not assigned.", this);
            return;
        }

        if (string.IsNullOrEmpty(blockName))
        {
            Debug.LogWarning("OneShotFungusTrigger: Block Name is empty.", this);
            return;
        }

        if (flowchart.HasExecutingBlocks())
        {
            return;
        }

        hasTriggered = true;
        DisableRepeatedTriggers();
        flowchart.ExecuteBlock(blockName);

        if (hidePromptAfterTrigger)
        {
            SetPromptVisible(false);
        }

        DisableTriggerTarget();
    }

    private void SetPromptVisible(bool visible)
    {
        if (promptUi != null)
        {
            promptUi.SetActive(visible);
        }
    }

    private void DisableRepeatedTriggers()
    {
        GameObject target = objectToDisableAfterTrigger != null ? objectToDisableAfterTrigger : gameObject;

        DisableColliders(target);

        if (disableFungusEventHandlers && flowchart != null)
        {
            DisableFungusEventHandlers(flowchart.gameObject);
        }

        if (target == gameObject || (flowchart != null && target == flowchart.gameObject))
        {
            enabled = false;
        }
    }

    private void DisableTriggerTarget()
    {
        GameObject target = objectToDisableAfterTrigger != null ? objectToDisableAfterTrigger : gameObject;

        if (flowchart == null || target != flowchart.gameObject)
        {
            target.SetActive(false);
        }
    }

    private void DisableColliders(GameObject target)
    {
        if (target == null)
        {
            return;
        }

        Collider2D[] colliders = target.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = false;
        }
    }

    private void DisableFungusEventHandlers(GameObject target)
    {
        Fungus.EventHandler[] eventHandlers = target.GetComponents<Fungus.EventHandler>();
        for (int i = 0; i < eventHandlers.Length; i++)
        {
            eventHandlers[i].enabled = false;
        }
    }
}
