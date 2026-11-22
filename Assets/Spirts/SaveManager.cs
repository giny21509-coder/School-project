using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    public static event Action<GameSaveData> GameSaved;

    private const string DefaultSaveId = "autosave";
    private GameSaveData loadedData;
    private bool shouldApplyLoadedDataToScene = false;

    public string SavePath
    {
        get { return GetSavePath(DefaultSaveId); }
    }

    public string SaveFolderPath
    {
        get { return Path.Combine(Application.persistentDataPath, "Saves"); }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateSaveManager()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject saveManagerObject = new GameObject("SaveManager");
        saveManagerObject.AddComponent<SaveManager>();
        DontDestroyOnLoad(saveManagerObject);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Directory.CreateDirectory(SaveFolderPath);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
        {
            Debug.Log("Saving is now handled by SavePoint objects.");
        }

        if (Input.GetKeyDown(KeyCode.F9))
        {
            LoadGame(DefaultSaveId);
        }
    }

    public void SaveGame()
    {
        SaveGame(DefaultSaveId, "Autosave", string.Empty, null);
    }

    public void SaveAtPoint(string savePointId, string displayName, Vector3? savePosition = null)
    {
        string saveId = string.IsNullOrEmpty(savePointId) ? DefaultSaveId : savePointId;
        SaveGame(saveId, displayName, savePointId, savePosition);
    }

    private void SaveGame(string saveId, string displayName, string savePointId, Vector3? savePosition)
    {
        GameSaveData data = BuildSaveData();
        data.saveId = CleanSaveId(saveId);
        data.displayName = string.IsNullOrEmpty(displayName) ? data.saveId : displayName;
        data.savePointId = savePointId;
        data.savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        if (savePosition.HasValue)
        {
            data.playerPosition = new SaveVector3(savePosition.Value);
        }

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(GetSavePath(data.saveId), json);
        loadedData = data;

        Debug.Log("Game saved: " + GetSavePath(data.saveId));
        GameSaved?.Invoke(data);
    }

    public void LoadGame()
    {
        LoadGame(DefaultSaveId);
    }

    public void StartNewGame()
    {
        loadedData = null;
        shouldApplyLoadedDataToScene = false;
        GameProgressState.Clear();
        GameDataManager.LoadUnlockedItemIds(null);
        InventoryManager.ResetSession();
        PlayerPrefs.DeleteKey("NextSpawnId");
        Time.timeScale = 1f;
    }

    public bool TryReadSaveData(string saveId, out GameSaveData data)
    {
        data = null;
        try
        {
            string path = GetSavePath(saveId);
            if (!File.Exists(path)) return false;
            data = JsonUtility.FromJson<GameSaveData>(File.ReadAllText(path));
            if (data != null && !string.IsNullOrWhiteSpace(data.sceneName)
                && Application.CanStreamedLevelBeLoaded(data.sceneName)) return true;
            data = null;
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException
            || exception is ArgumentException)
        {
            Debug.LogWarning("Cannot read save " + saveId + ": " + exception.Message);
        }
        return false;
    }

    private void RestoreStoryProgress(GameSaveData data)
    {
        GameProgressState.Restore(data.storyProgress);
        if (data.formatVersion >= 2) return;
        // Old saves did not store these flags. Preserve their former preference values once.
        string[] introKeys = { "OfficeIntroFlowchartPlayed", "corridorIntroFlowchartPlayed",
            "NeighIntroFlowchartPlayed", "GrandmaIntroFlowchartPlayed" };
        foreach (string key in introKeys) GameProgressState.SetInt(key, PlayerPrefs.GetInt(key, 0));
        foreach (string scene in new[] { "office", "Neighborhood", "Neighborhood  Cat Run" })
            GameProgressState.SetInt("RoomIntroPlayed_" + scene, PlayerPrefs.GetInt("RoomIntroPlayed", 0));
        GameProgressState.SetInt("LightState", PlayerPrefs.GetInt("LightState", 1));
        foreach (string key in PlayerPrefs.GetString("OneTimeEvents_TriggeredList", "").Split('\n'))
            if (!string.IsNullOrWhiteSpace(key)) GameProgressState.SetInt("event/" + key.Trim(), 1);
    }

    public void LoadGame(string saveId)
    {
        if (!TryReadSaveData(saveId, out GameSaveData data))
        {
            Debug.LogWarning("Save is missing, invalid, or references a scene outside the build: " + saveId);
            return;
        }
        loadedData = data;
        RestoreStoryProgress(loadedData);
        PlayerPrefs.DeleteKey("NextSpawnId");

        GameDataManager.LoadUnlockedItemIds(loadedData.unlockedSceneItemIds);

        Time.timeScale = 1f;
        shouldApplyLoadedDataToScene = true;
        SceneManager.LoadScene(loadedData.sceneName);
    }

    public List<GameSaveData> GetAllSaves()
    {
        Directory.CreateDirectory(SaveFolderPath);
        List<GameSaveData> saves = new List<GameSaveData>();
        string[] files = Directory.GetFiles(SaveFolderPath, "*.json");

        for (int i = 0; i < files.Length; i++)
        {
            try
            {
                string json = File.ReadAllText(files[i]);
                GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);
                if (data != null && !string.IsNullOrEmpty(data.saveId))
                {
                    saves.Add(data);
                }
            }
            catch
            {
                Debug.LogWarning("Skip broken save file: " + files[i]);
            }
        }

        saves.Sort((a, b) => string.CompareOrdinal(b.savedAt, a.savedAt));
        return saves;
    }

    public string GetSavePath(string saveId)
    {
        Directory.CreateDirectory(SaveFolderPath);
        return Path.Combine(SaveFolderPath, CleanSaveId(saveId) + ".json");
    }

    public bool IsPickupCollected(string pickupId)
    {
        if (loadedData == null || loadedData.collectedPickupIds == null)
        {
            return false;
        }

        return loadedData.collectedPickupIds.Contains(pickupId);
    }

    public void MarkPickupCollected(string pickupId)
    {
        if (string.IsNullOrEmpty(pickupId))
        {
            return;
        }

        if (loadedData == null)
        {
            loadedData = BuildSaveData();
        }

        if (loadedData.collectedPickupIds == null)
        {
            loadedData.collectedPickupIds = new List<string>();
        }

        if (!loadedData.collectedPickupIds.Contains(pickupId))
        {
            loadedData.collectedPickupIds.Add(pickupId);
        }

        SyncLoadedInventoryFromCurrentScene();
    }

    public void MarkGhostCaptured(string ghostId)
    {
        if (string.IsNullOrEmpty(ghostId))
        {
            return;
        }

        if (loadedData == null)
        {
            loadedData = BuildSaveData();
        }

        if (loadedData.capturedGhostIds == null)
        {
            loadedData.capturedGhostIds = new List<string>();
        }

        if (!loadedData.capturedGhostIds.Contains(ghostId))
        {
            loadedData.capturedGhostIds.Add(ghostId);
        }
    }

    private void SyncLoadedInventoryFromCurrentScene()
    {
        if (loadedData == null)
        {
            return;
        }

        InventoryManager inventoryManager = InventoryManager.Instance;
        if (inventoryManager == null)
        {
            return;
        }

        loadedData.collectItemIds = inventoryManager.GetCollectItemIds();
        loadedData.backpackItemIds = inventoryManager.GetBackpackItemIds();
        loadedData.questItemIds = inventoryManager.GetQuestItemIds();
        loadedData.inventoryItemIds = loadedData.backpackItemIds;
    }

    public bool IsGhostCaptured(string ghostId)
    {
        if (loadedData == null || loadedData.capturedGhostIds == null)
        {
            return false;
        }

        return loadedData.capturedGhostIds.Contains(ghostId);
    }

    private GameSaveData BuildSaveData()
    {
        GameSaveData data = loadedData ?? new GameSaveData();
        data.formatVersion = 2;
        data.storyProgress = GameProgressState.Capture();
        data.sceneName = SceneManager.GetActiveScene().name;
        data.playerPosition = new SaveVector3(GetPlayerPosition());

        InventoryManager inventoryManager = InventoryManager.Instance;
        if (inventoryManager == null)
        {
            data.inventoryItemIds = new List<string>();
            data.collectItemIds = new List<string>();
            data.backpackItemIds = new List<string>();
            data.questItemIds = new List<string>();
        }
        else
        {
            data.collectItemIds = inventoryManager.GetCollectItemIds();
            data.backpackItemIds = inventoryManager.GetBackpackItemIds();
            data.questItemIds = inventoryManager.GetQuestItemIds();
            data.inventoryItemIds = data.backpackItemIds;
        }

        if (data.collectedPickupIds == null)
        {
            data.collectedPickupIds = new List<string>();
        }

        if (data.capturedGhostIds == null)
        {
            data.capturedGhostIds = new List<string>();
        }

        data.unlockedSceneItemIds = GameDataManager.GetUnlockedItemIds();

        return data;
    }

    private Vector3 GetPlayerPosition()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        return player == null ? Vector3.zero : player.transform.position;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (shouldApplyLoadedDataToScene && loadedData != null && scene.name == loadedData.sceneName)
        {
            shouldApplyLoadedDataToScene = false;
            StartCoroutine(ApplyLoadedDataNextFrame());
        }
    }

    private IEnumerator ApplyLoadedDataNextFrame()
    {
        yield return null;

        GameDataManager.LoadUnlockedItemIds(loadedData.unlockedSceneItemIds);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && loadedData.playerPosition != null)
        {
            player.transform.position = loadedData.playerPosition.ToVector3();
        }

        InventoryManager inventoryManager = InventoryManager.Instance;
        if (inventoryManager != null)
        {
            if (HasAnyItemId(loadedData.collectItemIds) ||
                HasAnyItemId(loadedData.backpackItemIds) ||
                HasAnyItemId(loadedData.questItemIds))
            {
                inventoryManager.LoadInventoryFromPageItemIds(
                    loadedData.collectItemIds,
                    loadedData.backpackItemIds,
                    loadedData.questItemIds,
                    ItemDatabase.Instance);
            }
            else
            {
                inventoryManager.LoadInventoryFromItemIds(loadedData.inventoryItemIds, ItemDatabase.Instance);
            }
        }
    }

    private bool HasAnyItemId(List<string> itemIds)
    {
        if (itemIds == null)
        {
            return false;
        }

        for (int i = 0; i < itemIds.Count; i++)
        {
            if (!string.IsNullOrEmpty(itemIds[i]))
            {
                return true;
            }
        }

        return false;
    }

    private string CleanSaveId(string saveId)
    {
        string cleanId = string.IsNullOrEmpty(saveId) ? DefaultSaveId : saveId.Trim();
        foreach (char invalidChar in Path.GetInvalidFileNameChars())
        {
            cleanId = cleanId.Replace(invalidChar, '_');
        }

        return cleanId;
    }
}
