using UnityEngine;
using UnityEngine.Playables;

public class RoomManager : MonoBehaviour
{
    public PlayableDirector introTimeline;
    public GameObject player;
    public MonoBehaviour PlayerController;

    private string IntroKey => "RoomIntroPlayed_" + gameObject.scene.name;

    void Start()
    {
        if (GameProgressState.GetInt(IntroKey, 0) == 0)
        {
            PlayIntroAnimation();
        }
        else
        {
            SkipIntroAnimation();
        }
    }

    void PlayIntroAnimation()
    {
        Debug.Log("Play room intro for the first time.");

        if (introTimeline != null)
        {
            introTimeline.enabled = true;
            introTimeline.Play();
        }
        else
        {
            RestorePlayerControl();
        }

        GameProgressState.SetInt(IntroKey, 1);
    }

    void SkipIntroAnimation()
    {
        Debug.Log("Room intro already played. Skip timeline.");

        if (introTimeline != null)
        {
            introTimeline.Stop();
            introTimeline.enabled = false;
        }

        RestorePlayerControl();
    }

    private void RestorePlayerControl()
    {
        Time.timeScale = 1f;

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

        if (PlayerController != null)
        {
            PlayerController.enabled = true;
            Debug.Log("Player controller enabled.");
        }
    }

    [ContextMenu("Reset Intro Flag")]
    public void ResetIntroFlag()
    {
        GameProgressState.DeleteKey(IntroKey);
        Debug.Log("Room intro flag reset.");
    }
}
