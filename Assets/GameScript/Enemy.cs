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

    private Collider2D leftWallCol;
    private Collider2D rightWallCol;

    //HPÉoÅ[
    private Canvas hpCanvas;

    void Start()
    {
        hp = maxHp;

        sr = GetComponent<SpriteRenderer>();
        enemyCol = GetComponent<Collider2D>();

        // ï«éÊìæ
        GameObject leftWall = GameObject.Find("LeftWall");
        GameObject rightWall = GameObject.Find("RightWall");

        if (leftWall != null)
        {
            leftWallCol = leftWall.GetComponent<Collider2D>();

            Physics2D.IgnoreCollision(
                enemyCol,
                leftWallCol,
                true);
        }

        if (rightWall != null)
        {
            rightWallCol = rightWall.GetComponent<Collider2D>();

            Physics2D.IgnoreCollision(
                enemyCol,
                rightWallCol,
                true);
        }

        hpCanvas = GetComponentInChildren<Canvas>();

        if (hpCanvas != null)
        {
            hpCanvas.enabled = false;
        }
    }

    void Update()
    {
        if (!enteredScreen)
        {
            Vector3 viewPos =
                Camera.main.WorldToViewportPoint(
                    transform.position);

            if (viewPos.x >= 0f &&
                viewPos.x <= 1f)
            {
                enteredScreen = true;

                // ï«Ç∆ÇÃìñÇΩÇËîªíËÇñﬂÇ∑
                if (leftWallCol != null)
                {
                    Physics2D.IgnoreCollision(
                        enemyCol,
                        leftWallCol,
                        false);
                }

                if (rightWallCol != null)
                {
                    Physics2D.IgnoreCollision(
                        enemyCol,
                        rightWallCol,
                        false);
                }

                Debug.Log("ìGÇ™ì¸èÍÇµÇΩ");
            }
        }
    }

    public void TakeDamage(int damage)
    {
        hp -= damage;

        EnemyAI ai = GetComponent<EnemyAI>();

        if (ai != null)
        {
            Debug.Log("[Enemy]Call To Stun");
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
            //ai.enabled = false;
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

        Canvas hpCanvas =
            GetComponentInChildren<Canvas>();

        if (hpCanvas != null)
        {
            hpCanvas.enabled = false;
        }


        if(gameObject == null)
        {
            Debug.LogWarning("gameObject null");
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
        if (hpCanvas != null)
        {
            hpCanvas.enabled = show;
        }
    }
}
