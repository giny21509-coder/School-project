using System.Collections.Generic;
using UnityEngine;

public class Attack : MonoBehaviour
{
    public int damage;

    public float attackRange;

    public float attackRate;

    private readonly Dictionary<Chatacher, float> nextDamageTimes = new Dictionary<Chatacher, float>();

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryHit(other);
    }

    private void TryHit(Collider2D other)
    {
        Chatacher chatacher = other.GetComponent<Chatacher>();

        // 停用的舊血量元件不應接收傷害；TopDown 傷害由 DamageOnTouch 處理。
        if (chatacher == null || !chatacher.isActiveAndEnabled)
        {
            return;
        }

        if (!CanDamage(chatacher))
        {
            return;
        }

        nextDamageTimes[chatacher] = Time.time + GetHitCooldown();
        chatacher.TakeDamage(this);

        if (chatacher.currenHealthl <= 0)
        {
            return;
        }

        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeHit(transform);
        }
    }

    private bool CanDamage(Chatacher target)
    {
        InnerWorldMistShield shield = target.GetComponentInParent<InnerWorldMistShield>();
        if (shield != null && shield.IsShielded) return false;
        if (nextDamageTimes.TryGetValue(target, out float nextDamageTime))
        {
            return Time.time >= nextDamageTime;
        }

        return true;
    }

    private float GetHitCooldown()
    {
        return attackRate > 0f ? attackRate : 0.35f;
    }

}
