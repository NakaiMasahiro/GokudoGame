using UnityEngine;

public class Enemy : MonoBehaviour
{
    // HP
    public int maxHp = 4000;
    public int hp = 4000;

    // ì|ÇÍâÊëú
    public Sprite downSprite;

    private SpriteRenderer sr;
    private Collider2D enemyCol;

    private bool enteredScreen = false;

    // âÊñ ì‡Ç…ì¸Ç¡ÇΩÇ©
    public bool HasEnteredScreen => enteredScreen;

    //ï«
    private Collider2D leftWallCol;
    private Collider2D rightWallCol;

    //HPÉoÅ[
    private Canvas hpCanvas;

    // ì|Ç≥ÇÍÇΩÇ©
    private bool isDead = false;
    public bool IsDead => isDead;

    void Start()
    {
        hp = maxHp;

        sr = GetComponent<SpriteRenderer>();
        enemyCol = GetComponent<Collider2D>();

        // ï«éÊìæ
        leftWallCol = FindWallCollider("LeftWall");
        rightWallCol = FindWallCollider("RightWall");

        // ì¸èÍÇ∑ÇÈÇ‹Ç≈ï«Ç∆ìñÇΩÇÁÇ»Ç¢
        SetIgnoreWalls(true);

        hpCanvas = GetComponentInChildren<Canvas>();

        if (hpCanvas != null)
        {
            hpCanvas.enabled = false;
        }
    }

    Collider2D FindWallCollider(string wallName)
    {
        GameObject wall = GameObject.Find(wallName);

        if (wall == null)
            return null;

        return wall.GetComponent<Collider2D>();
    }

    void SetIgnoreWalls(bool ignore)
    {
        if (enemyCol == null)
            return;

        if (leftWallCol != null)
        {
            Physics2D.IgnoreCollision(enemyCol, leftWallCol, ignore);
        }

        if (rightWallCol != null)
        {
            Physics2D.IgnoreCollision(enemyCol, rightWallCol, ignore);
        }
    }

    void Update()
    {
        if (enteredScreen)
            return;

        Camera cam = Camera.main;

        if (cam == null)
            return;

        Vector3 viewPos =
            cam.WorldToViewportPoint(
                transform.position);

        if (viewPos.x >= 0f &&
            viewPos.x <= 1f)
        {
            enteredScreen = true;

            // ï«Ç∆ÇÃìñÇΩÇËîªíËÇñﬂÇ∑
            SetIgnoreWalls(false);

            Debug.Log("ìGÇ™ì¸èÍÇµÇΩ");
        }
    }

    public void TakeDamage(int damage)
    {
        // ì|ÇÍÇΩå„ÇÃí«åÇÇÕñ≥å¯
        if (isDead)
            return;

        hp -= damage;

        EnemyAI ai = GetComponent<EnemyAI>();

        if (ai != null)
        {
            ai.Stun();
        }

        Debug.Log("ìGHP : " + hp);

        if (hp <= 0)
        {
            hp = 0;
            Die();
        }
    }

    void Die()
    {
        if (isDead)
            return;

        isDead = true;

        Player player =
            FindAnyObjectByType<Player>();

        if (player != null)
        {
            player.AddKill();
        }

        Debug.Log("ìGÇì|ÇµÇΩ");

        Boss boss = FindAnyObjectByType<Boss>();

        if (boss != null)
        {
            boss.CheckEnemyDeath();
        }

        if (sr != null)
        {
            sr.sprite = downSprite;
        }

        transform.rotation =
            Quaternion.Euler(0f, 0f, -90f);

        transform.position +=
            new Vector3(0f, -0.4f, 0f);

        EnemyAI ai = GetComponent<EnemyAI>();

        if (ai != null)
        {
            ai.SetDead();
        }

        Rigidbody2D rb =
            GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (enemyCol != null)
        {
            enemyCol.enabled = false;
        }

        if (hpCanvas != null)
        {
            hpCanvas.enabled = false;
        }

        Destroy(gameObject, 1f);

        SpawnManager spawnManager =
            FindAnyObjectByType<SpawnManager>();

        if (spawnManager != null)
        {
            spawnManager.EnemyDefeated();
        }
    }

    public void ShowHpBar(bool show)
    {
        if (isDead)
            show = false;

        if (hpCanvas != null)
        {
            hpCanvas.enabled = show;
        }
    }
}