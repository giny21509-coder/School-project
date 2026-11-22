using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class LightStateMemory : MonoBehaviour
{
    [Header("Save")]
    [SerializeField] private string prefsKey = "LightState";
    [SerializeField] private bool defaultIsOn = true;

    [Header("Interaction")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private KeyCode interactKey = KeyCode.R;
    [FormerlySerializedAs("interactHintUI")]
    [SerializeField] private GameObject hintWhenLightOn;
    [SerializeField] private GameObject hintWhenLightOff;
    [HideInInspector, SerializeField] private GameObject[] extraInteractHintUIs;

    [Header("Optional UI")]
    [SerializeField] private Toggle lightToggle;

    [Header("Scene Objects")]
    [SerializeField] private Light[] lightsToControl;
    [SerializeField] private GameObject[] objectsActiveWhenOn;
    [SerializeField] private GameObject[] objectsActiveWhenOff;

    private bool isOn;
    private bool isApplyingState;
    private bool playerInside;

    private void Awake()
    {
        isOn = GameProgressState.GetInt(prefsKey, defaultIsOn ? 1 : 0) == 1;
        ApplyState();
        RefreshInteractHint();
    }

    private void Update()
    {
        if (playerInside && Input.GetKeyDown(interactKey))
        {
            ToggleLight();
        }
    }

    private void OnEnable()
    {
        if (lightToggle != null)
        {
            lightToggle.onValueChanged.AddListener(SetLight);
        }
    }

    private void OnDisable()
    {
        if (lightToggle != null)
        {
            lightToggle.onValueChanged.RemoveListener(SetLight);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        playerInside = true;
        RefreshInteractHint();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        playerInside = false;
        RefreshInteractHint();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        playerInside = true;
        RefreshInteractHint();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        playerInside = false;
        RefreshInteractHint();
    }

    public void TurnOn()
    {
        SetLight(true);
    }

    public void TurnOff()
    {
        SetLight(false);
    }

    public void ToggleLight()
    {
        SetLight(!isOn);
    }

    public void SetLight(bool value)
    {
        if (isApplyingState)
        {
            return;
        }

        isOn = value;
        ApplyState();

        GameProgressState.SetInt(prefsKey, isOn ? 1 : 0);
    }

    private void ApplyState()
    {
        isApplyingState = true;

        SetLightsEnabled(isOn);
        SetObjectsActive(objectsActiveWhenOn, isOn);
        SetObjectsActive(objectsActiveWhenOff, !isOn);

        if (lightToggle != null)
        {
            lightToggle.isOn = isOn;
        }

        RefreshInteractHint();
        isApplyingState = false;
    }

    private void SetLightsEnabled(bool enabled)
    {
        if (lightsToControl == null)
        {
            return;
        }

        for (int i = 0; i < lightsToControl.Length; i++)
        {
            if (lightsToControl[i] != null)
            {
                lightsToControl[i].enabled = enabled;
            }
        }
    }

    private void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null)
        {
            return;
        }

        for (int i = 0; i < objects.Length; i++)
        {
            if (objects[i] != null)
            {
                objects[i].SetActive(active);
            }
        }
    }

    private void RefreshInteractHint()
    {
        SetObjectActive(hintWhenLightOn, playerInside && isOn);
        SetObjectActive(hintWhenLightOff, playerInside && !isOn);

        if (hintWhenLightOff != null || extraInteractHintUIs == null)
        {
            return;
        }

        for (int i = 0; i < extraInteractHintUIs.Length; i++)
        {
            SetObjectActive(extraInteractHintUIs[i], playerInside && !isOn);
        }
    }

    private void SetObjectActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }
}
