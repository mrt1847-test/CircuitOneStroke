#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace CircuitOneStroke.Editor
{
    public static class CreateDebugPanel
    {
        [MenuItem("Circuit One-Stroke/Create Debug Panel")]
        public static void Create()
        {
            var canvas = Object.FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("No Canvas found. Create a Canvas first.");
                return;
            }

            var panel = new GameObject("DebugPanel");
            panel.transform.SetParent(canvas.transform, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(10, -10);
            rect.sizeDelta = new Vector2(240, 200);
            var img = panel.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

            var debugPanel = panel.AddComponent<CircuitOneStroke.UI.DebugPanel>();

            var toggleGo = new GameObject("NoAdsToggle");
            toggleGo.transform.SetParent(panel.transform, false);
            var toggleRect = toggleGo.AddComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(0, 0.8f);
            toggleRect.anchorMax = new Vector2(1, 0.95f);
            toggleRect.offsetMin = toggleRect.offsetMax = Vector2.zero;
            toggleGo.AddComponent<Toggle>();
            var label = new GameObject("Label").AddComponent<Text>();
            label.transform.SetParent(toggleGo.transform, false);
            label.text = "No Ads (forced ads off)";
            label.fontSize = 14;
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;

            var statusGo = new GameObject("StatusText");
            statusGo.transform.SetParent(panel.transform, false);
            var statusRect = statusGo.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0, 0.4f);
            statusRect.anchorMax = new Vector2(1, 0.78f);
            statusRect.offsetMin = statusRect.offsetMax = Vector2.zero;
            var statusText = statusGo.AddComponent<Text>();
            statusText.text = "HasNoAds: false";
            statusText.fontSize = 12;

            var btn1 = CreateButton("Simulate Level Clear");
            btn1.transform.SetParent(panel.transform, false);
            SetRect(btn1, 0.05f, 0.25f, 0.95f, 0.42f);

            var btn2 = CreateButton("Set Hearts 0");
            btn2.transform.SetParent(panel.transform, false);
            SetRect(btn2, 0.05f, 0.12f, 0.95f, 0.24f);

            var btn3 = CreateButton("Simulate Heavy Load (1s)");
            btn3.transform.SetParent(panel.transform, false);
            SetRect(btn3, 0.05f, 0.02f, 0.95f, 0.11f);

            var so = new SerializedObject(debugPanel);
            so.FindProperty("noAdsToggle").objectReferenceValue = toggleGo.GetComponent<Toggle>();
            so.FindProperty("statusText").objectReferenceValue = statusText;
            so.FindProperty("simulateLevelClearButton").objectReferenceValue = btn1.GetComponent<Button>();
            so.FindProperty("setHeartsZeroButton").objectReferenceValue = btn2.GetComponent<Button>();
            so.FindProperty("simulateHeavyLoadButton").objectReferenceValue = btn3.GetComponent<Button>();
            so.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeGameObject = panel;
            Debug.Log("Created DebugPanel. Only active in Editor/Development Build.");
        }

        private static GameObject CreateButton(string label)
        {
            var go = new GameObject(label + "Button");
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(100, 30);
            go.AddComponent<Image>().color = new Color(0.3f, 0.3f, 0.4f);
            go.AddComponent<Button>();
            var text = new GameObject("Text").AddComponent<Text>();
            text.transform.SetParent(go.transform, false);
            text.text = label;
            text.fontSize = 12;
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            return go;
        }

        private static void SetRect(GameObject go, float x1, float y1, float x2, float y2)
        {
            var rect = go.GetComponent<RectTransform>();
            if (rect == null) return;
            rect.anchorMin = new Vector2(x1, y1);
            rect.anchorMax = new Vector2(x2, y2);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}

#endif
