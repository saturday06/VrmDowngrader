using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace VrmDowngrader.Editor
{
    public class CodeFormatter : AssetsModifiedProcessor
    {
        public static readonly string DotnetFileName = "C:\\Program Files\\dotnet\\dotnet.exe"; // TODO: 触る環境が増えたら考える

        /// <summary>
        ///     JetBrains CleanupCode を実行します。
        ///     asyncにするとコードフォーマット中にコンパイルが走って大変なことになるので同期的に実行します。
        /// </summary>
        [MenuItem("Tools/Reformat & Apply Syntax Style with JetBrains CleanupCode")]
        public static void ReformatAndApplySyntaxStyle()
        {
            Debug.Log("CleanupCode Start");

            // フォルダ名 + ".sln" ができるはず
            var slnPath =
                Path.GetFileName(Path.GetFullPath(Path.Combine(Application.dataPath, "..")))
                + ".sln";
            if (!File.Exists(slnPath))
            {
                Debug.LogError($"No {slnPath}");
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
                    // "--profile=Built-in: Reformat & Apply Syntax Style",
                    "--profile=Built-in: Full Cleanup",
                    slnPath
                },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            var output = new StringBuilder();
            var error = new StringBuilder();
            int? exitCode = null;
            Exception? exception = null;
            try
            {
                EditorUtility.DisplayProgressBar(
                    "JetBrains CleanupCode",
                    "JetBrains CleanupCodeを実行しています",
                    0
                );

                using var process = new Process();
                process.StartInfo = processStartInfo;
                if (!process.Start())
                {
                    Debug.LogError($"Failed to start {DotnetFileName}");
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
                process.WaitForExit();
                exitCode = process.ExitCode;
            }
            catch (Win32Exception e)
            {
                exception = e;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Debug.Log($"CleanupCode Completed ExitCode={exitCode}");
            Debug.Log($"CleanupCode Output={output}");
            Debug.Log($"CleanupCode Error={error}");
            if (exception != null)
            {
                Debug.Log($"Cleanup Exception={exception}");
            }
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
                    return;
                }

                foreach (var formattingAssetPath in formattingAssetPaths)
                {
                    ReportAssetChanged(formattingAssetPath);
                }
            }
        }
    }
}
