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
            catch (Win32Exception e)
            {
#if UNITY_EDITOR_OSX
                if (e.NativeErrorCode == 2)
                {
                    return;
                }
#elif UNITY_EDITOR_WIN
                if (e.NativeErrorCode == 2)
                {
                    return;
                }
#endif
                throw e;
            }
        }
    }
}
