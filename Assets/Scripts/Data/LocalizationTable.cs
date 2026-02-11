using System;
using System.Collections.Generic;
using UnityEngine;

namespace CircuitOneStroke.Data
{
    /// <summary>
    /// ScriptableObject 기반 로컬라이제이션 테이블. 키별 en/ko 문자열 보관.
    /// Unity Localization 패키지 도입 전까지 최소 구현. Resources/LocalizationTable 로드.
    /// </summary>
    [CreateAssetMenu(fileName = "LocalizationTable", menuName = "Circuit One-Stroke/Localization Table", order = 0)]
    public class LocalizationTable : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string key;
            public string en;
            public string ko;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        private Dictionary<string, Entry> _byKey;

        private void BuildIndex()
        {
            if (_byKey != null) return;
            _byKey = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);
            if (entries == null) return;
            foreach (var e in entries)
            {
                if (string.IsNullOrEmpty(e?.key)) continue;
                if (!_byKey.ContainsKey(e.key))
                    _byKey[e.key] = e;
            }
        }

        /// <summary>언어별 문자열 반환. preferred가 없으면 Application.systemLanguage 사용. 없으면 en → key 순으로 폴백.</summary>
        public string Get(string key, SystemLanguage? preferred = null)
        {
            if (string.IsNullOrEmpty(key)) return key;
            BuildIndex();
            if (_byKey == null || !_byKey.TryGetValue(key, out var entry))
                return key;

            var lang = preferred ?? Application.systemLanguage;
            if (lang == SystemLanguage.Korean && !string.IsNullOrEmpty(entry.ko))
                return entry.ko;
            return !string.IsNullOrEmpty(entry.en) ? entry.en : key;
        }
    }
}
