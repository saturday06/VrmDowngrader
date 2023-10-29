using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace VrmDowngrader.Editor
{
    internal static class Release
    {
        [MenuItem("Tools/Build WebGL Player with Release Configuration")]
        public static void DoRelease()
        {
            var buildFolderName = "Build";
            try
            {
                Directory.Delete(
                    Path.Combine(Application.dataPath, "..", buildFolderName),
                    recursive: true
                );
            }
            catch (DirectoryNotFoundException) { }

            PlayerSettings.SetManagedStrippingLevel(
                BuildTargetGroup.WebGL,
                ManagedStrippingLevel.High
            );
            PlayerSettings.SetIl2CppCompilerConfiguration(
                BuildTargetGroup.WebGL,
                Il2CppCompilerConfiguration.Master
            );
            PlayerSettings.SetIl2CppCodeGeneration(
                NamedBuildTarget.WebGL,
                Il2CppCodeGeneration.OptimizeSize
            );

            // 本当はbrotliを有効化したい
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;

            PlayerSettings.WebGL.debugSymbolMode = WebGLDebugSymbolMode.Off;

            var build = BuildPipeline.BuildPlayer(
                EditorBuildSettings.scenes
                    .Where(scene => scene.enabled)
                    .Select(Scene => Scene.path)
                    .ToArray(),
                buildFolderName,
                BuildTarget.WebGL,
                BuildOptions.None
            );

            Debug.Log(
                $"Build Result: {build.summary.result}"
                    + (build.summary.result == BuildResult.Succeeded ? " | " : "\n")
                    + $"OutputPath: {build.summary.outputPath}\n"
                    + $"Guid: {build.summary.guid}\n"
                    + $"Platform: {build.summary.platform}\n"
                    + $"PlatformGroup: {build.summary.platformGroup}\n"
                    + $"BuildStartedAt: {build.summary.buildStartedAt}\n"
                    + $"BuildEndedAt: {build.summary.buildEndedAt}\n"
                    + $"TotalSize: {build.summary.totalSize}\n"
                    + $"TotalTime: {build.summary.totalTime}\n"
                    + $"TotalWarnings: {build.summary.totalWarnings}\n"
                    + $"TotalErrors: {build.summary.totalErrors}\n"
            );

            if (!Application.isBatchMode)
            {
                return;
            }

            if (build.summary.result == BuildResult.Succeeded)
            {
                EditorApplication.Exit(0);
            }
            else
            {
                EditorApplication.Exit(1);
            }
        }
    }
}
