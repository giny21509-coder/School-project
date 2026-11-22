using UnityEngine;
using UnityEngine.Events;

public class WisherPhotoTarget : MonoBehaviour
{
    [Header("Wisher")]
    [SerializeField] private string wisherId;
    [SerializeField] private int requiredPhotoHits = 3;
    [SerializeField] private bool startsHidden = true;

    [Header("Dodge")]
    [SerializeField] private bool dodgeAfterPhoto = true;
    [SerializeField] private Transform[] dodgePoints;

    [Header("Visuals")]
    [SerializeField] private GameObject blackMistVisual;
    [SerializeField] private GameObject revealedWisherVisual;

    [Header("Events")]
    public UnityEvent<int, int> onPhotoProgressChanged;
    public UnityEvent onPhotoHit;
    public UnityEvent onFullyRevealed;
    public UnityEvent onFlashHit;

    private int currentPhotoHits;
    private bool fullyRevealed;

    public string WisherId => wisherId;
    public int CurrentPhotoHits => currentPhotoHits;
    public int RequiredPhotoHits => requiredPhotoHits;
    public bool FullyRevealed => fullyRevealed;

    private void Awake()
    {
        SetRevealed(!startsHidden);
    }

    public void ReceivePhotoHit(Vector2 hitPosition)
    {
        if (fullyRevealed)
        {
            return;
        }

        currentPhotoHits++;
        onPhotoHit?.Invoke();
        onPhotoProgressChanged?.Invoke(currentPhotoHits, requiredPhotoHits);

        if (currentPhotoHits >= requiredPhotoHits)
        {
            SetRevealed(true);
            onFullyRevealed?.Invoke();
            return;
        }

        if (dodgeAfterPhoto)
        {
            MoveToRandomDodgePoint();
        }
    }

    public void ReceiveFlashHit(Vector2 hitPosition)
    {
        onFlashHit?.Invoke();
    }

    public void ForceReveal()
    {
        if (fullyRevealed)
        {
            return;
        }

        currentPhotoHits = requiredPhotoHits;
        onPhotoProgressChanged?.Invoke(currentPhotoHits, requiredPhotoHits);
        SetRevealed(true);
        onFullyRevealed?.Invoke();
    }

    private void SetRevealed(bool revealed)
    {
        fullyRevealed = revealed;

        if (blackMistVisual != null)
        {
            blackMistVisual.SetActive(!revealed);
        }

        if (revealedWisherVisual != null)
        {
            revealedWisherVisual.SetActive(revealed);
        }
    }

    private void MoveToRandomDodgePoint()
    {
        if (dodgePoints == null || dodgePoints.Length == 0)
        {
            return;
        }

        int index = Random.Range(0, dodgePoints.Length);
        transform.position = dodgePoints[index].position;
    }
}
