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

    //振り向き前のセリフ
    public string[] warningTexts =
    {
    "そろそろ桐生の様子を見てみるか...",
    "え？今、やった...",
    "まだ桐生はやらねえのか...",
    "銃声が聞こえた気が...",
    "そろそろみよっと"
};

    // 戻る時のセリフ
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

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();

        sr.sprite = lookSprite;

        dialoguePanel.SetActive(false);
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

            dialoguePanel.SetActive(true);

            // ランダムなセリフを3回表示
            for (int i = 0; i < 3; i++)
            {
                string randomText =
                    warningTexts[
                        Random.Range(0, warningTexts.Length)];

                countDownText.text = randomText;

                yield return new WaitForSeconds(1f);
            }

            dialoguePanel.SetActive(false);

            // 振り向き
            isLooking = true;
            sr.sprite = lookSprite;

            Debug.Log("振り向いた");

            yield return new WaitForSeconds(lookTime);

            // 戻る時のセリフ
            dialoguePanel.SetActive(true);

            for (int i = 0; i < 2; i++)
            {
                string randomText =
                    returnTexts[
                        Random.Range(0, returnTexts.Length)];

                countDownText.text = randomText;

                yield return new WaitForSeconds(1f);
            }

            dialoguePanel.SetActive(false);

            // 元に戻る
            isLooking = false;
            sr.sprite = backSprite;

            Debug.Log("後ろ向き");
        }
    }
    public void CheckEnemyDeath()
    {
        if (isLooking)
        {
            StartCoroutine(GameOverSequence());
        }
    }

    IEnumerator GameOverSequence()
    {
        Player player =
            FindAnyObjectByType<Player>();

        if (player == null)
            yield break;

        dialoguePanel.SetActive(true);

        countDownText.text = gameOverText;

        yield return new WaitForSeconds(2f);

        dialoguePanel.SetActive(false);

        player.GameOver();
    }

    public void StartDarumaGame()
    {
        sr.sprite = backSprite;
        isLooking = false;
        StartCoroutine(DarumaRoutine());
    }
}