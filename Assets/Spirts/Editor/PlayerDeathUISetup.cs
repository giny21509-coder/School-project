#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using MoreMountains.TopDownEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class PlayerDeathUISetup
{
    private const string ScenePath = "Assets/Scene/Cat internal world.unity";
    private const string PrefabPath = "Assets/prefeb/PlayerDeathUI.prefab";

    [MenuItem("Tools/Inner World/Install Player Death UI")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("請先停止 Play 再建立死亡畫面。");
        Scene previous = SceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool opened = !scene.isLoaded;
        try
        {
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            PlayerDeathUI existing = FindInScene<PlayerDeathUI>(scene);
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                Debug.Log("死亡 UI 已存在，保留目前的美術與設定。", existing);
                return;
            }
            InnerWorldDistanceSpawner spawner = FindInScene<InnerWorldDistanceSpawner>(scene);
            if (spawner == null) throw new InvalidOperationException("找不到內心世界生成器。");
            Transform player = new SerializedObject(spawner).FindProperty("player").objectReferenceValue as Transform;
            Health health = player != null ? player.GetComponentInChildren<Health>(true) : null;
            if (health == null && player != null) health = player.GetComponentInParent<Health>();
            if (health == null) throw new InvalidOperationException("生成器的 Player 找不到 Health。");

            PlayerDeathUI prefab = AssetDatabase.LoadAssetAtPath<PlayerDeathUI>(PrefabPath);
            if (prefab == null)
            {
                PlayerDeathUI template = CreateUI();
                try { prefab = PrefabUtility.SaveAsPrefabAsset(template.gameObject, PrefabPath).GetComponent<PlayerDeathUI>(); }
                finally { Object.DestroyImmediate(template.gameObject); }
            }
            PlayerDeathUI ui = ((GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, scene)).GetComponent<PlayerDeathUI>();
            Undo.RegisterCreatedObjectUndo(ui.gameObject, "Create player death UI");
            var data = new SerializedObject(ui);
            data.FindProperty("playerHealth").objectReferenceValue = health;
            var controls = new List<Behaviour>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                controls.AddRange(root.GetComponentsInChildren<MenuList>(true));
                controls.AddRange(root.GetComponentsInChildren<SaveLoadMenuUI>(true));
            }
            controls.AddRange(player.GetComponentsInChildren<PlayerController>(true));
            var array = data.FindProperty("disableOnDeath");
            array.arraySize = controls.Count;
            for (int i = 0; i < controls.Count; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = controls[i];
            data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(ui);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("死亡 UI 已加入，但場景未成功儲存。");
            Selection.activeGameObject = ui.gameObject;
            Debug.Log("死亡 UI 已接上玩家 Health：重新挑戰重新載入內心世界；返回主選單載入 Menu。", ui);
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            if (opened && scene.IsValid() && scene.isLoaded && !scene.isDirty)
                EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static PlayerDeathUI CreateUI()
    {
        Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Text/Cubic_11.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var root = new GameObject("PlayerDeathUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(PlayerDeathUI));
        root.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        RectTransform panel = Rect("DeathPanel", root.transform, Vector2.zero, Vector2.zero);
        panel.anchorMin = Vector2.zero;
        panel.anchorMax = Vector2.one;
        panel.offsetMin = panel.offsetMax = Vector2.zero;
        Image shade = panel.gameObject.AddComponent<Image>();
        shade.color = new Color(0.035f, 0.025f, 0.045f, 0.86f);
        RectTransform card = Rect("Card", panel, Vector2.zero, new Vector2(740f, 520f));
        Image cardImage = card.gameObject.AddComponent<Image>();
        cardImage.color = new Color(0.15f, 0.105f, 0.13f, 1f);
        Outline outline = card.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.72f, 0.52f, 0.33f, 1f);
        outline.effectDistance = new Vector2(3f, -3f);
        TextObject("Title", card, font, "挑戰失敗", new Vector2(0f, 168f), new Vector2(640f, 90f), 64, new Color(0.97f, 0.78f, 0.52f));
        TextObject("Message", card, font, "深呼吸，再試一次吧。", new Vector2(0f, 86f), new Vector2(640f, 54f), 28, new Color(0.87f, 0.81f, 0.75f));
        RectTransform line = Rect("Divider", card, new Vector2(0f, 40f), new Vector2(520f, 3f));
        Image lineImage = line.gameObject.AddComponent<Image>();
        lineImage.color = new Color(0.72f, 0.52f, 0.33f, 0.6f);
        lineImage.raycastTarget = false;
        Button retry = ButtonObject("RetryButton", card, font, "重新挑戰", new Vector2(0f, -28f), true);
        Button menu = ButtonObject("MainMenuButton", card, font, "返回主選單", new Vector2(0f, -124f), false);
        TextObject("Hint", card, font, "重新挑戰將從本關起點開始", new Vector2(0f, -214f), new Vector2(650f, 44f), 23, new Color(0.65f, 0.60f, 0.61f));
        PlayerDeathUI ui = root.GetComponent<PlayerDeathUI>();
        var data = new SerializedObject(ui);
        data.FindProperty("deathPanel").objectReferenceValue = panel.gameObject;
        data.FindProperty("retryButton").objectReferenceValue = retry;
        data.FindProperty("mainMenuButton").objectReferenceValue = menu;
        data.FindProperty("mainMenuScene").stringValue = "Menu";
        data.ApplyModifiedPropertiesWithoutUndo();
        panel.gameObject.SetActive(false);
        return ui;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static Text TextObject(string name, Transform parent, Font font, string text, Vector2 position, Vector2 size, int fontSize, Color color)
    {
        Text label = Rect(name, parent, position, size).gameObject.AddComponent<Text>();
        label.font = font;
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        return label;
    }

    private static Button ButtonObject(string name, Transform parent, Font font, string label, Vector2 position, bool primary)
    {
        RectTransform rect = Rect(name, parent, position, new Vector2(470f, 74f));
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = primary ? new Color(0.77f, 0.52f, 0.3f) : new Color(0.26f, 0.20f, 0.23f);
        Outline outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.81f, 0.63f, 0.43f);
        outline.effectDistance = new Vector2(2f, -2f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
        colors.selectedColor = colors.highlightedColor;
        colors.pressedColor = new Color(0.78f, 0.78f, 0.78f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        TextObject("Label", rect, font, label, Vector2.zero, new Vector2(450f, 70f), 34,
            primary ? new Color(0.16f, 0.10f, 0.12f) : new Color(0.97f, 0.85f, 0.68f));
        return button;
    }

    public static void Verify()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("此驗證請在編輯模式執行。");
        Scene previous = SceneManager.GetActiveScene();
        Scene temp = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        float savedScale = Time.timeScale;
        var checks = new List<string>();
        try
        {
            SceneManager.SetActiveScene(temp);
            PlayerDeathUI ui = CreateUI();
            var player = new GameObject("DeathUITestPlayer");
            Health health = player.AddComponent<Health>();
            health.InitialHealth = health.MaximumHealth = 6f;
            health.DestroyOnDeath = false;
            health.Initialization();
            health.SetHealth(6f);
            var menu = new GameObject("DeathUITestMenu").AddComponent<MenuList>();
            menu.enabled = true;
            var data = new SerializedObject(ui);
            data.FindProperty("playerHealth").objectReferenceValue = health;
            data.FindProperty("disableOnDeath").arraySize = 1;
            data.FindProperty("disableOnDeath").GetArrayElementAtIndex(0).objectReferenceValue = menu;
            data.ApplyModifiedPropertiesWithoutUndo();
            ui.SendMessage("OnEnable");
            GameObject panel = data.FindProperty("deathPanel").objectReferenceValue as GameObject;
            Require(!ui.IsShowing && !panel.activeSelf, "Alive player starts with death panel hidden.", checks);
            health.Kill();
            Require(health.CurrentHealth == 0f && ui.IsShowing && panel.activeSelf, "Real Health.Kill event shows the screen at zero health.", checks);
            Require(Time.timeScale == 0f && !menu.enabled, "Death pauses gameplay and disables ESC menu input.", checks);
            player.SetActive(false);
            Require(ui.gameObject.activeInHierarchy && panel.activeInHierarchy, "Screen survives player deactivation.", checks);
            ui.ShowDeathScreen();
            Time.timeScale = 1f;
            ui.SendMessage("LateUpdate");
            Require(Time.timeScale == 0f, "Duplicate death events and other pause menus cannot resume gameplay.", checks);
            Require(ui.RetryScene == "" && ui.MainMenuScene == "Menu", "Retry derives its scene while main menu targets Menu.", checks);
            foreach (string path in new[] { ScenePath, "Assets/Scene/Menu.unity" })
                Require(Array.Exists(EditorBuildSettings.scenes, entry => entry.enabled && entry.path == path), path + " is enabled in Build Settings.", checks);
            ui.SendMessage("OnDisable");
            Require(Time.timeScale == savedScale && menu.enabled && !ui.IsShowing, "Cleanup restores previous time scale and input state.", checks);
            health.OnDeath?.Invoke();
            Require(!ui.IsShowing, "Disabled controller unsubscribes from the death event.", checks);
            Debug.Log("DEATH_UI_VERIFICATION_PASS\n" + string.Join("\n", checks));
        }
        finally
        {
            EditorSceneManager.CloseScene(temp, true);
            Time.timeScale = savedScale;
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        }
    }

    private static void Require(bool condition, string message, List<string> checks)
    {
        if (!condition) throw new InvalidOperationException(message);
        checks.Add(message);
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T result = root.GetComponentInChildren<T>(true);
            if (result != null) return result;
        }
        return null;
    }
}
#endif
