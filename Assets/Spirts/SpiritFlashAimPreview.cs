using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class SpiritFlashAimPreview : MonoBehaviour
{
    private const int ArcSegments = 96;
    private static readonly int BeamColorId = Shader.PropertyToID("_BeamColor");

    private static readonly int FloorRectsId = Shader.PropertyToID("_FloorRects");
    private static readonly int FloorRectCountId = Shader.PropertyToID("_FloorRectCount");

    [Header("Spotlight Appearance")]
    [SerializeField] private Color readyFill = new Color(1f, 0.97f, 0.85f, 0.42f);
    [SerializeField] private Color cooldownFill = new Color(0.55f, 0.65f, 0.7f, 0.12f);
    [SerializeField] private bool illuminateSprites = true;
    [SerializeField, Range(0f, 3f)] private float lightIntensity = 1.2f;
    [SerializeField, Min(8f)] private float reticlePixels = 36f;

    [Header("Dashed Range Boundaries")]
    [SerializeField] private Color readyLine = new Color(1f, 0.97f, 0.85f, 0.95f);
    [SerializeField] private Color cooldownLine = new Color(0.6f, 0.7f, 0.75f, 0.5f);
    [SerializeField, Min(0.01f)] private float dashLength = 0.25f;
    [SerializeField, Min(0.01f)] private float dashGap = 0.18f;
    [SerializeField, Min(0.005f)] private float lineWidth = 0.035f;
    [SerializeField, Min(0f)] private float spiritClearance = 0.06f;

    private GameObject root;
    private Transform footprint;
    private Mesh mesh;
    private MeshRenderer fill;
    private Mesh aimBoundaryMesh;
    private MeshRenderer aimBoundaries;
    private readonly List<Vector3> boundaryVertices = new List<Vector3>(256);
    private readonly List<int> boundaryTriangles = new List<int>(384);
    private readonly List<Color> boundaryColors = new List<Color>(256);
    private Light2D spotlight;
    private Transform reticle;
    private LineRenderer[] reticleCorners;
    private MaterialPropertyBlock propertyBlock;
    private readonly Vector3[] vertices = new Vector3[ArcSegments + 2];
    private readonly Vector2[] uv = new Vector2[ArcSegments + 2];

    public void Show(Vector3 origin, Vector2 aimPosition, Camera camera, float range, float angle,
        LayerMask obstacles, SpriteRenderer source, bool ready, bool flashing, float chargeProgress = 0f)
    {
        ShowGround(new FlashGroundRange(origin, 1f, range), angle, origin,
            aimPosition, camera, obstacles, source, ready, flashing, chargeProgress);
    }

    internal void ShowGround(FlashGroundRange area, float angle, Vector3 origin, Vector2 aimPosition, Camera camera,
        LayerMask obstacles, SpriteRenderer source, bool ready, bool flashing, float chargeProgress,
        float groundScale = 1f, bool showAim = true)
    {
        Vector2 direction = aimPosition - (Vector2)origin;
        if (!isActiveAndEnabled || camera == null)
        {
            Hide();
            return;
        }
        if (!Prepare()) return;
        root.SetActive(true);
        root.transform.SetPositionAndRotation(origin, Quaternion.identity);
        float rotation = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        footprint.position = new Vector3(area.Origin.x, area.Origin.y, origin.z);
        footprint.localScale = new Vector3(1f, area.DepthScale, 1f);
        float range = area.Range;
        float groundRadius = range * groundScale;

        vertices[0] = Vector3.zero;
        uv[0] = Vector2.zero;
        if (obstacles.value != 0) Physics2D.SyncTransforms();
        for (int i = 0; i <= ArcSegments; i++)
        {
            float fraction = (float)i / ArcSegments;
            float radians = fraction * 2f * Mathf.PI;
            Vector3 localDirection = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f);
            float reach = 1f;
            if (obstacles.value != 0)
            {
                Vector2 endpoint = area.Origin + area.ProjectOffset(localDirection * groundRadius);
                RaycastHit2D hit = Physics2D.Linecast(area.Origin, endpoint, obstacles);
                if (hit.collider != null) reach = hit.fraction;
            }
            vertices[i + 1] = localDirection * (groundRadius * reach);
            uv[i + 1] = (Vector2)localDirection * reach;
        }
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.RecalculateBounds();

        Color beamColor = flashing ? new Color(1f, 1f, 0.95f, 0.65f) : ready ? readyFill : cooldownFill;
        propertyBlock.SetColor(BeamColorId, new Color(1f, 1f, 1f, beamColor.a));
        propertyBlock.SetFloat(FloorRectCountId, area.FloorRects == null ? -1 : area.FloorRectCount);
        if (area.FloorRectCount > 0) propertyBlock.SetVectorArray(FloorRectsId, area.FloorRects);
        fill.SetPropertyBlock(propertyBlock);

        Color lineColor = flashing ? Color.white : ready ? readyLine : cooldownLine;
        float startDistance = Mathf.Max(0f, spiritClearance);
        if (source != null && source.sprite != null)
        {
            // Recheck animated world bounds so moving, flipping and scaling stay clear.
            Bounds bounds = source.bounds;
            startDistance += ((Vector2)bounds.extents).magnitude
                + Vector2.Distance(origin, bounds.center);
        }
        startDistance += Mathf.Max(0.005f, lineWidth) * 0.5f;
        // The spirit's sight lines live in world space above the separate floor footprint.
        aimBoundaries.enabled = showAim && direction.sqrMagnitude >= 0.0001f;
        if (aimBoundaries.enabled)
        {
            aimBoundaries.sortingLayerID = source != null ? source.sortingLayerID : SortingLayer.NameToID("Characters");
            aimBoundaries.sortingOrder = source != null ? source.sortingOrder + 4 : 4;
            for (int side = -1; side <= 1; side += 2)
            {
                float radians = (rotation + side * angle * 0.5f) * Mathf.Deg2Rad;
                Vector2 ray = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
                float distance = range;
                if (obstacles.value != 0)
                {
                    RaycastHit2D hit = Physics2D.Raycast(origin, ray, range, obstacles);
                    if (hit.collider != null) distance = hit.distance;
                }
                AddDashedBoundary(ray * distance, lineColor, startDistance);
            }
            UploadBoundaries(aimBoundaryMesh);
        }

        // Light2D uses local up as the center of its cone; the beam mesh points right.
        spotlight.transform.localRotation = Quaternion.Euler(0f, 0f, rotation - 90f);
        spotlight.pointLightOuterRadius = range;
        spotlight.pointLightInnerRadius = range * 0.05f;
        spotlight.pointLightOuterAngle = angle;
        spotlight.pointLightInnerAngle = angle * 0.45f;
        spotlight.color = new Color(beamColor.r, beamColor.g, beamColor.b, 1f);
        spotlight.intensity = lightIntensity * (flashing ? 1.5f : ready ? 1f : 0.25f);
        // Collider-only obstacles lack shadow casters; keep their clipped mesh authoritative.
        spotlight.enabled = showAim && flashing && illuminateSprites && obstacles.value == 0;

        if (reticle != null) reticle.gameObject.SetActive(showAim);
        if (reticle != null && showAim)
        {
            reticle.position = new Vector3(aimPosition.x, aimPosition.y, origin.z);
            reticle.rotation = camera.transform.rotation;
            float depth = Mathf.Abs(Vector3.Dot(reticle.position - camera.transform.position, camera.transform.forward));
            float viewHeight = camera.orthographic ? camera.orthographicSize * 2f
                : depth * Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * 2f;
            float pixelSize = viewHeight / Mathf.Max(1, camera.pixelHeight);
            float halfSize = pixelSize * reticlePixels * 0.5f * Mathf.Lerp(1f, 0.65f, chargeProgress);
            float cornerLength = halfSize * 0.6f;
            bool inRange = area.Contains(aimPosition);
            Color color = flashing ? Color.white : ready && inRange
                ? new Color(1f, 0.97f, 0.85f, 1f) : new Color(0.6f, 0.7f, 0.75f, 0.75f);
            if (chargeProgress >= 1f) color = new Color(1f, 0.78f, 0.22f, 1f);
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

    private void UploadBoundaries(Mesh target)
    {
        target.Clear();
        target.SetVertices(boundaryVertices);
        target.SetColors(boundaryColors);
        target.SetTriangles(boundaryTriangles, 0);
        target.RecalculateBounds();
        boundaryVertices.Clear();
        boundaryColors.Clear();
        boundaryTriangles.Clear();
    }

    private void AddDashedBoundary(Vector3 endpoint, Color color, float startDistance)
    {
        float distance = endpoint.magnitude;
        if (distance < 0.0001f) return;
        Vector3 direction = endpoint / distance;
        Vector3 side = new Vector3(-direction.y, direction.x, 0f) * Mathf.Max(0.005f, lineWidth) * 0.5f;
        float length = Mathf.Max(0.01f, dashLength);
        float step = length + Mathf.Max(0.01f, dashGap);
        for (float start = startDistance; start < distance; start += step)
        {
            Vector3 a = direction * start;
            Vector3 b = direction * Mathf.Min(start + length, distance);
            AddBoundaryQuad(a, b, side, color);
        }
    }

    private void AddBoundaryQuad(Vector3 a, Vector3 b, Vector3 side, Color color)
    {
        int index = boundaryVertices.Count;
        boundaryVertices.Add(a - side);
        boundaryVertices.Add(a + side);
        boundaryVertices.Add(b + side);
        boundaryVertices.Add(b - side);
        for (int i = 0; i < 4; i++) boundaryColors.Add(color);
        boundaryTriangles.Add(index);
        boundaryTriangles.Add(index + 1);
        boundaryTriangles.Add(index + 2);
        boundaryTriangles.Add(index);
        boundaryTriangles.Add(index + 2);
        boundaryTriangles.Add(index + 3);
    }

    private bool Prepare()
    {
        if (root != null) return true;
        Material rangeMaterial = Resources.Load<Material>("InnerWorldCombat/SpotlightBeam");
        Material reticleMaterial = Resources.Load<Material>("InnerWorldCombat/Reticle");
        if (rangeMaterial == null || reticleMaterial == null) return false;
        root = new GameObject("SpiritFlashAimPreview");
        SceneManager.MoveGameObjectToScene(root, gameObject.scene);
        footprint = new GameObject("Floor projection").transform;
        footprint.SetParent(root.transform, false);
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
        footprint.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        fill = footprint.gameObject.AddComponent<MeshRenderer>();
        fill.sharedMaterial = rangeMaterial;
        fill.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        fill.receiveShadows = false;
        propertyBlock = new MaterialPropertyBlock();

        // The corridor floor art includes Foreground sprites at order 0, below Characters.
        fill.sortingLayerName = "Foreground";
        fill.sortingOrder = 10;

        var aimLines = new GameObject("Spirit aim dashes");
        aimLines.transform.SetParent(root.transform, false);
        aimBoundaryMesh = new Mesh { name = "SpiritAimDashes" };
        aimBoundaryMesh.MarkDynamic();
        aimLines.AddComponent<MeshFilter>().sharedMesh = aimBoundaryMesh;
        aimBoundaries = aimLines.AddComponent<MeshRenderer>();
        aimBoundaries.sharedMaterial = reticleMaterial;
        aimBoundaries.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        aimBoundaries.receiveShadows = false;

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
        lightObject.transform.SetParent(root.transform, false);
        spotlight = lightObject.AddComponent<Light2D>();
        spotlight.lightType = Light2D.LightType.Point;
        spotlight.falloffIntensity = 0.8f;
        spotlight.volumetricEnabled = false;

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
        DestroyOwned(aimBoundaryMesh);
    }
}
