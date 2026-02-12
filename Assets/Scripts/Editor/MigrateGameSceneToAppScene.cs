#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace CircuitOneStroke.Editor
{
    /// <summary>
    /// AppScene은 GameScene을 불러와 붙이는 방식으로 만들지 않음.
    /// 반드시 "Create AppScene (Tab Flow + Set First Build)" 또는 아래 메뉴로만 생성.
    /// </summary>
    public static class MigrateGameSceneToAppScene
    {
        /// <summary>AppScene을 GameScene 없이 처음부터 생성. (Create AppScene과 동일)</summary>
        [MenuItem("Circuit One-Stroke/Scenes/Create AppScene (From Scratch, No GameScene)")]
        public static void CreateAppSceneFromScratch()
        {
            CircuitOneStroke.Editor.CreateAppScene.Create();
            Debug.Log("AppScene created from scratch. Do not merge or load GameScene into AppScene.");
        }
    }
}
#endif
