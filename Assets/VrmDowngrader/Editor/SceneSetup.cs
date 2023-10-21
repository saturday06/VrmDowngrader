using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;
using System.Threading.Tasks;

namespace UrpScreenshot
{
    public static class SceneSetup
    {
        [InitializeOnLoadMethod]
        private static async void Execute()
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            EditorApplication.delayCall += () => taskCompletionSource.SetResult(true);
            await taskCompletionSource.Task;

            var currentScene = EditorSceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(currentScene.name))
            {
                return;
            }
            var scene = EditorBuildSettings.scenes.OrderBy(scene => scene.enabled).FirstOrDefault();
            if (scene == null)
            {
                return;
            }
            EditorSceneManager.OpenScene(scene.path);
        }
    }
}
