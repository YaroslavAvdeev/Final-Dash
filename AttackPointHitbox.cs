using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SharkAttackHitboxBridge : MonoBehaviour
{
    private SharkBossAI parentAI;

    void Awake()
    {
        parentAI = GetComponentInParent<SharkBossAI>();
        if (parentAI == null)
            Debug.LogError("[HitboxBridge] SharkGoldAI не знайдено на батьківському об'єкті!");

        // Вимикаємо колайдер одразу — AI вмикатиме сам
        GetComponent<Collider2D>().enabled = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (parentAI != null)
            parentAI.OnAttackHitboxTriggered(other);
    }

    // На випадок якщо гравець стоїть всередині зони під час активації
    void OnTriggerStay2D(Collider2D other)
    {
        if (parentAI != null)
            parentAI.OnAttackHitboxTriggered(other);
    }
}
