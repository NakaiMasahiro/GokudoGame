using UnityEngine;

public class Enemy : MonoBehaviour
{
    // HP
    public int maxHp = 4000;
    public int hp = 4000;

    // “|‚ê‰æ‘œ
    public Sprite downSprite;

    private SpriteRenderer sr;

    private void Start()
    {
        hp = maxHp;

        sr = GetComponent<SpriteRenderer>();
    }

    public void TakeDamage(int damage)
    {
        hp -= damage;

        EnemyAI ai = GetComponent<EnemyAI>();

        if (ai != null)
        {
            ai.Stun();
        }

        Debug.Log("“GHP : " + hp);

        Debug.Log("“GHP : " + hp);

        if (hp <= 0)
        {
            hp = 0;

            Die();
        }
    }

    // š’Ç‰Á
    void Die()
    {
        Debug.Log("“G‚ğ“|‚µ‚½");

        Boss boss = FindAnyObjectByType<Boss>();

        if (boss != null)
        {
            boss.CheckEnemyDeath();
        }

        // “|‚ê‰æ‘œ
        if (sr != null)
        {
            sr.sprite = downSprite;
        }

        transform.rotation = Quaternion.Euler(0f, 0f, -90f);
        transform.position += new Vector3(0f, -0.4f, 0f);

        // AI’â~
        EnemyAI ai = GetComponent<EnemyAI>();

        if (ai != null)
        {
            ai.SetDead();
            ai.enabled = false;
        }

        // ˆÚ“®’â~
        Rigidbody2D rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // “–‚½‚è”»’èOFF
        Collider2D col = GetComponent<Collider2D>();

        if (col != null)
        {
            col.enabled = false;
        }

        // šHPƒo[‚àÁ‚·
        Canvas hpCanvas = GetComponentInChildren<Canvas>();

        if (hpCanvas != null)
        {
            hpCanvas.enabled = false;
        }

        // š1•bŒã‚ÉÁ–Å
        Destroy(gameObject, 1f);
    }
}