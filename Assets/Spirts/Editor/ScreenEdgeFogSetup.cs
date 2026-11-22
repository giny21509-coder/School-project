#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

public static class ScreenEdgeFogSetup
{
    private const string PrefabPath = "Assets/prefeb/特效/FogMAin 1.prefab";
    private const string MaterialPath = "Assets/Shaders/ScreenEdgeFog.mat";

    [MenuItem("Tools/Inner World/Install Screen Edge Fog")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("請先停止 Play，再安裝畫面四周煙霧。");
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/ScreenEdgeFog.shader");
        if (shader == null || ShaderUtil.ShaderHasError(shader))
            throw new InvalidOperationException("Screen Edge Fog shader 尚未成功匯入。");
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        GameObject prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            SpriteRenderer renderer = prefab.GetComponent<SpriteRenderer>();
            if (renderer == null || renderer.sprite == null)
                throw new InvalidOperationException("FogMAin 1 需要有圖片的 Sprite Renderer。");
            renderer.sharedMaterial = material;
            if (prefab.GetComponent<ScreenEdgeFog>() == null) prefab.AddComponent<ScreenEdgeFog>();
            PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }
        AssetDatabase.SaveAssets();
        Debug.Log("四周煙霧已安裝到 FogMAin 1；自動跟隨鏡頭，每打倒一隻圖片怪，於 2.5 秒內消退一階段。原本 RollingFog 材質保持不變。");
    }
}
#endif
