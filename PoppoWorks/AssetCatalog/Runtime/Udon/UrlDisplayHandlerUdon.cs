// SPDX-License-Identifier: CC0-1.0

using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

namespace AssetCatalog
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class UrlDisplayHandlerUdon : UdonSharpBehaviour
    {
        public InputField urlInputField;
        public GameObject inputFieldContainer;
        public string url;

        public void OnQRCodeClicked()
        {
            if (urlInputField != null)
            {
                urlInputField.text = url;
            }

            if (inputFieldContainer != null)
            {
                inputFieldContainer.SetActive(true);
            }
        }

        public void SetUrl(string newUrl)
        {
            url = newUrl;
        }
    }
}
