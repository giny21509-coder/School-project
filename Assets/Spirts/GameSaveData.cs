using System;
using System.Collections.Generic;

[Serializable]
public class GameSaveData
{
    public int formatVersion;
    public List<StoryProgressValue> storyProgress = new List<StoryProgressValue>();
    public string saveId;
    public string displayName;
    public string savedAt;
    public string savePointId;
    public string sceneName;
    public SaveVector3 playerPosition;
    public List<string> inventoryItemIds = new List<string>();
    public List<string> collectItemIds = new List<string>();
    public List<string> backpackItemIds = new List<string>();
    public List<string> questItemIds = new List<string>();
    public List<string> collectedPickupIds = new List<string>();
    public List<string> capturedGhostIds = new List<string>();
    public List<string> unlockedSceneItemIds = new List<string>();
}

[Serializable]
public class SaveVector3
{
    public float x;
    public float y;
    public float z;

    public SaveVector3()
    {
    }

    public SaveVector3(UnityEngine.Vector3 vector)
    {
        x = vector.x;
        y = vector.y;
        z = vector.z;
    }

    public UnityEngine.Vector3 ToVector3()
    {
        return new UnityEngine.Vector3(x, y, z);
    }
}
