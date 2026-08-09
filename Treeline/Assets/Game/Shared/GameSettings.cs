using System;
using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 플레이어가 고른 환경 설정. 브라우저에 저장되고 장이 바뀌어도 남는다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 값을 쓰는 곳(<see cref="TitleScreen"/> · <see cref="EndingScreen"/>)과 값을
    /// 읽는 곳(<see cref="PlayerLook"/>)이 서로 다른 담당의 파일이다. 저장 키
    /// 문자열을 양쪽에 적어 두면 <b>한쪽만 고쳤을 때 설정이 조용히 갈라진다</b> —
    /// 저장은 되는데 반영이 안 되고, 무엇이 틀렸는지 화면에 아무 표시도 안 난다.
    /// 그래서 키를 여기 하나로 모은다.
    /// </para>
    /// <para>
    /// IMPORTANT: 마우스 감도는 <b>각도가 아니라 배율</b>이다. 기준값은
    /// <see cref="PlayerLook"/> 의 인스펙터에 있고 여기 있는 것은 거기에 곱하는
    /// 수다. 절대값으로 저장하면 A 가 기준 감도를 다시 잡을 때마다 이미 저장된
    /// 사람들의 설정이 의미를 잃는다.
    /// </para>
    /// <para>
    /// WebGL 이라 <c>PlayerPrefs</c> 는 브라우저 저장소로 간다. 쓰기가 공짜가 아니므로
    /// 슬라이더를 끄는 동안에는 값만 바꾸고, <see cref="Save"/> 는 화면을 떠날 때
    /// 한 번 부른다.
    /// </para>
    /// </remarks>
    public static class GameSettings
    {
        private const string MasterVolumeKey = "Treeline.Audio.MasterVolume";
        private const string LookSensitivityKey = "Treeline.Input.LookSensitivityScale";

        public const float DefaultMasterVolume = 0.8f;
        public const float DefaultLookSensitivity = 1f;
        public const float MinLookSensitivity = 0.3f;
        public const float MaxLookSensitivity = 3f;

        private static bool loaded;
        private static float masterVolume = DefaultMasterVolume;
        private static float lookSensitivity = DefaultLookSensitivity;

        /// <summary>값이 바뀔 때마다 부른다. 이미 떠 있는 화면이 따라오게 하려는 것이다.</summary>
        public static event Action Changed;

        /// <summary>0 ~ 1. 실제로 <see cref="AudioListener"/> 에 넣는 값은 이것의 제곱이다.</summary>
        public static float MasterVolume
        {
            get
            {
                Load();
                return masterVolume;
            }
            set
            {
                Load();
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(masterVolume, value))
                {
                    return;
                }
                masterVolume = value;
                PlayerPrefs.SetFloat(MasterVolumeKey, value);
                ApplyVolume();
                Changed?.Invoke();
            }
        }

        /// <summary><see cref="PlayerLook"/> 의 기준 감도에 곱하는 배율.</summary>
        public static float LookSensitivity
        {
            get
            {
                Load();
                return lookSensitivity;
            }
            set
            {
                Load();
                value = Mathf.Clamp(value, MinLookSensitivity, MaxLookSensitivity);
                if (Mathf.Approximately(lookSensitivity, value))
                {
                    return;
                }
                lookSensitivity = value;
                PlayerPrefs.SetFloat(LookSensitivityKey, value);
                Changed?.Invoke();
            }
        }

        /// <summary>브라우저에 실제로 기록한다. 화면을 떠날 때 한 번 부른다.</summary>
        public static void Save()
        {
            Load();
            PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
            PlayerPrefs.SetFloat(LookSensitivityKey, lookSensitivity);
            PlayerPrefs.Save();
        }

        public static void ResetToDefaults()
        {
            MasterVolume = DefaultMasterVolume;
            LookSensitivity = DefaultLookSensitivity;
        }

        /// <summary>
        /// 저장된 음량을 첫 씬이 뜨기 전에 적용한다. 타이틀을 거치지 않고 바로
        /// 어느 장으로 들어가도 음량이 맞는다.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyOnBoot()
        {
            Load();
            ApplyVolume();
        }

        private static void ApplyVolume()
        {
            // 귀는 소리 세기를 로그로 듣는다. 슬라이더를 그대로 넣으면 위쪽 절반이
            // 거의 안 변하고 아래쪽에서 뚝 끊긴다. 제곱을 거치면 손잡이가 고르게 는다.
            AudioListener.volume = masterVolume * masterVolume;
        }

        private static void Load()
        {
            if (loaded)
            {
                return;
            }
            loaded = true;
            masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, DefaultMasterVolume));
            lookSensitivity = Mathf.Clamp(
                PlayerPrefs.GetFloat(LookSensitivityKey, DefaultLookSensitivity),
                MinLookSensitivity,
                MaxLookSensitivity);
        }
    }
}
