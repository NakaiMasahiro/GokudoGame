using System.Collections.Generic;
using UnityEngine;

public class HitSystem : MonoBehaviour
{
    // UŒ‚”ÍˆÍ“à‚É‚¢‚é“G
    private readonly List<Enemy> enemiesInRange = new List<Enemy>();

    // ”ÍˆÍ“à‚Åˆê”Ô‹ß‚¢u¶‚«‚Ä‚¢‚év“G
    public Enemy currentEnemy
    {
        get
        {
            Enemy nearest = null;
            float nearestDistance = Mathf.Infinity;

            for (int i = enemiesInRange.Count - 1; i >= 0; i--)
            {
                Enemy enemy = enemiesInRange[i];

                // ”jŠü‚³‚ê‚½“GE“|‚ê‚½“G‚Íˆê——‚©‚çŠO‚·
                if (enemy == null || enemy.IsDead)
                {
                    enemiesInRange.RemoveAt(i);
                    continue;
                }

                float distance =
                    ((Vector2)enemy.transform.position -
                     (Vector2)transform.position).sqrMagnitude;

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = enemy;
                }
            }

            return nearest;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy"))
            return;

        Enemy enemy = other.GetComponent<Enemy>();

        if (enemy != null && !enemiesInRange.Contains(enemy))
        {
            enemiesInRange.Add(enemy);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Enemy"))
            return;

        Enemy enemy = other.GetComponent<Enemy>();

        if (enemy != null)
        {
            enemiesInRange.Remove(enemy);
        }
    }
}