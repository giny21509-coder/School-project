using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Connect to MMHealthBar using its Existing mode.
/// Replaces the complete image instead of stretching or clipping the artwork.
/// </summary>
[AddComponentMenu("Game UI/Film Health Bar Progress")]
[RequireComponent(typeof(Image))]
[DisallowMultipleComponent]
public class FilmHealthBarProgress : MMProgressBar
{
    [Header("Film Health Bar")]
    [Tooltip("Always uses the UI Image on this same object. This field is assigned at runtime.")]
    public Image TargetImage;

    [Tooltip("Exactly seven sprites: index 0 = empty, index 6 = full.")]
    public Sprite[] HealthSprites = new Sprite[7];

    protected virtual void Awake()
    {
        Initialization();
    }

    public override void Initialization()
    {
        // Initialization may also be requested by MMHealthBar or MMProgressBar.
        // Only apply the opening full-health state once, never after damage.
        if (_initialized)
        {
            return;
        }

        // Bind locally, never search other UI objects or follow a stale reference.
        TargetImage = GetComponent<Image>();
        TargetImage.type = Image.Type.Simple;
        TargetImage.preserveAspect = true;
        ForegroundBar = TargetImage.transform;
        DelayedBarDecreasing = null;
        DelayedBarIncreasing = null;
        MinimumBarFillValue = 0f;
        MaximumBarFillValue = 1f;
        BumpScaleOnChange = false;
        BumpOnIncrease = false;
        BumpOnDecrease = false;
        SetInitialFillValueOnStart = true;
        InitialFillValue = 1f;
        base.Initialization();
    }

    public override void UpdateBar(float currentValue, float minValue, float maxValue)
    {
        // Switch directly to the requested state, without intermediate frames.
        SetBar(currentValue, minValue, maxValue);
    }

    protected override void SetBarInternal(
        float newAmount, Transform bar, Image image, Vector2 initialSize)
    {
        if (bar != ForegroundBar || TargetImage == null)
        {
            return;
        }

        if (HealthSprites == null || HealthSprites.Length != 7)
        {
            return;
        }

        float normalized = Mathf.Clamp01(newAmount);
        // Keep a partly filled cell visible, and show empty only at zero health.
        int cells = normalized <= 0f ? 0 : Mathf.Clamp(
            Mathf.CeilToInt(normalized * 6f - 0.0001f), 1, 6);
        Sprite nextSprite = HealthSprites[cells];
        if (nextSprite != null)
        {
            TargetImage.sprite = nextSprite;
        }
    }
}
