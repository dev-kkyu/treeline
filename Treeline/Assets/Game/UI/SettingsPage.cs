using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Treeline
{
    /// <summary>
    /// 「환경 설정」 쪽. 마우스 감도 · 전체 음량 · 음소거 · 기본값 · 뒤로.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 타이틀 · 일시정지 · 엔딩 셋이 <b>같은 것</b>을 띄운다. 화면마다 따로 지으면
    /// 항목이 갈라져서, 게임 중에 고친 감도가 타이틀에는 없거나 그 반대가 된다.
    /// </para>
    /// <para>
    /// 값 자체는 <see cref="GameSettings"/> 가 든다. 이 쪽은 손잡이만 그리고 바로
    /// 밀어 넣는다 — 「확인」을 누를 때까지 모아 두지 않는 이유는, 감도를 눈으로
    /// 확인하려면 <b>끄는 즉시 반영</b>돼야 하기 때문이다.
    /// </para>
    /// </remarks>
    public sealed class SettingsPage
    {
        private readonly Slider sensitivitySlider;
        private readonly TMP_Text sensitivityValue;
        private readonly Slider volumeSlider;
        private readonly TMP_Text volumeValue;
        private readonly TMP_Text muteLabel;

        private float lastAudibleVolume = GameSettings.DefaultMasterVolume;
        private bool syncing;

        public SettingsPage(MenuChrome chrome, Transform frame, string name,
            UnityEngine.Events.UnityAction back)
        {
            Root = chrome.CreatePage(name, frame);
            Transform t = Root.transform;

            chrome.CreateText("SettingsTitle", t, "환경 설정", 34f, MenuChrome.Frost,
                TextAlignmentOptions.Left, new Vector2(80f, -318f), new Vector2(330f, 54f), true);

            sensitivityValue = CreateRow(chrome, t, "Sensitivity", "마우스 감도",
                "값이 클수록 시점이 빨리 돈다", -382f,
                GameSettings.MinLookSensitivity, GameSettings.MaxLookSensitivity,
                ApplySensitivity, out sensitivitySlider);

            volumeValue = CreateRow(chrome, t, "Volume", "전체 음량",
                "고른 값은 브라우저에 저장된다", -516f, 0f, 1f,
                ApplyVolume, out volumeSlider);

            Button mute = chrome.CreateMenuButton(t, "MuteButton", "음소거",
                new Vector2(80f, -646f), 74f, ToggleMute, 225f);
            muteLabel = mute.transform.Find("Label").GetComponent<TMP_Text>();

            chrome.CreateMenuButton(t, "ResetButton", "기본값", new Vector2(325f, -646f), 74f,
                ResetToDefaults, 225f);

            BackButton = chrome.CreateMenuButton(t, "SettingsBackButton", "뒤로",
                new Vector2(80f, -738f), 74f, back);

            Sync();
        }

        public GameObject Root { get; }

        public Button BackButton { get; }

        public Selectable FirstFocus => sensitivitySlider;

        /// <summary>저장된 값을 손잡이에 되비춘다. 되비추는 동안 콜백이 다시 돌지 않게 막는다.</summary>
        public void Sync()
        {
            syncing = true;

            float volume = GameSettings.MasterVolume;
            if (volume > 0.001f)
            {
                lastAudibleVolume = volume;
            }
            volumeSlider.SetValueWithoutNotify(volume);
            sensitivitySlider.SetValueWithoutNotify(GameSettings.LookSensitivity);

            syncing = false;

            RedrawVolume(volume);
            RedrawSensitivity(GameSettings.LookSensitivity);
        }

        public void Save()
        {
            GameSettings.Save();
        }

        private TMP_Text CreateRow(MenuChrome chrome, Transform parent, string name, string label,
            string hint, float y, float min, float max,
            UnityEngine.Events.UnityAction<float> onChanged, out Slider slider)
        {
            chrome.CreateText($"{name}Label", parent, label, 18f, MenuChrome.Muted,
                TextAlignmentOptions.Left, new Vector2(84f, y), new Vector2(250f, 34f), false);
            TMP_Text value = chrome.CreateText($"{name}Value", parent, "—", 18f, MenuChrome.Frost,
                TextAlignmentOptions.Right, new Vector2(410f, y), new Vector2(136f, 34f), true);

            slider = chrome.CreateSlider(parent, $"{name}Slider", new Vector2(84f, y - 44f),
                new Vector2(462f, 40f), min, max);
            slider.onValueChanged.AddListener(onChanged);

            chrome.CreateText($"{name}Hint", parent, hint, 13f,
                new Color(MenuChrome.Muted.r, MenuChrome.Muted.g, MenuChrome.Muted.b, 0.7f),
                TextAlignmentOptions.Left, new Vector2(84f, y - 94f), new Vector2(462f, 22f), false);

            return value;
        }

        private void ApplyVolume(float value)
        {
            if (syncing)
            {
                return;
            }
            GameSettings.MasterVolume = value;
            if (value > 0.001f)
            {
                lastAudibleVolume = value;
            }
            RedrawVolume(value);
        }

        private void ApplySensitivity(float value)
        {
            if (syncing)
            {
                return;
            }
            GameSettings.LookSensitivity = value;
            RedrawSensitivity(value);
        }

        private void RedrawVolume(float value)
        {
            volumeValue.text = $"{Mathf.RoundToInt(value * 100f)}%";
            muteLabel.text = value <= 0.001f ? "음소거 해제" : "음소거";
        }

        private void RedrawSensitivity(float value)
        {
            sensitivityValue.text = $"{value:0.00}×";
        }

        private void ToggleMute()
        {
            volumeSlider.value = volumeSlider.value <= 0.001f
                ? Mathf.Max(0.1f, lastAudibleVolume)
                : 0f;
        }

        private void ResetToDefaults()
        {
            GameSettings.ResetToDefaults();
            Sync();
        }
    }
}
