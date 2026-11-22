#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using MoreMountains.TopDownEngine;
using MoreMountains.Tools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>Creates editable scene UI and a pickup prefab with Unity-generated metadata.</summary>
public static class InnerWorldHealingSetup
{
    private const string ScenePath = "Assets/Scene/Cat internal world.unity";
    private const string PickupPath = "Assets/prefeb/內心世界/HealingPickup.prefab";
    private const string IconPath = "Assets/TopDownEngine/ThirdParty/MoreMountains/MMInterface/Styles/Mini/Sprites/Icons/MMIconHeart.png";
    private const string FontPath = "Assets/Text/Cubic_11.ttf";
    private static readonly List<string> checks = new List<string>();

    [MenuItem("Tools/Inner World/Install Healing Supplies")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("請先停止 Play，再建立補血 UI。");
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        // Keep other open scenes (including unsaved work) loaded while configuring this chapter.
        if (!scene.isLoaded) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        SceneManager.SetActiveScene(scene);

        InnerWorldDistanceSpawner spawner = FindInScene<InnerWorldDistanceSpawner>(scene);
        if (spawner == null) throw new InvalidOperationException("找不到內心世界生成器。");
        var spawnerData = new SerializedObject(spawner);
        Transform player = spawnerData.FindProperty("player").objectReferenceValue as Transform;
        Health health = player != null ? player.GetComponentInChildren<Health>(true) : null;
        if (health == null && player != null) health = player.GetComponentInParent<Health>();
        if (health == null) throw new InvalidOperationException("生成器指定的玩家找不到 Health。");
        MMHealthBar healthBar = health.GetComponentInParent<MMHealthBar>();
        Canvas canvas = healthBar != null && healthBar.TargetProgressBar != null
            ? healthBar.TargetProgressBar.GetComponentInParent<Canvas>() : null;
        if (canvas == null) throw new InvalidOperationException("找不到玩家血條所在的 Canvas。");

        Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>(IconPath);
        if (icon == null) throw new InvalidOperationException("找不到補血圖示。");
        Font font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        HealingItemPickup pickup = EnsurePickupPrefab(icon);
        InnerWorldHealingSystem system = spawner.GetComponent<InnerWorldHealingSystem>();
        if (system == null) system = Undo.AddComponent<InnerWorldHealingSystem>(spawner.gameObject);
        SetReference(system, "spawner", spawner);
        SetReference(system, "playerHealth", health);
        SetReference(system, "pickupPrefab", pickup);

        Transform existing = canvas.transform.Find("HealingSuppliesUI");
        HealingInventoryUI ui = existing != null ? existing.GetComponent<HealingInventoryUI>() : null;
        if (existing != null && ui == null)
            throw new InvalidOperationException("HealingSuppliesUI 名稱已被其他物件使用。");
        ui = CreateHud(canvas.transform, icon, font);
        SetReference(ui, "healingSystem", system);
        EditorUtility.SetDirty(system);
        EditorUtility.SetDirty(ui);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = ui.gameObject;
        Debug.Log("補血系統已建立：一般怪每 3～4 隻掉落、收集後按 H 或點擊 UI 回復 2 格。");
    }

    public static void InstallAndVerify()
    {
        Scene previouslyActive = SceneManager.GetActiveScene();
        try
        {
            Install();
            Verify();
            string report = Argument("-healingReport");
            if (!string.IsNullOrEmpty(report))
                File.WriteAllText(report, "PASS\n" + string.Join("\n", checks));
            Debug.Log("HEALING_VERIFICATION_PASS\n" + string.Join("\n", checks));
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            else throw;
        }
        finally
        {
            if (previouslyActive.IsValid() && previouslyActive.isLoaded)
                SceneManager.SetActiveScene(previouslyActive);
        }
    }

