using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// BGM と SE の音量を、AudioMixer + Slider で調整する設定画面。
// 右上の設定ボタンで、設定パネルを開閉する。音量は PlayerPrefs に保存される。
public class SoundSettings : MonoBehaviour
{
    [Header("AudioMixer")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private string bgmParam = "BGMVolume";   // Mixer で Expose した名前
    [SerializeField] private string seParam = "SEVolume";     // Mixer で Expose した名前

    [Header("UI")]
    [SerializeField] private Button settingsButton;           // 右上の設定ボタン
    [SerializeField] private GameObject settingsPanel;        // 設定パネル(ボタンとは別のオブジェクトにする)
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider seSlider;
    [SerializeField] private Button closeButton;              // パネルを閉じるボタン(空でもOK。設定ボタンでも閉じられる)
    [SerializeField] private bool closeOnOutsideClick = true; // ONにすると、パネルの外をクリックしたときにパネルが閉じる

    [Header("初期の音量(0?1)")]
    [Range(0f, 1f)][SerializeField] private float defaultBgm = 0.8f;
    [Range(0f, 1f)][SerializeField] private float defaultSe = 0.8f;

    private const string BgmKey = "BGMVolume";
    private const string SeKey = "SEVolume";

    private void Start()
    {
        SetupSlider(bgmSlider, PlayerPrefs.GetFloat(BgmKey, defaultBgm), OnBgmChanged);
        SetupSlider(seSlider, PlayerPrefs.GetFloat(SeKey, defaultSe), OnSeChanged);

        // 保存してあった音量を、Mixer に反映する
        Apply(bgmParam, bgmSlider ? bgmSlider.value : defaultBgm);
        Apply(seParam, seSlider ? seSlider.value : defaultSe);

        if (settingsPanel) settingsPanel.SetActive(false);
        if (settingsButton) settingsButton.onClick.AddListener(Toggle);
        if (closeButton) closeButton.onClick.AddListener(Close);
    }

    private void Update()
    {
        if (!closeOnOutsideClick || settingsPanel == null || !settingsPanel.activeSelf) return;
        if (!ClickedThisFrame(out Vector2 pos)) return;

        // パネルの中、または設定ボタンの上のクリックは、ここでは閉じない(設定ボタンは自分で開け閉めする)
        if (IsInside(settingsPanel, pos)) return;
        if (settingsButton != null && IsInside(settingsButton.gameObject, pos)) return;

        Close();
    }

    private static bool ClickedThisFrame(out Vector2 pos)
    {
#if ENABLE_INPUT_SYSTEM
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            pos = mouse.position.ReadValue();
            return true;
        }
#else
        if (Input.GetMouseButtonDown(0))
        {
            pos = Input.mousePosition;
            return true;
        }
#endif
        pos = default;
        return false;
    }

    private static bool IsInside(GameObject target, Vector2 screenPos)
    {
        var rt = target.transform as RectTransform;
        if (rt == null) return false;

        var canvas = target.GetComponentInParent<Canvas>();
        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;
        return RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, cam);
    }

    private void OnDestroy()
    {
        PlayerPrefs.Save();
    }

    private void SetupSlider(Slider slider, float value, UnityEngine.Events.UnityAction<float> onChanged)
    {
        if (slider == null) return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.SetValueWithoutNotify(value);
        slider.onValueChanged.AddListener(onChanged);
    }

    private void OnBgmChanged(float v)
    {
        Apply(bgmParam, v);
        PlayerPrefs.SetFloat(BgmKey, v);
    }

    private void OnSeChanged(float v)
    {
        Apply(seParam, v);
        PlayerPrefs.SetFloat(SeKey, v);
    }

    // 0?1 を、デシベル(-80?0)に変えて Mixer に渡す
    private void Apply(string param, float value)
    {
        if (mixer == null) return;

        float db = value <= 0.0001f ? -80f : Mathf.Log10(value) * 20f;
        mixer.SetFloat(param, db);
    }

    private void Toggle()
    {
        if (settingsPanel == null) return;

        if (settingsPanel.activeSelf) Close();
        else Open();
    }

    private void Open()
    {
        if (settingsPanel) settingsPanel.SetActive(true);
        ClearSelection();
    }

    private void Close()
    {
        if (settingsPanel) settingsPanel.SetActive(false);
        PlayerPrefs.Save();
        ClearSelection();
    }

    // ボタンを押したあとの選択状態を外す(会話を進める Space で、ボタンが押されなおすのを防ぐ)
    private static void ClearSelection()
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }
}