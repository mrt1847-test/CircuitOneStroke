using UnityEngine;
using UnityEngine.UI;

namespace CircuitOneStroke.Core
{
    /// <summary>
    /// 앱 시작 시 세로 고정 + 게임 카메라 배경 보정. 캔버스가 가로로 넓으면 세로(1080×1920)로 강제.
    /// </summary>
    public static class AppBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnAppLoad()
        {
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = true;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoad()
        {
            var cam = Camera.main;
            if (cam != null && Luminance(cam.backgroundColor) < 0.5f)
                cam.backgroundColor = Color.white;

            var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                if (canvas.GetComponent<ForcePortraitCanvas>() == null)
                    canvas.gameObject.AddComponent<ForcePortraitCanvas>();
            }
        }

        private static float Luminance(Color c) => 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
    }
}
