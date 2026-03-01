// SPDX-License-Identifier: CC0-1.0

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Threading.Tasks;
using System.IO;
using System;

namespace AssetCatalog.Editor
{
    [CustomEditor(typeof(CatalogGeneratorBase), true)]
    public class CatalogGeneratorEditor : UnityEditor.Editor
    {
        private CatalogGeneratorBase _target;
        private int _res;

        private SerializedProperty _tsvFileProperty;
        private SerializedProperty _widthProperty;
        private SerializedProperty _heightProperty;
        private SerializedProperty _qrCodeSizeProperty;
        private SerializedProperty _showQRCodeProperty;
        private SerializedProperty _textColorProperty;
        private SerializedProperty _textResolutionProperty;
        private SerializedProperty _qrApiTimeoutSecondsProperty;
        private SerializedProperty _qrApiRetryCountProperty;
        private SerializedProperty _qrApiRetryDelaySecondsProperty;

        private void OnEnable()
        {
            _target = target as CatalogGeneratorBase;
            _widthProperty = serializedObject.FindProperty("width");
            _heightProperty = serializedObject.FindProperty("height");
            _qrCodeSizeProperty = serializedObject.FindProperty("qrCodeSize");
            _showQRCodeProperty = serializedObject.FindProperty("showQRCode");
            _textColorProperty = serializedObject.FindProperty("textColor");
            _textResolutionProperty = serializedObject.FindProperty("textResolution");
            _qrApiTimeoutSecondsProperty = serializedObject.FindProperty("qrApiTimeoutSeconds");
            _qrApiRetryCountProperty = serializedObject.FindProperty("qrApiRetryCount");
            _qrApiRetryDelaySecondsProperty = serializedObject.FindProperty("qrApiRetryDelaySeconds");
            _tsvFileProperty = serializedObject.FindProperty("tsvFile");

        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Size
            EditorGUILayout.PropertyField(_widthProperty, new GUIContent("Width"));
            EditorGUILayout.PropertyField(_heightProperty, new GUIContent("Height"));
            EditorGUILayout.PropertyField(_qrCodeSizeProperty, new GUIContent("QR Code Size"));

            EditorGUILayout.Space();

            // Settings
            EditorGUILayout.PropertyField(_showQRCodeProperty, new GUIContent("Show QR Code"));
            EditorGUILayout.PropertyField(_textColorProperty, new GUIContent("Text Color"));
            EditorGUILayout.PropertyField(_textResolutionProperty, new GUIContent("Text Resolution"));
            EditorGUILayout.PropertyField(_qrApiTimeoutSecondsProperty, new GUIContent("QR API Timeout (sec)"));
            EditorGUILayout.PropertyField(_qrApiRetryCountProperty, new GUIContent("QR API Retry Count"));
            EditorGUILayout.PropertyField(_qrApiRetryDelaySecondsProperty, new GUIContent("QR API Delay (sec)"));

            // Enforce sane API configuration ranges regardless of inspector input mode.
            _qrApiTimeoutSecondsProperty.intValue = Mathf.Max(1, _qrApiTimeoutSecondsProperty.intValue);
            _qrApiRetryCountProperty.intValue = Mathf.Max(0, _qrApiRetryCountProperty.intValue);
            _qrApiRetryDelaySecondsProperty.floatValue = Mathf.Max(0f, _qrApiRetryDelaySecondsProperty.floatValue);

            EditorGUILayout.Space();

            // TSV File Drop Area
            EditorGUILayout.PropertyField(_tsvFileProperty, new GUIContent("TSV File"));
            string selectedTsvPath = GetSelectedTsvPath();

            GUI.enabled = !string.IsNullOrEmpty(selectedTsvPath);
            if (GUILayout.Button("Open With Default App", GUILayout.Height(24)))
            {
                OpenTsvWithDefaultApp(selectedTsvPath);
            }
            GUI.enabled = true;

            if (GUILayout.Button("Load", GUILayout.Height(30)))
            {
                if (!string.IsNullOrEmpty(selectedTsvPath))
                {
                    Undo.RecordObject(_target, "Load TSV");
                    _target.groups = TSVHelper.LoadCatalog(selectedTsvPath);
                    EditorUtility.SetDirty(_target);
                    Debug.Log($"Loaded: {selectedTsvPath} ({_target.groups?.Length ?? 0} groups)");
                }
                else
                {
                    Debug.LogWarning("Please drag & drop a TSV file first");
                }
            }

            GUI.enabled = _target.groups != null && _target.groups.Length > 0;
            if (GUILayout.Button("Generate", GUILayout.Height(30)))
            {
                serializedObject.ApplyModifiedProperties();
                Generate();
                return;
            }
            GUI.enabled = true;

            // Show loaded data
            if (_target.groups != null && _target.groups.Length > 0)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                foreach (var group in _target.groups)
                {
                    if (group.groupName == TSVHelper.SPACER_MARKER)
                    {
                        EditorGUILayout.LabelField("---", EditorStyles.centeredGreyMiniLabel);
                        continue;
                    }

                    EditorGUILayout.LabelField($"■ {group.groupName}", EditorStyles.boldLabel);
                    if (group.entries != null)
                    {
                        EditorGUI.indentLevel++;
                        foreach (var entry in group.entries)
                        {
                            EditorGUILayout.LabelField($"{entry.entryName}");
                        }
                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("No data loaded. Load a TSV file first.", MessageType.Warning);
            }

            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();
        }

        private string GetSelectedTsvPath()
        {
            UnityEngine.Object selectedTsv = _tsvFileProperty.objectReferenceValue;
            if (selectedTsv == null)
            {
                return null;
            }

            string path = AssetDatabase.GetAssetPath(selectedTsv);
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".tsv", System.StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return path;
        }

        private void OpenTsvWithDefaultApp(string assetPath)
        {
            try
            {
                string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
                if (string.IsNullOrEmpty(projectRoot))
                {
                    Debug.LogError("Failed to resolve project root path.");
                    return;
                }

                string fullPath = Path.GetFullPath(Path.Combine(projectRoot, assetPath));
                if (!File.Exists(fullPath))
                {
                    Debug.LogWarning($"TSV file not found: {fullPath}");
                    return;
                }

                var startInfo = new System.Diagnostics.ProcessStartInfo(fullPath)
                {
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(startInfo);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to open TSV file: {ex.Message}");
            }
        }

        private async void Generate()
        {
            _res = Mathf.Max(1, _target.textResolution);
            UnpackPrefabIfNeeded();
            ClearChildren();
            CreateScrollView();
            QRCodeCache.ResetStats();
            try
            {
                await CreateContent();
            }
            finally
            {
                QRCodeCache.LogStatsSummary();
            }
        }

        private void UnpackPrefabIfNeeded()
        {
            var status = PrefabUtility.GetPrefabInstanceStatus(_target.gameObject);
            if (status == PrefabInstanceStatus.Connected)
            {
                PrefabUtility.UnpackPrefabInstance(_target.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }
        }

        private void ClearChildren()
        {
            while (_target.transform.childCount > 0)
            {
                DestroyImmediate(_target.transform.GetChild(0).gameObject);
            }
        }

        private GameObject _content;

        private void CreateScrollView()
        {
            float scale = 0.001f / _res;
            float scrollbarWidth = 16 * _res;
            float scrollbarPadding = 4 * _res;

            RemoveDirectCanvasComponents(_target.gameObject);

            var canvasRoot = new GameObject("CanvasRoot");
            canvasRoot.transform.SetParent(_target.transform, false);

            var canvasRootRt = canvasRoot.AddComponent<RectTransform>();
            canvasRootRt.anchorMin = Vector2.zero;
            canvasRootRt.anchorMax = Vector2.one;
            canvasRootRt.offsetMin = Vector2.zero;
            canvasRootRt.offsetMax = Vector2.zero;
            canvasRootRt.sizeDelta = new Vector2(_target.width * _res, _target.height * _res);
            canvasRootRt.localScale = Vector3.one * scale;

            var canvas = canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            if (canvasRoot.GetComponent<GraphicRaycaster>() == null)
            {
                canvasRoot.AddComponent<GraphicRaycaster>();
            }

            EnsureVrcUiShape(canvasRoot);
            int defaultLayer = LayerMask.NameToLayer("Default");
            SetLayerRecursively(canvasRoot, defaultLayer);

            // ScrollView
            var scrollView = new GameObject("ScrollView");
            scrollView.transform.SetParent(canvasRoot.transform, false);

            var scrollRect = scrollView.AddComponent<ScrollRect>();
            var scrollRt = scrollView.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = Vector2.zero;
            scrollRt.offsetMax = Vector2.zero;
            scrollRt.localScale = Vector3.one;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 0f;

            var scrollImage = scrollView.AddComponent<Image>();
            scrollImage.color = new Color(0, 0, 0, 0.5f);

            // Viewport
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollView.transform, false);

            var viewportRt = viewport.AddComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = new Vector2(-(scrollbarWidth + scrollbarPadding), 0);
            viewportRt.localScale = Vector3.one;

            var mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            viewport.AddComponent<Image>();

            // Content
            _content = new GameObject("Content");
            _content.transform.SetParent(viewport.transform, false);

            var contentRt = _content.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;
            contentRt.localScale = Vector3.one;

            var layout = _content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 5 * _res;
            layout.padding = new RectOffset(10 * _res, 10 * _res, 10 * _res, 10 * _res);
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = _content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Vertical scrollbar
            var scrollbarObj = new GameObject("Scrollbar Vertical");
            scrollbarObj.transform.SetParent(scrollView.transform, false);

            var scrollbarRt = scrollbarObj.AddComponent<RectTransform>();
            scrollbarRt.anchorMin = new Vector2(1, 0);
            scrollbarRt.anchorMax = new Vector2(1, 1);
            scrollbarRt.pivot = new Vector2(1, 1);
            scrollbarRt.offsetMin = new Vector2(-scrollbarWidth, 0);
            scrollbarRt.offsetMax = Vector2.zero;
            scrollbarRt.localScale = Vector3.one;

            var scrollbarBg = scrollbarObj.AddComponent<Image>();
            scrollbarBg.color = new Color(1f, 1f, 1f, 0.15f);

            var scrollbar = scrollbarObj.AddComponent<Scrollbar>();
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.value = 1f;
            var navigation = scrollbar.navigation;
            navigation.mode = Navigation.Mode.None;
            scrollbar.navigation = navigation;

            var slidingArea = new GameObject("Sliding Area");
            slidingArea.transform.SetParent(scrollbarObj.transform, false);

            var slidingRt = slidingArea.AddComponent<RectTransform>();
            slidingRt.anchorMin = Vector2.zero;
            slidingRt.anchorMax = Vector2.one;
            slidingRt.offsetMin = new Vector2(2 * _res, 2 * _res);
            slidingRt.offsetMax = new Vector2(-2 * _res, -2 * _res);
            slidingRt.localScale = Vector3.one;

            var handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(slidingArea.transform, false);

            var handleRt = handleObj.AddComponent<RectTransform>();
            handleRt.anchorMin = Vector2.zero;
            handleRt.anchorMax = Vector2.one;
            handleRt.offsetMin = Vector2.zero;
            handleRt.offsetMax = Vector2.zero;
            handleRt.localScale = Vector3.one;

            var handleImage = handleObj.AddComponent<Image>();
            handleImage.color = new Color(1f, 1f, 1f, 0.7f);

            scrollbar.targetGraphic = handleImage;
            scrollbar.handleRect = handleRt;

            // Setup ScrollRect
            scrollRect.viewport = viewportRt;
            scrollRect.content = contentRt;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scrollRect.verticalScrollbarSpacing = scrollbarPadding;

            // Ensure all generated UI objects under CanvasRoot stay on the Default layer.
            SetLayerRecursively(canvasRoot, defaultLayer);
        }

        private static void RemoveDirectCanvasComponents(GameObject targetObject)
        {
            if (targetObject == null) return;

            var directCanvas = targetObject.GetComponent<Canvas>();
            if (directCanvas != null)
            {
                DestroyImmediate(directCanvas);
            }

            var directRaycaster = targetObject.GetComponent<GraphicRaycaster>();
            if (directRaycaster != null)
            {
                DestroyImmediate(directRaycaster);
            }

            var uiShapeType = FindType("VRC.SDK3.Components.VRCUiShape");
            if (uiShapeType == null) return;

            var directUiShape = targetObject.GetComponent(uiShapeType);
            if (directUiShape != null)
            {
                DestroyImmediate(directUiShape);
            }
        }

        private static void EnsureVrcUiShape(GameObject canvasObject)
        {
            if (canvasObject == null) return;

            var uiShapeType = FindType("VRC.SDK3.Components.VRCUiShape");
            if (uiShapeType == null) return;
            if (canvasObject.GetComponent(uiShapeType) != null) return;

            canvasObject.AddComponent(uiShapeType);
        }

        private static Type FindType(string fullTypeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullTypeName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            if (root == null || layer < 0) return;

            root.layer = layer;
            var transform = root.transform;
            for (int i = 0; i < transform.childCount; i++)
            {
                SetLayerRecursively(transform.GetChild(i).gameObject, layer);
            }
        }

        private async Task CreateContent()
        {
            if (_target.groups == null) return;

            foreach (var group in _target.groups)
            {
                // スペーサー
                if (group.groupName == TSVHelper.SPACER_MARKER)
                {
                    CreateSpacer();
                    continue;
                }

                CreateGroupHeader(group.groupName);

                if (group.entries != null)
                {
                    foreach (var entry in group.entries)
                    {
                        await CreateEntry(entry);
                    }
                }
            }
        }

        private void CreateSpacer()
        {
            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(_content.transform, false);

            var rt = spacer.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 20 * _res);
            rt.localScale = Vector3.one;
        }

        private void CreateGroupHeader(string groupName)
        {
            var header = new GameObject($"Group_{groupName}");
            header.transform.SetParent(_content.transform, false);

            var rt = header.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 30 * _res);
            rt.localScale = Vector3.one;

            var text = header.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18 * _res;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleLeft;
            text.text = $"■ {groupName}";
            text.color = _target.textColor;
        }

