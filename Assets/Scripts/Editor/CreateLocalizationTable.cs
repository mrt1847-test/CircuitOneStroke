#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using CircuitOneStroke.Data;

namespace CircuitOneStroke.Editor
{
    /// <summary>
    /// 기본 LocalizationTable 에셋을 Resources에 생성. Settings 등 하드코딩 문자열을 테이블로 이전할 때 사용.
    /// </summary>
    public static class CreateLocalizationTable
    {
        private const string ResourcePath = "Assets/Resources";
        private const string AssetPath = "Assets/Resources/LocalizationTable.asset";

        [MenuItem("Circuit One-Stroke/Localization/Create LocalizationTable (Resources)")]
        public static void Create()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                if (!AssetDatabase.IsValidFolder("Assets"))
                    AssetDatabase.CreateFolder("Assets", "Resources");
                else
                    AssetDatabase.CreateFolder("Assets", "Resources");
            }
            AssetDatabase.Refresh();

            var table = AssetDatabase.LoadAssetAtPath<LocalizationTable>(AssetPath);
            if (table != null)
            {
                Debug.Log("LocalizationTable already exists at " + AssetPath);
                Selection.activeObject = table;
                return;
            }

            table = ScriptableObject.CreateInstance<LocalizationTable>();
            AssetDatabase.CreateAsset(table, AssetPath);

            // 기본 엔트리는 Inspector에서 수정. 리플렉션으로 entries 추가는 에셋 직렬화 구조에 따라 다르므로
            // 여기서는 빈 에셋만 만들고, 기본값은 아래 DefaultEntries()로 별도 에셋에 넣거나 수동 입력.
            PopulateDefaultEntries(table);
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssetIfDirty(table);

            Debug.Log("Created LocalizationTable at " + AssetPath);
            Selection.activeObject = table;
        }

        private static void PopulateDefaultEntries(LocalizationTable table)
        {
            var entries = new System.Collections.Generic.List<LocalizationTable.Entry>
            {
                new LocalizationTable.Entry { key = "settings", en = "Settings", ko = "설정" },
                new LocalizationTable.Entry { key = "haptics_light", en = "Light", ko = "약하게" },
                new LocalizationTable.Entry { key = "haptics_normal", en = "Normal", ko = "보통" },
                new LocalizationTable.Entry { key = "node_small", en = "Small", ko = "작게" },
                new LocalizationTable.Entry { key = "node_normal", en = "Normal", ko = "보통" },
                new LocalizationTable.Entry { key = "node_large", en = "Large", ko = "크게" },
                new LocalizationTable.Entry { key = "line_thin", en = "Thin", ko = "얇게" },
                new LocalizationTable.Entry { key = "line_normal", en = "Normal", ko = "보통" },
                new LocalizationTable.Entry { key = "line_thick", en = "Thick", ko = "굵게" },
                new LocalizationTable.Entry { key = "how_to_play_toast", en = "How to Play: Draw one continuous path through all bulbs. Switches toggle gates.", ko = "방법: 모든 전구를 한 붓으로 연결하세요. 스위치는 문을 열고 닫습니다." },
                new LocalizationTable.Entry { key = "no_ads_desc", en = "Removes forced ads (interstitial/banners). Optional rewarded ads for bonuses may still be available.", ko = "강제 광고(전면/배너)를 제거합니다. 보너스용 선택 광고는 유지될 수 있습니다." },
                new LocalizationTable.Entry { key = "toast_continue_from_tail", en = "Continue from the last node", ko = "마지막 노드에서 이어서 그리세요." }
            };

            var so = new SerializedObject(table);
            var listProp = so.FindProperty("entries");
            if (listProp != null && listProp.isArray)
            {
                listProp.ClearArray();
                for (int i = 0; i < entries.Count; i++)
                {
                    listProp.InsertArrayElementAtIndex(i);
                    var elem = listProp.GetArrayElementAtIndex(i);
                    elem.FindPropertyRelative("key").stringValue = entries[i].key;
                    elem.FindPropertyRelative("en").stringValue = entries[i].en;
                    elem.FindPropertyRelative("ko").stringValue = entries[i].ko;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
#endif
