using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace VrmDowngrader
{
    internal static class WebGL
    {
        internal static async Task Yield()
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();
            SynchronizationContext.Current.Post(_ => taskCompletionSource.SetResult(true), null);
            await taskCompletionSource.Task;
        }

        internal static async Task WaitForNextFrame()
        {
            // TODO: やっぱりUniTaskの方がいいかもしれない
            var frameCount = Time.frameCount;
            while (frameCount == Time.frameCount)
            {
                await Yield();
            }
        }
    }
}