    private static HealingItemPickup EnsurePickupPrefab(Sprite icon)
    {
        HealingItemPickup existing = AssetDatabase.LoadAssetAtPath<HealingItemPickup>(PickupPath);
        if (existing != null) return existing;
        var root = new GameObject("HealingPickup");
        try
        {
            HealingItemPickup pickup = root.AddComponent<HealingItemPickup>();
            var visual = new GameObject("Visual", typeof(SpriteRenderer));
            visual.transform.SetParent(root.transform, false);
            SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
            renderer.sprite = icon;
            renderer.color = new Color(1f, 0.35f, 0.4f, 1f);
            renderer.sortingLayerName = "Top";
            renderer.sortingOrder = 30;
            float scale = 0.8f / Mathf.Max(0.01f, icon.bounds.size.x);
            visual.transform.localScale = Vector3.one * scale;
            SetReference(pickup, "visual", visual.transform);
            return PrefabUtility.SaveAsPrefabAsset(root, PickupPath).GetComponent<HealingItemPickup>();
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static HealingInventoryUI CreateHud(Transform canvas, Sprite icon, Font font)
    {
        Transform existing = canvas.Find("HealingSuppliesUI");
        GameObject root = existing != null ? existing.gameObject
            : new GameObject("HealingSuppliesUI", typeof(RectTransform),
                typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(HealingInventoryUI));
        if (existing == null) Undo.RegisterCreatedObjectUndo(root, "Create healing supplies UI");
        root.transform.SetParent(canvas, false);
        root.layer = LayerMask.NameToLayer("UI");
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(50f, -385f);
        rect.sizeDelta = new Vector2(280f, 148f);
        Image background = root.GetComponent<Image>();
        background.color = new Color(0.16f, 0.12f, 0.14f, 0.96f);
        Outline outline = root.GetComponent<Outline>();
        if (outline == null) outline = root.AddComponent<Outline>();
        outline.effectColor = new Color(0.72f, 0.5f, 0.32f, 1f);
        outline.effectDistance = new Vector2(2f, -2f);
        if (existing == null)
        {
            Shadow shadow = root.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            shadow.effectDistance = new Vector2(4f, -5f);
        }
        Button button = root.GetComponent<Button>();
        button.targetGraphic = background;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        ColorBlock colors = button.colors;
        colors.disabledColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        button.colors = colors;

        // Replace only the UI children owned by this setup tool.
        foreach (string childName in new[] { "Icon", "Count", "Title", "Hint", "Slot1", "Slot2", "Slot3" })
        {
            Transform child = root.transform.Find(childName);
            if (child != null) Undo.DestroyObjectImmediate(child.gameObject);
        }
        Text title = MakeText("Title", root.transform, font, "補血",
            new Vector2(16f, -6f), new Vector2(248f, 30f), 22);
        var slots = new Image[HealingSuppliesState.Capacity];
        for (int i = 0; i < slots.Length; i++)
        {
            RectTransform slot = MakeRect("Slot" + (i + 1), root.transform,
                new Vector2(16f + i * 84f, -40f), new Vector2(64f, 64f));
            Image slotBackground = slot.gameObject.AddComponent<Image>();
            slotBackground.color = new Color(0.09f, 0.07f, 0.09f, 1f);
            slotBackground.raycastTarget = false;
            Outline slotOutline = slot.gameObject.AddComponent<Outline>();
            slotOutline.effectColor = new Color(0.4f, 0.29f, 0.24f, 1f);
            slotOutline.effectDistance = new Vector2(1f, -1f);
            RectTransform iconRect = MakeRect("Icon", slot,
                new Vector2(8f, -8f), new Vector2(48f, 48f));
            Image slotIcon = iconRect.gameObject.AddComponent<Image>();
            slotIcon.sprite = icon;
            slotIcon.preserveAspect = true;
            slotIcon.color = new Color(0.45f, 0.4f, 0.4f, 1f);
            slotIcon.raycastTarget = false;
            slots[i] = slotIcon;
        }
        Text hint = MakeText("Hint", root.transform, font, "H 使用 · 回復 2 格",
            new Vector2(16f, -111f), new Vector2(248f, 26f), 17);
        hint.color = new Color(0.86f, 0.75f, 0.62f, 1f);
        HealingInventoryUI ui = root.GetComponent<HealingInventoryUI>();
        SetReference(ui, "useButton", button);
        SetReference(ui, "titleText", title);
        SetReference(ui, "hintText", hint);
        var uiData = new SerializedObject(ui);
        SerializedProperty slotArray = uiData.FindProperty("itemSlots");
        slotArray.arraySize = slots.Length;
        for (int i = 0; i < slots.Length; i++) slotArray.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
        uiData.ApplyModifiedPropertiesWithoutUndo();
        return ui;
    }

    private static RectTransform MakeRect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.layer = LayerMask.NameToLayer("UI");
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static Text MakeText(string name, Transform parent, Font font, string value,
        Vector2 position, Vector2 size, int fontSize)
    {
        RectTransform rect = MakeRect(name, parent, position, size);
        Text text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.text = value;
        text.color = new Color(1f, 0.9f, 0.73f, 1f);
        text.alignment = TextAnchor.MiddleLeft;
        text.raycastTarget = false;
        return text;
    }

    private static void Verify()
    {
        checks.Clear();
        var choices = new Queue<int>(new[] { 3, 4, 3 });
        var state = new HealingSuppliesState(3, 4, (min, max) => choices.Dequeue());
        Require(!state.RegisterNormalDefeat() && !state.RegisterNormalDefeat(), "First two kills do not drop.");
        Require(state.RegisterNormalDefeat() && state.NextDropAt == 4, "Third kill drops once and next cycle can require four.");
        Require(!state.RegisterNormalDefeat() && !state.RegisterNormalDefeat() && !state.RegisterNormalDefeat(), "Four-kill cycle waits through three kills.");
        Require(state.RegisterNormalDefeat() && state.KillsSinceDrop == 0, "Fourth kill drops and resets counter.");
        Require(!state.Store(0) && state.Store(2) && state.StoredCount == 2, "Only valid pickups add stored inventory.");
        Require(!state.Store(2) && state.Store(1) && !state.Store(1) && state.StoredCount == 3,
            "Inventory holds at most three items and rejects overflow without changing inventory.");
        Require(state.TryConsume() && state.TryConsume() && state.TryConsume() && !state.TryConsume(),
            "Stored inventory cannot become negative.");

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        InnerWorldHealingSystem installed = FindInScene<InnerWorldHealingSystem>(scene);
        HealingInventoryUI hud = FindInScene<HealingInventoryUI>(scene);
        Require(installed != null && hud != null && installed.HealAmount == 2 && installed.UseKey == KeyCode.H,
            "Scene system and HUD exist, with two-health restoration and H binding.");

        Scene testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            var playerObject = new GameObject("HealingTestPlayer");
            SceneManager.MoveGameObjectToScene(playerObject, testScene);
            Health health = playerObject.AddComponent<Health>();
            health.InitialHealth = health.MaximumHealth = 6f;
            health.SetHealth(6f);
            var host = new GameObject("HealingTestSpawner");
            SceneManager.MoveGameObjectToScene(host, testScene);
            InnerWorldDistanceSpawner spawner = host.AddComponent<InnerWorldDistanceSpawner>();
            InnerWorldHealingSystem system = host.AddComponent<InnerWorldHealingSystem>();
            SetReference(system, "spawner", spawner);
            SetReference(system, "playerHealth", health);
            SetReference(system, "pickupPrefab", AssetDatabase.LoadAssetAtPath<HealingItemPickup>(PickupPath));
            system.SendMessage("OnEnable", SendMessageOptions.RequireReceiver);
            Require(system.TryStorePickup(2) && health.CurrentHealth == 6f && system.StoredCount == 2,
                "Picking up items stores them without healing or consuming at full health.");
            Require(!system.TryUseItem() && system.StoredCount == 2, "Full health does not consume an item.");
            health.SetHealth(4f);
            Require(system.TryUseItem() && health.CurrentHealth == 6f && system.StoredCount == 1,
                "Using one item restores exactly two health and consumes one item.");
            health.SetHealth(5f);
            Require(system.TryUseItem() && health.CurrentHealth == 6f && system.StoredCount == 0,
                "Healing is capped at maximum health.");
            health.SetHealth(4f);
            Require(!system.TryUseItem() && health.CurrentHealth == 4f, "Empty inventory cannot heal.");
            system.TryStorePickup();
            health.SetHealth(0f);
            Require(!system.TryUseItem() && system.StoredCount == 1 && !system.TryStorePickup(),
                "Dead players cannot use or collect healing items.");
            health.SetHealth(4f);
            float previousTimeScale = Time.timeScale;
            try
            {
                Time.timeScale = 0f;
                Require(!system.TryUseItem() && system.StoredCount == 1, "Pause blocks item use without consumption.");
            }
            finally { Time.timeScale = previousTimeScale; }

            int notifications = 0;
            spawner.NormalMonsterDefeated += position => notifications++;
            var monsterObject = new GameObject("HealingTestNormalMonster");
            SceneManager.MoveGameObjectToScene(monsterObject, testScene);
            InnerWorldSpawnedMonster monster = monsterObject.AddComponent<InnerWorldSpawnedMonster>();
            monster.Initialize(spawner, InnerWorldDistanceSpawner.MonsterKind.Normal, -1, playerObject.transform, 3f);
            monster.NotifyDefeated();
            monster.NotifyDefeated();
            Require(notifications == 1, "A normal monster reports its death once even if notified twice.");
            var imageObject = new GameObject("HealingTestImageMonster");
            SceneManager.MoveGameObjectToScene(imageObject, testScene);
            InnerWorldSpawnedMonster image = imageObject.AddComponent<InnerWorldSpawnedMonster>();
            image.Initialize(spawner, InnerWorldDistanceSpawner.MonsterKind.Image, 0, playerObject.transform, 3f);
            image.NotifyDefeated();
            Require(notifications == 1, "Picture monsters do not advance healing drop count.");
            int remaining = system.NextDropAt - system.KillsSinceDrop;
            for (int i = 0; i < remaining; i++) spawner.ReportNormalDefeated(new Vector3(30f, 0f, 0f));
            Require(host.GetComponentsInChildren<HealingItemPickup>().Length == 1 && system.StoredCount == 1,
                "Reaching the normal-kill threshold spawns a world pickup, not instant inventory or healing.");

            HealingInventoryUI testHud = Object.Instantiate(hud, host.transform);
            testHud.SendMessage("OnDisable", SendMessageOptions.RequireReceiver);
            SetReference(testHud, "healingSystem", system);
            testHud.SendMessage("OnEnable", SendMessageOptions.RequireReceiver);
            Button useButton = testHud.GetComponent<Button>();
            Require(useButton.interactable, "HUD button is enabled while injured with a stored item.");
            useButton.onClick.Invoke();
            Require(health.CurrentHealth == 6f && system.StoredCount == 0 && !useButton.interactable,
                "Clicking the HUD consumes the stored item, heals, and refreshes availability.");
            var hudData = new SerializedObject(testHud);
            SerializedProperty slots = hudData.FindProperty("itemSlots");
            Color grey = hudData.FindProperty("emptyColor").colorValue;
            Color bright = hudData.FindProperty("readyColor").colorValue;
            Require(slots.arraySize == 3, "HUD has exactly three fixed item icons.");
            Require(SlotColor(slots, 0) == grey && SlotColor(slots, 1) == grey && SlotColor(slots, 2) == grey,
                "Empty inventory shows three grey icons.");
            system.TryStorePickup();
            Require(SlotColor(slots, 0) == bright && SlotColor(slots, 1) == grey && SlotColor(slots, 2) == grey,
                "One collected item lights only the first icon.");
            system.TryStorePickup(2);
            Require(SlotColor(slots, 0) == bright && SlotColor(slots, 1) == bright && SlotColor(slots, 2) == bright,
                "Three collected items light all three icons.");
            HealingItemPickup worldPickup = host.GetComponentInChildren<HealingItemPickup>();
            health.transform.position = worldPickup.transform.position;
            var pickupData = new SerializedObject(worldPickup);
            pickupData.FindProperty("collectDelay").floatValue = 0f;
            pickupData.ApplyModifiedPropertiesWithoutUndo();
            worldPickup.SendMessage("Update", SendMessageOptions.RequireReceiver);
            Require(worldPickup != null && worldPickup.gameObject.activeSelf && system.StoredCount == 3,
                "At capacity, a nearby world pickup stays on the ground.");
            health.SetHealth(4f);
            system.TryUseItem();
            Require(SlotColor(slots, 0) == bright && SlotColor(slots, 1) == bright && SlotColor(slots, 2) == grey,
                "Using an item greys the last occupied icon and preserves the first two.");
            testHud.SendMessage("OnDisable", SendMessageOptions.RequireReceiver);
            system.SendMessage("OnDisable", SendMessageOptions.RequireReceiver);
        }
        finally
        {
            EditorSceneManager.CloseScene(testScene, true);
            SceneManager.SetActiveScene(scene);
        }
    }

    public static void VerifyCustomSprites()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("請先停止 Play，再驗證自訂補血圖示。");
        checks.Clear();
        Scene previous = SceneManager.GetActiveScene();
        Scene testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(testScene);
        Sprite emptySprite = null;
        Sprite ownedSprite = null;
        HealingInventoryUI ui = null;
        try
        {
            emptySprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
            ownedSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
            Sprite fallback = AssetDatabase.LoadAssetAtPath<Sprite>(IconPath);
            var player = new GameObject("CustomIconTestPlayer");
            Health health = player.AddComponent<Health>();
            health.InitialHealth = health.MaximumHealth = 6f;
            health.SetHealth(6f);
            InnerWorldHealingSystem system = player.AddComponent<InnerWorldHealingSystem>();
            SetReference(system, "playerHealth", health);
            var root = new GameObject("CustomIconTestUI", typeof(RectTransform));
            ui = root.AddComponent<HealingInventoryUI>();
            SetReference(ui, "healingSystem", system);
            SetReference(ui, "emptyItemSprite", emptySprite);
            SetReference(ui, "ownedItemSprite", ownedSprite);
            var images = new Image[3];
            var data = new SerializedObject(ui);
            SerializedProperty slots = data.FindProperty("itemSlots");
            slots.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                var slot = new GameObject("Icon" + i, typeof(RectTransform), typeof(Image));
                slot.transform.SetParent(root.transform, false);
                images[i] = slot.GetComponent<Image>();
                images[i].sprite = fallback;
                slots.GetArrayElementAtIndex(i).objectReferenceValue = images[i];
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            ui.SendMessage("OnEnable", SendMessageOptions.RequireReceiver);
            Require(images[0].sprite == emptySprite && images[1].sprite == emptySprite && images[2].sprite == emptySprite,
                "Zero items displays the custom empty image in all three slots.");
            Require(images[0].color == Color.white && images[1].color == Color.white && images[2].color == Color.white,
                "Custom artwork keeps its original colors.");
            system.TryStorePickup();
            Require(images[0].sprite == ownedSprite && images[1].sprite == emptySprite && images[2].sprite == emptySprite,
                "Collecting one item swaps only the first slot to the custom owned image.");
            system.TryStorePickup(2);
            Require(images[0].sprite == ownedSprite && images[1].sprite == ownedSprite && images[2].sprite == ownedSprite,
                "Collecting three items displays the custom owned image in all slots.");
            health.SetHealth(4f);
            system.TryUseItem();
            Require(images[0].sprite == ownedSprite && images[1].sprite == ownedSprite && images[2].sprite == emptySprite,
                "Using one item restores the third slot's custom empty image.");
            SetReference(ui, "emptyItemSprite", null);
            ui.SendMessage("OnValidate", SendMessageOptions.RequireReceiver);
            ui.SendMessage("Refresh", SendMessageOptions.RequireReceiver);
            Require(images[0].sprite == ownedSprite && images[2].sprite == fallback,
                "Leaving the empty sprite unassigned safely restores the original slot image.");
            SetReference(ui, "ownedItemSprite", null);
            ui.SendMessage("OnValidate", SendMessageOptions.RequireReceiver);
            ui.SendMessage("Refresh", SendMessageOptions.RequireReceiver);
            Require(images[0].sprite == fallback && images[1].sprite == fallback && images[2].sprite == fallback,
                "Unassigned custom sprites preserve the original tint-based behavior.");
            Debug.Log("HEALING_CUSTOM_ICONS_VERIFICATION_PASS\n" + string.Join("\n", checks));
        }
        finally
        {
            if (ui != null) ui.SendMessage("OnDisable", SendMessageOptions.RequireReceiver);
            EditorSceneManager.CloseScene(testScene, true);
            if (emptySprite != null) Object.DestroyImmediate(emptySprite);
            if (ownedSprite != null) Object.DestroyImmediate(ownedSprite);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        }
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

    private static Color SlotColor(SerializedProperty slots, int index)
    {
        return ((Image)slots.GetArrayElementAtIndex(index).objectReferenceValue).color;
    }

    private static void SetReference(Object target, string property, Object value)
    {
        var data = new SerializedObject(target);
        SerializedProperty field = data.FindProperty(property);
        if (field == null) throw new InvalidOperationException(target.name + ": missing " + property);
        field.objectReferenceValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Healing verification failed: " + message);
        checks.Add(message);
    }

    private static string Argument(string name)
    {
        string[] arguments = Environment.GetCommandLineArgs();
        for (int i = 0; i < arguments.Length - 1; i++)
            if (arguments[i] == name) return arguments[i + 1];
        return null;
    }
}
#endif
