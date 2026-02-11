using UnityEngine;

namespace CircuitOneStroke.Generation
{
    /// <summary>
    /// N range and target success rate band for a difficulty tier. Used by generator and diode tuner.
    /// </summary>
    public static class DifficultyProfile
    {
        public const int EasyNMin = 4;
        public const int EasyNMax = 12;
        public const float EasyTargetRate = 0.50f;
        public const float EasyBand = 0.05f;
        public const int EasyTrialsK = 400;

        public const int NormalNMin = 10;
        public const int NormalNMax = 18;
        public const float NormalTargetRate = 0.30f;
        public const float NormalBand = 0.05f;
        public const int NormalTrialsK = 400;

        public const int HardNMin = 16;
        public const int HardNMax = 25;
        public const float HardTargetRate = 0.10f;
        public const float HardBand = 0.02f;
        public const int HardTrialsK = 600;

        /// <summary>For N &lt;= 10 use more trials for stability.</summary>
        public const int SmallNTrialsK = 1200;

        public static void GetNRange(DifficultyTier tier, out int nMin, out int nMax)
        {
            switch (tier)
            {
                case DifficultyTier.Easy:
                    nMin = EasyNMin;
                    nMax = EasyNMax;
                    break;
                case DifficultyTier.Medium:
                    nMin = NormalNMin;
                    nMax = NormalNMax;
                    break;
                case DifficultyTier.Hard:
                    nMin = HardNMin;
                    nMax = HardNMax;
                    break;
                default:
                    nMin = EasyNMin;
                    nMax = EasyNMax;
                    break;
            }
        }

        public static void GetTargetRate(DifficultyTier tier, out float target, out float band)
        {
            switch (tier)
            {
                case DifficultyTier.Easy:
                    target = EasyTargetRate;
                    band = EasyBand;
                    break;
                case DifficultyTier.Medium:
                    target = NormalTargetRate;
                    band = NormalBand;
                    break;
                case DifficultyTier.Hard:
                    target = HardTargetRate;
                    band = HardBand;
                    break;
                default:
                    target = EasyTargetRate;
                    band = EasyBand;
                    break;
            }
        }

        public static int GetTrialsK(DifficultyTier tier, int N)
        {
            if (N <= 10) return SmallNTrialsK;
            switch (tier)
            {
                case DifficultyTier.Easy: return EasyTrialsK;
                case DifficultyTier.Medium: return NormalTrialsK;
                case DifficultyTier.Hard: return HardTrialsK;
                default: return EasyTrialsK;
            }
        }

        public static bool IsInBand(float measuredRate, DifficultyTier tier)
        {
            GetTargetRate(tier, out float target, out float band);
            return measuredRate >= target - band && measuredRate <= target + band;
        }
    }
}
