using UnityEngine;
using UnityEngine.UI;
using CircuitOneStroke.Core;
using CircuitOneStroke.Services;

namespace CircuitOneStroke.UI
{
    /// <summary>
    /// 설정 화면. Audio, Haptics, Controls, Visuals, Accessibility, About, Shop.
    /// </summary>
    public class SettingsPanel : MonoBehaviour, IUIScreen
    {
        [Header("Top Bar")]
        [SerializeField] private Button backButton;
        [SerializeField] private Text titleText;

        [Header("Audio & Haptics")]
        [SerializeField] private Toggle musicToggle;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Text musicVolumeValueText;
        [SerializeField] private Toggle sfxToggle;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Text sfxVolumeValueText;
        [SerializeField] private Toggle hapticsToggle;
        [SerializeField] private Dropdown hapticsStrengthDropdown;

        [Header("Controls & Visuals")]
        [SerializeField] private Slider snapAssistSlider;
        [SerializeField] private Text snapAssistValueText;
        [SerializeField] private Toggle rejectFeedbackToggle;
        [SerializeField] private Toggle confirmExitToggle;
        [SerializeField] private Toggle hardModeToggle;
        [SerializeField] private Dropdown nodeSizeDropdown;
        [SerializeField] private Dropdown lineThicknessDropdown;
        [SerializeField] private Toggle showIconAndTextToggle;

        [Header("Accessibility")]
        [SerializeField] private Toggle colorBlindToggle;
        [SerializeField] private Toggle highContrastToggle;
        [SerializeField] private Toggle largeTextToggle;

        [Header("About")]
        [SerializeField] private Button howToPlayButton;
        [SerializeField] private Button privacyPolicyButton;
        [SerializeField] private Button termsButton;
        [SerializeField] private Button feedbackButton;
        [SerializeField] private Text versionText;

        [Header("Shop")]
        [SerializeField] private Button removeAdsButton;
        [SerializeField] private Text shopNoteText;

        private UIScreenRouter _router;

        public void BindRouter(UIScreenRouter router)
        {
            _router = router;
        }

        private void Start()
        {
            var gs = GameSettings.Instance;
            if (gs == null) return;
            var d = gs.Data;

            // Top
            if (titleText != null) titleText.text = "Settings";
            if (backButton != null) backButton.onClick.AddListener(() =>
            {
                if (AppRouter.Instance != null) AppRouter.Instance.ShowTab(MainTab.Home);
                else _router?.GoBack();
            });

            // Audio
            if (musicToggle != null)
            {
                musicToggle.isOn = d.musicEnabled;
                musicToggle.onValueChanged.AddListener(v => { gs.MusicEnabled = v; UpdateMusicSliderState(); });
            }
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = d.musicVolume;
                musicVolumeSlider.onValueChanged.AddListener(v => { gs.MusicVolume = v; RefreshMusicVolumeText(); });
            }
            UpdateMusicSliderState();
            RefreshMusicVolumeText();

            if (sfxToggle != null)
            {
                sfxToggle.isOn = d.sfxEnabled;
                sfxToggle.onValueChanged.AddListener(v => { gs.SfxEnabled = v; UpdateSfxSliderState(); });
            }
            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = d.sfxVolume;
                sfxVolumeSlider.onValueChanged.AddListener(v => { gs.SfxVolume = v; RefreshSfxVolumeText(); });
            }
            UpdateSfxSliderState();
            RefreshSfxVolumeText();

            // Haptics
            if (hapticsToggle != null)
            {
                hapticsToggle.isOn = d.hapticsEnabled;
                hapticsToggle.onValueChanged.AddListener(v => { gs.HapticsEnabled = v; UpdateHapticsDropdownState(); });
            }
            if (hapticsStrengthDropdown != null)
            {
                hapticsStrengthDropdown.ClearOptions();
                hapticsStrengthDropdown.AddOptions(new System.Collections.Generic.List<string> { "Light", "Normal" });
                hapticsStrengthDropdown.value = d.hapticsStrength;
                hapticsStrengthDropdown.onValueChanged.AddListener(v => gs.HapticsStrengthValue = (HapticsStrength)v);
            }
            UpdateHapticsDropdownState();

            // Controls & Visuals
            if (snapAssistSlider != null)
            {
                snapAssistSlider.minValue = 0; snapAssistSlider.maxValue = 1;
                snapAssistSlider.value = d.snapAssist;
                snapAssistSlider.onValueChanged.AddListener(v => { gs.SnapAssist = v; RefreshSnapAssistText(); });
            }
            RefreshSnapAssistText();

