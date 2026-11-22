using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CGCollectionProgressUI : MonoBehaviour
{
    [Serializable]
    public class CGDot
    {
        public ItemData cgItem;
        public Image image;
    }

    [Header("每個圓點對應一張 CG，依畫面順序設定")]
    [SerializeField] private CGDot[] dots = new CGDot[3];
    [SerializeField] private Sprite uncollectedSprite;
    [SerializeField] private Sprite collectedSprite;

    [Header("新收集時的放大提示")]
    [SerializeField, Min(0.01f)] private float pulseDuration = 0.4f;
    [SerializeField, Min(1f)] private float pulseScale = 1.3f;

    private Vector3[] originalScales;
    private Coroutine[] pulses;

    private void Awake()
    {
        originalScales = new Vector3[dots.Length];
        pulses = new Coroutine[dots.Length];
        for (int i = 0; i < dots.Length; i++)
            originalScales[i] = dots[i] != null && dots[i].image != null
                ? dots[i].image.transform.localScale : Vector3.one;
    }

    private void OnEnable()
    {
        InventoryManager.ItemAdded += OnItemAdded;
        RefreshDots();
    }

    private void OnDisable()
    {
        InventoryManager.ItemAdded -= OnItemAdded;
        StopAllCoroutines();
        for (int i = 0; i < dots.Length; i++)
        {
            if (dots[i] != null && dots[i].image != null)
                dots[i].image.transform.localScale = originalScales[i];
            pulses[i] = null;
        }
    }

    // 同步讀檔、清空背包與跨場景恢復，不將它們當作新收集。
    private void LateUpdate()
    {
        RefreshDots();
    }

    public void RefreshDots()
    {
        for (int i = 0; i < dots.Length; i++)
        {
            CGDot dot = dots[i];
            if (dot == null || dot.image == null) continue;
            bool collected = HasItem(dot.cgItem);
            Sprite sprite = collected ? collectedSprite : uncollectedSprite;
            if (dot.image.sprite != sprite) dot.image.sprite = sprite;
        }
    }

    private bool HasItem(ItemData target)
    {
        InventoryManager inventory = InventoryManager.Instance;
        if (target == null || inventory == null) return false;
        return Contains(inventory.collectSlots, target)
            || Contains(inventory.backpackSlots, target)
            || Contains(inventory.questSlots, target);
    }

    private static bool SameItem(ItemData a, ItemData b)
    {
        return a != null && b != null && (a == b
            || (!string.IsNullOrEmpty(a.itemId) && a.itemId == b.itemId));
    }

    private static bool Contains(ItemData[] slots, ItemData target)
    {
        if (slots == null) return false;
        foreach (ItemData item in slots)
            if (SameItem(item, target)) return true;
        return false;
    }

    private void OnItemAdded(ItemData item, bool firstAcquisition)
    {
        RefreshDots();
        if (!firstAcquisition) return;
        for (int i = 0; i < dots.Length; i++)
        {
            if (dots[i] == null || dots[i].image == null
                || !SameItem(dots[i].cgItem, item)) continue;
            if (pulses[i] != null) StopCoroutine(pulses[i]);
            pulses[i] = StartCoroutine(Pulse(i));
        }
    }

    private IEnumerator Pulse(int index)
    {
        Transform dotTransform = dots[index].image.transform;
        float duration = Mathf.Max(0.01f, pulseDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (dotTransform == null) yield break;
            float scale = 1f + Mathf.Sin(elapsed / duration * Mathf.PI) * (pulseScale - 1f);
            dotTransform.localScale = originalScales[index] * scale;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        if (dotTransform != null) dotTransform.localScale = originalScales[index];
        pulses[index] = null;
    }
}
