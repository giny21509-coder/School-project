using UnityEngine;

/// <summary>
/// 失控（E = 100）時的玩家效果（設計文件 §4.4）：移動變慢、被附近最近的食物吸過去。
/// 碰到食物自動吃掉由 FoodPickup 處理；解除方法「傾聽自己」由 PlayerListenAbility 處理。
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class BingeController : MonoBehaviour
{
    [SerializeField] private EmptinessMeter meter;

    private PlayerController controller;

    private void Awake() => controller = GetComponent<PlayerController>();

    private void FixedUpdate()
    {
        bool binge = meter != null && meter.IsBinge;
        controller.speedMultiplier = binge ? meter.Config.bingeMoveMultiplier : 1f;
        if (!binge) return;

        FoodPickup nearest = null;
        float radius = meter.Config.bingePullRadius, bestSq = radius * radius;
        Vector2 me = transform.position;
        foreach (var food in FoodPickup.Active)
        {
            if (food == null) continue;
            float d = ((Vector2)food.transform.position - me).sqrMagnitude;
            if (d <= bestSq) { bestSq = d; nearest = food; }
        }
        if (nearest == null) return;
        Vector2 dir = ((Vector2)nearest.transform.position - me).normalized;
        controller.AddExternalVelocity(dir * meter.Config.bingePullSpeed);
    }

    private void OnDisable()
    {
        if (controller != null) controller.speedMultiplier = 1f;
    }
}
