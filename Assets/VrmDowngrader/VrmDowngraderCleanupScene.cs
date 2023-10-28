using UnityEngine;
using UnityEngine.SceneManagement;

namespace VrmDowngrader
{
    public class VrmDowngraderCleanupScene : MonoBehaviour
    {
        private void Start()
        {
            Resources.UnloadUnusedAssets().completed += _ =>
            {
                SceneManager.LoadScene(SceneBuildIndex.VrmDowngraderConverterScene);
            };
        }
    }
}
