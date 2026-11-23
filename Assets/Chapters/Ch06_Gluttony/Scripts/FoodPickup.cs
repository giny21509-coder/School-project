using System.Collections.Generic;
using MoreMountains.TopDownEngine;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 場上的食物（設計文件 §4.2）。
/// 靠近按互動鍵：HP 未滿就吃（回血、E 上升），HP 已滿就放進食物袋（袋子滿了就留在地上）。
/// 失控時與食物雨：碰到就自動吃。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class FoodPickup : MonoBehaviour
{
    public static readonly List<FoodPickup> Active = new List<FoodPickup>();

    [Tooltip("食物雨或失控時：碰到就吃，不用按鍵")]
    [SerializeField] private bool eatOnTouch;
    [SerializeField] private string playerTag = "Player";

    public UnityEvent onEaten = new UnityEvent();
    public UnityEvent onStored = new UnityEvent();

    private EmptinessMeter meter;
    private GluttonyFoodPouch pouch;
    private Health playerHealth;
    private bool playerInside;

    private void OnEnable() => Active.Add(this);
    private void OnDisable() => Active.Remove(this);

    public void SetEatOnTouch(bool value) => eatOnTouch = value;

    private void Start()
    {
        meter = FindAnyObjectByType<EmptinessMeter>();
        pouch = FindAnyObjectByType<GluttonyFoodPouch>();
    }

    private void Update()
    {
        if (!playerInside || Time.timeScale <= 0f) return;
        if (Ch06Input.InteractPressed) Interact();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInside = true;
        if (playerHealth == null) playerHealth = other.GetComponentInParent<Health>();
        if (eatOnTouch || (meter != null && meter.IsBinge)) Eat();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag)) playerInside = false;
    }

    private void Interact()
    {
        bool fullHealth = playerHealth != null && playerHealth.CurrentHealth >= playerHealth.MaximumHealth;
        if (fullHealth)
        {
            if (pouch != null && pouch.TryStore()) { onStored?.Invoke(); Destroy(gameObject); }
            return; // 袋子滿了：留在地上（保留誘惑）
        }
        Eat();
    }

    public void Eat()
    {
        if (meter == null) meter = FindAnyObjectByType<EmptinessMeter>();
        float heal = meter != null ? meter.Config.eatHeal : 20f;
        if (playerHealth != null) playerHealth.ReceiveHealth(heal, gameObject);
        if (meter != null) meter.Eat();
        onEaten?.Invoke();
        Destroy(gameObject);
    }
}
