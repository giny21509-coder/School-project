using UnityEngine;
using UnityEngine.Events;

public class Chatacher : MonoBehaviour
{
    [Header("基本屬性")]

    public float maxHealth;

    public float currenHealthl;

   
    [Header("受傷無敵")]

    public float invulnerableDuration;

    private float invulnerableCounter;

    public bool invulnerable;

    public bool isDead;

    public UnityEvent<Transform> OnTakrDamage;
    public UnityEvent OnDie;


    public void Start()
    {
        currenHealthl = maxHealth;
    }

    public void Update()
    {
        if (invulnerable)
        {
            invulnerableCounter -= Time.deltaTime;

            if (invulnerableCounter <= 0)
            {


                invulnerable = false;
            }
        }
    }

    public void TakeDamage(Attack attacker)
    {

        InnerWorldMistShield shield = GetComponentInParent<InnerWorldMistShield>();
        if (isDead || invulnerable || (shield != null && shield.IsShielded))
            return;
        // Debug.Log(attacker.damage);
        if (currenHealthl - attacker.damage > 0)
        {
            currenHealthl -=  attacker.damage;
            TriggerInvulnvulnerable();

            OnTakrDamage?.Invoke(attacker.transform);
        }
        else
        {
            currenHealthl = 0;
            isDead = true;
            //死

            OnDie?.Invoke();
        }

    }
      
    

    private void TriggerInvulnvulnerable()
    {
        if (!invulnerable)
        {
            invulnerable = true;
            invulnerableCounter = invulnerableDuration;
        }
    }
}
