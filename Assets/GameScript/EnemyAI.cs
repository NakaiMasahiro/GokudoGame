using UnityEngine;

public class EnemyAI : MonoBehaviour
{

    //‰æ‘œ
    public Sprite idleSprite;
    public Sprite ReadyAttack;
    public Sprite attack1Sprite;
    public Sprite attack2Sprite;

    //UŒ‚‰æ‘œŠÔ
    public float attackTime = 1.0f;
    // ˆÚ“®‘¬“x
    public float moveSpeed = 2f;

    // š”­Œ©‹——£
    public float detectRange = 5f;

    // ƒvƒŒƒCƒ„[‚Ì‘O‚Å~‚Ü‚é‹——£
    public float attackRange = 1.5f;

    //UŒ‚
    public int attack1 = 50;
    public int attack2 = 100;

    //UŒ‚Š´Šo
    public float attackCooldown = 2f;

    private bool canAttack = true;

    private Transform player;
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    //UŒ‚’†
    private bool isAttacking = false;

    //UŒ‚—\”õ“®ì
    public float attackWindupTime = 0.5f;

    public GameObject attackPoint;

    public float attackRadius = 0.5f;

    //Playerê—p
    public LayerMask playerLayer;

    private bool isDead = false;

    //‚Ğ‚é‚İ
    private bool isStunned = false;
    public float stunTime = 0.2f;

    private Vector3 attackPointStartPos;

    //ƒJƒEƒ“ƒ^[ó•tŠÔ
    public float counterWindow = 0.2f;


    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        attackPointStartPos = attackPoint.transform.localPosition;
    }

    void FixedUpdate()
    {
        if (isStunned)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (isAttacking)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (player == null)
            return;

        float distance =
            Vector2.Distance(
                transform.position,
                player.position);

        if (player.position.x < transform.position.x)
        {
            sr.flipX = false;
        }
        else
        {
            sr.flipX = true;
        }

        // UŒ‚‹——£‚Ü‚Å‹ß‚Ã‚¢‚½‚ç’â~
        if (distance <= attackRange)
        {
            rb.linearVelocity = Vector2.zero;

            if (canAttack)
            {
                Attack();
            }
        }

        // ”­Œ©‹——£“à‚È‚ç’ÇÕ
        else if (distance <= detectRange)
        {
            Vector2 direction =
                (player.position - transform.position).normalized;

            rb.linearVelocity =
                new Vector2(
                    direction.x * moveSpeed,
                    rb.linearVelocity.y);
        }

        // Œ©¸‚Á‚½‚ç’â~
        else
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Player‘¤‚ÉUŒ‚”»’è‚ğˆÚ“®
        if (player.position.x < transform.position.x)
        {
            // Player‚ª¶
            attackPoint.transform.localPosition =
                new Vector3(
                    -Mathf.Abs(attackPointStartPos.x),
                    attackPointStartPos.y,
                    attackPointStartPos.z);
        }
        else
        {
            // Player‚ª‰E
            attackPoint.transform.localPosition =
                new Vector3(
                    Mathf.Abs(attackPointStartPos.x),
                    attackPointStartPos.y,
                    attackPointStartPos.z);
        }
    }

    void EnableAttack()
    {
        canAttack = true;
    }

    // UŒ‚
    void Attack()
    {
        if (isStunned)
            return;

        if (isDead)
            return;

        if (player == null)
            return;

        canAttack = false;
        isAttacking = true;

        int attackType = Random.Range(0, 2);

        // ‹¤’Ê—\”õ“®ì
        sr.sprite = ReadyAttack;
        if (attackType == 0)
        {
            Invoke(nameof(Attack1), attackWindupTime);
        }
        else
        {
            Invoke(nameof(Attack2), attackWindupTime);
        }
    }

    //‰Eƒpƒ“ƒ`
    void Attack1()
    {
        Player playerScript =
            player.GetComponent<Player>();

        if (playerScript != null)
        {
            if (playerScript.IsTigerDropReady())
            {
                playerScript.TigerDropSuccess(
                    GetComponent<Enemy>());

                Stun();

                return;
            }
        }

        if (isStunned)
            return;

        if (isDead)
            return;

        if (playerScript != null)
        {
            sr.sprite = attack1Sprite;

            DealDamage(attack1);

            Debug.Log("“G‚ÌUŒ‚‡@");
        }

        Invoke(nameof(ReturnToIdle), attackTime);
        Invoke(nameof(EnableAttack), attackCooldown);
    }

    //¶ƒpƒ“ƒ`
    void Attack2()
    {
        if (isStunned)
            return;

        if (isDead)
            return;

        Player playerScript = player.GetComponent<Player>();

        if (playerScript != null)
        {
            sr.sprite = attack2Sprite;

            DealDamage(attack2);

            Debug.Log("“G‚ÌUŒ‚‡A");
        }

        Invoke(nameof(ReturnToIdle), attackTime);
        Invoke(nameof(EnableAttack), attackCooldown);
    }

    void ReturnToIdle()
    {
        sr.sprite = idleSprite;

        isAttacking = false;
    }

    void DealDamage(int damage)
    {
        Debug.Log("UŒ‚”»’è”­¶");
        Collider2D[] hits =
            Physics2D.OverlapCircleAll(
                attackPoint.transform.position,
                attackRadius,
                playerLayer);

        foreach (Collider2D hit in hits)
        {
            Player playerScript =
                hit.GetComponent<Player>();

            if (playerScript != null)
            {
                Debug.Log("ƒqƒbƒg");

                playerScript.TakeDamage(damage);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;

        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            attackPoint.transform.position,
            attackRadius);
    }

    public void SetDead()
    {
        isDead = true;

        CancelInvoke();
    }

    public void Stun()
    {
        // €–S’†‚Í–³‹
        if (isDead)
            return;

        isStunned = true;

        //UŒ‚ƒLƒƒƒ“ƒZƒ‹
        isAttacking = false;

        //—\”õ“®ì‚âUŒ‚‚ğƒLƒƒƒ“ƒZƒ‹
        CancelInvoke(nameof(Attack1));
        CancelInvoke(nameof(Attack2));
        CancelInvoke(nameof(ReturnToIdle));

        canAttack = true;
        rb.linearVelocity = Vector2.zero;

        sr.sprite = idleSprite;

        Invoke(nameof(EndStun), stunTime);
    }

    void EndStun()
    {
        isStunned = false;

        if (!isAttacking)
        {
            sr.sprite = idleSprite;
        }
    }
}