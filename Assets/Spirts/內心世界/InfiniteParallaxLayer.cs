using UnityEngine;

public class InfiniteParallaxLayer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform[] pieces;

    [Header("Settings")]
    [SerializeField] private float parallaxFactor = 1f;
    [SerializeField] private float pieceWidth = 20f;

    private Vector3 lastCameraPosition;

    private void Start()
    {
        Camera assignedCamera = cameraTransform != null
            ? cameraTransform.GetComponent<Camera>()
            : null;
        if (assignedCamera == null || !assignedCamera.isActiveAndEnabled)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
                cameraTransform = mainCamera.transform;
            else
                cameraTransform = null;
        }

        if (cameraTransform == null || pieces == null || pieces.Length < 3 || pieceWidth <= 0f)
        {
            Debug.LogError("InfiniteParallaxLayer requires a camera, at least three pieces, and a positive piece width.", this);
            enabled = false;
            return;
        }

        lastCameraPosition = cameraTransform.position;
    }

    private void LateUpdate()
    {
        Vector3 cameraDelta = cameraTransform.position - lastCameraPosition;

        transform.position += new Vector3(cameraDelta.x * parallaxFactor, cameraDelta.y * parallaxFactor, 0f);

        lastCameraPosition = cameraTransform.position;

        RepositionPieces();
    }

    private void RepositionPieces()
    {
        float totalWidth = pieceWidth * pieces.Length;
        float halfSpan = totalWidth * 0.5f;

        for (int i = 0; i < pieces.Length; i++)
        {
            Transform piece = pieces[i];
            if (piece == null)
                continue;

            // Wrap over the full cycle so a moved piece cannot bounce back next frame.
            float distance = piece.position.x - cameraTransform.position.x;
            float wrappedDistance = Mathf.Repeat(distance + halfSpan, totalWidth) - halfSpan;
            piece.position += Vector3.right * (wrappedDistance - distance);
        }
    }
}

