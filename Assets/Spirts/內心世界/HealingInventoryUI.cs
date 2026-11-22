using UnityEngine;
using UnityEngine.UI;

/// <summary>Three fixed grey/colored item slots and a click-to-heal button.</summary>
[DisallowMultipleComponent]
public class HealingInventoryUI : MonoBehaviour
{
    [SerializeField] private InnerWorldHealingSystem healingSystem;
    [SerializeField] private Button useButton;
    [SerializeField] private Image[] itemSlots = new Image[HealingSuppliesState.Capacity];
    [SerializeField] private Text titleText;
    [SerializeField] private Text hintText;

    [Header("自訂補血圖示")]
    [Tooltip("未持有時使用的圖片，例如灰色版本。留空則沿用各格原圖並套用 Empty Color。")]
    [SerializeField] private Sprite emptyItemSprite;
    [Tooltip("持有時使用的圖片，例如亮起版本。指定後保留圖片原色，不套用 Ready Color。")]
    [SerializeField] private Sprite ownedItemSprite;

    [Header("未指定自訂圖片時的顏色")]
    [SerializeField] private Color readyColor = new Color(1f, 0.35f, 0.4f, 1f);
    [SerializeField] private Color emptyColor = new Color(0.45f, 0.4f, 0.4f, 1f);

    private int lastCount = -1;
    private int lastStatus = -1;
    private Sprite[] originalSlotSprites;

    private void OnEnable()
    {
        CacheOriginalSprites();
        if (healingSystem != null) healingSystem.InventoryChanged += Refresh;
        if (useButton != null) useButton.onClick.AddListener(HandleClick);
        lastCount = lastStatus = -1;
        Refresh();
    }

    private void OnDisable()
    {
        if (healingSystem != null) healingSystem.InventoryChanged -= Refresh;
        if (useButton != null) useButton.onClick.RemoveListener(HandleClick);
    }

    private void LateUpdate()
    {
        // Also refresh when damage, pause or death changes whether healing is allowed.
        Refresh();
    }

    private void HandleClick()
    {
        if (healingSystem != null) healingSystem.TryUseItem();
    }

    private void Refresh()
    {
        if (healingSystem == null) return;
        int count = healingSystem.StoredCount;
        int status = healingSystem.CanUseItem ? 0
            : !healingSystem.PlayerAlive ? 1
            : count == 0 ? 3 : healingSystem.IsFullHealth ? 2 : 4;
        if (count == lastCount && status == lastStatus) return;
        lastCount = count;
        lastStatus = status;
        if (titleText != null) titleText.text = "補血";
        if (hintText != null)
            hintText.text = status == 0 ? healingSystem.UseKey + " 使用 · 回復 " + healingSystem.HealAmount + " 格"
                : status == 1 ? "無法使用"
                : status == 2 ? "血量已滿"
                : status == 3 ? healingSystem.UseKey + " 使用 · 尚未持有" : "暫停中";
        if (useButton != null) useButton.interactable = status == 0;
        CacheOriginalSprites();
        for (int i = 0; itemSlots != null && i < itemSlots.Length; i++)
        {
            Image image = itemSlots[i];
            if (image == null) continue;
            bool owned = i < count;
            Sprite customSprite = owned ? ownedItemSprite : emptyItemSprite;
            image.sprite = customSprite != null ? customSprite : originalSlotSprites[i];
            image.color = customSprite != null ? Color.white : owned ? readyColor : emptyColor;
        }
    }

    private void CacheOriginalSprites()
    {
        if (itemSlots == null) return;
        if (originalSlotSprites != null && originalSlotSprites.Length == itemSlots.Length) return;
        originalSlotSprites = new Sprite[itemSlots.Length];
        for (int i = 0; i < itemSlots.Length; i++)
            if (itemSlots[i] != null) originalSlotSprites[i] = itemSlots[i].sprite;
    }

    private void OnValidate()
    {
        // Refresh on the next frame when custom pictures are changed during Play.
        lastCount = lastStatus = -1;
    }
}
