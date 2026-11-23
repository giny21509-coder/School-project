using MoreMountains.TopDownEngine;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 隨身食物袋（設計文件 §4.2）：最多 N 份，按補血鍵吃一份。效果與場上食物相同，E 一樣上升。
/// 取代第六章中的 InnerWorldHealingSystem（那個系統綁定貓咪章的生成器）。
/// </summary>
public class GluttonyFoodPouch : MonoBehaviour
{
    [SerializeField] private EmptinessMeter meter;
    [SerializeField] private Health playerHealth;

    public UnityEvent<int> onCountChanged = new UnityEvent<int>();

    public int Count { get; private set; }
    public int Capacity => meter != null ? meter.Config.pouchCapacity : 3;

    private void Update()
    {
        if (Time.timeScale <= 0f) return;
        if (Ch06Input.EatFromPouchPressed) TryEat();
    }

    public bool TryStore()
    {
        if (Count >= Capacity) return false;
        Count++;
        onCountChanged?.Invoke(Count);
        return true;
    }

    public bool TryEat()
    {
        if (Count <= 0 || playerHealth == null || meter == null) return false;
        if (playerHealth.CurrentHealth >= playerHealth.MaximumHealth) return false;
        playerHealth.ReceiveHealth(meter.Config.eatHeal, gameObject);
        meter.Eat();
        Count--;
        onCountChanged?.Invoke(Count);
        return true;
    }
}
