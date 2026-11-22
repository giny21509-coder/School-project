using System.Collections;
using TMPro;
using UnityEngine;

public class ItemPickupUI : MonoBehaviour
{
    public static ItemPickupUI Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text messageText;

    [Header("Display Settings")]
    [SerializeField] private float showTime = 2f;
    [SerializeField] private float fadeTime = 0.25f;

    private Coroutine showCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        HideImmediately();
    }

    public void Show(ItemData itemData)
    {
        if (itemData == null)
        {
            Debug.LogWarning("ItemPickupUI.Show got null itemData.");
            return;
        }

        if (canvasGroup == null || messageText == null)
        {
            Debug.LogWarning("ItemPickupUI needs CanvasGroup and Message Text assigned in Inspector.");
            return;
        }

        gameObject.SetActive(true);

        if (showCoroutine != null)
        {
            StopCoroutine(showCoroutine);
        }

        showCoroutine = StartCoroutine(ShowRoutine(itemData));
    }

    private IEnumerator ShowRoutine(ItemData itemData)
    {
        messageText.text = itemData.pageType == ItemPageType.Collect
            ? "收集到一張 CG 圖片！"
            : "包包裡有 " + itemData.itemName;

        yield return FadeTo(1f);
        yield return new WaitForSeconds(showTime);
        yield return FadeTo(0f);

        showCoroutine = null;
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        float startAlpha = canvasGroup.alpha;
        float timer = 0f;

        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / fadeTime);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }

    private void HideImmediately()
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = 0f;
    }
}