using UnityEngine;
using UnityEngine.UI;
using CircuitOneStroke.Core;

namespace CircuitOneStroke.UI
{
    /// <summary>
    /// 게임 중 HUD: 상태에 따라 성공/실패 패널·재시도 버튼 표시, 레벨 번호 라벨, 재시도 시 LoadCurrent.
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        [SerializeField] private LevelLoader levelLoader;
        [SerializeField] private GameObject retryButton;
        [SerializeField] private GameObject successPanel;
        [SerializeField] private GameObject failPanel;
        [SerializeField] private Text levelLabel;

        private GameStateMachine _stateMachine;

        private void Start()
        {
            if (levelLoader != null)
            {
                levelLoader.OnStateMachineChanged += HandleStateMachineChanged;
                HandleStateMachineChanged(levelLoader.StateMachine);
            }

            if (retryButton != null)
            {
                var btn = retryButton.GetComponent<Button>();
                if (btn != null)
                    btn.onClick.AddListener(OnRetryClicked);
            }

            RefreshVisibility();
            UpdateLevelLabel();
        }

        private void OnDestroy()
        {
            if (levelLoader != null)
                levelLoader.OnStateMachineChanged -= HandleStateMachineChanged;
            if (_stateMachine != null)
                _stateMachine.OnStateChanged -= OnStateChanged;
        }

        private void HandleStateMachineChanged(GameStateMachine stateMachine)
        {
            if (_stateMachine != null)
                _stateMachine.OnStateChanged -= OnStateChanged;

            _stateMachine = stateMachine;

            if (_stateMachine != null)
                _stateMachine.OnStateChanged += OnStateChanged;

            RefreshVisibility();
            UpdateLevelLabel();
        }

        private void UpdateLevelLabel()
        {
            if (levelLabel != null && levelLoader?.LevelData != null)
                levelLabel.text = $"Level {levelLoader.LevelData.levelId}";
        }

        private void OnStateChanged(GameState state)
        {
            RefreshVisibility();
            if (state == GameState.Success)
                Core.GameFeedback.Instance?.PlaySuccess();
            else if (state == GameState.Fail)
                Core.GameFeedback.Instance?.PlayFail();
        }

        /// <summary>Success/Fail일 때만 패널·재시도 버튼 표시.</summary>
        private void RefreshVisibility()
        {
            var state = levelLoader?.StateMachine?.State ?? GameState.Idle;
            if (successPanel != null) successPanel.SetActive(state == GameState.Success);
            if (failPanel != null) failPanel.SetActive(state == GameState.Fail);
            if (retryButton != null) retryButton.SetActive(state == GameState.Success || state == GameState.Fail);
        }

        /// <summary>재시도 시 현재 레벨 재로드(LoadCurrent) 후 패널 숨김.</summary>
        private void OnRetryClicked()
        {
            if (levelLoader != null)
            {
                levelLoader.LoadCurrent();
                RefreshVisibility();
            }
        }
    }
}
