using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

public class OfficeIntroFlowchartGate : MonoBehaviour
{
    [SerializeField] private string playedKey = "OfficeIntroFlowchartPlayed";
    [SerializeField] private GameObject playerToEnable;
    [SerializeField] private MonoBehaviour playerController;
    [SerializeField] private PlayableDirector introTimeline;

    private void Awake()
    {
        if (GameProgressState.GetInt(playedKey, 0) == 1)
        {
            DisableFlowchartBehaviours();
            StartCoroutine(RestorePlayerControlAfterSceneStarts());
            return;
        }

        GameProgressState.SetInt(playedKey, 1);
    }

    [ContextMenu("Reset Office Intro")]
    private void ResetOfficeIntro()
    {
        GameProgressState.DeleteKey(playedKey);
    }

    private void RestorePlayerControl()
    {
        Time.timeScale = 1f;

        if (introTimeline != null)
        {
            introTimeline.Stop();
            introTimeline.enabled = false;
        }

        GameObject player = playerToEnable;
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }

        if (player != null)
        {
            player.SetActive(true);

            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.simulated = true;
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.constraints &= ~RigidbodyConstraints2D.FreezePositionX;
                rb.constraints &= ~RigidbodyConstraints2D.FreezePositionY;
                rb.linearVelocity = Vector2.zero;
                rb.WakeUp();
            }
        }

        if (playerController != null)
        {
            playerController.enabled = true;
        }
    }

    private IEnumerator RestorePlayerControlAfterSceneStarts()
    {
        for (int i = 0; i < 5; i++)
        {
            yield return null;
            RestorePlayerControl();
        }

        yield return new WaitForFixedUpdate();
        RestorePlayerControl();
    }

    private void DisableFlowchartBehaviours()
    {
        MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            MonoBehaviour behaviour = behaviours[i];
            if (behaviour != null && behaviour != this)
            {
                behaviour.enabled = false;
            }
        }
    }
}
