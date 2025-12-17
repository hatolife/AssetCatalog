// SPDX-License-Identifier: CC0-1.0

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace AssetCatalog
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class UrlInputFieldControllerUdon : UdonSharpBehaviour
    {
        public GameObject inputFieldContainer;

        public void CloseInputField()
        {
            if (inputFieldContainer != null)
            {
                inputFieldContainer.SetActive(false);
            }
        }
    }
}
