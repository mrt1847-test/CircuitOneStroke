using UnityEngine;
using UnityEngine.UI;
using CircuitOneStroke.Core;

namespace CircuitOneStroke.UI
{
    /// <summary>
    /// 설정 토글과 GameSettings 연동. 소리·진동·Fail 모드(RejectOnly vs ImmediateFail).
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        [SerializeField] private Toggle soundToggle;
        [SerializeField] private Toggle vibrateToggle;
        [SerializeField] private Toggle hardModeToggle;

        private void Start()
        {
            if (soundToggle != null)
            {
                soundToggle.isOn = GameSettings.SoundEnabled;
                soundToggle.onValueChanged.AddListener(v => { GameSettings.SoundEnabled = v; GameSettings.Save(); });
            }
            if (vibrateToggle != null)
            {
                vibrateToggle.isOn = GameSettings.VibrateEnabled;
                vibrateToggle.onValueChanged.AddListener(v => { GameSettings.VibrateEnabled = v; GameSettings.Save(); });
            }
            if (hardModeToggle != null)
            {
                hardModeToggle.isOn = GameSettings.FailMode == FailFeedbackMode.ImmediateFail;
                hardModeToggle.onValueChanged.AddListener(v =>
                {
                    GameSettings.FailMode = v ? FailFeedbackMode.ImmediateFail : FailFeedbackMode.RejectOnly;
                    GameSettings.Save();
                });
            }
        }
    }
}
