using UnityEngine;
using CircuitOneStroke.Core;
using CircuitOneStroke.Services;

namespace CircuitOneStroke.UI
{
    /// <summary>
    /// UI 레이어에서 Core/Services 접근을 한 곳으로 모읍니다.
    /// GameFlowController, IAdService 조회 패턴을 공통화해 의존성과 중복을 줄입니다.
    /// </summary>
    public static class UIServices
    {
        /// <summary>게임 플로우 컨트롤러. 없으면 null.</summary>
        public static GameFlowController GetFlow()
        {
            return GameFlowController.Instance != null
                ? GameFlowController.Instance
                : Object.FindFirstObjectByType<GameFlowController>();
        }

        /// <summary>광고 서비스. Registry → serialized component → AdServiceMock 순으로 조회.</summary>
        public static IAdService GetAdService(MonoBehaviour adServiceComponent = null)
        {
            if (AdServiceRegistry.Instance is MonoBehaviour regMb)
            {
                if (regMb.isActiveAndEnabled)
                    return AdServiceRegistry.Instance;
            }
            else if (AdServiceRegistry.Instance != null)
            {
                return AdServiceRegistry.Instance;
            }

            if (adServiceComponent != null && adServiceComponent is IAdService s)
            {
                if (adServiceComponent.isActiveAndEnabled)
                    return s;
            }

            // Fallback to an active mock in scene.
            var mocks = Object.FindObjectsByType<AdServiceMock>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < mocks.Length; i++)
            {
                if (mocks[i] != null && mocks[i].isActiveAndEnabled)
                    return mocks[i];
            }
            return null;
        }
    }
}
