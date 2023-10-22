using UnityEngine;
using UnityEngine.SceneManagement;

namespace VrmDowngrader
{
    public class Cleanup : MonoBehaviour
    {
        private void Start()
        {
            Resources.UnloadUnusedAssets().completed += _ =>
            {
                SceneManager.LoadScene(SceneBuildIndex.VrmDowngraderScene);
            };
        }
    }
}
