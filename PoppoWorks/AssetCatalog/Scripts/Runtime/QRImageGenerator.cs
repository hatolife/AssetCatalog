// SPDX-License-Identifier: CC0-1.0

using UnityEngine;
using UnityEngine.UI;

namespace AssetCatalog
{
    public class QRImageGenerator : MonoBehaviour
    {
        [Header("UI References")]
        public Text titleLabel;
        public Text commentLabel;
        public InputField linkField;
        public Image backgroundPanel;
        public Image qrImage;

        [Header("Settings")]
        public string category = "";
        public string title = "";
        public string comment = "";
        public string link = "";
        public Color textColor = Color.white;
        public Color backgroundColor = Color.black;

        public void ApplyTexts()
        {
            if (titleLabel != null)
                titleLabel.text = title;
            if (commentLabel != null)
                commentLabel.text = comment;
            if (linkField != null)
                linkField.text = link;
        }

        public void ApplyColors()
        {
            if (titleLabel != null)
                titleLabel.color = textColor;
            if (commentLabel != null)
                commentLabel.color = textColor;
            if (linkField != null && linkField.textComponent != null)
                linkField.textComponent.color = textColor;
            if (backgroundPanel != null)
                backgroundPanel.color = backgroundColor;
        }
    }
}
