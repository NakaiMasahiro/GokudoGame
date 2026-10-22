using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// 歩いて登場するときの、出てくる方向
public enum WalkIn
{
    None,        // 歩かない(ふつうに表示)
    FromLeft,    // 画面の左端から入ってくる
    FromRight    // 画面の右端から入ってくる
}

// 歩いて去るときの、向かう方向
public enum WalkOut
{
    None,        // 歩かない(ふつうにフェードアウトして消える)
    ToLeft,      // 画面の左端へ去っていく
    ToRight      // 画面の右端へ去っていく
}

// (旧バージョンのデータを引き継ぐためだけに残している)
public enum CharacterSlot
{
    Left,
    Center,
    Right
}

// 背景の見せ方
public enum BackgroundFit
{
    Stretch,      // 画面いっぱいに引き伸ばす(縦横比は変わる)  ← 初期値
    KeepAspect,   // 縦横比を保つ(絵が伸びない。画面とずれる分は黒い余白になる)
    Fill          // 縦横比を保ったまま画面いっぱいにする(はみ出る分は切れる)
}

// 人物1人分の指定(この場面で出す・入れ替える・消す)
[Serializable]
public class CharacterEntry
{
    public string label;   // 自動で入ります(触らなくてOK)

    [Tooltip("立ち位置の番号。EndingManager の Character Slots の何番目か(0から数える)")]
    public int slot;

    [Tooltip("人物の絵。空なら、その位置の人物は今のまま")]
    public Sprite sprite;

    [Tooltip("ONにすると、その位置の人物を消す")]
    public bool hide;

    [Tooltip("画面の端から入ってくる。着いてからセリフが出る")]
    public WalkIn walkIn;

    [Tooltip("「消す(Hide)」がONのときだけ有効。フェードアウトではなく、画面の端へ歩いて去っていく。去ったあとにセリフが出る")]
    public WalkOut walkOut;
}

// 結末の会話1つ分(背景 + 人物 + セリフ)
[Serializable]
public class EndingSlide
{
    [FormerlySerializedAs("image")]
    [Tooltip("背景の絵。空なら前の背景のまま。入れると、画面全体がフェードして切り替わる")]
    public Sprite background;

    [Tooltip("背景の見せ方。背景の絵を入れた場面で有効。Stretch=画面いっぱいに引き伸ばす(初期値) / KeepAspect=縦横比を保つ(余白は黒) / Fill=縦横比を保ったまま画面いっぱい(はみ出る分は切れる)")]
    public BackgroundFit backgroundStyle;

    [Tooltip("ONにすると、この場面は挿絵: 上の Background の絵だけを映し、人物は隠す。次の場面で元の背景と人物に戻る(この場面の Characters の指定は使われない)")]
    public bool illustration;

    [Tooltip("この場面で出す・入れ替える・消す人物。人物がいない場面は Size を 0 のままにする。書かなかった位置の人物は今のまま")]
    public CharacterEntry[] characters;

    public string speaker;                                  // 話者名(空なら名前欄を隠す)

    [FormerlySerializedAs("caption")]
    [TextArea(2, 4)] public string text;                    // セリフ本文
    [HideInInspector] public Sprite leftCharacter;
    [HideInInspector] public bool hideLeft;
    [FormerlySerializedAs("character")]
    [HideInInspector] public Sprite centerCharacter;
    [FormerlySerializedAs("hideCharacter")]
    [HideInInspector] public bool hideCenter;
    [HideInInspector] public Sprite rightCharacter;
    [HideInInspector] public bool hideRight;
    [HideInInspector] public WalkIn walkIn;
    [HideInInspector] public CharacterSlot walkSlot = CharacterSlot.Center;
    [HideInInspector] public bool migrated;
}

// 結末1つ分
[Serializable]
public class Ending
{
    public string name;                       // 管理用の名前(例: HappyEnd)
    public int minAffection;                  // この好感度以上で当てはまる
    public EndingSlide[] slides;              // 会話を順番に(背景・人物・セリフ)

    [Tooltip("最後に暗転したあと、画面に出す表記(例: End1)。空なら表記なし")]
    public string endLabel;
}