        private async Task CreateEntry(CatalogEntry entry)
        {
            bool hasLink = !string.IsNullOrEmpty(entry.entryLink);
            bool showQR = hasLink && _target.showQRCode;
            float textHeight = (22 + (string.IsNullOrEmpty(entry.entryNote) ? 0 : 20) + (hasLink ? 18 : 0)) * _res;
            float itemHeight = showQR ? Mathf.Max(textHeight, _target.qrCodeSize * _res) : textHeight;

            var item = new GameObject(entry.entryName);
            item.transform.SetParent(_content.transform, false);

            var rt = item.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, itemHeight);
            rt.localScale = Vector3.one;

            var itemLayout = item.AddComponent<HorizontalLayoutGroup>();
            itemLayout.spacing = 10 * _res;
            itemLayout.childControlWidth = true;
            itemLayout.childControlHeight = true;
            itemLayout.childForceExpandWidth = false;
            itemLayout.childForceExpandHeight = false;

            // Text area
            var textArea = new GameObject("TextArea");
            textArea.transform.SetParent(item.transform, false);

            var textAreaLayout = textArea.AddComponent<VerticalLayoutGroup>();
            textAreaLayout.childControlWidth = true;
            textAreaLayout.childControlHeight = false;
            textAreaLayout.childForceExpandWidth = true;
            textAreaLayout.spacing = 2 * _res;

