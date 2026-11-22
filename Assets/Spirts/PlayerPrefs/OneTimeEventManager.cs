using System;
using UnityEngine;

public class OneTimeEventManager : MonoBehaviour
{
    private static OneTimeEventManager instance;
    public static OneTimeEventManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<OneTimeEventManager>();
                if (instance == null) instance = new GameObject("OneTimeEventManager").AddComponent<OneTimeEventManager>();
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool TryTrigger(string eventKey, Action action)
    {
        if (string.IsNullOrEmpty(eventKey) || HasTriggered(eventKey)) return false;
        action?.Invoke();
        MarkAsTriggered(eventKey);
        return true;
    }

    public bool HasTriggered(string eventKey)
    {
        return !string.IsNullOrEmpty(eventKey) && GameProgressState.GetInt("event/" + eventKey) == 1;
    }

    public void MarkAsTriggered(string eventKey)
    {
        if (!string.IsNullOrEmpty(eventKey)) GameProgressState.SetInt("event/" + eventKey, 1);
    }

    public void ResetEvent(string eventKey)
    {
        if (!string.IsNullOrEmpty(eventKey)) GameProgressState.DeleteKey("event/" + eventKey);
    }

    [ContextMenu("Reset All One Time Events")]
    public void ResetAll() { GameProgressState.ClearEvents(); }

    private void OnDestroy() { if (instance == this) instance = null; }
}
