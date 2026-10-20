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

    public void Add(int delta)
    {
        Value += delta;
        RefreshUI();
        OnChanged?.Invoke(Value);
    }

    private void RefreshUI()
    {
        if (affectionText) affectionText.text = $"好感度: {Value}";
    }
}