using UnityEngine;

public class EnemyAI : MonoBehaviour
{

    //画像
    public Sprite idleSprite;
    public Sprite ReadyAttack;
    public Sprite attack1Sprite;
    public Sprite attack2Sprite;

    //攻撃画像時間
    public float attackTime = 1.0f;
    // 移動速度
    public float moveSpeed = 2f;

    // 発見距離
    public float detectRange = 20f;

    // プレイヤーの前で止まる距離
    public float attackRange = 1.5f;

    //攻撃
    public int attack1 = 50;
    public int attack2 = 100;

    //攻撃間隔
    public float attackCooldown = 2f;

    private bool canAttack = true;

    private Transform player;
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    //攻撃中
    private bool isAttacking = false;

    //攻撃予備動作
    public float attackWindupTime = 0.5f;

    public GameObject attackPoint;

    public float attackRadius = 0.5f;

    //Player専用
    public LayerMask playerLayer;

    private bool isDead = false;

    //ひるみ
    private bool isStunned = false;
    public float stunTime = 0.2f;

    private Vector3 attackPointStartPos;

    //カウンター受付時間
    public float counterWindow = 0.2f;

    //勝手に中央へ歩く
    private bool entering = true;
    private float targetX;

    //入場に時間がかかりすぎた時の保険
    public float enterTimeout = 6f;
    private float enteringTimer = 0f;

    //動かさない
    public bool canMove = true;

    private bool battleStarted = false;

    //整列
    public bool startInBattle = true;

    //攻撃SE
    public AudioClip hitAttack;

    //空振りSE
    public AudioClip missAttack;

    //AudioSource
    public AudioSource audioSource;


    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            Debug.LogWarning("[EnemyAI]Rigidbody2D が無いので追加します: " + gameObject.name);
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            player = playerObj.transform;
        }

        if (attackPoint != null)
        {
            attackPointStartPos = attackPoint.transform.localPosition;
        }
        else
        {
            Debug.LogWarning("[EnemyAI]attackPoint が未設定です: " + gameObject.name);
        }

        if (startInBattle)
        {
            StartBattle();
        }
    }

    void FixedUpdate()
    {
        if (rb == null)
            return;

        // 倒された後は歩かせない
        if (isDead)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        if (!canMove)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (entering)
        {
            enteringTimer += Time.fixedDeltaTime;

            float direction =
                Mathf.Sign(
                    targetX - transform.position.x);

            rb.linearVelocity =
                new Vector2(
                    direction * moveSpeed,
                    rb.linearVelocity.y);

            if (Mathf.Abs(
                    transform.position.x - targetX) < 0.2f ||
                enteringTimer >= enterTimeout)
            {
                entering = false;

                rb.linearVelocity = Vector2.zero;
            }

            return;
        }

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

        // Playerの方を向く
        sr.flipX = player.position.x >= transform.position.x;

        // 攻撃距離まで近づいたら停止
        if (distance <= attackRange)
        {
            rb.linearVelocity = Vector2.zero;

            // 戦闘開始前会話中は殴らない
            if (canAttack && battleStarted)
            {
                Attack();
            }
        }

        // 発見距離内なら追跡
        else if (battleStarted &&
          distance <= detectRange)
        {

            Vector2 direction =
                (player.position - transform.position).normalized;

            rb.linearVelocity =
                new Vector2(
                    direction.x * moveSpeed,
                    rb.linearVelocity.y);
        }

        // 見失ったら停止
        else
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Player側に攻撃判定を移動
        if (attackPoint != null)
        {
            float side =
                player.position.x < transform.position.x ? -1f : 1f;

            attackPoint.transform.localPosition =
                new Vector3(
                    side * Mathf.Abs(attackPointStartPos.x),
                    attackPointStartPos.y,
                    attackPointStartPos.z);
        }
    }

    // 効果音を1回鳴らす
    void PlaySE(AudioClip clip)
    {
        if (audioSource == null || clip == null)
            return;

        audioSource.PlayOneShot(clip);
    }

    void EnableAttack()
    {
        canAttack = true;
    }

    // 攻撃
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

        // 共通予備動作
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

    // Playerが虎落とし待ち受け中なら、カウンターを受けて怯む
    bool TryGetCountered(Player playerScript)
    {
        if (playerScript == null || !playerScript.IsTigerDropReady())
            return false;

        Enemy self = GetComponent<Enemy>();

        if (self == null)
            return false;

        playerScript.TigerDropSuccess(self);
        Stun();

        return true;
    }

    //右パンチ
    void Attack1()
    {
        PerformAttack(attack1Sprite, attack1);
    }

    //左パンチ
    void Attack2()
    {
        PerformAttack(attack2Sprite, attack2);
    }

    void PerformAttack(Sprite attackSprite, int damage)
    {
        if (player == null)
            return;

        Player playerScript =
            player.GetComponent<Player>();

        // 虎落とし(カウンター)はどちらの攻撃にも成立する
        if (TryGetCountered(playerScript))
            return;

        if (isStunned)
            return;

        if (isDead)
            return;

        if (playerScript != null)
        {
            sr.sprite = attackSprite;

            // 当たれば命中音、外れれば空振り音（どちらか一方だけ鳴る）
            bool hit = DealDamage(damage);

            PlaySE(hit ? hitAttack : missAttack);
        }

        Invoke(nameof(ReturnToIdle), attackTime);
        Invoke(nameof(EnableAttack), attackCooldown);
    }

    void ReturnToIdle()
    {
        sr.sprite = idleSprite;

        isAttacking = false;
    }

    // Playerに当たったら true
    bool DealDamage(int damage)
    {
        if (attackPoint == null)
            return false;

        Collider2D[] hits =
            Physics2D.OverlapCircleAll(
                attackPoint.transform.position,
                attackRadius,
                playerLayer);

        bool damaged = false;

        foreach (Collider2D hit in hits)
        {
            Player playerScript =
                hit.GetComponent<Player>();

            if (playerScript != null)
            {
                playerScript.TakeDamage(damage);
                damaged = true;
            }
        }

        return damaged;
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
        // 死亡中は無視
        if (isDead)
            return;

        if (rb == null)
        {
            Debug.LogError(gameObject.name + " の rb が null");
            return;
        }

        isStunned = true;

        //攻撃キャンセル
        isAttacking = false;

        //予備動作や攻撃をキャンセル
        CancelInvoke(nameof(Attack1));
        CancelInvoke(nameof(Attack2));
        CancelInvoke(nameof(ReturnToIdle));

        canAttack = true;
        rb.linearVelocity = Vector2.zero;

        sr.sprite = idleSprite;

        // 連続で殴られた時に、古いタイマーで怯みが早く解けないようにする
        CancelInvoke(nameof(EndStun));
        Invoke(nameof(EndStun), stunTime);
    }

    private void EndStun()
    {
        isStunned = false;

        if (!isAttacking)
        {
            sr.sprite = idleSprite;
        }
    }

    public void SetTargetPosition(float x)
    {
        targetX = x;
    }

    public void StartBattle()
    {
        battleStarted = true;

        //保険の窓口
        if (detectRange <= 0f)
        {
            detectRange = 20f;
        }

        Debug.Log("StartBattle");
    }
}