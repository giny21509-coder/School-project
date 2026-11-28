using System.Collections.Generic;
using Fungus;
using MoreMountains.Tools;
using MoreMountains.TopDownEngine;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class InnerWorldEncounterTutorial : MonoBehaviour
{
    private const string NormalKey = "IW_Tutorial_Normal";
    private const string ShieldKey = "IW_Tutorial_Shield";

    [Header("場景引用")]
    [SerializeField] private PlayerController player;
    [SerializeField] private SpiritCameraFlash spiritFlash;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Flowchart tutorialFlowchart;
    [SerializeField] private TMP_Text hintText;

    [Header("遇敵判定")]
    [SerializeField] private bool startWatching;
    [SerializeField, Min(1f)] private float encounterDistance = 6f;
    [SerializeField, Min(1f)] private float cancelDistance = 20f;
    [Tooltip("只選牆壁或視線障礙物")]
    [SerializeField] private LayerMask obstacleLayers;

    private bool watching, shieldLesson, explainedBreak, defeated, lockedPlayer;
    private float nextSearch;
    private enum Stage { Searching, Dialogue, Practice, Complete }
    private Stage stage;
    private InnerWorldSpawnedMonster target;
    private InnerWorldMistShield shield;
    private Health targetHealth;
    private Chatacher legacyHealth;
    private Block speaking;
    private readonly List<Behaviour> paused = new List<Behaviour>();
    private readonly Dictionary<Rigidbody2D, RigidbodyConstraints2D> frozen =
        new Dictionary<Rigidbody2D, RigidbodyConstraints2D>();

    public void BeginWatching() => watching = true;

    private void Start()
    {
        watching |= startWatching;
        ShowHint("");
        foreach (string blockName in new[] { "IW_NormalIntro", "IW_ShieldIntro", "IW_ShieldBroken" })
        {
            if (tutorialFlowchart != null && tutorialFlowchart.FindBlock(blockName) != null) continue;
            Debug.LogError("遇敵教學：請指定包含 " + blockName + " 的 Tutorial Flowchart。", this);
            enabled = false;
            return;
        }
    }

    private void Update()
    {
        if (!watching || Time.timeScale <= 0f) return;
        switch (stage)
        {
            case Stage.Searching:
                if (Time.time < nextSearch) return;
                nextSearch = Time.time + 0.2f;
                FindEncounter();
                break;
            case Stage.Dialogue:
                if (speaking != null && speaking.IsExecuting() && tutorialFlowchart.isActiveAndEnabled) return;
                speaking = null;
                RestoreControl();
                stage = Stage.Practice;
                break;
            case Stage.Practice:
                Practice();
                break;
            case Stage.Complete:
                if (Time.time >= nextSearch) ResetEncounter();
                break;
        }
    }

    private void FindEncounter()
    {
        bool needNormal = GameProgressState.GetInt(NormalKey) == 0;
        bool needShield = GameProgressState.GetInt(ShieldKey) == 0;
        if ((!needNormal && !needShield) || PrologueExplorationGuide.IsDialogueRunning) return;
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        if (spiritFlash == null) spiritFlash = FindFirstObjectByType<SpiritCameraFlash>();
        if (worldCamera == null) worldCamera = Camera.main;
        if (worldCamera == null || player == null || !player.canMove || spiritFlash == null ||
            !spiritFlash.isActiveAndEnabled || !tutorialFlowchart.isActiveAndEnabled) return;
        float closest = encounterDistance;
        foreach (var monster in FindObjectsByType<InnerWorldSpawnedMonster>(FindObjectsSortMode.None))
        {
            if (monster.gameObject.scene != gameObject.scene || monster.IsDefeated) continue;
            bool normal = monster.Kind == InnerWorldDistanceSpawner.MonsterKind.Normal;
            if (normal ? !needNormal : !needShield) continue;
            if (!normal && (!monster.TryGetComponent(out InnerWorldMistShield mist) || !mist.IsShielded)) continue;
            Vector3 pos = monster.transform.position;
            float distance = Vector2.Distance(player.transform.position, pos);
            if (distance > closest) continue;
            Vector3 view = worldCamera.WorldToViewportPoint(pos);
            if (view.z <= 0 || view.x < 0 || view.x > 1 || view.y < 0 || view.y > 1) continue;
            if (Physics2D.Linecast(player.transform.position, pos, obstacleLayers).collider != null) continue;
            closest = distance;
            target = monster;
        }
        if (target == null) return;
        shieldLesson = target.Kind != InnerWorldDistanceSpawner.MonsterKind.Normal;
        shield = target.GetComponent<InnerWorldMistShield>();
        targetHealth = target.GetComponent<Health>();
        if (targetHealth != null && targetHealth.isActiveAndEnabled)
            targetHealth.OnDeath += OnDefeated;
        else
        {
            legacyHealth = target.GetComponent<Chatacher>();
            if (legacyHealth != null) legacyHealth.OnDie?.AddListener(OnDefeated);
        }
        PlayBlock(shieldLesson ? "IW_ShieldIntro" : "IW_NormalIntro");
    }

    private void OnDefeated() => defeated = true;

    private void Practice()
    {
        if (defeated || (target != null && target.IsDefeated))
        {
            GameProgressState.SetInt(shieldLesson ? ShieldKey : NormalKey, 1);
            ShowHint(shieldLesson ? "已學會蓄力破盾！" : "已學會閃光照射！");
            stage = Stage.Complete;
            nextSearch = Time.time + 1.5f;
            return;
        }
        if (target == null || !target.isActiveAndEnabled || player == null || spiritFlash == null ||
            Vector2.Distance(player.transform.position, target.transform.position) > Mathf.Max(cancelDistance, encounterDistance))
        {
            ResetEncounter();
            return;
        }
        if (!player.canMove || PrologueExplorationGuide.IsDialogueRunning)
        {
            ShowHint("");
            return;
        }
        if (shieldLesson && !explainedBreak && shield != null && !shield.IsShielded)
        {
            explainedBreak = true;
            PlayBlock("IW_ShieldBroken");
        }
        else if (!spiritFlash.IsPhotoMode) ShowHint("按 F 開啟拍攝模式。");
        else if (shieldLesson && shield != null && shield.IsShielded)
            ShowHint($"瞄準，按住左鍵 3 秒後放開。　有效命中 {shield.FlashHits}／{shield.FlashesToBreak}");
        else ShowHint("移動滑鼠瞄準怪物，再點擊左鍵。持續照射直到怪物消散。");
    }

    private void PlayBlock(string blockName)
    {
        ShowHint("");
        speaking = tutorialFlowchart.FindBlock(blockName);
        stage = Stage.Dialogue;
        lockedPlayer = player.canMove;
        player.StopControl();
        foreach (var monster in FindObjectsByType<InnerWorldSpawnedMonster>(FindObjectsSortMode.None))
        {
            if (monster.gameObject.scene != gameObject.scene || monster.IsDefeated) continue;
            foreach (var script in monster.GetComponentsInChildren<MonoBehaviour>())
            {
                if (script is Enemy || script is AIBrain || script is CharacterMovement ||
                    script is DamageOnTouch || script is Weapon || script is Attack)
                    Pause(script);
            }
            foreach (var body in monster.GetComponentsInChildren<Rigidbody2D>())
            {
                if (frozen.ContainsKey(body)) continue;
                frozen.Add(body, body.constraints);
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
                body.constraints = RigidbodyConstraints2D.FreezeAll;
            }
        }
        foreach (var spawner in FindObjectsByType<InnerWorldDistanceSpawner>(FindObjectsSortMode.None))
            if (spawner.gameObject.scene == gameObject.scene) Pause(spawner);
        if (!tutorialFlowchart.ExecuteBlock(speaking)) ResetEncounter();
    }

    private void Pause(Behaviour component)
    {
        if (!component.enabled) return;
        paused.Add(component);
        component.enabled = false;
    }

    private void RestoreControl()
    {
        foreach (var pair in frozen)
            if (pair.Key != null) pair.Key.constraints = pair.Value;
        frozen.Clear();
        foreach (var component in paused)
            if (component != null) component.enabled = true;
        paused.Clear();
        if (lockedPlayer && player != null) player.StarControl();
        lockedPlayer = false;
    }

    private void ShowHint(string message)
    {
        if (hintText == null) return;
        if (hintText.text != message) hintText.text = message;
        bool visible = message.Length > 0;
        if (hintText.gameObject.activeSelf != visible) hintText.gameObject.SetActive(visible);
    }

    private void ResetEncounter()
    {
        if (targetHealth != null) targetHealth.OnDeath -= OnDefeated;
        if (legacyHealth != null) legacyHealth.OnDie?.RemoveListener(OnDefeated);
        targetHealth = null;
        legacyHealth = null;
        target = null;
        shield = null;
        speaking = null;
        defeated = false;
        explainedBreak = false;
        RestoreControl();
        ShowHint("");
        stage = Stage.Searching;
    }

    private void OnDisable()
    {
        if (speaking != null && speaking.IsExecuting()) speaking.Stop();
        ResetEncounter();
    }

    [ContextMenu("Reset Tutorial Progress")]
    private void ResetTutorialProgress()
    {
        GameProgressState.DeleteKey(NormalKey);
        GameProgressState.DeleteKey(ShieldKey);
    }
}
