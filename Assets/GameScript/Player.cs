using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore;
using System.Collections;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{

    //HPの最大値
    public int maxHp = 1000;
    public int hp = 1000;

    // 画像
    public Sprite idleSprite;
    public Sprite rightPanch;
    public Sprite leftPanch;
    public Sprite kikku;
    public Sprite gunSprite;
    public Sprite guardSprite;
    public Sprite tigerDrop;

    //Spriteを反転
    private bool facingLeft = false;

    // ダメージ
    public int rightPunchDamage = 50;
    public int leftPunchDamage = 60;
    public int kikkuDamage = 100;
    public int gunDamage = 500;

    //攻撃硬直
    public float attackMoveLockTime = 0.2f;
    private bool isMoveLocked = false;

    //銃の弾数
    public int maxBullets = 10;
    private int currentBullets;

    // ヒット判定
    public GameObject hitSystem;

    //攻撃画像表示時間
    public float attackAnimationTime = 0.5f;

    // 移動
    public float moveSpeed;
    public float guardSpeed = 2f; //構え
    public float normalSpeed = 5f; //通常速度

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Vector2 movement;

    // コンボ
    private int combo = 0;

    //コンボ途切れる時間
    public float comboResetTime = 1.2f;

    //最終攻撃時間
    private float lastAttackTime;

    // 攻撃可能か
    private bool canAttack = true;

    //構え
    private bool guard = false;

    //攻撃中には連打できない
    private bool isAttacking = false;

    //キック後の硬直
    public float comboEndCooldown = 0.7f;

    //回避
    public float backStepForce = 8f;
    public float backStepCooldown = 0.4f;

    private bool canBackStep = true;
    private bool isBackStepping = false;

    public float sideStepDistance = 1.5f;
    public float lockOnDistance = 3f;

    public float sideStepDuration = 0.15f;

    //GameOverのパネル
    public GameObject gameOverPanel;

    private bool isGameOver = false;

    // カウンター
    public int tigerDropDamage = 1000;

    private bool tigerDropReady = false;

    public float tigerDropWindow = 0.2f;
    public float tigerDropMissStiffness = 1.0f;

    private bool isTigerDropStance = false;

    // 虎落としクールタイム
    public float tigerDropCooldown = 2f;

    private bool canTigerDrop = true;

    public bool IsTigerDropReady()
    {
        return tigerDropReady;
    }


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();

        hp = maxHp;
        currentBullets = maxBullets;

        moveSpeed = normalSpeed;

        sr.sprite = idleSprite;

    }

    void Update()
    {
        if (isGameOver)
            return;

        //構え
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);

            Time.timeScale = 1f;
        }

        //構え
        if (Keyboard.current.spaceKey.isPressed)
        {
            guard = true;
            moveSpeed = guardSpeed;

            if (!isAttacking && !isTigerDropStance)
            {
                sr.sprite = guardSprite;
            }

            Transform nearestEnemy = GetClosestEnemy();

            if (nearestEnemy != null)
            {
                if (nearestEnemy.position.x < transform.position.x)
                {
                    sr.flipX = true;
                }
                else
                {
                    sr.flipX = false;
                }
            }
        }
        else
        {
            guard = false;
            moveSpeed = normalSpeed;

            if (!isAttacking)
            {
                sr.sprite = idleSprite;
            }
        }

        if (guard &&
      canTigerDrop &&
      !tigerDropReady &&
      Keyboard.current.eKey.wasPressedThisFrame)
        {
            StartTigerDrop();
        }

        // 移動
        Vector2 input = Vector2.zero;

        if (Keyboard.current.aKey.isPressed)
        {
            input.x = -1;

            if (!guard)
            {
                sr.flipX = true;
            }
        }

        if (Keyboard.current.dKey.isPressed)
        {
            input.x = 1;

            if (!guard)
            {
                sr.flipX = false;
            }
        }
        movement = input.normalized;

        if (canBackStep &&
      (Keyboard.current.leftShiftKey.wasPressedThisFrame ||
       Keyboard.current.rightShiftKey.wasPressedThisFrame))
        {
            // ★攻撃キャンセル
            if (isAttacking)
            {
                CancelInvoke(nameof(ReturnToIdle));
                CancelInvoke(nameof(EndAttack));

                isAttacking = false;
                isMoveLocked = false;

                ReturnToIdle();
            }

            BackStep();
        }

        // パンチ
        if (!isAttacking && canAttack && Mouse.current.leftButton.wasPressedThisFrame)
        {
            isAttacking = true;
            isMoveLocked = true;

            Invoke(nameof(EndMoveLock), attackMoveLockTime);

            if (Time.time - lastAttackTime > comboResetTime)
            {
                combo = 0;
            }

            lastAttackTime = Time.time;

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

                canAttack = false;
                Invoke(nameof(EnableComboAttack), comboEndCooldown);
            }

            CancelInvoke(nameof(ReturnToIdle));
            CancelInvoke(nameof(EndAttack));

            Invoke(nameof(ReturnToIdle), attackAnimationTime);
            Invoke(nameof(EndAttack), attackAnimationTime);
        }

        // 銃
        if (!isAttacking && canAttack && currentBullets > 0 && Mouse.current.rightButton.wasPressedThisFrame)
        {
            isAttacking = true;
            canAttack = false;

            sr.sprite = gunSprite;

            currentBullets--;

            Enemy target = GetClosestEnemyInFront();

            if (target != null)
            {
                target.TakeDamage(gunDamage);

                Debug.Log(target.name + "に命中");
            }
            else
            {
                Debug.Log("前方に敵がいない");
            }

            Debug.Log("銃 " + gunDamage + "ダメージ");
            Debug.Log("残弾 : " + currentBullets);

            CancelInvoke(nameof(ReturnToIdle));
            CancelInvoke(nameof(EndAttack));

            Invoke(nameof(ReturnToIdle), attackAnimationTime);

            Invoke(nameof(EndAttack), attackAnimationTime);
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
        if (!isBackStepping && !isMoveLocked)
        {
            rb.linearVelocity = movement * moveSpeed;
        }
        else if (isMoveLocked)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    void ReturnToIdle()
    {
        if (guard)
        {
            sr.sprite = guardSprite;
        }
        else
        {
            sr.sprite = idleSprite;
        }
    }

    void EnableAttack()
    {
        canAttack = true;
    }

    void EnableComboAttack()
    {
        canAttack = true;
    }

    //バックステップ
    void BackStep()
    {
        canBackStep = false;

        // 構え中
        if (guard)
        {
            Transform nearestEnemy = GetClosestEnemy();

            if (nearestEnemy != null)
            {
                bool movingTowardEnemy = false;

                // 敵が右側にいる
                if (nearestEnemy.position.x > transform.position.x)
                {
                    movingTowardEnemy = Keyboard.current.dKey.isPressed;
                }
                // 敵が左側にいる
                else
                {
                    movingTowardEnemy = Keyboard.current.aKey.isPressed;
                }

                // 敵方向へ入力している時だけ回り込み
                if (movingTowardEnemy)
                {
                    float distance =
                        Vector2.Distance(
                            transform.position,
                            nearestEnemy.position);

                    if (distance <= lockOnDistance)
                    {
                        Vector3 targetPos;

                        // 敵を挟んで反対側へ移動
                        if (transform.position.x < nearestEnemy.position.x)
                        {
                            targetPos =
                                nearestEnemy.position +
                                Vector3.right * sideStepDistance;
                        }
                        else
                        {
                            targetPos =
                                nearestEnemy.position +
                                Vector3.left * sideStepDistance;
                        }

                        StartCoroutine(SideStep(targetPos));

                        Invoke(nameof(EnableBackStep), backStepCooldown);
                        return;
                    }
                }
            }
        }

        // 通常のバックステップ
        isBackStepping = true;

        Vector2 direction;

        if (sr.flipX)
        {
            // 左向きなら右へ下がる
            direction = Vector2.right;
        }
        else
        {
            // 右向きなら左へ下がる
            direction = Vector2.left;
        }

        rb.linearVelocity =
            new Vector2(
                direction.x * backStepForce,
                rb.linearVelocity.y);

        Invoke(nameof(EndBackStep), 0.15f);
        Invoke(nameof(EnableBackStep), backStepCooldown);
    }

    void EnableBackStep() //いーなぶるバックステップ
    {
        canBackStep = true;
    }

    void EndBackStep()  //エンドバックステップ
    {
        isBackStepping = false;
        rb.linearVelocity = Vector2.zero;
    }

    void EndAttack()
    {
        isAttacking = false;
    }

    void EndMoveLock()
    {
        isMoveLocked = false;
    }

    public void TakeDamage(int damage)
    {
        hp -= damage;

        if (hp <= 0)
        {
            hp = 0;
            GameOver();
        }
    }

    Transform GetClosestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        Transform closestEnemy = null;
        float closestDistance = Mathf.Infinity;

        foreach (GameObject enemy in enemies)
        {
            float distance = Vector2.Distance(transform.position, enemy.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy.transform;
            }
        }
        return closestEnemy;
    }

    Enemy GetClosestEnemyInFront()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        Enemy closestEnemy = null;
        float closestDistance = Mathf.Infinity;

        foreach (GameObject enemyObj in enemies)
        {
            Enemy enemy = enemyObj.GetComponent<Enemy>();

            if (enemy == null)
                continue;

            float direction =
                enemy.transform.position.x - transform.position.x;

            // 左向き
            if (sr.flipX)
            {
                if (direction > 0)
                    continue;
            }
            // 右向き
            else
            {
                if (direction < 0)
                    continue;
            }

            float distance =
                Vector2.Distance(
                    transform.position,
                    enemy.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy;
            }
        }

        return closestEnemy;
    }

    // ★追加
    IEnumerator SideStep(Vector3 targetPos)
    {
        isBackStepping = true;

        Vector3 startPos = transform.position;

        float time = 0f;

        while (time < sideStepDuration)
        {
            time += Time.deltaTime;

            transform.position =
                Vector3.Lerp(startPos, targetPos, time / sideStepDuration);

            yield return null;
        }

        transform.position = targetPos;

        isBackStepping = false;
    }
    public void GameOver()
    {
        isGameOver = true;

        Debug.Log("GAME OVER");

        rb.linearVelocity = Vector2.zero;

        gameOverPanel.SetActive(true);

        Time.timeScale = 0f;
    }
    public void RestartGame()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex);
    }
    void StartTigerDrop()
    {
        canTigerDrop = false;
        tigerDropReady = true;

        isTigerDropStance = true;

        isMoveLocked = true;

        sr.sprite = tigerDrop;

        Debug.Log("虎落とし構え");

        Invoke(nameof(FailTigerDrop), tigerDropWindow);
    }


    //虎落とし失敗
    void FailTigerDrop()
    {
        if (!tigerDropReady)
            return;

        tigerDropReady = false;
        isTigerDropStance = false;

        Debug.Log("虎落とし失敗");

        canAttack = false;

        Invoke(nameof(EndMoveLock), tigerDropMissStiffness);
        Invoke(nameof(EnableAttack), tigerDropMissStiffness);

        Invoke(nameof(EnableTigerDrop), tigerDropCooldown);
    }

    //虎落とし成功
    public void TigerDropSuccess(Enemy enemy)
    {
        if (!tigerDropReady)
            return;

        tigerDropReady = false;
        isTigerDropStance = false;

        isMoveLocked = false;

        CancelInvoke(nameof(FailTigerDrop));


        enemy.TakeDamage(tigerDropDamage);

        Debug.Log("虎落とし成功！");

        Invoke(nameof(ReturnToIdle), 0.3f);

        Invoke(nameof(EnableTigerDrop),tigerDropCooldown);
    }
    void EnableTigerDrop()
    {
        canTigerDrop = true;
    }
}