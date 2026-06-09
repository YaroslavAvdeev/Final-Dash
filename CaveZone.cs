using UnityEngine;

public class CaveZone : MonoBehaviour
{
    public CaveParallaxLayer[] layers;

    void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        foreach (var layer in layers)
            if (layer != null) layer.Activate();

        // Вимикаємо — більше не спрацьовує
        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
    }
}