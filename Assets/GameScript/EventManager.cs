using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

[System.Serializable]
public class DialogueData
{
    public string speaker;

    [TextArea]
    public string text;
}


public class EventManager : MonoBehaviour
{
    public GameObject dialoguePanel;
    public TMP_Text dialogueText;

    public Player player;

    public DialogueData[] dialogues;

    public SpawnManager spawnManager;

    //文字を1つ出す間隔
    public float charInterval = 0.07f;

    //スペースキーを押すと全文表示
    private bool isTyping;

    //AudioManager
    public AudioSource textAudioSource;
    public AudioClip textSE;

    private bool IsSpacePressed()
    {
        return Keyboard.current != null &&
               Keyboard.current.spaceKey.wasPressedThisFrame;
    }

    private IEnumerator Start()
    {
        if (player != null)
        {
            player.enabled = false;
        }

        dialoguePanel.SetActive(true);

        bool firstWaveStarted = false;

        for (int i = 0; i < dialogues.Length; i++)
        {
            DialogueData dialogue = dialogues[i];

            yield return StartCoroutine(TypeText(dialogue.speaker, dialogue.text));

            // 3個目の会話で敵投入
            if (i == 2)
            {
                firstWaveStarted = true;

                if (spawnManager != null)
                {
                    spawnManager.StartFirstWave();
                }
            }

            // 全文表示に使ったスペースの同フレームを読み飛ばしてから、次の入力を待つ
            yield return null;
            yield return new WaitUntil(IsSpacePressed);
        }

        // 会話が2個以下でも、敵が出ないままにならないようにする
        if (!firstWaveStarted && spawnManager != null)
        {
            spawnManager.StartFirstWave();
        }

        dialoguePanel.SetActive(false);

        Boss boss = FindAnyObjectByType<Boss>();

        if (boss != null)
        {
            boss.StartDarumaGame();
        }

        EnemyAI[] enemies =
     FindObjectsByType<EnemyAI>(FindObjectsInactive.Exclude);

        foreach (EnemyAI enemy in enemies)
        {
            enemy.StartBattle();
        }

        if (player != null)
        {
            player.enabled = true;
        }
    }

    IEnumerator TypeText(string speaker, string message)
    {
        string header = $"<b>{speaker}</b>\n\n";

        dialogueText.text = header;

        // 前の会話を送ったスペースの同フレームで、いきなり全文表示にならないようにする
        yield return null;

        for (int i = 0; i < message.Length; i++)
        {
            dialogueText.text += message[i];

            //2文字に1回SEをならすんだよ（そうだよ）
            if (i % 2 == 0 && message[i] != ' ' && message[i] != '\n')
            {
                if (textAudioSource != null && textSE != null)
                {
                    textAudioSource.PlayOneShot(textSE);
                }
            }

            // 待っている間も毎フレーム、スキップ入力を見る
            float timer = 0f;

            while (timer < charInterval)
            {
                if (IsSpacePressed())
                {
                    dialogueText.text = header + message;
                    yield break;
                }

                timer += Time.deltaTime;

                yield return null;
            }
        }
    }
}