using System.Collections;
using TMPro;
using UnityEngine;

public class AutoSavePopupUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text messageText;

    [Header("Message")]
    [SerializeField] private string messagePrefix = "Autosaved";
    [SerializeField] private bool showSavePointName = true;

    [Header("Timing")]
    [SerializeField] private float fadeInTime = 0.2f;
    [SerializeField] private float stayTime = 1.2f;
    [SerializeField] private float fadeOutTime = 0.4f;

    private Coroutine showRoutine;

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        HideImmediately();
    }

    private void OnEnable()
    {
        SaveManager.GameSaved += ShowSavedMessage;
    }

    private void OnDisable()
    {
        SaveManager.GameSaved -= ShowSavedMessage;
    }

    public void ShowSavedMessage(GameSaveData data)
    {
        if (messageText != null)
        {
            string saveName = data == null ? string.Empty : data.displayName;
            messageText.text = showSavePointName && !string.IsNullOrEmpty(saveName)
                ? messagePrefix + " - " + saveName
                : messagePrefix;
        }

        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
        }

        showRoutine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        yield return FadeTo(1f, fadeInTime);
        yield return new WaitForSecondsRealtime(stayTime);
        yield return FadeTo(0f, fadeOutTime);
        showRoutine = null;
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        if (canvasGroup == null)
        {
            yield break;
        }

        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = duration <= 0f ? 1f : elapsed / duration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        canvasGroup.interactable = targetAlpha > 0f;
        canvasGroup.blocksRaycasts = targetAlpha > 0f;
    }

    private void HideImmediately()
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
}
