using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AffectionManager : MonoBehaviour
{
    [SerializeField] private int startValue = 30;
    [SerializeField] private TMP_Text affectionText;

    [Header("好感度メーター(Slider)")]
    [SerializeField] private Slider affectionSlider;
    [SerializeField] private int meterMin = 0;
    [SerializeField] private int meterMax = 100;
    [SerializeField] private float meterSpeed = 60f;
    [SerializeField] private GameObject[] meterAttachments;

    [Header("効果音")]
    [SerializeField] private AudioSource seSource;      // 効果音を鳴らすAudioSource(空なら鳴らさない)
    [SerializeField] private AudioClip gainSE;          // 好感度が上がったとき
    [SerializeField] private AudioClip lossSE;          // 好感度が下がったとき
    [Range(0f, 1f)]
    [SerializeField] private float seVolume = 1f;

    public int Value { get; private set; }

    public event Action<int> OnChanged;

    private float displayed;

    private void Awake()
    {
        Value = startValue;
        displayed = Value;

        if (affectionSlider)
        {
            affectionSlider.minValue = meterMin;
            affectionSlider.maxValue = meterMax;
            affectionSlider.wholeNumbers = false;
            affectionSlider.interactable = false;
            affectionSlider.value = displayed;
        }

        RefreshUI();
    }

    private void Update()
    {
        if (!affectionSlider) return;

        SyncAttachments();
        displayed = meterSpeed <= 0f
            ? Value
            : Mathf.MoveTowards(displayed, Value, meterSpeed * Time.deltaTime);

        affectionSlider.value = displayed;
    }
    private void SyncAttachments()
    {
        if (!affectionSlider || meterAttachments == null) return;

        bool visible = affectionSlider.gameObject.activeInHierarchy;
        foreach (var obj in meterAttachments)
        {
            if (obj != null && obj.activeSelf != visible) obj.SetActive(visible);
        }
    }

    public void ShowMeter(bool show)
    {
        if (affectionSlider) affectionSlider.gameObject.SetActive(show);
        SyncAttachments();
    }

    // 好感度を増減する
    public void Add(int delta)
    {
        Value += delta;
        PlaySE(delta);
        RefreshUI();
        OnChanged?.Invoke(Value);
    }

    private void PlaySE(int delta)
    {
        if (seSource == null || delta == 0) return;

        var clip = delta > 0 ? gainSE : lossSE;
        if (clip != null) seSource.PlayOneShot(clip, seVolume);
    }

    private void RefreshUI()
    {
        if (affectionText) affectionText.text = $"好感度: {Value}";
    }
}