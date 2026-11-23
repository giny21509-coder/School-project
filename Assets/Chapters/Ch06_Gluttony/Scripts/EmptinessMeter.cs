using JianQi.Ch06;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 內心世界場景裡唯一的空虛值來源：持有 EmptinessModel，把 E 推給黑霧、聲音與 UI。
/// 其他元件（食物、回音、生成器、最終戰）都透過這個元件讀寫 E。
/// </summary>
public class EmptinessMeter : MonoBehaviour
{
    [SerializeField] private GluttonyConfig config;

    [Header("表現（皆可留空）")]
    [Tooltip("沿用貓咪章的鏡頭邊緣霧：E 越高越濃")]
    [SerializeField] private ScreenEdgeFog edgeFog;
    [Tooltip("咀嚼聲（循環播放），E 越高越大聲")]
    [SerializeField] private AudioSource chewingLoop;
    [SerializeField, Range(0f, 1f)] private float chewingVolumeMin = 0.05f, chewingVolumeMax = 0.8f;
    [Tooltip("左上角「空碗」：Image Type = Filled；碗越空 fillAmount 越低")]
    [SerializeField] private Image bowlFill;

    [Header("事件")]
    public UnityEvent<float> onEmptinessChanged = new UnityEvent<float>();
    public UnityEvent onBingeStarted = new UnityEvent();
    public UnityEvent onBingeEnded = new UnityEvent();

    public GluttonyConfig Config => config;
    public EmptinessModel Model { get; private set; }
    public float Value => Model != null ? Model.Value : 0f;
    public float Normalized => Model != null ? Model.Normalized : 0f;
    public bool IsBinge => Model != null && Model.IsBinge;

    /// <summary>目前 Zone 是否會自然上升（ZoneGate 進入時設定）。</summary>
    public bool Drifting { get; set; }
    /// <summary>玩家是否正在傾聽（PlayerListenAbility 設定）；傾聽時暫停自然上升。</summary>
    public bool Listening { get; set; }

    private void Awake()
    {
        if (config == null)
        {
            Debug.LogError("EmptinessMeter：請指定 GluttonyConfig。", this);
            enabled = false;
            return;
        }
        Model = new EmptinessModel(config.ToRules());
        Model.Changed += HandleChanged;
        Model.BingeChanged += HandleBinge;
    }

    private void Start() => HandleChanged(Model.Value);

    private void OnDestroy()
    {
        if (Model == null) return;
        Model.Changed -= HandleChanged;
        Model.BingeChanged -= HandleBinge;
    }

    private void Update()
    {
        if (Time.timeScale <= 0f) return;
        Model.Tick(Time.deltaTime, Drifting, Listening);
    }

    public void Eat() => Model.Eat();
    public void CompleteEcho() => Model.CompleteEcho();
    public void WrongCard() => Model.WrongCard();
    public bool TrySelfListen() => Model.TrySelfListen();
    public void ResetTo(float value) => Model.ResetTo(value);

    private void HandleChanged(float value)
    {
        float e01 = Model.Normalized;
        if (edgeFog != null) edgeFog.SetProgress(1f - config.MistDensity(e01));
        if (chewingLoop != null) chewingLoop.volume = Mathf.Lerp(chewingVolumeMin, chewingVolumeMax, e01);
        if (bowlFill != null) bowlFill.fillAmount = 1f - e01;
        onEmptinessChanged?.Invoke(value);
    }

    private void HandleBinge(bool binge)
    {
        if (binge) onBingeStarted?.Invoke();
        else onBingeEnded?.Invoke();
    }
}
