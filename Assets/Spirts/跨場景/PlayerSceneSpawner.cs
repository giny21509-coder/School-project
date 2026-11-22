using UnityEngine;

public class PlayerSceneSpawner : MonoBehaviour
{
    void Start()
    {
        string nextSpawnId = PlayerPrefs.GetString("NextSpawnId", "");

        if (string.IsNullOrEmpty(nextSpawnId))
        {
            return;
        }

        SceneSpawnPoint[] spawnPoints = FindObjectsByType<SceneSpawnPoint>(FindObjectsSortMode.None);

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i].spawnId == nextSpawnId)
            {
                Vector3 spawnPosition = spawnPoints[i].transform.position;
                transform.position = spawnPosition;

                Rigidbody2D rb = GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.position = spawnPosition;
                    rb.linearVelocity = Vector2.zero;
                }

                break;
            }
        }

        PlayerPrefs.DeleteKey("NextSpawnId");
    }
}