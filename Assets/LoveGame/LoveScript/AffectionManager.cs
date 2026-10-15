using System;
using TMPro;
using UnityEngine;

public class AffectionManager : MonoBehaviour
{
    [SerializeField] private int startValue = 0;
    [SerializeField] private TMP_Text affectionText;

    public int Value { get; private set; }

    public event Action<int> OnChanged;

    private void Awake()
    {
        Value = startValue;
        RefreshUI();
    }

    // 好感度を増減する
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