            if (rejectFeedbackToggle != null)
            {
                rejectFeedbackToggle.isOn = d.rejectFeedbackEnabled;
                rejectFeedbackToggle.onValueChanged.AddListener(v => gs.RejectFeedbackEnabled = v);
            }
            if (confirmExitToggle != null)
            {
                confirmExitToggle.isOn = d.confirmExitFromGame;
                confirmExitToggle.onValueChanged.AddListener(v => gs.ConfirmExitFromGame = v);
            }
            if (hardModeToggle != null)
            {
                hardModeToggle.isOn = gs.FailMode == FailFeedbackMode.ImmediateFail;
                hardModeToggle.onValueChanged.AddListener(v =>
                {
                    gs.FailMode = v ? FailFeedbackMode.ImmediateFail : FailFeedbackMode.RejectOnly;
                    gs.Save();
                });
            }
            if (nodeSizeDropdown != null)
            {
                nodeSizeDropdown.ClearOptions();
                nodeSizeDropdown.AddOptions(new System.Collections.Generic.List<string> { "Small", "Normal", "Large" });
                nodeSizeDropdown.value = d.nodeSize;
                nodeSizeDropdown.onValueChanged.AddListener(v => gs.NodeSizeValue = (NodeSize)v);
            }
            if (lineThicknessDropdown != null)
            {
                lineThicknessDropdown.ClearOptions();
                lineThicknessDropdown.AddOptions(new System.Collections.Generic.List<string> { "Thin", "Normal", "Thick" });
                lineThicknessDropdown.value = d.lineThickness;
                lineThicknessDropdown.onValueChanged.AddListener(v => gs.LineThicknessValue = (LineThickness)v);
            }
            if (showIconAndTextToggle != null)
            {
                showIconAndTextToggle.isOn = d.showIconAndText;
                showIconAndTextToggle.onValueChanged.AddListener(v => gs.ShowIconAndText = v);
            }

            // Accessibility
            if (colorBlindToggle != null)
            {
                colorBlindToggle.isOn = d.colorBlindMode;
                colorBlindToggle.onValueChanged.AddListener(v => gs.ColorBlindMode = v);
            }
            if (highContrastToggle != null)
            {
                highContrastToggle.isOn = d.highContrastUI;
                highContrastToggle.onValueChanged.AddListener(v => gs.HighContrastUI = v);
            }
            if (largeTextToggle != null)
            {
                largeTextToggle.isOn = d.largeText;
                largeTextToggle.onValueChanged.AddListener(v => gs.LargeText = v);
            }

            // About
            if (versionText != null) versionText.text = "v" + Application.version;
            if (howToPlayButton != null) howToPlayButton.onClick.AddListener(OnHowToPlay);
            if (privacyPolicyButton != null) privacyPolicyButton.onClick.AddListener(OnPrivacyPolicy);
            if (termsButton != null) termsButton.onClick.AddListener(OnTerms);
            if (feedbackButton != null) feedbackButton.onClick.AddListener(OnFeedback);

            // Shop
            if (removeAdsButton != null) removeAdsButton.onClick.AddListener(OnRemoveAds);
            if (shopNoteText != null) shopNoteText.text = IAPCopyConstants.NoAdsProductDesc;
        }

        private void UpdateMusicSliderState()
        {
            if (musicVolumeSlider != null && GameSettings.Instance != null) musicVolumeSlider.interactable = GameSettings.Instance.Data.musicEnabled;
        }
        private void UpdateSfxSliderState()
        {
            if (sfxVolumeSlider != null && GameSettings.Instance != null) sfxVolumeSlider.interactable = GameSettings.Instance.Data.sfxEnabled;
        }
        private void UpdateHapticsDropdownState()
        {
            if (hapticsStrengthDropdown != null && GameSettings.Instance != null) hapticsStrengthDropdown.interactable = GameSettings.Instance.Data.hapticsEnabled;
        }
        private void RefreshMusicVolumeText()
        {
            if (musicVolumeValueText != null && musicVolumeSlider != null)
                musicVolumeValueText.text = (musicVolumeSlider.value * 100).ToString("F0") + "%";
        }
        private void RefreshSfxVolumeText()
        {
            if (sfxVolumeValueText != null && sfxVolumeSlider != null)
                sfxVolumeValueText.text = (sfxVolumeSlider.value * 100).ToString("F0") + "%";
        }
        private void RefreshSnapAssistText()
        {
            if (snapAssistValueText != null && snapAssistSlider != null)
                snapAssistValueText.text = (snapAssistSlider.value * 100).ToString("F0") + "%";
        }

        private void OnHowToPlay()
        {
            // TODO: Open Tutorial modal or route to Tutorial screen
            GameFeedback.RequestToast("How to Play: Draw one continuous path through all bulbs. Switches toggle gates.");
        }
        private void OnPrivacyPolicy()
        {
            // TODO: Replace with real URL
            Application.OpenURL("https://example.com/privacy");
        }
        private void OnTerms()
        {
            // TODO: Replace with real URL
            Application.OpenURL("https://example.com/terms");
        }
        private void OnFeedback()
        {
            // TODO: Replace with real mailto
            Application.OpenURL("mailto:support@example.com");
        }
        private void OnRemoveAds()
        {
            _router?.ShowShop();
        }
    }
}
