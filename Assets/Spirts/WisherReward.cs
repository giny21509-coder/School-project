using UnityEngine;
using UnityEngine.Events;

public class WisherReward : MonoBehaviour
{
    [Header("Reward Data")]
    [SerializeField] private string mementoName;
    [SerializeField] private Sprite mementoIcon;
    [SerializeField] private Sprite photoAlbumImage;

    [Header("Events")]
    public UnityEvent onRewardCollected;

    public string MementoName => mementoName;
    public Sprite MementoIcon => mementoIcon;
    public Sprite PhotoAlbumImage => photoAlbumImage;

    public void CollectReward()
    {
        onRewardCollected?.Invoke();
    }
}
