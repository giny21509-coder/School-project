using UnityEngine;
using UnityEngine.Events;

public class CameraItemPickup : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private bool destroyAfterPickup = true;

    public UnityEvent onPickedUp;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        CameraSystem cameraSystem = CameraSystem.Instance;
        if (cameraSystem != null)
        {
            cameraSystem.UnlockCamera();
        }

        onPickedUp?.Invoke();

        if (destroyAfterPickup)
        {
            Destroy(gameObject);
        }
    }
}
