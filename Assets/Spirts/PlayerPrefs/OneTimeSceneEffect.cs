using UnityEngine;

public class OneTimeSceneEffect : MonoBehaviour
{
    [SerializeField] private string eventKey = "Scene_Effect_01";
    [SerializeField] private GameObject effectObject;
    [SerializeField] private ParticleSystem particle;

    private void Start()
    {
        bool didTrigger = OneTimeEventManager.Instance.TryTrigger(eventKey, PlayEffect);

        if (!didTrigger)
        {
            if (effectObject != null)
            {
                effectObject.SetActive(false);
            }
        }
    }

    private void PlayEffect()
    {
        if (effectObject != null)
        {
            effectObject.SetActive(true);
        }

        if (particle != null)
        {
            particle.Play();
        }
    }

    [ContextMenu("Reset This Effect")]
    private void ResetThisEffect()
    {
        OneTimeEventManager.Instance.ResetEvent(eventKey);
    }
}
