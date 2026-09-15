using UnityEngine;

public class HitSystem : MonoBehaviour
{
    public Enemy currentEnemy;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            currentEnemy = other.GetComponent<Enemy>();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Enemy"))
        {
            currentEnemy = null;
        }
    }
}
