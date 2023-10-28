using UnityEngine;
using UnityEngine.Localization;

namespace UnityEditor.Localization.Samples
{
    /// <summary>
    /// This sample shows how to use the LocalizedString ChangeHandler to get a notification whenever a new translated string is available.
    /// This method is suited to dealing with static strings that will not change during game play for the selected Locale.
    /// </summary>
    public class LocalizedStringWithChangeHandlerExample : MonoBehaviour
    {
        // A LocalizedString provides an interface to retrieving translated strings.
        // This example assumes a String Table Collection with the name "My String Table" and an entry with the Key "Hello World" exists.
        // You can change the Table Collection and Entry target in the inspector.
        public LocalizedString stringRef =
            new() { TableReference = "My String Table", TableEntryReference = "Hello World" };

        private string m_TranslatedString;

        private void OnEnable()
        {
            stringRef.StringChanged += UpdateString;
        }

        private void OnDisable()
        {
            stringRef.StringChanged -= UpdateString;
        }

        private void UpdateString(string translatedValue)
        {
            m_TranslatedString = translatedValue;
            Debug.Log("Translated Value Updated: " + translatedValue);
        }

        private void OnGUI()
        {
            GUILayout.Label(m_TranslatedString);
        }
    }
}
