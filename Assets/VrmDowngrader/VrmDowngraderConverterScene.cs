using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

namespace VrmDowngrader
{
    [RequireComponent(typeof(UIDocument))]
    public class VrmDowngraderConverterScene : MonoBehaviour
    {
        private Button? _openButton;

        private Button OpenButton =>
            _openButton ??= GetComponent<UIDocument>().rootVisualElement.Q<Button>("OpenButton");

        private Button? _saveButton;

        private Button SaveButton =>
            _saveButton ??= GetComponent<UIDocument>().rootVisualElement.Q<Button>("SaveButton");

        private Button? _resetButton;

        private Button ResetButton =>
            _resetButton ??= GetComponent<UIDocument>().rootVisualElement.Q<Button>("ResetButton");

        private Label? _additionalMessageLabel;

        private Label AdditionalMessageLabel =>
            _additionalMessageLabel ??= GetComponent<UIDocument>().rootVisualElement.Q<Label>(
                "AdditionalMessageLabel"
            );

        private Label? _errorMessageLabel;

        private Label ErrorMessageLabel =>
            _errorMessageLabel ??= GetComponent<UIDocument>().rootVisualElement.Q<Label>(
                "ErrorMessageLabel"
            );

        private byte[]? _vrm0Bytes;

        private bool _opening = false;

        private async Task OnOpenButtonClicked(byte[] vrm1Bytes)
        {
            try
            {
                if (_opening)
                {
                    return;
                }

                _opening = true;

                ErrorMessageLabel.text = "";
                var logoLabel = GetComponent<UIDocument>().rootVisualElement.Q<Label>();
                logoLabel.text = "";
                OpenButton.text = LocalizationSettings.StringDatabase.GetLocalizedString(
                    LocalizationTable.StringTableName,
                    "LOADING"
                );
                await WebGL.WaitForNextFrame();

                _vrm0Bytes = await Vrm1ToVrm0Converter.Convert(vrm1Bytes);

                OpenButton.text = "Open VRM1";

                Debug.LogFormat("エクスポートしました {0} bytes", _vrm0Bytes.Length);

                OpenButton.style.display = DisplayStyle.None;
                SaveButton.style.display = DisplayStyle.Flex;
                ResetButton.style.display = DisplayStyle.Flex;
            }
            catch (Exception e)
            {
                Debug.LogException(e);

                OpenButton.style.display = DisplayStyle.Flex;
                OpenButton.SetEnabled(true);
                OpenButton.text = LocalizationSettings.StringDatabase.GetLocalizedString(
                    LocalizationTable.StringTableName,
                    "OPEN_VRM1"
                );
                ErrorMessageLabel.text = e.ToString();
                return;
            }
            finally
            {
                _opening = false;
            }
        }

        private void OnSaveButtonClicked()
        {
            if (_vrm0Bytes == null)
            {
                return;
            }
#if UNITY_EDITOR
            var path = UnityEditor.EditorUtility.SaveFilePanel(
                "Save VRM0",
                "",
                "output.vrm",
                "vrm"
            );
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            File.WriteAllBytes(path, _vrm0Bytes);
#elif UNITY_WEBGL
            WebBrowserVrm1Save(_vrm0Bytes, _vrm0Bytes.Length);
#endif
        }

        [DllImport("__Internal")]
        public static extern void WebBrowserVrm0Open();

        [DllImport("__Internal")]
        public static extern void WebBrowserVrm1Save(byte[] bytes, int bytesLength);

        [Preserve] // WebBrowser側からコールバックを受け取りたい
        public void WebBrowserVrm0Opened(string url)
        {
            Debug.LogFormat($"WebBrowserVrm0Opened {url}");

            // 関数分けたほうが良いかな
            _ = Task.Factory.StartNew(
                async () =>
                {
                    byte[] vrm1Bytes;
                    using (var unityWebRequest = UnityWebRequest.Get(url))
                    {
                        var taskCompletionSource = new TaskCompletionSource<bool>();
                        unityWebRequest.SendWebRequest().completed += _ =>
                        {
                            taskCompletionSource.SetResult(true);
                        };
                        await taskCompletionSource.Task;
                        if (unityWebRequest.result != UnityWebRequest.Result.Success)
                        {
                            Debug.LogErrorFormat(
                                "UnityWebRequest failed: {0}",
                                unityWebRequest.result
                            );
                            ResetScene();
                            return;
                        }

                        vrm1Bytes = unityWebRequest.downloadHandler.data;
                    }

                    await OnOpenButtonClicked(vrm1Bytes);
                },
                CancellationToken.None,
                TaskCreationOptions.None,
                TaskScheduler.FromCurrentSynchronizationContext()
            );
        }

        private void ResetScene()
        {
            // 不要なメモリを解放したいが、正直なんもわからんのでシーン遷移してUnloadUnusedAssetsをしてしまう
            // TODO: プログラマとしての矜持は無いのか!!!???
            SceneManager.LoadScene(SceneBuildIndex.VrmDowngraderCleanupScene);
        }

        private void Start()
        {
            // runInBackground = falseだと、WebGLでは起動後にフォーカスがない場合中途半端にUIが出た状態になる。
            // プロジェクトの設定としてはrunInBackground = trueとしておき、起動後にfalseにする。
            Application.runInBackground = false;

            OpenButton.text = LocalizationSettings.StringDatabase.GetLocalizedString(
                LocalizationTable.StringTableName,
                "OPEN_VRM1"
            );
            OpenButton.clicked += () =>
            {
                try
                {
#if UNITY_EDITOR
                    var path = UnityEditor.EditorUtility.OpenFilePanel("Open VRM1", "", "vrm");
                    if (string.IsNullOrEmpty(path))
                    {
                        ResetScene();
                        return;
                    }

                    var vrm1Bytes = File.ReadAllBytes(path);
                    _ = OnOpenButtonClicked(vrm1Bytes);
#elif UNITY_WEBGL
                    WebBrowserVrm0Open();
#else
#warning Not Implemented
#endif
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    ErrorMessageLabel.text = e.ToString();
                }
            };
            SaveButton.text = LocalizationSettings.StringDatabase.GetLocalizedString(
                LocalizationTable.StringTableName,
                "SAVE_VRM0"
            );
            SaveButton.clicked += () =>
            {
                try
                {
                    OnSaveButtonClicked();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            };
            ResetButton.text = LocalizationSettings.StringDatabase.GetLocalizedString(
                LocalizationTable.StringTableName,
                "RESET"
            );
            ResetButton.clicked += () =>
            {
                ResetScene();
            };

            AdditionalMessageLabel.text = LocalizationSettings.StringDatabase.GetLocalizedString(
                LocalizationTable.StringTableName,
                "NO_DATA_TRANSFER_MESSAGE"
            );
        }
    }
}
