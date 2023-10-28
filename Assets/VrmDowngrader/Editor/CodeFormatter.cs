using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEditor.Experimental;
using System.Linq;

namespace VrmDowngrader.Editor
{
    public class CodeFormatter : AssetsModifiedProcessor
    {
        public static readonly string DotnetFileName = "C:\\Program Files\\dotnet\\dotnet.exe"; // TODO: 触る環境が増えたら考える

        [MenuItem("Tools/Reformat & Apply Syntax Style with JetBrains CleanupCode")]
        public static async void ReformatAndApplySyntaxStyle()
        {
            UnityEngine.Debug.Log("Start Cleanup");

            // フォルダ名 + ".sln" ができるはず
            var slnPath =
                Path.GetFileName(Path.GetFullPath(Path.Combine(Application.dataPath, "..")))
                + ".sln";
            if (!File.Exists(slnPath))
            {
                UnityEngine.Debug.LogError($"No {slnPath}");
                return;
            }

            var processStartInfo = new ProcessStartInfo
            {
                FileName = DotnetFileName,
                ArgumentList =
                {
                    "tool",
                    "run",
                    "jb",
                    "CleanupCode",
                    "--profile=Built-in: Reformat & Apply Syntax Style",
                    slnPath
                },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var output = new StringBuilder();
            var error = new StringBuilder();
            try
            {
                using var process = new Process();
                process.StartInfo = processStartInfo;
                if (!process.Start())
                {
                    UnityEngine.Debug.LogError($"Failed to start {DotnetFileName}");
                    return;
                }

                process.OutputDataReceived += (_, args) =>
                {
                    if (args.Data != null)
                    {
                        output.AppendLine(args.Data);
                    }
                };
                process.ErrorDataReceived += (_, args) =>
                {
                    if (args.Data != null)
                    {
                        error.AppendLine(args.Data);
                    }
                };
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                while (!process.WaitForExit(0))
                {
                    await Task.Delay(100);
                }
            }
            catch (Win32Exception) { }

            UnityEngine.Debug.Log($"Output={output}");
            UnityEngine.Debug.Log($"Error={error}");
        }

        protected override void OnAssetsModified(
            string[] changedAssets,
            string[] addedAssets,
            string[] deletedAssets,
            AssetMoveInfo[] movedAssets
        )
        {
            var remainingAssetPaths = changedAssets
                .Concat(addedAssets)
                .Where(assetPath => assetPath.EndsWith(".cs"))
                .Distinct()
                .ToArray();
            while (remainingAssetPaths.Length > 0)
            {
                const int assetPathCountAtOneTime = 20;
                var formattingAssetPaths = remainingAssetPaths
                    .Take(assetPathCountAtOneTime)
                    .ToArray();
                remainingAssetPaths = remainingAssetPaths.Skip(assetPathCountAtOneTime).ToArray();
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = DotnetFileName,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    ArgumentList = { "dotnet", "csharpier" }
                };
                foreach (var formattingAssetPath in formattingAssetPaths)
                {
                    processStartInfo.ArgumentList.Add(formattingAssetPath);
                }

                try
                {
                    using var process = Process.Start(processStartInfo);
                    process.WaitForExit();
                }
                catch (Win32Exception)
                {
                    // TODO: 触る環境が増えたら考える
                    throw;
                }

                foreach (var formattingAssetPath in formattingAssetPaths)
                {
                    ReportAssetChanged(formattingAssetPath);
                }
            }
        }
    }
}
