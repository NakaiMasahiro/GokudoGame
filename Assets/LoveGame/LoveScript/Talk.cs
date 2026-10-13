using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// 選択肢1つ分
[Serializable]
public class DialogueChoice
{
    public string label;          // ボタンに表示する文字
    public int affectionDelta;    // 好感度の増減

    [Tooltip("ONにすると選択後に jumpTo の行へ飛ぶ。OFFなら次の行へ")]
    public bool jump;
    [Tooltip("飛び先のElement番号(-1なら会話終了)")]
    public int jumpTo;
}

[Serializable]
public class DialogueLine
{
    // 番号表示用(自動で入るので触らなくてOK)。先頭に置くとElementの見出しになる
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

    //好感度表示(空でもOK)
    [SerializeField] private TMP_Text affectionText;

    //選択肢
    [SerializeField] private Transform choiceContainer;   // VerticalLayoutGroupを付けた親
    [SerializeField] private Button choiceButtonPrefab;   // 子にTMP_Textを持つButton

    //暗転(画面いっぱいの黒いImage)
    [SerializeField] private Image fadePanel;
    [SerializeField] private float fadeDuration = 0.6f;   // 暗くなる/明るくなる時間
    [SerializeField] private float blackHoldTime = 1f;  // 真っ暗のまま止める時間

    //会話
    [SerializeField] private DialogueLine[] lines;

    //文字の時間
    [SerializeField] private float charInterval = 0.05f;

    // 現在の好感度(他のスクリプトから読める)
    public int Affection { get; private set; }

    private int index = -1;
    private bool isTyping;
    private bool isChoosing;
    private bool isFading;
    private bool isFinished;
    private Coroutine typingRoutine;

    // Inspectorで値を変えたり並べ替えたりするたびに、行番号を自動で振り直す
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
        UpdateAffectionUI();
        SetFadeAlpha(0f);
        ShowLine(0);
    }

    private void Update()
    {
        if (!SpacePressed()) return;

        if (isFinished) return;
        if (isChoosing || isFading) return; // 選択肢表示中・暗転中はSpaceで進めない

        if (isTyping)
        {
            // 表示途中なら全文を即表示
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

    // 指定した行を表示
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

        // 文字数を正しく数えるためにメッシュを更新
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

    // 文字の表示が終わったとき
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

        foreach (var choice in choices)
        {
            var captured = choice; // ラムダ用にコピー
            var button = Instantiate(choiceButtonPrefab, choiceContainer);
            button.GetComponentInChildren<TMP_Text>().text = captured.label;
            button.onClick.AddListener(() => OnChoiceSelected(captured));
        }
    }

    private void OnChoiceSelected(DialogueChoice choice)
    {
        // 選択肢ボタンを片付ける
        foreach (Transform child in choiceContainer) Destroy(child.gameObject);
        choiceContainer.gameObject.SetActive(false);
        isChoosing = false;

        Affection += choice.affectionDelta;
        UpdateAffectionUI();

        GoTo(choice.jump ? choice.jumpTo : index + 1);
    }

    // 次の行へ進む。今の行が blackoutAfter なら暗転をはさむ
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

        yield return Fade(0f, 1f);          // だんだん暗く
        nameText.text = "";                 // 暗い間に文字を消す
        talkText.text = "";
        yield return new WaitForSeconds(blackHoldTime);

        ShowLine(next);                     // 次の会話を表示(終了なら会話終了)
        yield return Fade(1f, 0f);          // だんだん明るく

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

    private void UpdateAffectionUI()
    {
        if (affectionText) affectionText.text = $"好感度: {Affection}";
    }

    private void EndDialogue()
    {
        isFinished = true;
        //if (nextIcon) nextIcon.SetActive(false);
        Debug.Log($"会話終了 / 最終好感度: {Affection}");
    }
}