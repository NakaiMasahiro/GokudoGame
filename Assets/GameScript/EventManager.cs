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

    //スペースキーを押すと全文表示
    private bool isTyping;

    //AudioManager
    public AudioSource textAudioSource;
    public AudioClip textSE;

    private IEnumerator Start()
    {
        player.enabled = false;

        dialoguePanel.SetActive(true);

        for (int i = 0; i < dialogues.Length; i++)
        {
            DialogueData dialogue = dialogues[i];

            yield return StartCoroutine(TypeText(dialogue.speaker,dialogue.text));

            // 3個目の会話で敵投入
            if (i == 2)
            {
                spawnManager.StartFirstWave();
            }

            yield return new WaitUntil(() => Keyboard.current.spaceKey.wasPressedThisFrame);

            yield return new WaitUntil(() => Keyboard.current.spaceKey.wasPressedThisFrame);
            //yield return null;
        }

        dialoguePanel.SetActive(false);

        Boss boss = FindAnyObjectByType<Boss>();

        if (boss != null)
        {
            boss.StartDarumaGame();
        }

        EnemyAI[] enemies = FindObjectsByType<EnemyAI>();

        foreach (EnemyAI enemy in enemies)
        {
            enemy.StartBattle();
        }

        player.enabled = true;
    }

    IEnumerator TypeText(string speaker, string message)
    {
        dialogueText.text = $"<b>{speaker}</b>\n\n";

        for (int i = 0; i < message.Length; i++)
        {
            dialogueText.text += message[i];

            //2文字に1回SEをならすんだよ（そうだよ）
            if (i % 2 == 0 && message[i] != ' ' && message[i] != '\n')
            {
                textAudioSource.PlayOneShot(textSE);
            }

            if (message[i] != ' ' &&
                message[i] != '\n')
            {
                textAudioSource.PlayOneShot(textSE);
            }

            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                dialogueText.text =
                    $"<b>{speaker}</b>\n\n{message}";
                yield break;
            }

            yield return new WaitForSeconds(0.07f);
        }
    }
}