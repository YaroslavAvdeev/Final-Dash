using UnityEngine;

public class BossTriggerZone : MonoBehaviour
{
    [Tooltip("Перетягни сюди BossHealthBar об'єкт з Canvas")]
    public BossHealthBarUI healthBar;

    [Tooltip("Перетягни сюди MusicZoneTrigger")]
    public MusicZoneTrigger musicTrigger;

    private bool triggered = false;

    void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;
        triggered = true;

        if (healthBar != null)
            healthBar.ShowBar();

        if (musicTrigger != null)
            musicTrigger.StartCoroutine("SwitchMusic");

        CameraFollow.Instance?.SetBossZone(true);
    }

    public void OnBossDead()
    {
        CameraFollow.Instance?.SetBossZone(false);
    }
}