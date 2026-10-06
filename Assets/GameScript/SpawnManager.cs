using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public GameObject enemyPrefab;

    public Transform rightSpawn;

    // 現在のフロア
    private int currentWave = 0;

    public GameObject bossPrefab;

    private bool bossSpawned = false;

    public Transform enemyFormationCenter;
    public float formationWidth = 2f;

    private bool firstWaveSpawned = false;

    [Header("ラスボスの解放人数")]
    public int bossUnlockKillCount = 10;

    [SerializeField] private int PreviewWave;

    [Header("各敵数")]
    public int[] waveEnemyCounts =
    {
        2,
        3,
        4
    };

    // 現在生存中の敵
    private int aliveEnemies;

    void SpawnWave()
    {
        PreviewWave = currentWave;

        Player player = FindAnyObjectByType<Player>();
        if(player == null )
        {
            Debug.LogWarning("Player ねえよ");
        }

        // 30人倒したらラスボス
        if (!bossSpawned &&
            player != null &&
            player.killCount >= bossUnlockKillCount)
        {
            bossSpawned = true;

            Instantiate(
                bossPrefab,
                rightSpawn.position,
                Quaternion.identity);

            Debug.Log("ラスボス登場！");

            return;
        }

        // ラスボス出現後は雑魚を出さない
        if (bossSpawned)
            return;

        if (currentWave >= waveEnemyCounts.Length)
        {
            currentWave = 0;
        }

        aliveEnemies = waveEnemyCounts[currentWave];

        for (int i = 0; i < aliveEnemies; i++)
        {
            float offsetX =
                (i - (aliveEnemies - 1) * 0.5f) * 1.5f;

            Vector3 spawnPos =
                rightSpawn.position +
                new Vector3(
                    offsetX,
                    0f,
                    0f);

            Debug.Log("Enemy spawn");

            GameObject enemyObj =
                Instantiate(
                    enemyPrefab,
                    spawnPos,
                    Quaternion.identity);

            EnemyAI ai =
                enemyObj.GetComponent<EnemyAI>();

            if (ai != null)
            {
                ai.canMove = true;

                // 最初のウェーブ
                if (!firstWaveSpawned)
                {
                    ai.startInBattle = false;
                }

                ai.SetTargetPosition(
                    enemyFormationCenter.position.x +
                    Random.Range(
                        -formationWidth,
                        formationWidth));

                if (firstWaveSpawned)
                {
                    ai.StartBattle();
                }
            }
        }

        firstWaveSpawned = true;

        Debug.Log(
            "フロア" +
            (currentWave + 1) +
            "開始！");
    }

    public void EnemyDefeated()
    {
        aliveEnemies--;

        Player player = FindAnyObjectByType<Player>();

        if (aliveEnemies <= 0)
        {
            // ラスボスへの到達人数
            if (player != null &&
                player.killCount >= 10)
            {
                SpawnWave();
                return;
            }

            currentWave++;

            Debug.LogWarning("EnemyDefeated return;");
            SpawnWave();
        }
    }

    public void StartFirstWave()
    {
        Debug.LogWarning("Enemy Spawn");
        currentWave = 0;

        SpawnWave();
    }
}
