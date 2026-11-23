using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class SpiritFlashAimPreview : MonoBehaviour
{
    private const int ArcSegments = 48;
    private static readonly int BeamColorId = Shader.PropertyToID("_BeamColor");

    [Header("Spotlight Appearance")]
    [SerializeField] private Color readyFill = new Color(1f, 0.97f, 0.85f, 0.42f);
    [SerializeField] private Color cooldownFill = new Color(0.55f, 0.65f, 0.7f, 0.12f);
    [SerializeField] private bool illuminateSprites = true;
    [SerializeField, Range(0f, 3f)] private float lightIntensity = 1.2f;
    [SerializeField, Min(8f)] private float reticlePixels = 36f;

    private GameObject root;
    private Transform sector;
    private Mesh mesh;
    private MeshRenderer fill;
    private Light2D spotlight;
    private Transform reticle;
    private LineRenderer[] reticleCorners;
    private MaterialPropertyBlock propertyBlock;
    private readonly Vector3[] vertices = new Vector3[ArcSegments + 2];
    private readonly Vector2[] uv = new Vector2[ArcSegments + 2];

    public void Show(Vector3 origin, Vector2 aimPosition, Camera camera, float range, float angle,
        LayerMask obstacles, SpriteRenderer source, bool ready, bool flashing)
    {
        Vector2 direction = aimPosition - (Vector2)origin;
        if (!isActiveAndEnabled || camera == null || direction.sqrMagnitude < 0.0001f)
        {
            Hide();
            return;
        }
        if (!Prepare()) return;
        root.SetActive(true);
        root.transform.SetPositionAndRotation(origin, Quaternion.identity);
        float rotation = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        sector.rotation = Quaternion.Euler(0f, 0f, rotation);
        range = Mathf.Max(0.1f, range);
        angle = Mathf.Clamp(angle, 1f, 180f);

        vertices[0] = Vector3.zero;
        uv[0] = new Vector2(0f, 0.5f);
        if (obstacles.value != 0) Physics2D.SyncTransforms();
        for (int i = 0; i <= ArcSegments; i++)
        {
            float fraction = (float)i / ArcSegments;
            float radians = Mathf.Lerp(-angle * 0.5f, angle * 0.5f, fraction) * Mathf.Deg2Rad;
            Vector3 localDirection = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);
            float distance = range;
            if (obstacles.value != 0)
            {
                Vector2 worldDirection = sector.TransformDirection(localDirection);
                RaycastHit2D hit = Physics2D.Raycast(origin, worldDirection, range, obstacles);
                if (hit.collider != null) distance = hit.distance;
            }
            vertices[i + 1] = localDirection * distance;
            float reach = distance / range;
            uv[i + 1] = new Vector2(reach, 0.5f + (fraction - 0.5f) * reach);
        }
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.RecalculateBounds();

        int layer = source != null ? source.sortingLayerID : 0;
        int order = source != null ? source.sortingOrder : 0;
        fill.sortingLayerID = layer;
        fill.sortingOrder = order + 1;
        Color beamColor = flashing ? new Color(1f, 1f, 0.95f, 0.65f) : ready ? readyFill : cooldownFill;
        propertyBlock.SetColor(BeamColorId, beamColor);
        fill.SetPropertyBlock(propertyBlock);

        // Light2D uses local up as the center of its cone; the beam mesh points right.
        spotlight.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
        spotlight.pointLightOuterRadius = range;
        spotlight.pointLightInnerRadius = range * 0.05f;
        spotlight.pointLightOuterAngle = angle;
        spotlight.pointLightInnerAngle = angle * 0.45f;
        spotlight.color = new Color(beamColor.r, beamColor.g, beamColor.b, 1f);
        spotlight.intensity = lightIntensity * (flashing ? 1.5f : ready ? 1f : 0.25f);
        // Collider-only obstacles lack shadow casters; keep their clipped mesh authoritative.
        spotlight.enabled = illuminateSprites && obstacles.value == 0;

        if (reticle != null)
        {
            reticle.position = new Vector3(aimPosition.x, aimPosition.y, origin.z);
            reticle.rotation = camera.transform.rotation;
            float depth = Mathf.Abs(Vector3.Dot(reticle.position - camera.transform.position, camera.transform.forward));
            float viewHeight = camera.orthographic ? camera.orthographicSize * 2f
                : depth * Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * 2f;
            float pixelSize = viewHeight / Mathf.Max(1, camera.pixelHeight);
            float halfSize = pixelSize * reticlePixels * 0.5f;
            float cornerLength = halfSize * 0.6f;
            bool inRange = Vector2.Distance(origin, aimPosition) <= range;
            Color color = flashing ? Color.white : ready && inRange
                ? new Color(1f, 0.97f, 0.85f, 1f) : new Color(0.6f, 0.7f, 0.75f, 0.75f);
            for (int i = 0; i < reticleCorners.Length; i++)
            {
                float x = i % 2 == 0 ? -1f : 1f;
                float y = i < 2 ? -1f : 1f;
                var corner = new Vector3(x * halfSize, y * halfSize, 0f);
                var line = reticleCorners[i];
                line.startColor = line.endColor = color;
                line.startWidth = line.endWidth = pixelSize * 1.5f;
                line.SetPosition(0, corner - new Vector3(x * cornerLength, 0f, 0f));
                line.SetPosition(1, corner);
                line.SetPosition(2, corner - new Vector3(0f, y * cornerLength, 0f));
            }
        }
    }

    private bool Prepare()
    {
        if (root != null) return true;
        Material rangeMaterial = Resources.Load<Material>("InnerWorldCombat/SpotlightBeam");
        if (rangeMaterial == null) return false;
        root = new GameObject("SpiritFlashAimPreview");
        SceneManager.MoveGameObjectToScene(root, gameObject.scene);
        var sectorObject = new GameObject("SpotlightBeam");
        sector = sectorObject.transform;
        sector.SetParent(root.transform, false);
        mesh = new Mesh { name = "SpiritSpotlightRange" };
        mesh.MarkDynamic();
        mesh.vertices = vertices;
        var triangles = new int[ArcSegments * 3];
        for (int i = 0; i < ArcSegments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }
        mesh.triangles = triangles;
        sectorObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        fill = sectorObject.AddComponent<MeshRenderer>();
        fill.sharedMaterial = rangeMaterial;
        fill.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        fill.receiveShadows = false;
        propertyBlock = new MaterialPropertyBlock();

        bool hasSceneLighting = false;
        foreach (Light2D light in Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None))
            if (light.isActiveAndEnabled) { hasSceneLighting = true; break; }
        if (!hasSceneLighting)
        {
            // URP renders lit sprites at full brightness when no 2D lights exist.
            var ambientObject = new GameObject("SpotlightBaselineLight");
            ambientObject.transform.SetParent(root.transform, false);
            var ambient = ambientObject.AddComponent<Light2D>();
            ambient.lightType = Light2D.LightType.Global;
            ambient.intensity = 1f;
        }

        var lightObject = new GameObject("SpiritSpotlight");
        lightObject.transform.SetParent(sector, false);
        spotlight = lightObject.AddComponent<Light2D>();
        spotlight.lightType = Light2D.LightType.Point;
        spotlight.falloffIntensity = 0.8f;
        spotlight.volumetricEnabled = false;

        Material reticleMaterial = Resources.Load<Material>("InnerWorldCombat/Reticle");
        if (reticleMaterial != null)
        {
            var reticleObject = new GameObject("Aim focus frame");
            reticle = reticleObject.transform;
            reticle.SetParent(root.transform, false);
            reticleCorners = new LineRenderer[4];
            SortingLayer[] layers = SortingLayer.layers;
            int topLayer = layers[layers.Length - 1].id;
            for (int i = 0; i < reticleCorners.Length; i++)
            {
                var cornerObject = new GameObject("Focus corner " + (i + 1));
                cornerObject.transform.SetParent(reticle, false);
                var line = cornerObject.AddComponent<LineRenderer>();
                line.sharedMaterial = reticleMaterial;
                line.useWorldSpace = false;
                line.positionCount = 3;
                line.numCornerVertices = 2;
                line.sortingLayerID = topLayer;
                line.sortingOrder = 32000;
                reticleCorners[i] = line;
            }
        }
        return true;
    }

    public void Hide()
    {
        if (root != null) root.SetActive(false);
    }

    private void OnDisable() { Hide(); }

    private static void DestroyOwned(Object value)
    {
        if (value == null) return;
        if (Application.isPlaying) Destroy(value);
        else DestroyImmediate(value);
    }

    private void OnDestroy()
    {
        DestroyOwned(root);
        DestroyOwned(mesh);
    }
}
