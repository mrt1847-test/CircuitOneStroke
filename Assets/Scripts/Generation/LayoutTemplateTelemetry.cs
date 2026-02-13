using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CircuitOneStroke.Generation
{
    /// <summary>
    /// Editor telemetry for template placement quality:
    /// tries, accepted candidates, chosen count, and fail reasons.
    /// </summary>
    public static class LayoutTemplateTelemetry
    {
#if UNITY_EDITOR
        private sealed class TemplateStats
        {
            public int tried;
            public int accepted;
            public int chosen;
            public readonly Dictionary<string, int> failReasons = new Dictionary<string, int>();
        }

        private static readonly Dictionary<string, TemplateStats> StatsByTemplate = new Dictionary<string, TemplateStats>();
        private static int _globalTries;
        private static int _fallbackCount;
        private static int _nextDumpTryThreshold = 60;
#endif

        public static void RecordTry(string templateName, bool accepted, string failReason = null)
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(templateName))
                return;
            if (!StatsByTemplate.TryGetValue(templateName, out var s))
            {
                s = new TemplateStats();
                StatsByTemplate[templateName] = s;
            }
            s.tried++;
            _globalTries++;
            if (accepted)
            {
                s.accepted++;
            }
            else
            {
                string reason = string.IsNullOrEmpty(failReason) ? "unknown" : failReason;
                if (!s.failReasons.ContainsKey(reason))
                    s.failReasons[reason] = 0;
                s.failReasons[reason]++;
            }
#endif
        }

        public static void RecordChosen(string templateName)
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(templateName))
                return;
            if (!StatsByTemplate.TryGetValue(templateName, out var s))
            {
                s = new TemplateStats();
                StatsByTemplate[templateName] = s;
            }
            s.chosen++;
#endif
        }

        public static void RecordFallback(string sourceTag, int nodeCount)
        {
#if UNITY_EDITOR
            _fallbackCount++;
            Debug.Log($"[LayoutTemplateTelemetry] fallback source={sourceTag} n={nodeCount} totalFallbacks={_fallbackCount}");
#endif
        }

        public static void DumpPeriodicIfNeeded(string sourceTag)
        {
#if UNITY_EDITOR
            if (_globalTries < _nextDumpTryThreshold)
                return;
            _nextDumpTryThreshold += 60;
            DumpNow(sourceTag);
#endif
        }

        public static void DumpNow(string sourceTag)
        {
#if UNITY_EDITOR
            if (StatsByTemplate.Count == 0)
            {
                Debug.Log($"[LayoutTemplateTelemetry] source={sourceTag} no-template-tries");
                return;
            }

            var sb = new StringBuilder();
            sb.Append("[LayoutTemplateTelemetry] source=").Append(sourceTag)
                .Append(" tries=").Append(_globalTries)
                .Append(" fallbacks=").Append(_fallbackCount);
            foreach (var kv in StatsByTemplate)
            {
                var name = kv.Key;
                var s = kv.Value;
                float acceptRate = s.tried > 0 ? s.accepted / (float)s.tried : 0f;
                float chosenRate = s.accepted > 0 ? s.chosen / (float)s.accepted : 0f;
                sb.Append(" | ").Append(name)
                    .Append(": tried=").Append(s.tried)
                    .Append(", accepted=").Append(s.accepted)
                    .Append(", acceptRate=").Append(acceptRate.ToString("F2"))
                    .Append(", chosen=").Append(s.chosen)
                    .Append(", chosenFromAccepted=").Append(chosenRate.ToString("F2"));
                if (s.failReasons.Count > 0)
                {
                    sb.Append(", fail=");
                    bool first = true;
                    foreach (var fk in s.failReasons)
                    {
                        if (!first) sb.Append("/");
                        first = false;
                        sb.Append(fk.Key).Append(":").Append(fk.Value);
                    }
                }
            }
            Debug.Log(sb.ToString());
#endif
        }
    }
}
