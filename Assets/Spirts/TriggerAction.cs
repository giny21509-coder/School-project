using System.Collections;
using UnityEngine;

public class TriggerAction : MonoBehaviour
{
    [Header("碰到這個 Tag 才觸發")]
    [SerializeField] private string triggerTag = "Player";

    [Header("要開啟的物品 ID")]
    [SerializeField] private string unlockIdToOpen;

    [Header("同場景要立刻開啟的物件")]
    [SerializeField] private GameObject[] objectsToEnable;

    [Header("設定")]
    [SerializeField] private bool triggerOnlyOnce = true;
    [SerializeField] private bool checkOverlapsOnStart = true;
    [SerializeField] private bool checkOverlapsEveryFrame = true;

    private bool hasTriggered = false;
    private readonly Collider2D[] overlapResults = new Collider2D[16];

    private void Start()
    {
        if (checkOverlapsOnStart)
        {
            StartCoroutine(CheckOverlapsNextFrame());
        }
    }

    private void Update()
    {
        if (checkOverlapsEveryFrame && (!triggerOnlyOnce || !hasTriggered))
        {
            CheckCurrentOverlaps();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryTrigger(collision.gameObject);
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        TryTrigger(collision.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryTrigger(collision.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryTrigger(collision.gameObject);
    }

    private IEnumerator CheckOverlapsNextFrame()
    {
        yield return null;
        CheckCurrentOverlaps();
    }

    private void CheckCurrentOverlaps()
    {
        Collider2D[] ownColliders = GetComponents<Collider2D>();
        ContactFilter2D filter = new ContactFilter2D();
        filter.NoFilter();

        for (int i = 0; i < ownColliders.Length; i++)
        {
            Collider2D ownCollider = ownColliders[i];
            if (ownCollider == null || !ownCollider.enabled)
            {
                continue;
            }

            int count = ownCollider.Overlap(filter, overlapResults);
            for (int hitIndex = 0; hitIndex < count; hitIndex++)
            {
                Collider2D hit = overlapResults[hitIndex];
                if (hit != null)
                {
                    TryTrigger(hit.gameObject);
                }

                if (triggerOnlyOnce && hasTriggered)
                {
                    return;
                }
            }
        }
    }

    private void TryTrigger(GameObject other)
    {
        if (other == null || (triggerOnlyOnce && hasTriggered))
        {
            return;
        }

        if (!other.CompareTag(triggerTag))
        {
            return;
        }

        hasTriggered = true;
        GameDataManager.UnlockItem(unlockIdToOpen);
        EnableObjectsInScene();

        string openedName = string.IsNullOrEmpty(unlockIdToOpen) ? "legacy item" : unlockIdToOpen;
        Debug.Log("觸發成功，已開啟：" + openedName);
    }

    private void EnableObjectsInScene()
    {
        if (objectsToEnable == null)
        {
            return;
        }

        for (int i = 0; i < objectsToEnable.Length; i++)
        {
            if (objectsToEnable[i] != null)
            {
                objectsToEnable[i].SetActive(true);
            }
        }
    }
}
