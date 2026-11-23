using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 依 Zone 生成填充怪（設計文件 §4.5、§5）：間隔與同時存活上限隨 E 變化。
/// 在鏡頭外的左右兩側、地板帶（Y 範圍）內生成。由 ZoneGate 啟用／停用。
/// </summary>
public class FillerSpawner : MonoBehaviour
{
    [Serializable]
    public class WeightedFiller
    {
        public GameObject prefab;
        [Min(0f)] public float weight = 1f;
    }

    [SerializeField] private EmptinessMeter meter;
    [SerializeField] private Transform player;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private WeightedFiller[] pool = new WeightedFiller[0];

    [Header("生成位置")]
    [Tooltip("地板帶的 Y 範圍（與 PlayerController 的 minY／maxY 一致）")]
    [SerializeField] private float floorMinY = -3f, floorMaxY = -1f;
    [Tooltip("鏡頭邊緣外多遠生成")]
    [SerializeField] private float offscreenMargin = 1.5f;
    [Tooltip("本 Zone 的 X 範圍，避免生到 Zone 外")]
    [SerializeField] private float zoneMinX = -999f, zoneMaxX = 999f;

    private readonly List<GameObject> alive = new List<GameObject>();
    private float timer;

    private void OnEnable() => timer = 0f;

    private void Update()
    {
        if (meter == null || player == null || Time.timeScale <= 0f) return;
        alive.RemoveAll(go => go == null);
        timer += Time.deltaTime;
        if (timer < meter.Config.SpawnInterval(meter.Normalized)) return;
        timer = 0f;
        if (alive.Count >= meter.Config.MaxAlive(meter.Value)) return;
        SpawnOne();
    }

    private void SpawnOne()
    {
        GameObject prefab = PickWeighted();
        if (prefab == null) return;
        Camera cam = worldCamera != null ? worldCamera : Camera.main;
        float halfWidth = cam != null ? cam.orthographicSize * cam.aspect : 8f;
        float side = UnityEngine.Random.value < 0.5f ? -1f : 1f;
        float x = player.position.x + side * (halfWidth + offscreenMargin);
        if (x < zoneMinX || x > zoneMaxX) x = player.position.x - side * (halfWidth + offscreenMargin);
        x = Mathf.Clamp(x, zoneMinX, zoneMaxX);
        float y = UnityEngine.Random.Range(floorMinY, floorMaxY);
        alive.Add(Instantiate(prefab, new Vector3(x, y, 0f), Quaternion.identity, transform));
    }

    private GameObject PickWeighted()
    {
        float total = 0f;
        foreach (var w in pool) if (w != null && w.prefab != null) total += w.weight;
        if (total <= 0f) return null;
        float r = UnityEngine.Random.value * total;
        foreach (var w in pool)
        {
            if (w == null || w.prefab == null) continue;
            r -= w.weight;
            if (r <= 0f) return w.prefab;
        }
        return null;
    }

    /// <summary>Zone 完成或玩家死亡時清場。</summary>
    public void DespawnAll()
    {
        foreach (var go in alive) if (go != null) Destroy(go);
        alive.Clear();
    }
}
