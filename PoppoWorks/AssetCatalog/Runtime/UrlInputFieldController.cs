// SPDX-License-Identifier: CC0-1.0

using UnityEngine;

namespace AssetCatalog
{
    public class UrlInputFieldController : MonoBehaviour
    {
        [SerializeField] private GameObject inputFieldContainer;

        public void CloseInputField()
        {
            if (inputFieldContainer != null)
            {
                inputFieldContainer.SetActive(false);
            }
        }
    }
}
