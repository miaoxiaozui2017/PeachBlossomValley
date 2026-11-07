using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using PBV.Core;

namespace PBV.Dialogue
{
    /// <summary>
    /// 简易对话系统（v0.1.0 MVP）：
    /// - 文本框显示 + 逐字打字机效果 + 按键翻页（E）
    /// - UI 在 Awake 自动构建灰盒版，后续可替换为场景内正式 UI
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        public bool IsDialogueActive { get; private set; }

        /// <summary>上次对话结束的时间（NPC 据此判断冷却，避免同帧 E 键结束又重新触发）</summary>
        public float LastEndTime { get; private set; } = -10f;

        [Header("Typewriter Settings")]
        [SerializeField] private float _charDelay = 0.04f;

        [Header("Scene UI (可选，留空则自动构建灰盒UI)")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _contentText;

        private string[] _currentLines;
        private int _lineIndex;
        private string _speakerName;
        private bool _isTyping;
        private Coroutine _typewriterCoroutine;
        private int _startFrame;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            if (_panel == null) BuildFallbackUI();
            Hide();
        }

        private void Start()
        {
            if (InputHandler.Instance != null)
            {
                InputHandler.Instance.InteractEvent += OnAdvanceInput;
            }
        }

        private void OnDestroy()
        {
            if (InputHandler.Instance != null)
            {
                InputHandler.Instance.InteractEvent -= OnAdvanceInput;
            }
        }

        /// <summary>
        /// 开始一段对话
        /// </summary>
        public void StartDialogue(string speakerName, string[] lines)
        {
            if (lines == null || lines.Length == 0) return;

            _speakerName = speakerName;
            _currentLines = lines;
            _lineIndex = 0;
            _startFrame = Time.frameCount; // 防止同帧 E 键既触发对话又翻页
            IsDialogueActive = true;

            _panel.SetActive(true);
            ShowLine(_lineIndex);
        }

        /// <summary>
        /// 翻页输入（E / Interact）：打字中->立即显示全文；已完整->下一句或结束
        /// </summary>
        private void OnAdvanceInput()
        {
            if (!IsDialogueActive) return;
            if (Time.frameCount == _startFrame) return; // 触发对话的同一帧忽略翻页

            if (_isTyping)
            {
                FinishTypingImmediately();
                return;
            }

            _lineIndex++;
            if (_lineIndex >= _currentLines.Length)
            {
                EndDialogue();
            }
            else
            {
                ShowLine(_lineIndex);
            }
        }

        private void ShowLine(int index)
        {
            if (_nameText != null) _nameText.text = _speakerName;
            if (_contentText != null) _contentText.text = string.Empty;

            if (_typewriterCoroutine != null) StopCoroutine(_typewriterCoroutine);
            _typewriterCoroutine = StartCoroutine(TypewriterRoutine(_currentLines[index]));
        }

        private IEnumerator TypewriterRoutine(string line)
        {
            _isTyping = true;
            if (_contentText != null) _contentText.text = string.Empty;

            foreach (char c in line)
            {
                if (_contentText != null) _contentText.text += c;
                yield return new WaitForSeconds(_charDelay);
            }

            _isTyping = false;
            _typewriterCoroutine = null;
        }

        private void FinishTypingImmediately()
        {
            if (_typewriterCoroutine != null)
            {
                StopCoroutine(_typewriterCoroutine);
                _typewriterCoroutine = null;
            }
            _isTyping = false;
            if (_contentText != null) _contentText.text = _currentLines[_lineIndex];
        }

        private void EndDialogue()
        {
            Hide();
            _currentLines = null;
            IsDialogueActive = false;
            LastEndTime = Time.time; // 结束冷却起点
        }

        private void Hide()
        {
            IsDialogueActive = false;
            if (_panel != null) _panel.SetActive(false);
        }

        /// <summary>
        /// 构建灰盒对话 UI（屏幕下方文本框）
        /// </summary>
        private void BuildFallbackUI()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvasGo = new GameObject("DialogueUI_Canvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            _panel = new GameObject("DialoguePanel");
            _panel.transform.SetParent(canvasGo.transform, false);
            var panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.1f, 0.03f);
            panelRect.anchorMax = new Vector2(0.9f, 0.22f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            var panelImage = _panel.AddComponent<Image>();
            panelImage.color = new Color(0.08f, 0.06f, 0.04f, 0.92f);

            _nameText = CreateText("NameText", _panel.transform, new Vector2(0.03f, 0.75f), new Vector2(0.97f, 1f), 26, FontStyle.Bold, new Color(1f, 0.93f, 0.75f), font);
            _contentText = CreateText("ContentText", _panel.transform, new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.72f), 24, FontStyle.Normal, Color.white, font);
        }

        private static Text CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, int size, FontStyle style, Color color, Font font)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var text = go.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }
    }
}
