using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[Serializable]
public class DialogueChoice
{
    public string label;
    public int affectionDelta;

    [Tooltip("ONにすると選択後に jumpTo の行へ飛ぶ。OFFなら次の行へ")]
    public bool jump;
    [Tooltip("飛び先のElement番号(-1なら会話終了)")]
    public int jumpTo;
}

[Serializable]
public class DialogueLine
{
    public string number;

    public string speaker;
    [TextArea(2, 4)] public string text;

    [Tooltip("このセリフの後に選択肢を出す場合は設定")]
    public DialogueChoice[] choices;

    [Tooltip("ONにするとSpaceで next の行へ飛ぶ。OFFなら次の行へ(選択肢がある行では無視)")]
    public bool jump;
    [Tooltip("飛び先のElement番号(-1なら会話終了)")]
    public int next;

    [Tooltip("ONにすると、このセリフの後に暗転してから次の会話へ進む")]
    public bool blackoutAfter;
}

public class Talk : MonoBehaviour
{
    //名前
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text talkText;
    //[SerializeField] private GameObject nextIcon;

    //好感度
    [SerializeField] private AffectionManager affection;

    //ラスト(紙芝居)
    [SerializeField] private EndingManager endingManager;

    //選択肢
    [SerializeField] private Transform choiceContainer;
    [SerializeField] private Button choiceButtonPrefab;

    //暗転
    [SerializeField] private Image fadePanel;
    [SerializeField] private float fadeDuration = 0.6f;
    [SerializeField] private float blackHoldTime = 0.8f;

    //会話
    [SerializeField] private DialogueLine[] lines;

    //文字の時間
    [SerializeField] private float charInterval = 0.04f;
    [SerializeField] private bool shuffleChoices = true;

    private int index = -1;
    private bool isTyping;
    private bool isChoosing;
    private bool isFading;
    private bool isFinished;
    private Coroutine typingRoutine;

    private void OnValidate()
    {
        if (lines == null) return;
        for (int i = 0; i < lines.Length; i++)
        {
            lines[i].number = $"[{i}] {lines[i].speaker}";
        }
    }

    private void Start()
    {
        choiceContainer.gameObject.SetActive(false);
        SetFadeAlpha(0f);
        ShowLine(0);
    }

    private void Update()
    {
        if (!SpacePressed()) return;

        if (isFinished) return;
        if (isChoosing || isFading) return;

        if (isTyping)
        {
            CompleteTyping();
        }
        else
        {
            var line = lines[index];
            GoTo(line.jump ? line.next : index + 1);
        }
    }

    private bool SpacePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }

    private void ShowLine(int i)
    {
        if (i < 0 || i >= lines.Length)
        {
            EndDialogue();
            return;
        }

        index = i;
        var line = lines[index];
        nameText.text = line.speaker;
        typingRoutine = StartCoroutine(TypeText(line.text));
    }

    private IEnumerator TypeText(string full)
    {
        isTyping = true;
        // if (nextIcon) nextIcon.SetActive(false);
        talkText.text = full;
        talkText.maxVisibleCharacters = 0;

        talkText.ForceMeshUpdate();
        int total = talkText.textInfo.characterCount;

        for (int i = 1; i <= total; i++)
        {
            talkText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(charInterval);
        }

        OnLineDisplayed();
    }

    private void CompleteTyping()
    {
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        talkText.maxVisibleCharacters = int.MaxValue;
        OnLineDisplayed();
    }

    private void OnLineDisplayed()
    {
        isTyping = false;
        // if (nextIcon) nextIcon.SetActive(true);

        var choices = lines[index].choices;
        if (choices != null && choices.Length > 0)
        {
            ShowChoices(choices);
        }
    }

    private void ShowChoices(DialogueChoice[] choices)
    {
        isChoosing = true;
        choiceContainer.gameObject.SetActive(true);

        var order = new List<DialogueChoice>(choices);
        if (shuffleChoices)
        {
            for (int i = order.Count - 1; i >= 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }
        }
        foreach (var choice in order)
        {
            var captured = choice;
            var button = Instantiate(choiceButtonPrefab, choiceContainer);
            button.GetComponentInChildren<TMP_Text>().text = captured.label;
            button.onClick.AddListener(() => OnChoiceSelected(captured));
        }
    }

    private void OnChoiceSelected(DialogueChoice choice)
    {
        foreach (Transform child in choiceContainer) Destroy(child.gameObject);
        choiceContainer.gameObject.SetActive(false);
        isChoosing = false;

        if (affection != null) affection.Add(choice.affectionDelta);

        GoTo(choice.jump ? choice.jumpTo : index + 1);
    }

    private void GoTo(int next)
    {
        if (fadePanel != null && lines[index].blackoutAfter)
        {
            StartCoroutine(BlackoutRoutine(next));
        }
        else
        {
            ShowLine(next);
        }
    }

    private IEnumerator BlackoutRoutine(int next)
    {
        isFading = true;

        yield return Fade(0f, 1f);
        nameText.text = "";
        talkText.text = "";
        yield return new WaitForSeconds(blackHoldTime);

        ShowLine(next);
        yield return Fade(1f, 0f);

        isFading = false;
    }

    private IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            SetFadeAlpha(Mathf.Lerp(from, to, t / fadeDuration));
            yield return null;
        }
        SetFadeAlpha(to);
    }

    private void SetFadeAlpha(float a)
    {
        if (fadePanel == null) return;
        var c = fadePanel.color;
        c.a = a;
        fadePanel.color = c;
    }

    private void EndDialogue()
    {
        isFinished = true;
        //if (nextIcon) nextIcon.SetActive(false);
        Debug.Log($"会話終了 / 最終好感度: {(affection != null ? affection.Value : 0)}");

        if (endingManager != null) StartCoroutine(StartEndingRoutine());
    }

    // 暗転して、好感度に応じた紙芝居を始める
    private IEnumerator StartEndingRoutine()
    {
        while (isFading) yield return null;   // 直前の暗転が終わるのを待つ
        isFading = true;

        if (fadePanel != null) yield return Fade(0f, 1f);   // だんだん暗く
        nameText.text = "";
        talkText.text = "";

        endingManager.Begin(affection != null ? affection.Value : 0);

        if (fadePanel != null)
        {
            yield return new WaitForSeconds(blackHoldTime);
            yield return Fade(1f, 0f);                       // だんだん明るく(紙芝居の1枚目が見える)
        }

        endingManager.Play();   // 明るくなったら、結末の会話文を読み始められるようにする
        isFading = false;
    }
}