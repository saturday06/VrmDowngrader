using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UniVRM10;
using VRMShaders;

public class VrmDowngrader : MonoBehaviour
{
    private async void Start()
    {
        Debug.Log("開始");
        var taskCompletionSource = new TaskCompletionSource<bool>();
        Debug.Log("リソースのロード開始");
        var textAssetRequest = Resources.LoadAsync<TextAsset>("Seed-san.vrm");
        textAssetRequest.completed += _ => taskCompletionSource.SetResult(true);
        await taskCompletionSource.Task;
        Debug.Log("リソースのロード完了");
        var textAsset = textAssetRequest.asset as TextAsset;
        if (textAsset == null)
        {
            Debug.Log("リソースのロードに失敗");
            return;
        }
        var vrmBytes = textAsset.bytes;
        Debug.Log("VRMのバイト配列の取得完了");

        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var vrm10Instance = await Vrm10.LoadBytesAsync(
            vrmBytes,
            canLoadVrm0X: false,
            showMeshes: true,
            awaitCaller: new ImmediateCaller(),
            ct: cancellationToken
        );

        if (vrm10Instance == null)
        {
            Debug.LogWarning("LoadPathAsync is null");
            return;
        }

        Debug.Log("うまくいきました");
    }
}
