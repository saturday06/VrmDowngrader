using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;

namespace VrmDowngrader
{
    public class VrmDowngraderInitializationScene : MonoBehaviour
    {
        private async void Start()
        {
            await LocalizationSettings.StringDatabase
                .PreloadTables(LocalizationTable.StringTableName)
                .Task;
            SceneManager.LoadScene(SceneBuildIndex.VrmDowngraderConverterScene);
        }
    }
}
