using System.Collections;
using Fungus;
using MoreMountains.TopDownEngine;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class EnemyDeathReward : MonoBehaviour
{
    [Header("Death Source")]
    [SerializeField] private Health topDownHealth;

    [Header("CG Reward")]
    [SerializeField] private ItemData cgItem;
    [SerializeField] private bool spawnPickup = true;
    [SerializeField] private ItemPickup pickupPrefab;
    [SerializeField] private Transform dropPoint;

    [Header("Save")]
    [SerializeField] private string rewardId;

    [Header("Fungus Dialogue")]
    [SerializeField] private Flowchart flowchart;
    [SerializeField] private string blockName;
    [SerializeField] private float dialogueDelay = 0.2f;
    [SerializeField] private bool waitUntilFlowchartIsFree = true;

    [Header("Events")]
    public UnityEvent onRewardDropped;
    public UnityEvent onRewardCollectedDirectly;
    public UnityEvent onDialogueStarted;

    private Chatacher chatacher;
    private bool hasTriggered;

    private void Awake()
    {
        if (topDownHealth == null)
        {
            topDownHealth = GetComponent<Health>();
        }

        // 只監聽一套正在使用的血量，避免舊元件誤報死亡而提早掉落。
        if (topDownHealth != null && topDownHealth.isActiveAndEnabled)
        {
            topDownHealth.OnDeath += HandleEnemyDeath;
        }
        else
        {
            topDownHealth = null;
            chatacher = GetComponent<Chatacher>();
            if (chatacher != null && chatacher.isActiveAndEnabled)
            {
                if (chatacher.OnDie == null) chatacher.OnDie = new UnityEvent();
                chatacher.OnDie.AddListener(HandleEnemyDeath);
            }
            else
            {
                chatacher = null;
            }
        }

        if (topDownHealth == null && chatacher == null)
        {
            Debug.LogWarning("EnemyDeathReward: 找不到 TopDown Health 或 Chatacher，死亡時不會觸發掉落。", this);
        }
    }

    private void OnDestroy()
    {
        if (topDownHealth != null)
        {
            topDownHealth.OnDeath -= HandleEnemyDeath;
        }

        if (chatacher != null)
        {
            chatacher.OnDie.RemoveListener(HandleEnemyDeath);
        }
    }

    private void HandleEnemyDeath()
    {
        if (hasTriggered)
        {
            return;
        }

        hasTriggered = true;
        GiveOrDropReward();
       
    }

    private void GiveOrDropReward()
    {
        if (cgItem == null)
        {
            Debug.LogWarning("EnemyDeathReward: CG Item 沒有設定，無法發放獎勵。", this);
            return;
        }

        string collectedId = GetRewardId();
        if (SaveManager.Instance != null && SaveManager.Instance.IsPickupCollected(collectedId))
        {
            return;
        }

        var spawnedMonster = GetComponent<InnerWorldSpawnedMonster>();
        bool isCollectionTarget = spawnedMonster != null && spawnedMonster.TargetId >= 0;
        // 五個名額即使共用 ItemData，也必須各自完成掉落／收集。
        if (!isCollectionTarget && InventoryAlreadyHasItem(cgItem))
        {
            MarkRewardCollected(collectedId);
            NotifyInnerWorldCollected();
            PlayDialogueNow();
            return;
        }

        if (spawnPickup)
        {
            if (pickupPrefab == null)
            {
                Debug.LogWarning("EnemyDeathReward: 已勾選 Spawn Pickup，但 Pickup Prefab 沒有設定。請指定帶有 ItemPickup 的掉落 Prefab。", this);
                return;
            }
            SpawnRewardPickup(collectedId);
            return;
        }

        AddRewardDirectly(collectedId);
    }

    private void SpawnRewardPickup(string collectedId)
    {
        Vector3 spawnPosition = dropPoint != null ? dropPoint.position : transform.position;
        ItemPickup pickup = Instantiate(pickupPrefab, spawnPosition, Quaternion.identity);
        pickup.itemData = cgItem;
        pickup.pickupId = collectedId;
        pickup.SetDialogue(flowchart, blockName, dialogueDelay, waitUntilFlowchartIsFree, onDialogueStarted);
        GetComponent<InnerWorldSpawnedMonster>()?.BindPickup(pickup);
        pickup.gameObject.SetActive(true);
        onRewardDropped?.Invoke();
    }

    private void AddRewardDirectly(string collectedId)
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("EnemyDeathReward: InventoryManager was not found.", this);
            return;
        }

        if (!InventoryManager.Instance.AddItem(cgItem))
        {
            return;
        }

        MarkRewardCollected(collectedId);

        NotifyInnerWorldCollected();

        if (ItemPickupUI.Instance != null)
        {
            ItemPickupUI.Instance.Show(cgItem);
        }

        onRewardCollectedDirectly?.Invoke();
        PlayDialogueNow();
    }
    private void PlayDialogueNow()
    {
        DialoguePlayer.Play(flowchart, blockName, dialogueDelay, waitUntilFlowchartIsFree, onDialogueStarted);
    }

    private bool InventoryAlreadyHasItem(ItemData item)
    {
        if (InventoryManager.Instance == null || item == null)
        {
            return false;
        }

        return SlotsContainItem(InventoryManager.Instance.collectSlots, item) ||
               SlotsContainItem(InventoryManager.Instance.backpackSlots, item) ||
               SlotsContainItem(InventoryManager.Instance.questSlots, item);
    }

    private bool SlotsContainItem(ItemData[] slots, ItemData item)
    {
        if (slots == null)
        {
            return false;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == item ||
                (slots[i] != null && !string.IsNullOrEmpty(item.itemId) && slots[i].itemId == item.itemId))
            {
                return true;
            }
        }

        return false;
    }

    private void MarkRewardCollected(string collectedId)
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.MarkPickupCollected(collectedId);
        }
    }

    private void NotifyInnerWorldCollected()
    {
        GetComponent<InnerWorldSpawnedMonster>()?.Collect();
    }

    private string GetRewardId()
    {
        var spawnedMonster = GetComponent<InnerWorldSpawnedMonster>();
        if (spawnedMonster != null && spawnedMonster.TargetId >= 0)
        {
            return spawnedMonster.CollectionSaveId;
        }

        if (!string.IsNullOrEmpty(rewardId))
        {
            return rewardId;
        }

        string itemId = cgItem != null && !string.IsNullOrEmpty(cgItem.itemId) ? cgItem.itemId : "CG";
        return SceneManager.GetActiveScene().name + "_" + gameObject.name + "_" + itemId;
    }
}

