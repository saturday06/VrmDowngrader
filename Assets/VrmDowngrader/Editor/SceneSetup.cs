using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace VrmDowngrader
{
    public class SceneSetup : AssetPostprocessor
    {
        [InitializeOnLoadMethod]
        private static async void Execute()
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            EditorApplication.delayCall += () => taskCompletionSource.SetResult(true);
            await taskCompletionSource.Task;

            var currentScene = SceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(currentScene.name))
            {
                return;
            }
            var scene = EditorBuildSettings.scenes.FirstOrDefault(scene => scene.enabled);
            if (scene == null)
            {
                return;
            }
            EditorSceneManager.OpenScene(scene.path);
        }

        void OnPreprocessAsset()
        {
            if (!assetPath.StartsWith("Assets/") || !assetPath.EndsWith(".unity"))
            {
                return;
            }

            CreateSceneBuildIndexSource();
        }

        [MenuItem("Tools/Create Scene Build Index Source")]
        private static void CreateSceneBuildIndexSource()
        {
            var sceneBuildIndexSourceBuilder = new StringBuilder();
            sceneBuildIndexSourceBuilder.Append(
                @"// このクラスは自動生成されたやつです。Toolメニューに再生成するやつがあるのでそれで作ってください

namespace VrmDowngrader
{
    public class SceneBuildIndex
    {
"
            );

            foreach (
                var (sceneName, index) in EditorBuildSettings.scenes
                    .Where(scene => scene.enabled)
                    .Select((scene, index) =>
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);
                        if (asset == null)
                        {
                            throw new FileNotFoundException($"EditorBuildSettingsに登録されているシーン {scene.path} が見つかりません");
                        }
                        return (asset.name, index);
                    })
            )
            {
                if (!new Regex("\\A[A-Za-z0-9]+\\Z").IsMatch(sceneName))
                {
                    throw new InvalidDataException($"シーン名に英数字以外が含まれています: [{sceneName}]");
                }
                sceneBuildIndexSourceBuilder.Append(
                    $"        internal const int {sceneName} = {index};"
                );
                sceneBuildIndexSourceBuilder.AppendLine();
            }
            sceneBuildIndexSourceBuilder.Append("    }");
            sceneBuildIndexSourceBuilder.AppendLine();
            sceneBuildIndexSourceBuilder.Append("}");
            sceneBuildIndexSourceBuilder.AppendLine();

            var sceneBuildIndexSourcePath = Path.Combine(
                UnityEngine.Application.dataPath,
                "VrmDowngrader",
                "SceneBuildIndex.g.cs"
            );
            var sceneBuildIndexSourceBytes = Encoding.UTF8.GetBytes(
                sceneBuildIndexSourceBuilder.ToString().Replace("\r\n", "\n")
            );
            var oldSceneBuildIndexSourceBytes = File.ReadAllBytes(sceneBuildIndexSourcePath);
            if (sceneBuildIndexSourceBytes != oldSceneBuildIndexSourceBytes)
            {
                File.WriteAllBytes(sceneBuildIndexSourcePath, sceneBuildIndexSourceBytes);
            }
        }
    }
}
