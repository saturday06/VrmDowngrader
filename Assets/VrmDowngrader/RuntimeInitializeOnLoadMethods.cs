using UnityEngine;
using UnityEngine.CrashReportHandler;

namespace VrmDowngrader
{
    internal static class RuntimeInitializeOnLoadMethods
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        internal static void BeforeSplashScreen()
        {
            CrashReportHandler.enableCaptureExceptions = false;
        }
    }
}
