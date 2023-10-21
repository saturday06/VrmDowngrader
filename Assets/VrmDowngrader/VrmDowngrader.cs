using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using UniVRM10;
using VRM;
using VRMShaders;

namespace VrmDowngrader
{
    [RequireComponent(typeof(UIDocument))]
    public class VrmDowngrader : MonoBehaviour
    {
        private async Task OnStartButtonClicked(Button button)
        {
            button.text = "Loading...";
            button.SetEnabled(false);
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
                button.text = "Error 0 / Restart";
                button.SetEnabled(true);
                return;
            }
            var vrmBytes = textAsset.bytes;
            Debug.Log("VRMのバイト配列の取得完了");

            var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            Vrm10Instance vrm10Instance;
            try
            {
                vrm10Instance = await Vrm10.LoadBytesAsync(
                    vrmBytes,
                    canLoadVrm0X: false,
                    showMeshes: true,
                    awaitCaller: new ImmediateCaller(),
                    ct: cancellationToken
                );
                if (vrm10Instance == null)
                {
                    Debug.LogWarning("LoadPathAsync is null");
                    button.text = "Error 1 / Restart";
                    button.SetEnabled(true);
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                button.text = "Error 2 / Restart";
                button.SetEnabled(true);
                return;
            }
            using var _ = vrm10Instance.Runtime;

            button.text = "OK";
            Debug.Log("インポートはうまくいきました");

            Debug.Log("VRM1のコンポーネントをVRM0で置換していきます");
            // https://github.com/vrm-c/UniVRM/blob/7e052b19b3c0b4cd02e63159fc37db820729554e/Assets/VRM10/Runtime/Migration/MigrationVrmMeta.cs
            var vrm1Meta = vrm10Instance.Vrm.Meta;
            var vrm0Meta = vrm10Instance.gameObject.AddComponent<VRMMeta>().Meta;
            vrm0Meta.Title = vrm1Meta.Name;
            vrm0Meta.Author = string.Join("/ ", vrm1Meta.Authors);
            vrm0Meta.Version = vrm1Meta.Version;

            Debug.Log("エクスポートします");
        }

        private void Start()
        {
            var startButton = GetComponent<UIDocument>().rootVisualElement.Query<Button>().First();
            startButton.clicked += () => _ = OnStartButtonClicked(startButton);
        }
    }
}
