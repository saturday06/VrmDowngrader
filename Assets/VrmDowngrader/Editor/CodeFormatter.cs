using System.Diagnostics;
using UnityEditor;

namespace VrmDowngrader.Editor
{
    public class CodeFormatter : AssetPostprocessor
    {
        void OnPreprocessAsset()
        {
            if (!assetPath.StartsWith("Assets/") || !assetPath.EndsWith(".cs"))
            {
                return;
            }

            using var process = Process.Start(
                new ProcessStartInfo
                {
                    FileName = "dotnet",
                    ArgumentList = { "csharpier", assetPath },
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            );
        }
    }
}
