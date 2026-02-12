using System;
using UnityEngine;
using UnityEngine.UI;

namespace CircuitOneStroke.UI
{
    /// <summary>
    /// Bottom tab bar: Home, Shop, Settings. Calls ScreenRouter.ShowTab on click.
    /// Selected state: different background/tint for active tab (use highlight GameObjects or button colors).
    /// CHECKLIST: Under Canvas, sibling of HomeScreenRoot/ShopScreenRoot/SettingsScreenRoot (NOT under GameScreenRoot).
    /// </summary>
    public class BottomNavBar : MonoBehaviour
    {
        [SerializeField] private Button homeButton;
        [SerializeField] private Button shopButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private GameObject homeHighlight;
        [SerializeField] private GameObject shopHighlight;
        [SerializeField] private GameObject settingsHighlight;
        [Header("Optional: tint when selected")]
        [SerializeField] private Color selectedColor = new Color(1f, 0.95f, 0.7f);
        [SerializeField] private Color normalColor = new Color(0.7f, 0.7f, 0.75f);

        private ScreenRouter _router;

        public void Bind(ScreenRouter router)
        {
            _router = router;
            if (CircuitOneStroke.UI.AppRouter.DebugAppScene) Debug.Log($"[AppScene] BottomNavBar.Bind: router={router != null}, homeBtn={homeButton != null}, shopBtn={shopButton != null}, settingsBtn={settingsButton != null}");
            if (homeButton != null) homeButton.onClick.AddListener(() => _router?.ShowTab(ScreenRouter.ScreenId.HomeTab));
            if (shopButton != null) shopButton.onClick.AddListener(() => _router?.ShowTab(ScreenRouter.ScreenId.ShopTab));
            if (settingsButton != null) settingsButton.onClick.AddListener(() => _router?.ShowTab(ScreenRouter.ScreenId.SettingsTab));
        }

        /// <summary>Update visual selected state (highlight object and/or button color).</summary>
        public void SetSelected(ScreenRouter.ScreenId tab)
        {
            if (homeHighlight != null) homeHighlight.SetActive(tab == ScreenRouter.ScreenId.HomeTab);
            if (shopHighlight != null) shopHighlight.SetActive(tab == ScreenRouter.ScreenId.ShopTab);
            if (settingsHighlight != null) settingsHighlight.SetActive(tab == ScreenRouter.ScreenId.SettingsTab);
            var c = selectedColor;
            var n = normalColor;
            if (homeButton != null) homeButton.targetGraphic?.SetColor(tab == ScreenRouter.ScreenId.HomeTab ? c : n);
            if (shopButton != null) shopButton.targetGraphic?.SetColor(tab == ScreenRouter.ScreenId.ShopTab ? c : n);
            if (settingsButton != null) settingsButton.targetGraphic?.SetColor(tab == ScreenRouter.ScreenId.SettingsTab ? c : n);
        }
    }

    internal static class GraphicColorExt
    {
        public static void SetColor(this Graphic g, Color c)
        {
            if (g != null) g.color = c;
        }
    }
}
