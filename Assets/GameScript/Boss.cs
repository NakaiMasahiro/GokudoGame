using System.Collections;
using UnityEngine;
using TMPro;


public class Boss : MonoBehaviour
{
    // 画像
    public Sprite backSprite;
    public Sprite lookSprite;

    // ランダム待機時間
    public float minWaitTime = 3f;
    public float maxWaitTime = 8f;

    // 見ている時間
    public float lookTime = 2f;

    // 振り向き中か
    public bool isLooking = false;

    private SpriteRenderer sr;

    //Panel
    public GameObject dialoguePanel;

    //振り向き前
    public string[] warningTexts =
    {
    "そろそろ桐生の様子を見てみるか...",
    "え？今、やった...",
    "まだ桐生はやらねえのか...",
    "銃声が聞こえた気が...",
    "そろそろみよっと"
};

    // 戻る時
    public string[] returnTexts =
    {
    "チッ...",
    "仕事に戻るか...",
    "まだやってねえのか...",
    "惜しいな...",
    "フン..."
};

    //GameOver時のセリフ
    [TextArea]
    public string gameOverText = "何をしている！";

    //コメント
    public TMP_Text countDownText;

    // カウントの進行中コルーチン
    private Coroutine darumaCoroutine;

    // ゲームオーバー演出が始まっているか
    private bool gameOverTriggered = false;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();

        sr.sprite = lookSprite;

        SetPanel(false);
    }

    void SetPanel(bool active)
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(active);
        }
    }

    // 配列からランダムに1つ選んで表示する
    void ShowRandomText(string[] texts)
    {
        if (countDownText == null || texts == null || texts.Length == 0)
            return;

        countDownText.text =
            texts[Random.Range(0, texts.Length)];
    }

    IEnumerator DarumaRoutine()
    {
        while (true)
        {
            // ランダム待機
            float waitTime =
                Random.Range(
                    minWaitTime,
                    maxWaitTime);

            yield return new WaitForSeconds(waitTime);

            SetPanel(true);

            // ランダムなセリフを3回表示
            for (int i = 0; i < 3; i++)
            {
                ShowRandomText(warningTexts);

                yield return new WaitForSeconds(1f);
            }

            SetPanel(false);

            // 振り向き
            isLooking = true;
            sr.sprite = lookSprite;

            Debug.Log("振り向いた");

            yield return new WaitForSeconds(lookTime);

            // 戻る時のセリフ
            SetPanel(true);

            for (int i = 0; i < 2; i++)
            {
                ShowRandomText(returnTexts);

                yield return new WaitForSeconds(1f);
            }

            SetPanel(false);

            // 元に戻る
            isLooking = false;
            sr.sprite = backSprite;

            Debug.Log("後ろ向き");
        }
    }

    public void CheckEnemyDeath()
    {
        if (!isLooking || gameOverTriggered)
            return;

        Player player =
            FindAnyObjectByType<Player>();

        if (player == null)
            return;

        gameOverTriggered = true;

        // カウントの進行を止める
        if (darumaCoroutine != null)
        {
            StopCoroutine(darumaCoroutine);
            darumaCoroutine = null;
        }

        StartCoroutine(GameOverSequence(player));
    }

    IEnumerator GameOverSequence(Player player)
    {
        SetPanel(true);

        if (countDownText != null)
        {
            countDownText.text = gameOverText;
        }

        yield return new WaitForSeconds(1.5f);

        SetPanel(false);

        if (player != null)
        {
            player.GameOver();
        }
    }

    public void StartDarumaGame()
    {
        sr.sprite = backSprite;
        isLooking = false;

        if (darumaCoroutine != null)
        {
            StopCoroutine(darumaCoroutine);
        }

        darumaCoroutine = StartCoroutine(DarumaRoutine());
    }
}