using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RoomFlowchartImageBuildFix : MonoBehaviour
{
    private class MirrorEntry
    {
        public GameObject source;
        public GameObject mirror;
    }

    private static readonly Vector2 PopupSize = new Vector2(760f, 428f);
    private static readonly Vector2 PopupOffset = new Vector2(0f, 135f);

    private readonly List<MirrorEntry> mirrors = new List<MirrorEntry>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        InstallForScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InstallForScene(scene);
    }

    private static void InstallForScene(Scene scene)
    {
        if (scene.name != "room")
        {
            return;
        }

        GameObject runnerObject = new GameObject("RoomFlowchartImageBuildFix");
        RoomFlowchartImageBuildFix runner = runnerObject.AddComponent<RoomFlowchartImageBuildFix>();
        runner.Setup(scene);
    }

    private void Setup(Scene scene)
    {
        GameObject canvasObject = FindSceneObject(scene, "相機照片");
        if (canvasObject == null)
        {
            Debug.LogWarning("RoomFlowchartImageBuildFix: 相機照片 Canvas not found.");
            return;
        }

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 50;
        }

        CreateMirror(scene, canvasObject.transform, "相機關起來");
        CreateMirror(scene, canvasObject.transform, "相機打開");
        CreateMirror(scene, canvasObject.transform, "相機說明書");
    }

    private void CreateMirror(Scene scene, Transform canvasTransform, string sourceName)
    {
        GameObject source = FindSceneObject(scene, sourceName);
        if (source == null)
        {
            Debug.LogWarning("RoomFlowchartImageBuildFix: source not found: " + sourceName);
            return;
        }

        SpriteRenderer spriteRenderer = source.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null || spriteRenderer.sprite == null)
        {
            Debug.LogWarning("RoomFlowchartImageBuildFix: source has no sprite: " + sourceName);
            return;
        }

        spriteRenderer.enabled = false;

        GameObject mirror = new GameObject(sourceName + " UI Mirror", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        mirror.transform.SetParent(canvasTransform, false);

        RectTransform rectTransform = mirror.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = PopupOffset;
        rectTransform.localScale = Vector3.one;
        rectTransform.sizeDelta = PopupSize;

        Image image = mirror.GetComponent<Image>();
        image.sprite = spriteRenderer.sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;

        mirrors.Add(new MirrorEntry { source = source, mirror = mirror });
        mirror.SetActive(source.activeSelf);
    }

    private void LateUpdate()
    {
        for (int i = 0; i < mirrors.Count; i++)
        {
            MirrorEntry entry = mirrors[i];
            if (entry.source == null || entry.mirror == null)
            {
                continue;
            }

            entry.mirror.SetActive(entry.source.activeSelf);
        }
    }

    private static GameObject FindSceneObject(Scene scene, string objectName)
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject candidate = allObjects[i];
            if (candidate.scene == scene && candidate.name == objectName)
            {
                return candidate;
            }
        }

        return null;
    }
}


