using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using Unity.VisualScripting;

public class DoorController : MonoBehaviour
{
    //��
    private Animator anim;
    private bool InDoor = false;
    private bool IsOpen;
    public GameObject DoorUi;

    public string DoorTeleporter;
    public string TargetSpawnId;

    public Animator Corss;

    [Header("Door Audio")]
    [Tooltip("開門時播放的音效。")]
    [SerializeField] private AudioClip openDoorSound;
    [Tooltip("關門時播放的音效。")]
    [SerializeField] private AudioClip closeDoorSound;
    [SerializeField, Range(0f, 1f)] private float doorSoundVolume = 1f;
    [Tooltip("可留空，程式會自動建立播放音效的 AudioSource。")]
    [SerializeField] private AudioSource doorAudioSource;


   // public float fadeDuration;
    //public CanvasGroup fadeCanvasGroup;
    //public float delayTime = 1.5f;
    void Start()
    {
        anim = transform.gameObject.GetComponent<Animator>();
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && InDoor)
        {
            IsOpen = !IsOpen;
            if (anim != null)
            {
                anim.SetBool("open", IsOpen);
            }
            PlayDoorSound();
            //if (string.IsNullOrEmpty(DoorTeleporter))
            // {
            if(IsOpen)
            {
                StartCoroutine(WaitAndChangeScene());
            }
           // }
        }
    }

    private void PlayDoorSound()
    {
        AudioClip clip = IsOpen ? openDoorSound : closeDoorSound;
        if (clip == null) return;

        if (doorAudioSource == null)
        {
            doorAudioSource = gameObject.AddComponent<AudioSource>();
            doorAudioSource.playOnAwake = false;
            doorAudioSource.loop = false;
            doorAudioSource.spatialBlend = 0f;
        }

        doorAudioSource.PlayOneShot(clip, doorSoundVolume);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            InDoor = true;
            DoorUi.SetActive(true);
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        InDoor = false;
        DoorUi.SetActive(false);
    }
    
    IEnumerator WaitAndChangeScene()
    {
        
        yield return new WaitForSeconds(2.0f);

        Corss.SetTrigger("Start");

          ExecuteSceneChange();
    }
   public void ExecuteSceneChange()
    {
        if (!string.IsNullOrEmpty(TargetSpawnId))
        {
            PlayerPrefs.SetString("NextSpawnId", TargetSpawnId);
        }

        SceneManager.LoadScene(DoorTeleporter);
    }
}