            var textAreaFitter = textArea.AddComponent<LayoutElement>();
            textAreaFitter.flexibleWidth = 1;

            // Title
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(textArea.transform, false);

            var titleRt = titleObj.AddComponent<RectTransform>();
            titleRt.sizeDelta = new Vector2(0, 22 * _res);

            var titleText = titleObj.AddComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 14 * _res;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleLeft;
            titleText.color = _target.textColor;
            titleText.text = entry.entryName;

            // Comment
            if (!string.IsNullOrEmpty(entry.entryNote))
            {
                var commentObj = new GameObject("Comment");
                commentObj.transform.SetParent(textArea.transform, false);

                var commentRt = commentObj.AddComponent<RectTransform>();
                commentRt.sizeDelta = new Vector2(0, 18 * _res);

                var commentText = commentObj.AddComponent<Text>();
                commentText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                commentText.fontSize = 11 * _res;
                commentText.alignment = TextAnchor.MiddleLeft;
                commentText.color = new Color(_target.textColor.r, _target.textColor.g, _target.textColor.b, 0.8f);
                commentText.text = entry.entryNote;
            }

            // URL (InputField for easy copy)
            if (hasLink)
            {
                var urlObj = new GameObject("URL");
                urlObj.transform.SetParent(textArea.transform, false);

                var urlRt = urlObj.AddComponent<RectTransform>();
                urlRt.sizeDelta = new Vector2(0, 16 * _res);

                var urlInputField = urlObj.AddComponent<InputField>();
                urlInputField.readOnly = true;

                // InputField Text
                var urlTextObj = new GameObject("Text");
                urlTextObj.transform.SetParent(urlObj.transform, false);

                var urlTextRt = urlTextObj.AddComponent<RectTransform>();
                urlTextRt.anchorMin = Vector2.zero;
                urlTextRt.anchorMax = Vector2.one;
                urlTextRt.offsetMin = Vector2.zero;
                urlTextRt.offsetMax = Vector2.zero;

                var urlText = urlTextObj.AddComponent<Text>();
                urlText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                urlText.fontSize = 9 * _res;
                urlText.alignment = TextAnchor.MiddleLeft;
                urlText.color = new Color(_target.textColor.r, _target.textColor.g, _target.textColor.b, 0.6f);
                urlText.supportRichText = false;

                urlInputField.textComponent = urlText;
                urlInputField.text = entry.entryLink;
            }

