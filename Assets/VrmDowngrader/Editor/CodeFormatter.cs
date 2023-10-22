using System.ComponentModel;
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

            try
            {
                var dotnetFileName = "C:\\Program Files\\dotnet\\dotnet.exe";
                using var process = Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = dotnetFileName,
                        ArgumentList = { "csharpier", assetPath },
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                );
            }
            catch (Win32Exception)
            {
                // TODO: 触る環境が増えたら考える
            }
        }
    }
}
