using System.Collections;
using UnityEngine;
using CircuitOneStroke.Data;
using CircuitOneStroke.Services;
using CircuitOneStroke.UI;

namespace CircuitOneStroke.Core
{
    public enum IntentType { StartLevel, RetryLevel, NextLevel }

    public struct LastIntent
    {
        public IntentType type;
        public int levelId;
    }

    /// <summary>
    /// 게임 흐름 중앙 조율. 시작/재시도/다음 레벨, 클리어/실패 처리.
    /// </summary>
    public class GameFlowController : MonoBehaviour
    {
        [SerializeField] private UIScreenRouter router;
        [SerializeField] private LevelLoader levelLoader;
        [SerializeField] private LevelManifest levelManifest;

        public static GameFlowController Instance { get; private set; }

        public LastIntent LastIntent { get; private set; }

        public void SetLastIntent(LastIntent intent) => LastIntent = intent;

        private const int DefaultMaxLevels = 20;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (router == null) router = FindFirstObjectByType<UIScreenRouter>();
            if (levelLoader == null) levelLoader = FindFirstObjectByType<LevelLoader>();
            if (levelManifest == null) levelManifest = Resources.Load<LevelManifest>("Levels/GeneratedLevelManifest");
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Boot()
        {
            router.ShowHome();
        }

        public void RequestStartLevel(int levelId)
        {
            LastIntent = new LastIntent { type = IntentType.StartLevel, levelId = levelId };
            if (!HeartsManager.Instance.CanStartAttempt())
            {
                router?.ShowOutOfHearts(OutOfHeartsContext.FromLevelSelect);
                return;
            }
            StartCoroutine(RunBuildLevelAndShowGame(levelId));
        }

        public void RequestRetryCurrent()
        {
            int currentId = levelLoader?.LevelData != null ? levelLoader.LevelData.levelId : LastIntent.levelId;
            if (currentId <= 0) currentId = 1;
            LastIntent = new LastIntent { type = IntentType.RetryLevel, levelId = currentId };
            if (!HeartsManager.Instance.CanStartAttempt())
            {
                router?.ShowOutOfHearts(OutOfHeartsContext.FromResultLose);
                return;
            }
            StartCoroutine(RunBuildLevelAndShowGame(currentId));
        }

        public void RequestNextLevel()
        {
            int currentId = levelLoader?.LevelData != null ? levelLoader.LevelData.levelId : 1;
            int nextId = GetNextLevelId(currentId);
            LastIntent = new LastIntent { type = IntentType.NextLevel, levelId = nextId };
            if (!HeartsManager.Instance.CanStartAttempt())
            {
                router?.ShowOutOfHearts(OutOfHeartsContext.FromResultWin);
                return;
            }
            StartCoroutine(TryInterstitialThenBuildAndShow(nextId));
        }

        private IEnumerator TryInterstitialThenBuildAndShow(int levelId)
        {
            var service = AdServiceRegistry.Instance ?? FindFirstObjectByType<AdServiceMock>() as IAdService;
            int levelIndex = Mathf.Max(0, levelId - 2);
            var config = AdPlacementConfig.Instance?.GetConfig(AdPlacement.Interstitial_EveryNClears)
                ?? AdPlacementConfig.GetDefaultConfig(AdPlacement.Interstitial_EveryNClears);
            int n = config.frequencyN <= 0 ? 3 : config.frequencyN;
            bool shouldShow = InterstitialTracker.Instance.LevelsClearedSinceLastInterstitial >= n &&
                InterstitialTracker.Instance.CanAttemptInterstitial() &&
                AdDecisionService.Instance.CanShow(AdPlacement.Interstitial_EveryNClears, userInitiated: false, levelIndex) &&
                service != null && service.IsInterstitialReady(AdPlacement.Interstitial_EveryNClears);

            if (shouldShow && service != null)
            {
                bool done = false;
                service.ShowInterstitial(AdPlacement.Interstitial_EveryNClears,
                    onClosed: () =>
                    {
                        InterstitialTracker.Instance.ResetAfterInterstitialShown();
                        AdDecisionService.Instance.RecordShown(AdPlacement.Interstitial_EveryNClears);
                        done = true;
                    },
                    onFailed: _ => { done = true; });
                while (!done) yield return null;
            }
            yield return RunBuildLevelAndShowGame(levelId);
        }

        private int GetNextLevelId(int currentId)
        {
            if (levelManifest == null) return currentId + 1;
            var next = levelManifest.GetLevel(currentId);
            return next != null ? currentId + 1 : currentId;
        }

        private IEnumerator RunBuildLevelAndShowGame(int levelId)
        {
            if (TransitionManager.Instance != null)
                yield return TransitionManager.Instance.RunTransition(BuildLevelCoroutine(levelId));
            else
                yield return BuildLevelCoroutine(levelId);
            router?.ShowGame();
        }

        public IEnumerator BuildLevelCoroutine(int levelId)
        {
            if (levelLoader == null) yield break;
            LevelRecords.LastPlayedLevelId = levelId;

            LevelData data = null;
            if (levelManifest != null)
                data = levelManifest.GetLevel(levelId - 1);
            if (data == null)
                data = Resources.Load<LevelData>($"Levels/Level_{levelId}");

            if (data == null) yield break;

            yield return levelLoader.LoadLevelCoroutine(data);
            if (levelLoader.StateMachine != null)
                levelLoader.StateMachine.ResetToIdle();
        }

        public void OnLevelComplete()
        {
            var appRouter = UI.AppRouter.Instance;
            if (appRouter != null) { appRouter.OnLevelComplete(); return; }
            int levelId = levelLoader?.LevelData != null ? levelLoader.LevelData.levelId : 1;
            router?.ShowResultWin(levelId);
        }

        public void OnHardFail(string reason)
        {
            var failReason = reason == "incomplete" ? FailReason.Incomplete
                : reason == "revisit_node" ? FailReason.RevisitNode
                : FailReason.Other;
            var appRouter = UI.AppRouter.Instance;
            if (appRouter != null) { appRouter.OnHardFail(failReason); return; }
            router?.ShowResultLose();
        }

        public void ResumeLastIntent()
        {
            var appRouter = UI.AppRouter.Instance;
            if (appRouter != null) { appRouter.ResumeLastIntent(); return; }
            var intent = LastIntent;
            switch (intent.type)
            {
                case IntentType.StartLevel:
                    RequestStartLevel(intent.levelId);
                    break;
                case IntentType.RetryLevel:
                    RequestRetryCurrent();
                    break;
                case IntentType.NextLevel:
                    RequestNextLevel();
                    break;
            }
        }
    }
}
