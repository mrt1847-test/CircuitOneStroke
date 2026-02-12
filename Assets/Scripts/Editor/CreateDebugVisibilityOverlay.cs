#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace CircuitOneStroke.Editor
{
    public static class CreateDebugVisibilityOverlay
    {
        [MenuItem("Circuit One-Stroke/Create Debug Visibility Overlay")]
        public static void Create()
        {
            var existing = Object.FindFirstObjectByType<CircuitOneStroke.View.DebugVisibilityOverlay>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                Debug.Log("DebugVisibilityOverlay already exists.");
                return;
            }

            var go = new GameObject("DebugVisibilityOverlay");
            var overlay = go.AddComponent<CircuitOneStroke.View.DebugVisibilityOverlay>();
            var loader = Object.FindFirstObjectByType<CircuitOneStroke.Core.LevelLoader>();
            var cam = Camera.main;
            var so = new UnityEditor.SerializedObject(overlay);
            if (loader != null) so.FindProperty("levelLoader").objectReferenceValue = loader;
            if (cam != null) so.FindProperty("cam").objectReferenceValue = cam;
            so.ApplyModifiedPropertiesWithoutUndo();

            Selection.activeGameObject = go;
            Debug.Log("Created DebugVisibilityOverlay. Press F3 in Play mode to toggle.");
        }
    }
}

#endif
