// SPDX-License-Identifier: CC0-1.0

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Threading.Tasks;
using System.Net.Http;

namespace AssetCatalog.Editor
{
    [CustomEditor(typeof(QRImageGenerator))]
    public class QRImageGeneratorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var qr = (QRImageGenerator)target;

            EditorGUILayout.LabelField("UI References", EditorStyles.boldLabel);
            qr.titleLabel = (Text)EditorGUILayout.ObjectField("Title Label", qr.titleLabel, typeof(Text), true);
            qr.commentLabel = (Text)EditorGUILayout.ObjectField("Comment Label", qr.commentLabel, typeof(Text), true);
            qr.linkField = (InputField)EditorGUILayout.ObjectField("Link Field", qr.linkField, typeof(InputField), true);
            qr.backgroundPanel = (Image)EditorGUILayout.ObjectField("Background Panel", qr.backgroundPanel, typeof(Image), true);
            qr.qrImage = (Image)EditorGUILayout.ObjectField("QR Image", qr.qrImage, typeof(Image), true);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            qr.category = EditorGUILayout.TextField("Category", qr.category);
            qr.title = EditorGUILayout.TextField("Title", qr.title);
            qr.comment = EditorGUILayout.TextField("Comment", qr.comment);
            qr.link = EditorGUILayout.TextField("Link", qr.link);
            qr.textColor = EditorGUILayout.ColorField("Text Color", qr.textColor);
            qr.backgroundColor = EditorGUILayout.ColorField("Background Color", qr.backgroundColor);

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Settings"))
            {
                SaveSettings(qr);
            }
            if (GUILayout.Button("Load Settings"))
            {
                LoadSettings(qr);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (GUILayout.Button("Generate QR Code"))
            {
                if (qr.qrImage != null && !string.IsNullOrEmpty(qr.link))
                {
                    UnpackPrefabIfNeeded(qr.gameObject);
                    qr.ApplyTexts();
                    qr.ApplyColors();
                    GenerateQRCodeAsync(qr);
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "QR Image and Link are required", "OK");
                }
            }
        }

        private void SaveSettings(QRImageGenerator qr)
        {
            string defaultName = string.IsNullOrEmpty(qr.title) ? "qr_settings" : qr.title;
            string path = EditorUtility.SaveFilePanel("Save Settings", "", defaultName, "tsv");
            if (!string.IsNullOrEmpty(path))
            {
                TSVHelper.SaveQRSettings(path, qr.category, qr.title, qr.comment, qr.link);
                Debug.Log($"Settings saved to: {path}");
            }
        }

        private void LoadSettings(QRImageGenerator qr)
        {
            string path = EditorUtility.OpenFilePanel("Load Settings", "", "tsv");
            if (!string.IsNullOrEmpty(path))
            {
                Undo.RecordObject(qr, "Load QR Settings");
                var (category, title, comment, link) = TSVHelper.LoadQRSettings(path);
                qr.category = category;
                qr.title = title;
                qr.comment = comment;
                qr.link = link;
                EditorUtility.SetDirty(qr);
                Debug.Log($"Settings loaded from: {path}");
            }
        }

        private void UnpackPrefabIfNeeded(GameObject obj)
        {
            var status = PrefabUtility.GetPrefabInstanceStatus(obj);
            if (status == PrefabInstanceStatus.Connected)
            {
                PrefabUtility.UnpackPrefabInstance(obj, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                Debug.Log("Prefab unpacked");
            }
        }

        private async void GenerateQRCodeAsync(QRImageGenerator qr)
        {
            var qrData = await FetchQRCode(qr.link);
            if (!string.IsNullOrEmpty(qrData))
            {
                ApplyQRImage(qrData, qr.qrImage);
            }
        }

        private async Task<string> FetchQRCode(string url)
        {
            try
            {
                string apiUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=150x150&data={UnityEngine.Networking.UnityWebRequest.EscapeURL(url)}&format=png";
                using (var client = new HttpClient())
                {
                    var response = await client.GetByteArrayAsync(apiUrl);
                    return System.Convert.ToBase64String(response);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"QR Code generation failed: {ex.Message}");
                return null;
            }
        }

        private void ApplyQRImage(string base64Data, Image targetImage)
        {
            var texture = new Texture2D(1, 1);
            texture.LoadImage(System.Convert.FromBase64String(base64Data));
            targetImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
        }
    }
}
