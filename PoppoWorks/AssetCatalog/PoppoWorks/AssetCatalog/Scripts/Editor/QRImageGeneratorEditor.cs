// SPDX-License-Identifier: CC0-1.0

using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AssetCatalog.Editor
{
    [CustomEditor(typeof(QRImageGenerator))]
    public class QRImageGeneratorEditor : UnityEditor.Editor
    {
        private SerializedProperty _titleLabelProperty;
        private SerializedProperty _commentLabelProperty;
        private SerializedProperty _linkFieldProperty;
        private SerializedProperty _backgroundPanelProperty;
        private SerializedProperty _qrImageProperty;
        private SerializedProperty _categoryProperty;
        private SerializedProperty _titleProperty;
        private SerializedProperty _commentProperty;
        private SerializedProperty _linkProperty;
        private SerializedProperty _textColorProperty;
        private SerializedProperty _backgroundColorProperty;
        private SerializedProperty _qrApiTimeoutSecondsProperty;
        private SerializedProperty _qrApiRetryCountProperty;
        private SerializedProperty _qrApiRetryDelaySecondsProperty;

        private void OnEnable()
        {
            _titleLabelProperty = serializedObject.FindProperty("titleLabel");
            _commentLabelProperty = serializedObject.FindProperty("commentLabel");
            _linkFieldProperty = serializedObject.FindProperty("linkField");
            _backgroundPanelProperty = serializedObject.FindProperty("backgroundPanel");
            _qrImageProperty = serializedObject.FindProperty("qrImage");
            _categoryProperty = serializedObject.FindProperty("category");
            _titleProperty = serializedObject.FindProperty("title");
            _commentProperty = serializedObject.FindProperty("comment");
            _linkProperty = serializedObject.FindProperty("link");
            _textColorProperty = serializedObject.FindProperty("textColor");
            _backgroundColorProperty = serializedObject.FindProperty("backgroundColor");
            _qrApiTimeoutSecondsProperty = serializedObject.FindProperty("qrApiTimeoutSeconds");
            _qrApiRetryCountProperty = serializedObject.FindProperty("qrApiRetryCount");
            _qrApiRetryDelaySecondsProperty = serializedObject.FindProperty("qrApiRetryDelaySeconds");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("UI References", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_titleLabelProperty, new GUIContent("Title Label"));
            EditorGUILayout.PropertyField(_commentLabelProperty, new GUIContent("Comment Label"));
            EditorGUILayout.PropertyField(_linkFieldProperty, new GUIContent("Link Field"));
            EditorGUILayout.PropertyField(_backgroundPanelProperty, new GUIContent("Background Panel"));
            EditorGUILayout.PropertyField(_qrImageProperty, new GUIContent("QR Image"));

            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_categoryProperty, new GUIContent("Category"));
            EditorGUILayout.PropertyField(_titleProperty, new GUIContent("Title"));
            EditorGUILayout.PropertyField(_commentProperty, new GUIContent("Comment"));
            EditorGUILayout.PropertyField(_linkProperty, new GUIContent("Link"));
            EditorGUILayout.PropertyField(_textColorProperty, new GUIContent("Text Color"));
            EditorGUILayout.PropertyField(_backgroundColorProperty, new GUIContent("Background Color"));
            EditorGUILayout.PropertyField(_qrApiTimeoutSecondsProperty, new GUIContent("QR API Timeout (sec)"));
            EditorGUILayout.PropertyField(_qrApiRetryCountProperty, new GUIContent("QR API Retry Count"));
            EditorGUILayout.PropertyField(_qrApiRetryDelaySecondsProperty, new GUIContent("QR API Delay (sec)"));

            _qrApiTimeoutSecondsProperty.intValue = Mathf.Max(1, _qrApiTimeoutSecondsProperty.intValue);
            _qrApiRetryCountProperty.intValue = Mathf.Max(0, _qrApiRetryCountProperty.intValue);
            _qrApiRetryDelaySecondsProperty.floatValue = Mathf.Max(0f, _qrApiRetryDelaySecondsProperty.floatValue);

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Settings"))
            {
                SaveSettings();
            }
            if (GUILayout.Button("Load Settings"))
            {
                LoadSettings();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (GUILayout.Button("Generate QR Code"))
            {
                GenerateQrCode();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void SaveSettings()
        {
            string defaultName = string.IsNullOrEmpty(_titleProperty.stringValue) ? "qr_settings" : _titleProperty.stringValue;
            string path = EditorUtility.SaveFilePanel("Save Settings", "", defaultName, "tsv");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            TSVHelper.SaveQRSettings(
                path,
                _categoryProperty.stringValue,
                _titleProperty.stringValue,
                _commentProperty.stringValue,
                _linkProperty.stringValue);
            Debug.Log($"Settings saved to: {path}");
        }

        private void LoadSettings()
        {
            string path = EditorUtility.OpenFilePanel("Load Settings", "", "tsv");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            Undo.RecordObject(target, "Load QR Settings");
            var (category, title, comment, link) = TSVHelper.LoadQRSettings(path);
            _categoryProperty.stringValue = category;
            _titleProperty.stringValue = title;
            _commentProperty.stringValue = comment;
            _linkProperty.stringValue = link;
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
            Debug.Log($"Settings loaded from: {path}");
        }

        private void GenerateQrCode()
        {
            var qr = (QRImageGenerator)target;
            var qrImage = _qrImageProperty.objectReferenceValue as Image;
            string link = _linkProperty.stringValue;

            if (qrImage == null || string.IsNullOrEmpty(link))
            {
                EditorUtility.DisplayDialog("Error", "QR Image and Link are required", "OK");
                return;
            }

            UnpackPrefabIfNeeded(qr.gameObject);
            serializedObject.ApplyModifiedProperties();
            qr.ApplyTexts();
            qr.ApplyColors();
            GenerateQRCodeAsync(qr, qrImage, link);
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

        private async void GenerateQRCodeAsync(QRImageGenerator qr, Image targetImage, string link)
        {
            QRCodeCache.ResetStats();
            try
            {
                var qrBytes = await FetchQRCode(
                    link,
                    Mathf.Max(1, qr.qrApiTimeoutSeconds),
                    Mathf.Max(0, qr.qrApiRetryCount),
                    Mathf.Max(0f, qr.qrApiRetryDelaySeconds));

                if (qrBytes != null && qrBytes.Length > 0)
                {
                    ApplyQRImage(qrBytes, targetImage);
                }
            }
            finally
            {
                QRCodeCache.LogStatsSummary();
            }
        }

        private async Task<byte[]> FetchQRCode(string url, int timeoutSeconds, int retryCount, float retryDelaySeconds)
        {
            try
            {
                return await QRCodeCache.GetOrFetchPngAsync(
                    url,
                    150,
                    timeoutSeconds,
                    retryCount,
                    retryDelaySeconds);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"QR Code generation failed: {ex.Message}");
                return null;
            }
        }

        private static void ApplyQRImage(byte[] qrBytes, Image targetImage)
        {
            var texture = new Texture2D(1, 1);
            texture.LoadImage(qrBytes);
            targetImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
        }
    }
}
