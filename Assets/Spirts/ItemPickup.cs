using System.Collections.Generic;
using Fungus;
using UnityEngine;
using UnityEngine.Events;

public class ItemPickup : MonoBehaviour
{
    [Header("道具資料")]
    public ItemData itemData;

    [Header("存檔用 ID，請每個場景物件都填不同值")]
    public string pickupId;

    [Header("撿取後的 Fungus 對話（可選）")]
    [SerializeField] private Flowchart flowchart;
    [Tooltip("填入對話 Block 名稱；Flowchart 留空時，自動尋找目前場景中名稱相符的 Block。") ]
    [SerializeField] private string blockName;
    [SerializeField] private float dialogueDelay = 0.2f;
    [SerializeField] private bool waitUntilFlowchartIsFree = true;

    [Header("事件")]
    public UnityEvent onPickedUp;
    public UnityEvent onDialogueStarted;

    [Header("共用互動範圍（可選，留空沿用自身 Trigger）")]
    [SerializeField] private Collider2D interactionRange = null;
    [SerializeField] private GameObject interactionPrompt = null;
    private readonly List<Collider2D> interactionOverlaps = new List<Collider2D>();

    private bool isPlayerInRange = false;

    private void Start()
    {
        if (string.IsNullOrEmpty(pickupId))
        {
            pickupId = gameObject.scene.name + "_" + gameObject.name;
        }

        if (SaveManager.Instance != null && SaveManager.Instance.IsPickupCollected(pickupId))
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (interactionRange != null)
        {
            // 每次按鍵前重新確認實際重疊，避免停用碰撞器或傳送後留下舊狀態。
            isPlayerInRange = IsPlayerInsideInteractionRange();
            bool canInteract = isPlayerInRange && Time.timeScale > 0f && !IsSceneDialoguePlaying();
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(canInteract);
            }
            if (!canInteract) return;
        }

        if (isPlayerInRange && Input.GetKeyDown(KeyCode.R))
        {
            PickUpItem();
        }
    }
    /// <summary>
    /// 讓外部（例如 EnemyDeathReward）在生成這個 pickup 時，
    /// 把「撿起後要播放的對話」設定進來。
    /// </summary>
    public void SetDialogue(
        Flowchart flowchart,
        string blockName,
        float dialogueDelay,
        bool waitUntilFlowchartIsFree,
        UnityEvent onDialogueStarted)
    {
        // CG 掉落物自己的對話優先；未設定時才沿用怪物傳入的設定。
        if (this.flowchart != null || !string.IsNullOrWhiteSpace(this.blockName))
        {
            return;
        }

        this.flowchart = flowchart;
        this.blockName = blockName;
        this.dialogueDelay = dialogueDelay;
        this.waitUntilFlowchartIsFree = waitUntilFlowchartIsFree;
        this.onDialogueStarted = onDialogueStarted;
    }

    private void PickUpItem()
    {
        if (itemData == null)
        {
            Debug.LogWarning("ItemPickup: Item Data 沒有設定。", this);
            return;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("ItemPickup: 場景中找不到 InventoryManager。", this);
            return;
        }

        bool isSuccess = InventoryManager.Instance.AddItem(itemData);
        if (!isSuccess)
        {
            Debug.LogWarning($"ItemPickup: {itemData.itemName} 撿取失敗，可能是背包頁面已滿。", this);
            return;
        }

        if (isSuccess)
        {
            if (ItemPickupUI.Instance != null)
            {
                ItemPickupUI.Instance.Show(itemData);
            }

            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.MarkPickupCollected(pickupId);
            }

            Debug.Log($"成功撿起道具：{itemData.itemName}！");

            onPickedUp?.Invoke();

            // 撿取成功後才觸發對話（如果有設定）
            DialoguePlayer.Play(flowchart, blockName, dialogueDelay, waitUntilFlowchartIsFree, onDialogueStarted);


            Destroy(gameObject);


        }
    }

    private bool IsPlayerInsideInteractionRange()
    {
        if (!interactionRange.isActiveAndEnabled) return false;

        ContactFilter2D filter = ContactFilter2D.noFilter;
        filter.useTriggers = false;
        interactionRange.Overlap(filter, interactionOverlaps);
        foreach (Collider2D other in interactionOverlaps)
        {
            // 只接受角色本體，避免攻擊或其他感應 Trigger 提早觸發。
            if (other != null && other.CompareTag("Player")) return true;
        }
        return false;
    }

    private bool IsSceneDialoguePlaying()
    {
        foreach (Flowchart candidate in Flowchart.CachedFlowcharts)
        {
            if (candidate != null && candidate.isActiveAndEnabled &&
                candidate.gameObject.scene == gameObject.scene && candidate.HasExecutingBlocks())
            {
                return true;
            }
        }
        return false;
    }

    private void OnDisable()
    {
        isPlayerInRange = false;
        if (interactionPrompt != null) interactionPrompt.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (interactionRange != null) return;

        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = true;
            string displayName = itemData == null ? gameObject.name : itemData.itemName;
            Debug.Log($"靠近了 {displayName}，按 R 鍵可以撿起！");
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (interactionRange != null) return;

        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (interactionRange != null) return;

        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = false;
            Debug.Log("離開了撿取範圍");
        }
    }
}
