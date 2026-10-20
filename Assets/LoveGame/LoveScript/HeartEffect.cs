using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HeartEffect : MonoBehaviour
{
    [Header("つなぐもの")]
    [SerializeField] private AffectionManager affection;
    [SerializeField] private RectTransform spawnPoint;
    [SerializeField] private Sprite[] heartSprites;
    [SerializeField] private Sprite[] lossSprites;   // 好感度が下がったときの絵(割れたハートなど)。空なら、上がったときと同じ絵

    [Header("数")]
    [SerializeField] private int minHearts = 3;
    [SerializeField] private int maxHearts = 8;
    [SerializeField] private int deltaPerHeart = 5;

    [Header("見た目")]
    [SerializeField] private float heartSize = 60f;
    [SerializeField] private float spawnSpread = 140f;                          // 出てくる位置の左右のばらつき
    [SerializeField] private Vector2 distanceRange = new Vector2(220f, 420f);   // 動く距離。上がったときは上へ、下がったときは下へ
    [SerializeField] private Vector2 driftRange = new Vector2(-160f, 160f);     // 動きながら横へ広がる幅(この範囲でランダム)
    [SerializeField] private Color lossColor = Color.white;                     // 下がったときのハートの色(白なら絵のまま。暗めの色にすると悲しい感じ)
    [SerializeField] private Vector2 durationRange = new Vector2(1.0f, 1.6f);
    [SerializeField] private float swayAmount = 25f;
    [SerializeField] private float spawnInterval = 0.08f;
    private int previous;

    private void OnEnable()
    {
        if (affection) affection.OnChanged += OnAffectionChanged;
    }

    private void OnDisable()
    {
        if (affection) affection.OnChanged -= OnAffectionChanged;
    }

    private void Start()
    {
        if (affection) previous = affection.Value;
    }

    private void OnAffectionChanged(int value)
    {
        int gain = value - previous;
        previous = value;

        if (gain != 0) Play(gain);
    }

    public void Play(int gain)
    {
        if (spawnPoint == null || heartSprites == null || heartSprites.Length == 0) return;

        // gain がプラスなら上へ、マイナスなら下へ(量が大きいほどハートが多い)
        int count = Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(gain) / (float)Mathf.Max(1, deltaPerHeart)), minHearts, maxHearts);
        StartCoroutine(SpawnRoutine(count, gain > 0));
    }

    [ContextMenu("テスト: ハートを出す(上がったとき)")]
    private void TestPlay()
    {
        Play(10);
    }

    [ContextMenu("テスト: ハートを出す(下がったとき)")]
    private void TestPlayLoss()
    {
        Play(-10);
    }

    private IEnumerator SpawnRoutine(int count, bool up)
    {
        for (int i = 0; i < count; i++)
        {
            SpawnOne(up);
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnOne(bool up)
    {
        var pool = (!up && lossSprites != null && lossSprites.Length > 0) ? lossSprites : heartSprites;
        var sprite = pool[Random.Range(0, pool.Length)];
        if (sprite == null || sprite.rect.height <= 0f) return;

        var go = new GameObject("Heart", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(spawnPoint, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);

        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        if (!up) img.color = lossColor;
        float aspect = sprite.rect.width / sprite.rect.height;
        float size = heartSize * Random.Range(0.8f, 1.2f);
        rt.sizeDelta = new Vector2(size * aspect, size);
        rt.anchoredPosition = new Vector2(Random.Range(-spawnSpread, spawnSpread), 0f);

        float distance = Random.Range(distanceRange.x, distanceRange.y);
        float travel = up ? distance : -distance;              // 上がったら上へ、下がったら下へ
        float drift = Random.Range(driftRange.x, driftRange.y);
        float duration = Random.Range(durationRange.x, durationRange.y);
        StartCoroutine(FloatRoutine(img, travel, drift, duration));
    }

    private IEnumerator FloatRoutine(Image img, float travel, float drift, float duration)
    {
        var rt = img.rectTransform;
        Vector2 start = rt.anchoredPosition;

        float swayPhase = Random.value * Mathf.PI * 2f;
        float swaySpeed = Random.Range(2f, 4f);

        float t = 0f;
        while (t < duration && img != null)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);

            // 上へ行くときは、だんだんゆっくりに。下へ行くときは、だんだん速く(落ちる感じ)
            float ease = travel >= 0f ? 1f - (1f - p) * (1f - p) : p * p;
            float y = travel * ease;
            float x = drift * ease + Mathf.Sin(swayPhase + p * swaySpeed) * swayAmount * p;   // 横へ広がりながら、ゆれる
            rt.anchoredPosition = start + new Vector2(x, y);

            rt.localScale = Vector3.one * Mathf.Lerp(0.3f, 1f, Mathf.Clamp01(p / 0.25f));

            var c = img.color;
            c.a = p < 0.6f ? 1f : 1f - (p - 0.6f) / 0.4f;
            img.color = c;

            yield return null;
        }

        if (img != null) Destroy(img.gameObject);
    }
}