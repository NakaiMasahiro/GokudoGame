using UnityEngine;

public class LastBoss : MonoBehaviour
{
    //HP
    public int maxHp = 20000;
    private int hp = 0;

    //歩く速さ
    public float moveSpeed = 3f;

    //攻撃
    public int punchDamage = 50;
    public int kickDamage = 60;
    public int strongDamage = 300;

    public float attackRange = 2f;
    public float attackCooldown = 2f;

    public float dodgeChance = 30f;

    private bool canAttack = true;
    private bool phase2 = false;

    private Transform player;
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    void Start()
    {
        hp = maxHp;

        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        GameObject playerObj =
        GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }

    void FixedUpdate()
    {
        if (player == null)
            return;

        float distance =
        Vector2.Distance(
        transform.position,
        player.position);

        if (distance > attackRange)
        {
            Vector2 direction =
            (player.position - transform.position).normalized;

            rb.linearVelocity =
            new Vector2(
            direction.x * moveSpeed,
            rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;

            if (canAttack)
            {
                int attackType =
                Random.Range(0, 2);

                if (attackType == 0)
                {
                    ThreeHitCombo();
                }
                else
                {
                    StrongAttack();
                }
            }
        }

        CheckPhase2();
    }

    void ThreeHitCombo()
    {
        canAttack = false;

        Debug.Log("右パンチ");
        Debug.Log("左パンチ");
        Debug.Log("回し蹴り");

        Player playerScript =
        player.GetComponent<Player>();

        if (playerScript != null)
        {
            playerScript.TakeDamage(
            punchDamage +
            punchDamage +
            kickDamage);
        }

        Invoke(nameof(EnableAttack),
        attackCooldown);
    }

    void StrongAttack()
    {
        canAttack = false;

        Debug.Log("強攻撃");

        Player playerScript =
        player.GetComponent<Player>();

        if (playerScript != null)
        {
            playerScript.TakeDamage(
            strongDamage);
        }

        Invoke(nameof(EnableAttack),
        attackCooldown + 1f);
    }

    void EnableAttack()
    {
        canAttack = true;
    }

    void CheckPhase2()
    {
        if (phase2)
            return;

        if (hp <= maxHp / 2)
        {
            phase2 = true;

            moveSpeed = 5f;
            dodgeChance = 50f;
            attackCooldown = 1f;

            Debug.Log("第二形態！");
        }
    }

    public void TakeDamage(int damage)
    {
        if (Random.Range(0f, 100f) < dodgeChance)
        {
            Dodge();
            return;
        }

        hp -= damage;

        Debug.Log("Boss HP : " + hp);

        if (hp <= 0)
        {
            Die();
        }
    }

    void Dodge()
    {
        Debug.Log("回避");

        transform.position +=
        new Vector3(
        Random.Range(-2f, 2f),
        0f,
        0f);
    }

    void Die()
    {
        Debug.Log("ラスボス撃破！");

        Destroy(gameObject);
    }
}