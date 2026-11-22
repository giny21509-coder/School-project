using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class EscVolumeSlider : MonoBehaviour
{
    private const string VolumePrefsKey = "MasterVolume";
    private const float MinimumMixerVolumeDb = -80f;

    [Header("UI")]
    [SerializeField] private Slider volumeSlider;

    [Header("Audio Mixer (Optional)")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string exposedVolumeParameter = "MasterVolume";

    [Header("Default")]
    [SerializeField, Range(0f, 1f)] private float defaultVolume = 1f;

    private void Awake()
    {
        if (volumeSlider == null)
        {
            volumeSlider = GetComponentInChildren<Slider>(true);
        }

        float savedVolume = PlayerPrefs.GetFloat(VolumePrefsKey, defaultVolume);
        savedVolume = Mathf.Clamp01(savedVolume);

        if (volumeSlider != null)
        {
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.wholeNumbers = false;
            volumeSlider.value = savedVolume;
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }

        ApplyVolume(savedVolume);
    }

    private void OnDestroy()
    {
        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.RemoveListener(SetVolume);
        }
    }

    public void SetVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        ApplyVolume(volume);

        PlayerPrefs.SetFloat(VolumePrefsKey, volume);
        PlayerPrefs.Save();
    }

    private void ApplyVolume(float volume)
    {
        if (audioMixer == null || string.IsNullOrEmpty(exposedVolumeParameter))
        {
            AudioListener.volume = volume;
            return;
        }

        float mixerVolume = volume <= 0.0001f
            ? MinimumMixerVolumeDb
            : Mathf.Log10(volume) * 20f;

        audioMixer.SetFloat(exposedVolumeParameter, mixerVolume);
    }
}
