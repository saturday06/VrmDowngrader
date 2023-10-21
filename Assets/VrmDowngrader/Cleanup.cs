using UnityEngine;
using UnityEngine.SceneManagement;

namespace VrmDowngrader
{
    public class Cleanup : MonoBehaviour
    {
        void Start()
        {
            Resources.UnloadUnusedAssets().completed += _ =>
            {
                SceneManager.LoadScene(SceneBuildIndex.VrmDowngraderScene);
            };
        }
    }
}
