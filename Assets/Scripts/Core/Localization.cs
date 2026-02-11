using UnityEngine;
using CircuitOneStroke.Data;

namespace CircuitOneStroke.Core
{
    /// <summary>
    /// 런타임 로컬라이즈 문자열 조회. LocalizationTable을 Resources에서 로드해 사용.
    /// 테이블이 없거나 키가 없으면 key 그대로 반환(폴백).
    /// </summary>
    public static class Localization
    {
        private static LocalizationTable _table;

        public static LocalizationTable Table => _table ??= Resources.Load<LocalizationTable>("LocalizationTable");

        /// <summary>현재 언어 기준으로 키에 해당하는 문자열 반환. 테이블 없음/키 없음 시 key 반환.</summary>
        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return key;
            return Table != null ? Table.Get(key) : key;
        }
    }
}
