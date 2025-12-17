// SPDX-License-Identifier: CC0-1.0

using UnityEngine;

namespace AssetCatalog
{
    [AddComponentMenu("Asset Catalog/Asset Catalog Generator")]
    public class AssetCatalogGenerator : MonoBehaviour
    {
        [Header("Size")]
        public float width = 800f;
        public float height = 1200f;
        public float qrCodeSize = 60f;

        [Header("Settings")]
        public bool showQRCode = true;
        public Color textColor = Color.white;
        [Range(1, 8)]
        public int textResolution = 4;

        [HideInInspector]
        public EntryGroup[] groups;
    }
}
