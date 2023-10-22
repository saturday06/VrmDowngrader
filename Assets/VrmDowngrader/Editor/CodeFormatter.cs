using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Codice.Client.BaseCommands.CheckIn;
using UnityEditor;
using UnityEngine;

namespace VrmDowngrader.Editor
{
    public class CodeFormatter : AssetPostprocessor
    {
        public static readonly string DotnetFileName = "C:\\Program Files\\dotnet\\dotnet.exe"; // TODO: 触る環境が増えたら考える

        private void OnPreprocessAsset()
        {
            if (!assetPath.StartsWith("Assets/") || !assetPath.EndsWith(".cs"))
            {
                return;
            }

            var processStartInfo = new ProcessStartInfo
            {
                FileName = DotnetFileName,
                ArgumentList = { "csharpier", assetPath },
                UseShellExecute = false,
                CreateNoWindow = true
            };
            try
            {
                using var process = Process.Start(processStartInfo);
                process.WaitForExit();
            }
            catch (Win32Exception)
            {
                // TODO: 触る環境が増えたら考える
            }
        }

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
                        output.AppendLine(args.Data);
                };
                process.ErrorDataReceived += (_, args) =>
                {
                    if (args.Data != null)
                        error.AppendLine(args.Data);
                };
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                while (!process.WaitForExit(0))
                    await Task.Delay(100);
            }
            catch (Win32Exception) { }

            UnityEngine.Debug.Log($"Output={output}");
            UnityEngine.Debug.Log($"Error={error}");
        }
    }
}