// 好感度に応じた結末を、背景・人物・会話文で表示するスクリプト
public class EndingManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject endingRoot;     // 黒背景のImageを付けた、画面いっぱいの親
    [SerializeField] private CanvasGroup slideGroup;    // 背景・人物・文字をまとめた子(フェード用)

    [FormerlySerializedAs("slideImage")]
    [SerializeField] private Image backgroundImage;     // 背景を表示するImage(一番奥)

    [Header("人物の立ち位置(いくつでも増やせる。人物がいない結末なら空でOK)")]
    [SerializeField] private Image[] characterSlots;    // 立ち位置ごとのImage。番号は0から

    [Header("文字")]
    [SerializeField] private TMP_Text nameText;         // 名前の表示欄(空でもOK)
    [SerializeField] private GameObject nameBox;        // 名前の台(枠・背景)。あれば、名前の出し入れは台ごと行う(空でもOK)
    [SerializeField] private GameObject bubble;         // 吹き出し(セリフの背景)。セリフが空の間・入ってくる間は隠れる(空でもOK)

    [FormerlySerializedAs("captionText")]
    [SerializeField] private TMP_Text talkText;         // セリフの表示欄

    [Header("結末データ")]
    [SerializeField] private Ending[] endings;

    [Header("設定")]
    [SerializeField] private float fadeDuration = 0.5f;  // 絵が切り替わるときのフェード時間
    [SerializeField] private float charInterval = 0.04f; // 1文字あたりの秒数
    [SerializeField] private TypeSound typeSound;        // 文字が出るときの効果音

    [Header("歩いて登場")]
    [SerializeField] private float walkDuration = 1.5f;  // 端から立ち位置まで入ってくる時間(秒)
    [SerializeField] private bool artFacesLeft = true;   // 人物の絵が左向きならON(右向きならOFF)
    [SerializeField] private bool faceWalkDirection = true; // ONなら、入ってくる間は進む方向を向く(着いたら元の向きに戻る)

    [Header("人物の大きさ")]
    [SerializeField] private float characterHeight = 300f; // 人物の高さ(px)。横幅は絵の縦横比から自動。0にすると各Imageの設定のまま

    [Header("エンド表記(最後の暗転のあとに出す)")]
    [SerializeField] private TMP_Text endLabelText;          // 表記を出すText。endingRoot の直下で、slideGroup より手前に置く(空ならONにしない)
    [SerializeField] private float labelFadeDuration = 1f;   // 表記がじわっと出る時間(秒)
    [SerializeField] private float labelHoldTime = 3f;       // 表記を見せる時間(秒)。Spaceで飛ばせる

    [Header("全部見終わったとき")]
    [Tooltip("ここにStart画面のシーン名を入れると、エンド表記のあとにそのシーンへ戻る(Build Settings の Scenes In Build に入れておく)。同じシーン内のStart画面に戻す場合は空にして、下の On Finished に登録する")]
    [SerializeField] private string startSceneName;      // Start画面のシーン名
    [SerializeField] private UnityEvent onFinished;      // 終わったときの処理をここに登録(シーンを使わない場合のStart画面表示など)

    // ---- 旧バージョンのデータ(Character Slots に自動で引き継がれる) ----
    [SerializeField, HideInInspector] private Image leftCharacterImage;
    [FormerlySerializedAs("characterImage")]
    [SerializeField, HideInInspector] private Image centerCharacterImage;
    [SerializeField, HideInInspector] private Image rightCharacterImage;

    private Image[] slots = new Image[0];
    private int slotCount;
    private Vector2[] homePos;   // 各人物のエディタ上の位置(歩き終わりの位置)
    private Vector3[] homeScale; // 各人物のエディタ上の大きさ・向き
    private Ending current;
    private Sprite baseSprite;           // 挿絵ではない場面の背景(挿絵のあと、これに戻る)
    private BackgroundFit baseStyle;
    private int slideIndex;
    private bool isPlaying;
    private bool isBusy;
    private bool isTyping;
    private Coroutine typingRoutine;

    private void OnValidate()
    {
        MigrateAll();

        // 人物の指定に、立ち位置の名前を表示する(Inspectorで見やすくするため)
        if (endings == null) return;
        foreach (var ending in endings)
        {
            if (ending == null || ending.slides == null) continue;
            foreach (var slide in ending.slides)
            {
                if (slide == null || slide.characters == null) continue;
                foreach (var e in slide.characters)
                {
                    if (e != null) e.label = SlotLabel(e);
                }
            }
        }
    }

    private string SlotLabel(CharacterEntry e)
    {
        string slotName = "(未設定)";
        if (characterSlots != null && e.slot >= 0 && e.slot < characterSlots.Length && characterSlots[e.slot] != null)
        {
            slotName = characterSlots[e.slot].name;
        }
        return $"[{e.slot}] {slotName}" + (e.hide ? "  ×消す" : "");
    }

    private void MigrateAll()
    {
        // 旧: 左・中央・右の3つのImage → Character Slots
        if ((characterSlots == null || characterSlots.Length == 0) &&
            (leftCharacterImage != null || centerCharacterImage != null || rightCharacterImage != null))
        {
            characterSlots = new[] { leftCharacterImage, centerCharacterImage, rightCharacterImage };
            leftCharacterImage = centerCharacterImage = rightCharacterImage = null;
        }

        // 旧: 場面ごとの左・中央・右の人物 → characters
        if (endings == null) return;
        foreach (var ending in endings)
        {
            if (ending == null || ending.slides == null) continue;
            foreach (var slide in ending.slides)
            {
                if (slide != null) MigrateSlide(slide);
            }
        }
    }

    private static void MigrateSlide(EndingSlide s)
    {
        if (s.migrated) return;
        s.migrated = true;

        var list = new List<CharacterEntry>();
        if (s.characters != null) list.AddRange(s.characters);

        AddLegacy(list, 0, s.leftCharacter, s.hideLeft, s);
        AddLegacy(list, 1, s.centerCharacter, s.hideCenter, s);
        AddLegacy(list, 2, s.rightCharacter, s.hideRight, s);

        s.characters = list.ToArray();

        s.leftCharacter = s.centerCharacter = s.rightCharacter = null;
        s.hideLeft = s.hideCenter = s.hideRight = false;
        s.walkIn = WalkIn.None;
    }

    private static void AddLegacy(List<CharacterEntry> list, int slot, Sprite sprite, bool hide, EndingSlide s)
    {
        bool walks = s.walkIn != WalkIn.None && (int)s.walkSlot == slot;
        if (sprite == null && !hide && !walks) return;

        var e = new CharacterEntry { slot = slot, sprite = sprite, hide = hide };
        if (walks) e.walkIn = s.walkIn;
        list.Add(e);
    }

    private void Awake()
    {
        MigrateAll();

        slots = characterSlots != null ? characterSlots : new Image[0];
        slotCount = slots.Length;

        homePos = new Vector2[slotCount];
        homeScale = new Vector3[slotCount];
        for (int k = 0; k < slotCount; k++)
        {
            if (slots[k] == null) continue;
            homePos[k] = slots[k].rectTransform.anchoredPosition;
            homeScale[k] = slots[k].rectTransform.localScale;
        }

        if (endingRoot) endingRoot.SetActive(false);
    }

    private void Update()
    {
        if (!isPlaying || isBusy) return;
        if (!SpacePressed()) return;

        if (isTyping)
        {
            CompleteTyping();      // 表示途中なら全文を即表示
        }
        else
        {
            StartCoroutine(Next());
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

    // 好感度を受け取って、結末の1場面目を用意する(背景と人物が同時に出る。文字はまだ出さない)
    public void Begin(int affectionValue)
    {
        current = PickEnding(affectionValue);
        if (current == null || current.slides == null || current.slides.Length == 0)
        {
            Debug.LogWarning("EndingManager: 表示できる結末がありません(Endingsと絵を確認してください)");
            current = null;
            onFinished?.Invoke();
            return;
        }

        Debug.Log($"エンディング: {current.name} (好感度 {affectionValue})");

        endingRoot.SetActive(true);
        slideGroup.alpha = 1f;
        if (endLabelText) endLabelText.gameObject.SetActive(false);

        // 人物は、最初は全員いない状態・元の位置から始める
        for (int k = 0; k < slotCount; k++)
        {
            ApplyCharacter(k, null, 1f);
            if (slots[k] == null) continue;
            slots[k].rectTransform.anchoredPosition = homePos[k];
            slots[k].rectTransform.localScale = homeScale[k];
        }

        baseSprite = null;
        baseStyle = BackgroundFit.Stretch;

        slideIndex = 0;
        var target = new Sprite[slotCount];
        var walk = new WalkIn[slotCount];
        var exit = new WalkOut[slotCount];
        ComputePlan(current.slides[0], target, walk, exit);
        PrepareSlide(0, target, walk, null);
    }
    public void Play()
    {
        if (current == null) return;

        isPlaying = true;
        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        isBusy = true;

        var slide = current.slides[slideIndex];
        var target = new Sprite[slotCount];
        var walk = new WalkIn[slotCount];
        var exit = new WalkOut[slotCount];
        ComputePlan(slide, target, walk, exit);

        if (AnyWalk(walk))
        {
            yield return Walk(walk);
            ApplyName(slide);
        }

        isBusy = false;
        StartTyping();
    }

    // 好感度以上の結末のうち、minAffectionが一番大きいものを選ぶ
    // どれにも当てはまらなければ、minAffectionが一番小さいもの
    private Ending PickEnding(int value)
    {
        if (endings == null || endings.Length == 0) return null;

        Ending best = null;
        Ending lowest = endings[0];

        foreach (var e in endings)
        {
            if (e.minAffection < lowest.minAffection) lowest = e;
            if (value >= e.minAffection && (best == null || e.minAffection > best.minAffection))
            {
                best = e;
            }
        }
        return best ?? lowest;
    }

    // いま表示されている人物(いなければnull)
    private Sprite CurrentCharacter(int k)
    {
        var img = slots[k];
        return (img != null && img.enabled) ? img.sprite : null;
    }

    // この場面が終わったあとの、各立ち位置の人物(target)と、歩いて入ってくる方向(walk)を求める
    // 指定のない位置は今のまま。人物の指定がまったくない場面もOK
    private void ComputePlan(EndingSlide slide, Sprite[] target, WalkIn[] walk, WalkOut[] exit)
    {
        for (int k = 0; k < slotCount; k++)
        {
            target[k] = CurrentCharacter(k);
            walk[k] = WalkIn.None;
            exit[k] = WalkOut.None;
        }

        if (IsIllustration(slide) || slide.characters == null) return;

        foreach (var e in slide.characters)
        {
            if (e == null) continue;

            int k = e.slot;
            if (k < 0 || k >= slotCount || slots[k] == null)
            {
                Debug.LogWarning($"EndingManager: 人物の立ち位置の番号 {k} は使えません(Character Slots は 0?{slotCount - 1}、Imageの設定も確認してください)");
                continue;
            }

            if (e.hide)
            {
                // 今いる人物だけが、歩いて去れる
                if (target[k] != null) exit[k] = e.walkOut;
                target[k] = null;
                walk[k] = WalkIn.None;
            }
            else
            {
                if (e.sprite != null) target[k] = e.sprite;
                if (e.walkIn != WalkIn.None) walk[k] = e.walkIn;
            }
        }

        // 人物がいない位置では歩けない
        for (int k = 0; k < slotCount; k++)
        {
            if (target[k] == null) walk[k] = WalkIn.None;
        }
    }

    private void ApplyCharacter(int k, Sprite sprite, float alpha)
    {
        var img = slots[k];
        if (img == null) return;

        if (sprite == null)
        {
            img.enabled = false;
        }
        else
        {
            img.sprite = sprite;
            img.enabled = true;
            ApplySize(img, sprite);
        }
        SetCharacterAlpha(k, alpha);
    }

    private void SetCharacterAlpha(int k, float a)
    {
        var img = slots[k];
        if (img == null) return;
        var c = img.color;
        c.a = a;
        img.color = c;
    }

    // 絵の縦横比を保ったまま、高さを characterHeight にそろえる
    private void ApplySize(Image img, Sprite sprite)
    {
        if (characterHeight <= 0f || sprite == null || sprite.rect.height <= 0f) return;

        float aspect = sprite.rect.width / sprite.rect.height;
        var rt = img.rectTransform;
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, characterHeight);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, characterHeight * aspect);
    }

    private static bool Any(bool[] flags)
    {
        foreach (var f in flags) if (f) return true;
        return false;
    }

    private static bool AnyWalk(WalkIn[] walk)
    {
        foreach (var w in walk) if (w != WalkIn.None) return true;
        return false;
    }

    private static bool AnyExit(WalkOut[] exit)
    {
        foreach (var w in exit) if (w != WalkOut.None) return true;
        return false;
    }

    // 画面の外(左端または右端の外側)の位置
    private Vector2 OffscreenPos(int k, WalkIn from)
    {
        var rt = slots[k].rectTransform;
        var parent = rt.parent as RectTransform;

        float half = (parent != null ? parent.rect.width : Screen.width) * 0.5f;
        float x = half + rt.rect.width;   // 人物の幅ぶん余分に外へ出す
        if (from == WalkIn.FromLeft) x = -x;

        return new Vector2(x, homePos[k].y);
    }

    // 歩いて入ってくる人物が全員、端から立ち位置まですーっと入ってくる(だんだんゆっくりになって止まる)
    private IEnumerator Walk(WalkIn[] walk)
    {
        var start = new Vector2[slotCount];

        for (int k = 0; k < slotCount; k++)
        {
            if (walk[k] == WalkIn.None || slots[k] == null) continue;

            start[k] = OffscreenPos(k, walk[k]);

            // 絵の向きと進む方向が逆なら、入ってくる間だけ左右を反転する
            // (左から入る = 右へ進む。絵が左向きなら反転が必要)
            // bool flip = faceWalkDirection && ((walk[k] == WalkIn.FromLeft) == artFacesLeft);
            // Vector3 scale = homeScale[k];
            // if (flip) scale.x = -scale.x;
            // slots[k].rectTransform.localScale = scale;
        }

        float duration = Mathf.Max(0.01f, walkDuration);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            float eased = 1f - (1f - p) * (1f - p);   // だんだん減速する

            for (int k = 0; k < slotCount; k++)
            {
                if (walk[k] == WalkIn.None || slots[k] == null) continue;
                slots[k].rectTransform.anchoredPosition =
                    new Vector2(Mathf.Lerp(start[k].x, homePos[k].x, eased), homePos[k].y);
            }
            yield return null;
        }

        for (int k = 0; k < slotCount; k++)
        {
            if (walk[k] == WalkIn.None || slots[k] == null) continue;
            slots[k].rectTransform.anchoredPosition = homePos[k];
            slots[k].rectTransform.localScale = homeScale[k];   // 着いたら元の向きに戻す
        }
    }

    // 去っていく人物が全員、立ち位置から画面の端へ歩いて出ていく(だんだん速くなる)
    private IEnumerator WalkAway(WalkOut[] exit)
    {
        var goal = new Vector2[slotCount];

        for (int k = 0; k < slotCount; k++)
        {
            if (exit[k] == WalkOut.None || slots[k] == null) continue;
            goal[k] = OffscreenPos(k, exit[k] == WalkOut.ToLeft ? WalkIn.FromLeft : WalkIn.FromRight);
        }

        float duration = Mathf.Max(0.01f, walkDuration);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            float eased = p * p;   // だんだん加速する

            for (int k = 0; k < slotCount; k++)
            {
                if (exit[k] == WalkOut.None || slots[k] == null) continue;
                slots[k].rectTransform.anchoredPosition =
                    new Vector2(Mathf.Lerp(homePos[k].x, goal[k].x, eased), homePos[k].y);
            }
            yield return null;
        }

        // 画面の外に出たので、人物を消して、位置を元に戻しておく
        for (int k = 0; k < slotCount; k++)
        {
            if (exit[k] == WalkOut.None || slots[k] == null) continue;
            slots[k].enabled = false;
            slots[k].rectTransform.anchoredPosition = homePos[k];
            slots[k].rectTransform.localScale = homeScale[k];
        }
    }

    // 背景の見せ方を反映する(背景のImageは、画面いっぱいに広げてある前提)
    private void ApplyBackgroundFit(Sprite sprite, BackgroundFit fit)
    {
        var rt = backgroundImage.rectTransform;
        rt.localScale = Vector3.one;

        switch (fit)
        {
            case BackgroundFit.Stretch:
                backgroundImage.preserveAspect = false;
                break;

            case BackgroundFit.Fill:
                backgroundImage.preserveAspect = true;

                float w = rt.rect.width;
                float h = rt.rect.height;
                if (sprite == null || sprite.rect.height <= 0f || w <= 0f || h <= 0f) break;

                // 縦横比を保って枠に収めたときの大きさ → 枠いっぱいになる倍率
                float aspect = sprite.rect.width / sprite.rect.height;
                float fitW = (w / h > aspect) ? h * aspect : w;
                float fitH = (w / h > aspect) ? h : w / aspect;
                float scale = Mathf.Max(w / fitW, h / fitH);
                rt.localScale = new Vector3(scale, scale, 1f);
                break;

            default: // KeepAspect
                backgroundImage.preserveAspect = true;
                break;
        }
    }

    // 名前と吹き出しを、この場面の内容で表示する(名前が空なら名前の台を、セリフが空なら吹き出しを隠す)
    private void ApplyName(EndingSlide slide)
    {
        if (nameText) nameText.text = slide.speaker;
        SetNameVisible(!string.IsNullOrEmpty(slide.speaker));
        if (bubble) bubble.SetActive(!string.IsNullOrEmpty(slide.text));
    }

    // 名前を出す・隠す(名前の台があれば台ごと)
    private void SetNameVisible(bool visible)
    {
        GameObject target = nameBox != null ? nameBox : (nameText != null ? nameText.gameObject : null);
        if (target != null) target.SetActive(visible);
    }

    // 名前と吹き出しを隠す(人物が歩いて入ってくる間など)
    private void HideTextBox()
    {
        SetNameVisible(false);
        if (bubble) bubble.SetActive(false);
    }

    // 挿絵の場面か(挿絵にチェックがあり、絵が入っている)
    private static bool IsIllustration(EndingSlide slide)
    {
        return slide.illustration && slide.background != null;
    }

    // この場面で映す背景(挿絵ならその絵。そうでなければ、指定があればその絵、なければ元の背景)
    private Sprite DisplayBackground(EndingSlide slide)
    {
        if (IsIllustration(slide)) return slide.background;
        return slide.background != null ? slide.background : baseSprite;
    }

    private void PrepareSlide(int i, Sprite[] target, WalkIn[] walk, bool[] startHidden)
    {
        var slide = current.slides[i];

        bool illust = IsIllustration(slide);
        if (illust)
        {
            // 挿絵: その絵だけを映す
            backgroundImage.sprite = slide.background;
            ApplyBackgroundFit(slide.background, slide.backgroundStyle);
        }
        else
        {
            if (slide.background != null)
            {
                baseSprite = slide.background;
                baseStyle = slide.backgroundStyle;
            }
            if (baseSprite != null)
            {
                backgroundImage.sprite = baseSprite;
                ApplyBackgroundFit(baseSprite, baseStyle);
            }
        }
        backgroundImage.enabled = backgroundImage.sprite != null;   // 絵がないときの白い四角を防ぐ

        bool anyWalk = false;
        for (int k = 0; k < slotCount; k++)
        {
            bool hidden = startHidden != null && startHidden[k];
            ApplyCharacter(k, target[k], (hidden || illust) ? 0f : 1f);

            // 歩いて入ってくる人物は、画面の外から始める
            if (walk[k] != WalkIn.None && slots[k] != null)
            {
                anyWalk = true;
                slots[k].rectTransform.anchoredPosition = OffscreenPos(k, walk[k]);
                SetCharacterAlpha(k, 1f);
            }
        }

        // 名前と吹き出しは、入ってきてから出す
        if (anyWalk) HideTextBox();
        else ApplyName(slide);

        if (talkText)
        {
            talkText.text = slide.text;
            talkText.maxVisibleCharacters = 0;
        }
    }

    // 次のセリフへ。背景が変わるときは画面全体(背景と人物が一緒に)、人物だけ変わるときは変わる人物だけフェードする
    private IEnumerator Next()
    {
        isBusy = true;

        int next = slideIndex + 1;

        if (next >= current.slides.Length)
        {
            // 最後まで読み終わった(画面は黒のまま)
            yield return FadeGroup(1f, 0f);
            yield return ShowEndLabel(current.endLabel);             // 暗転のあとに「End1」などを出す
            isPlaying = false;
            isBusy = false;
            Debug.Log("エンディング終了");
            onFinished?.Invoke();
            if (!string.IsNullOrEmpty(startSceneName)) SceneManager.LoadScene(startSceneName);
            yield break;
        }

        var slide = current.slides[next];
        Sprite shownBackground = DisplayBackground(slide);
        bool backgroundChanges = shownBackground != null && shownBackground != backgroundImage.sprite;

        var target = new Sprite[slotCount];
        var walk = new WalkIn[slotCount];
        var exit = new WalkOut[slotCount];
        ComputePlan(slide, target, walk, exit);

        var changed = new bool[slotCount];   // 人物が入れ替わる位置
        var fadeOut = new bool[slotCount];   // 消える人物がいる位置
        var fadeIn = new bool[slotCount];    // 新しく出てくる人物がいる位置(歩いて入ってくる人物は除く)
        for (int k = 0; k < slotCount; k++)
        {
            Sprite oldChar = CurrentCharacter(k);
            changed[k] = target[k] != oldChar;
            // 歩いて去る人物は、フェードアウトではなく歩いて消える
            fadeOut[k] = oldChar != null && exit[k] == WalkOut.None && (changed[k] || walk[k] != WalkIn.None);
            fadeIn[k] = changed[k] && target[k] != null && walk[k] == WalkIn.None;
        }
        bool anyWalk = AnyWalk(walk);
        bool characterOnly = !backgroundChanges && (Any(changed) || anyWalk);

        if (AnyExit(exit))
        {
            HideTextBox();
            yield return WalkAway(exit);                             // 端へ歩いて去っていく
        }

        if (backgroundChanges)
        {
            yield return FadeGroup(1f, 0f);                          // 画面全体が消える
        }
        else if (characterOnly && Any(fadeOut))
        {
            yield return FadeCharacters(fadeOut, 1f, 0f);            // 入れ替わる人物が消える
        }

        slideIndex = next;
        PrepareSlide(slideIndex, target, walk, characterOnly ? fadeIn : null);

        if (backgroundChanges)
        {
            yield return FadeGroup(0f, 1f);                          // 背景と人物が一緒に出てくる
        }
        else if (characterOnly && Any(fadeIn))
        {
            yield return FadeCharacters(fadeIn, 0f, 1f);             // 新しい人物が出てくる
        }

        if (anyWalk)
        {
            yield return Walk(walk);                                 // 端から入ってくる
            ApplyName(slide);
        }

        isBusy = false;
        StartTyping();
    }
    // 暗転した画面に、どのエンドかの表記をじわっと出して、少し見せる(Spaceで飛ばせる)
    private IEnumerator ShowEndLabel(string label)
    {
        if (endLabelText == null || string.IsNullOrEmpty(label)) yield break;

        endLabelText.text = label;
        endLabelText.alpha = 0f;
        endLabelText.gameObject.SetActive(true);

        float t = 0f;
        float d = Mathf.Max(0.01f, labelFadeDuration);
        while (t < d)
        {
            t += Time.deltaTime;
            endLabelText.alpha = Mathf.Clamp01(t / d);
            yield return null;
        }
        endLabelText.alpha = 1f;

        float hold = 0f;
        while (hold < labelHoldTime)
        {
            hold += Time.deltaTime;
            if (SpacePressed()) break;
            yield return null;
        }
    }

    private void StartTyping()
    {
        typingRoutine = StartCoroutine(TypeText());
    }

    private IEnumerator TypeText()
    {
        isTyping = true;

        if (talkText)
        {
            talkText.maxVisibleCharacters = 0;
            talkText.ForceMeshUpdate();
            int total = talkText.textInfo.characterCount;

            for (int i = 1; i <= total; i++)
            {
                talkText.maxVisibleCharacters = i;
                typeSound?.Play(talkText.textInfo.characterInfo[i - 1].character, i);
                yield return new WaitForSeconds(charInterval);
            }
        }

        isTyping = false;
    }

    private void CompleteTyping()
    {
        if (typingRoutine != null) StopCoroutine(typingRoutine);
        if (talkText) talkText.maxVisibleCharacters = int.MaxValue;
        isTyping = false;
    }

    private IEnumerator FadeGroup(float from, float to)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            slideGroup.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }
        slideGroup.alpha = to;
    }

    // mask で指定した位置の人物だけをフェードさせる
    private IEnumerator FadeCharacters(bool[] mask, float from, float to)
    {
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, t / fadeDuration);
            for (int k = 0; k < slotCount; k++) if (mask[k]) SetCharacterAlpha(k, a);
            yield return null;
        }
        for (int k = 0; k < slotCount; k++) if (mask[k]) SetCharacterAlpha(k, to);
    }
}