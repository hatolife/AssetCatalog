// SPDX-License-Identifier: CC0-1.0

using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using System.Threading.Tasks;
using System.Net.Http;

namespace AssetCatalog.Editor
{
    [CustomEditor(typeof(AssetCatalogGenerator))]
    public class AssetCatalogGeneratorEditor : UnityEditor.Editor
    {
        private AssetCatalogGenerator _target;
        private int _res;
        private Object _tsvFile;

        private void OnEnable()
        {
            _target = target as AssetCatalogGenerator;
            if (_tsvFile == null)
            {
                _tsvFile = AssetDatabase.LoadAssetAtPath<Object>("Assets/PoppoWorks/AssetCatalog/assets_list.tsv");
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Size
            EditorGUILayout.PropertyField(serializedObject.FindProperty("width"), new GUIContent("Width"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("height"), new GUIContent("Height"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("qrCodeSize"), new GUIContent("QR Code Size"));

            EditorGUILayout.Space();

            // Settings
            EditorGUILayout.PropertyField(serializedObject.FindProperty("showQRCode"), new GUIContent("Show QR Code"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("textColor"), new GUIContent("Text Color"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("textResolution"), new GUIContent("Text Resolution"));

            EditorGUILayout.Space();

            // TSV File Drop Area
            _tsvFile = EditorGUILayout.ObjectField("TSV File", _tsvFile, typeof(Object), false);

            // Load & Generate Buttons (side by side)
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load", GUILayout.Height(30)))
            {
                if (_tsvFile != null)
                {
                    string path = AssetDatabase.GetAssetPath(_tsvFile);
                    if (!string.IsNullOrEmpty(path) && path.EndsWith(".tsv"))
                    {
                        Undo.RecordObject(_target, "Load TSV");
                        _target.groups = TSVHelper.LoadCatalog(path);
                        EditorUtility.SetDirty(_target);
                        Debug.Log($"Loaded: {path} ({_target.groups?.Length ?? 0} groups)");
                    }
                    else
                    {
                        Debug.LogWarning("Please select a .tsv file");
                    }
                }
                else
                {
                    Debug.LogWarning("Please drag & drop a TSV file first");
                }
            }
            GUI.enabled = _target.groups != null && _target.groups.Length > 0;
            if (GUILayout.Button("Generate", GUILayout.Height(30)))
            {
                Generate();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

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
                EditorGUILayout.HelpBox("No data loaded. Drag & drop a TSV file and click Load.", MessageType.Info);
            }

            serializedObject.ApplyModifiedProperties();
        }

        private async void Generate()
        {
            _res = _target.textResolution;
            UnpackPrefabIfNeeded();
            ClearChildren();
            CreateScrollView();
            await CreateContent();
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

            // Canvas (if not exist on parent)
            var canvas = _target.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = _target.gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                var rt = _target.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(_target.width * _res, _target.height * _res);
                rt.localScale = Vector3.one * scale;
            }
            else
            {
                var rt = _target.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.sizeDelta = new Vector2(_target.width * _res, _target.height * _res);
                    rt.localScale = Vector3.one * scale;
                }
            }

            // ScrollView
            var scrollView = new GameObject("ScrollView");
            scrollView.transform.SetParent(_target.transform, false);

            var scrollRect = scrollView.AddComponent<ScrollRect>();
            var scrollRt = scrollView.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = Vector2.zero;
            scrollRt.offsetMax = Vector2.zero;
            scrollRt.localScale = Vector3.one;

            var scrollImage = scrollView.AddComponent<Image>();
            scrollImage.color = new Color(0, 0, 0, 0.5f);

            // Viewport
            var viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollView.transform, false);

            var viewportRt = viewport.AddComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = Vector2.zero;
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

            // Setup ScrollRect
            scrollRect.viewport = viewportRt;
            scrollRect.content = contentRt;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
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

                var qrData = await FetchQRCode(entry.entryLink);
                await Task.Delay(500); // API負荷軽減のため500ms待機
                if (!string.IsNullOrEmpty(qrData))
                {
                    var texture = new Texture2D(1, 1);
                    texture.LoadImage(System.Convert.FromBase64String(qrData));
                    qrImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
                }
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
                Debug.LogError($"QR Code fetch failed: {ex.Message}");
                return null;
            }
        }
    }
}
