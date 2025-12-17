// SPDX-License-Identifier: CC0-1.0

using UnityEngine;
using UnityEngine.UI;

namespace AssetCatalog
{
    public class UrlDisplayHandler : MonoBehaviour
    {
        [SerializeField] private InputField urlInputField;
        [SerializeField] private GameObject inputFieldContainer;
        [SerializeField] private string url;

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
