namespace CircuitOneStroke.Services
{
    /// <summary>
    /// 3클리어마다 인터스티셜 표시를 위한 클리어 횟수 추적.
    /// </summary>
    public class InterstitialTracker
    {
        public static InterstitialTracker Instance { get; } = new InterstitialTracker();

        public int LevelsClearedSinceLastInterstitial { get; private set; }

        public void IncrementOnLevelClear()
        {
            LevelsClearedSinceLastInterstitial++;
        }

        public void ResetAfterInterstitialShown()
        {
            LevelsClearedSinceLastInterstitial = 0;
        }
    }
}
