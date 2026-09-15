using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    // HP
    public int hp = 1000;

    // 画像
    public Sprite idleSprite;
    public Sprite rightPanch;
    public Sprite leftPanch;
    public Sprite kikku;
    public Sprite gunSprite;

    // ダメージ
    public int rightPunchDamage = 50;
    public int leftPunchDamage = 60;
    public int kikkuDamage = 100;
    public int gunDamage = 500;

    // ヒット判定
    public GameObject hitSystem;

    // 移動
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Vector2 movement;

    // コンボ
    private int combo = 0;

    // 攻撃可能か
    private bool canAttack = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        sr.sprite = idleSprite;
    }

    void Update()
    {
        // 移動
        Vector2 input = Vector2.zero;

        if (Keyboard.current.aKey.isPressed)
            input.x = -1;

        if (Keyboard.current.dKey.isPressed)
            input.x = 1;

        movement = input.normalized;

        // パンチ
        if (canAttack && Mouse.current.leftButton.wasPressedThisFrame)
        {
            combo++;

            HitSystem hit = hitSystem.GetComponent<HitSystem>();

            if (combo == 1)
            {
                sr.sprite = rightPanch;

                if (hit.currentEnemy != null)
                {
                    hit.currentEnemy.TakeDamage(rightPunchDamage);
                }

                Debug.Log("右パンチ " + rightPunchDamage + "ダメージ");
            }
            else if (combo == 2)
            {
                sr.sprite = leftPanch;

                if (hit.currentEnemy != null)
                {
                    hit.currentEnemy.TakeDamage(leftPunchDamage);
                }

                Debug.Log("左パンチ " + leftPunchDamage + "ダメージ");
            }
            else if (combo == 3)
            {
                sr.sprite = kikku;

                if (hit.currentEnemy != null)
                {
                    hit.currentEnemy.TakeDamage(kikkuDamage);
                }

                Debug.Log("キック " + kikkuDamage + "ダメージ");

                combo = 0;
            }

            CancelInvoke(nameof(ReturnToIdle));
            Invoke(nameof(ReturnToIdle), 0.15f);
        }

        // 銃
        if (canAttack && Mouse.current.rightButton.wasPressedThisFrame)
        {
            canAttack = false;

            sr.sprite = gunSprite;

            Debug.Log("銃 " + gunDamage + "ダメージ");

            CancelInvoke(nameof(ReturnToIdle));
            Invoke(nameof(ReturnToIdle), 0.5f);

            Invoke(nameof(EnableAttack), 0.5f);
        }

        // テスト
        if (Keyboard.current.hKey.wasPressedThisFrame)
        {
            TakeDamage(100);
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = movement * moveSpeed;
    }

    void ReturnToIdle()
    {
        sr.sprite = idleSprite;
    }

    void EnableAttack()
    {
        canAttack = true;
    }

    public void TakeDamage(int damage)
    {
        hp -= damage;

        if (hp <= 0)
        {
            hp = 0;
            Debug.Log("ゲームオーバー");
        }

        Debug.Log("現在HP : " + hp);
    }
}