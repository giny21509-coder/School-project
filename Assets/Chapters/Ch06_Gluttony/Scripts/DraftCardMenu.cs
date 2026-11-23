using System;
using System.Collections.Generic;
using JianQi.Ch06;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 最終戰的草稿卡選單（設計文件 §3 S4）：列出已解鎖的草稿與（需要時）保底卡「我在聽。」，不計時。
/// 用一個按鈕範本複製出選項（範本需含 Button 與子物件 TMP_Text）。
/// 若組內決定改用 Fungus Menu（§12-5），只要換掉這個元件，GluttonyShadeBoss 不用改。
/// </summary>
public class DraftCardMenu : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Transform optionParent;
    [SerializeField] private Button optionTemplate;
    [SerializeField] private string fallbackText = "我在聽。";

    private readonly List<Button> spawned = new List<Button>();
    private Action<string> onChosen;

    private void Awake()
    {
        if (optionTemplate != null) optionTemplate.gameObject.SetActive(false);
        if (root != null) root.SetActive(false);
    }

    public void Show(IList<string> optionIds, ChapterDefinition chapter, Action<string> chosen)
    {
        Clear();
        onChosen = chosen;
        foreach (string id in optionIds)
        {
            Button b = Instantiate(optionTemplate, optionParent);
            b.gameObject.SetActive(true);
            var label = b.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = LabelFor(id, chapter);
            string captured = id;
            b.onClick.AddListener(() => Choose(captured));
            spawned.Add(b);
        }
        if (root != null) root.SetActive(true);
        if (spawned.Count > 0 && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(spawned[0].gameObject); // 手把可直接選
    }

    public void Hide()
    {
        Clear();
        if (root != null) root.SetActive(false);
    }

    private void Choose(string id)
    {
        Action<string> callback = onChosen;
        Hide();
        callback?.Invoke(id);
    }

    private string LabelFor(string id, ChapterDefinition chapter)
    {
        if (id == ShadeBossModel.FallbackId) return fallbackText;
        DraftMessageDefinition d = chapter != null ? chapter.FindDraft(id) : null;
        return d != null ? $"給{d.recipient}：{d.text}" : id;
    }

    private void Clear()
    {
        foreach (var b in spawned) if (b != null) Destroy(b.gameObject);
        spawned.Clear();
        onChosen = null;
    }
}