            // QR Code
            if (hasLink && _target.showQRCode)
            {
                var qrObj = new GameObject("QRCode");
                qrObj.transform.SetParent(item.transform, false);

                var qrLayout = qrObj.AddComponent<LayoutElement>();
                qrLayout.minWidth = _target.qrCodeSize * _res;
                qrLayout.minHeight = _target.qrCodeSize * _res;
                qrLayout.preferredWidth = _target.qrCodeSize * _res;
                qrLayout.preferredHeight = _target.qrCodeSize * _res;
                qrLayout.flexibleWidth = 0;
                qrLayout.flexibleHeight = 0;

                var qrImage = qrObj.AddComponent<Image>();
                qrImage.color = Color.white;
                qrImage.preserveAspect = true;

                var qrBytes = await FetchQRCode(entry.entryLink);
                if (qrBytes != null && qrBytes.Length > 0)
                {
                    var texture = new Texture2D(1, 1);
                    texture.LoadImage(qrBytes);
                    qrImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
                }
            }
        }

        private async Task<byte[]> FetchQRCode(string url)
        {
            try
            {
                return await QRCodeCache.GetOrFetchPngAsync(
                    url,
                    150,
                    Mathf.Max(1, _target.qrApiTimeoutSeconds),
                    Mathf.Max(0, _target.qrApiRetryCount),
                    Mathf.Max(0f, _target.qrApiRetryDelaySeconds));
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"QR Code fetch failed: {ex.Message}");
                return null;
            }
        }
    }
}
