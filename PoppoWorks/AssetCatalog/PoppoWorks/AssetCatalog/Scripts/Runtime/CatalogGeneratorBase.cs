// SPDX-License-Identifier: CC0-1.0

using UnityEngine;

namespace AssetCatalog
{
    public abstract class CatalogGeneratorBase : MonoBehaviour
    {
        [Header("Size")]
        public float width = 800f;
        public float height = 1200f;
        public float qrCodeSize = 60f;

        [Header("Settings")]
        public bool showQRCode = true;
        public bool syncScrollGlobally = false;
        public Color textColor = Color.white;
        [Range(1, 8)]
        public int textResolution = 4;
        public Object tsvFile;

        [Header("QR API")]
        [Min(1)]
        public int qrApiTimeoutSeconds = 10;
        [Range(0, 5)]
        public int qrApiRetryCount = 1;
        [Min(0f)]
        public float qrApiRetryDelaySeconds = 1f;

        [HideInInspector]
        public EntryGroup[] groups;
    }
}
