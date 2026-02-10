#if UNITY_EDITOR

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using CircuitOneStroke.UI.Theme;

namespace CircuitOneStroke.Editor
{
    public static class CreateTransitionOverlay
    {
        [MenuItem("Circuit One-Stroke/Create Transition Overlay Prefab")]
        public static void Create()
        {
            if (!AssetDatabase.IsValidFolder("Assets/UI"))
                AssetDatabase.CreateFolder("Assets", "UI");
            if (!AssetDatabase.IsValidFolder("Assets/UI/Prefabs"))
                AssetDatabase.CreateFolder("Assets/UI", "Prefabs");
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");

            var root = new GameObject("TransitionOverlay");
            var rect = root.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32767;
            canvas.overrideSorting = true;
            root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            root.AddComponent<GraphicRaycaster>();

            var view = root.AddComponent<CircuitOneStroke.Core.TransitionOverlayView>();

            var blocker = new GameObject("Blocker");
            blocker.transform.SetParent(root.transform, false);
            var br = blocker.AddComponent<RectTransform>();
            br.anchorMin = Vector2.zero;
            br.anchorMax = Vector2.one;
            br.offsetMin = br.offsetMax = Vector2.zero;
            var bi = blocker.AddComponent<Image>();
            bi.color = new Color(0, 0, 0, 0.01f);
            bi.raycastTarget = true;

            var fade = new GameObject("Fade");
            fade.transform.SetParent(root.transform, false);
            var fr = fade.AddComponent<RectTransform>();
            fr.anchorMin = Vector2.zero;
            fr.anchorMax = Vector2.one;
            fr.offsetMin = fr.offsetMax = Vector2.zero;
            var fi = fade.AddComponent<Image>();
            fi.color = new Color(0.06f, 0.07f, 0.12f, 0f);
            fi.raycastTarget = false;

            var spinnerRoot = new GameObject("SpinnerRoot");
            spinnerRoot.transform.SetParent(root.transform, false);
            var sr = spinnerRoot.AddComponent<RectTransform>();
            sr.anchorMin = sr.anchorMax = new Vector2(0.5f, 0.5f);
            sr.pivot = new Vector2(0.5f, 0.5f);
            sr.anchoredPosition = Vector2.zero;
            sr.sizeDelta = new Vector2(120, 120);
            spinnerRoot.SetActive(false);

            var spinnerIcon = new GameObject("SpinnerIcon");
            spinnerIcon.transform.SetParent(spinnerRoot.transform, false);
            var siRect = spinnerIcon.AddComponent<RectTransform>();
            siRect.anchorMin = Vector2.zero;
            siRect.anchorMax = Vector2.one;
            siRect.offsetMin = siRect.offsetMax = Vector2.zero;
            var siImg = spinnerIcon.AddComponent<Image>();
            siImg.color = UIStyleConstants.Primary;
            siImg.sprite = CreateCircleSprite();
            siImg.type = Image.Type.Simple;

            var loadingText = new GameObject("LoadingText");
            loadingText.transform.SetParent(spinnerRoot.transform, false);
            var ltRect = loadingText.AddComponent<RectTransform>();
            ltRect.anchorMin = new Vector2(0, -0.3f);
            ltRect.anchorMax = new Vector2(1, 0);
            ltRect.offsetMin = ltRect.offsetMax = Vector2.zero;
            var lt = loadingText.AddComponent<Text>();
            lt.text = "Loading…";
            lt.fontSize = 16;
            lt.color = UIStyleConstants.Primary;
            lt.alignment = TextAnchor.MiddleCenter;

            var so = new SerializedObject(view);
            so.FindProperty("blocker").objectReferenceValue = blocker;
            so.FindProperty("fadeImage").objectReferenceValue = fi;
            so.FindProperty("spinnerRoot").objectReferenceValue = spinnerRoot;
            so.FindProperty("spinnerIcon").objectReferenceValue = siRect;
            so.FindProperty("loadingText").objectReferenceValue = lt;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefabPath = "Assets/UI/Prefabs/TransitionOverlay.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            var resPath = "Assets/Resources/TransitionOverlay.prefab";
            if (AssetDatabase.CopyAsset(prefabPath, resPath))
                AssetDatabase.Refresh();

            Debug.Log("Created " + prefabPath + " and " + resPath);
        }

        private static Sprite CreateCircleSprite()
        {
            const int size = 64;
            var tex = new Texture2D(size, size);
            var center = new Vector2(size * 0.5f, size * 0.5f);
            float radius = size * 0.4f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = d <= radius ? 1f : (d <= radius + 2 ? 1f - (d - radius) / 2f : 0f);
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}

#endif
