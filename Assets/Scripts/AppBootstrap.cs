using UnityEngine;
using UnityEngine.SceneManagement;

namespace CircuitOneStroke
{
    /// <summary>
    /// Optional: place on a persistent object in AppScene. Logs when play starts in AppScene (single-scene architecture).
    /// </summary>
    public class AppBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            var scene = gameObject.scene;
            if (scene.IsValid() && scene.name == "AppScene")
                Debug.Log("[AppBootstrap] Running in AppScene (single-scene).");
        }
    }
}
