using UnityEngine;
using UnityEngine.UI;
using PBV.Systems;

namespace PBV.UI
{
    /// <summary>
    /// 成就面板：L 键开关，列出全部成就与解锁状态。
    /// 由 UIEventSystemBootstrap.Ensure() 运行时补挂，无需重建场景。
    /// </summary>
    public class AchievementUI : MonoBehaviour
    {
        private GameObject _canvasGo;
        private Transform _listContent;
        private Text _countText;
        private bool _isOpen;
        private static Font _font;
        private System.Action _closeAction;

        private void Awake()
        {
            _closeAction = Close;
            BuildUI();
            _canvasGo.SetActive(false);
        }

        private void Start()
        {
            if (AchievementSystem.Instance != null)
                AchievementSystem.Instance.OnAchievementUnlocked += OnUnlocked;
        }

        private void OnDestroy()
        {
            if (AchievementSystem.Instance != null)
                AchievementSystem.Instance.OnAchievementUnlocked -= OnUnlocked;
            UIEventSystemBootstrap.UnregisterTopPanel(_closeAction);
        }

        private void OnUnlocked(string name)
        {
            HUD.Instance?.ShowNotice($"成就解锁：{name}");
            if (_isOpen) Refresh();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.L) || (_isOpen && Input.GetKeyDown(KeyCode.Escape))) Toggle();
        }

        public void Toggle()
        {
            _isOpen = !_isOpen;
            _canvasGo.SetActive(_isOpen);
            if (_isOpen)
            {
                Refresh();
                UIEventSystemBootstrap.RegisterTopPanel(_closeAction);
            }
            else
            {
                UIEventSystemBootstrap.UnregisterTopPanel(_closeAction);
            }
        }

        private void Close() { if (_isOpen) Toggle(); }

        private void Refresh()
        {
            var sys = AchievementSystem.Instance;
            if (sys == null || _listContent == null) return;
            for (int i = _listContent.childCount - 1; i >= 0; i--)
                Destroy(_listContent.GetChild(i).gameObject);

            if (_countText != null) _countText.text = $"已解锁 {sys.UnlockedCount} / {sys.Names.Count}";

            for (int i = 0; i < sys.Names.Count; i++)
            {
                bool got = sys.IsUnlocked(i);
                CreateRow(got ? "✓" : "○", sys.Names[i],
                    got ? new Color(1f, 0.9f, 0.55f) : new Color(0.55f, 0.55f, 0.55f));
            }
        }

        private void CreateRow(string mark, string name, Color color)
        {
            var go = new GameObject("AchievementRow");
            go.transform.SetParent(_listContent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 36);
            var t = go.AddComponent<Text>();
            t.font = _font;
            t.fontSize = 18;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleLeft;
            t.color = color;
            t.text = $"  {mark}  {name}";
            t.raycastTarget = false;
        }

        private void BuildUI()
        {
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            _canvasGo = new GameObject("AchievementCanvas");
            _canvasGo.transform.SetParent(transform, false);
            var canvas = _canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40;
            var scaler = _canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            _canvasGo.AddComponent<GraphicRaycaster>();

            var bg = new GameObject("BG");
            bg.transform.SetParent(_canvasGo.transform, false);
            var bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.sizeDelta = Vector2.zero;
            bg.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
            bg.AddComponent<Button>().onClick.AddListener(Close);

            var panel = new GameObject("Panel");
            panel.transform.SetParent(_canvasGo.transform, false);
            var panelRt = panel.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(460, 420);
            panel.AddComponent<Image>().color = new Color(0.1f, 0.08f, 0.06f, 0.97f);

            var vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(20, 20, 16, 16);
            vlg.spacing = 6;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var title = CreateText(panel.transform, "成就 (L/Esc 关闭)", 22, FontStyle.Bold, new Color(1f, 0.88f, 0.45f));
            title.rectTransform.sizeDelta = new Vector2(0, 32);
            _countText = CreateText(panel.transform, "已解锁 0 / 7", 16, FontStyle.Normal, new Color(0.8f, 0.85f, 0.7f));
            _countText.rectTransform.sizeDelta = new Vector2(0, 24);
            CreateText(panel.transform, "达成条件后自动解锁，进度会存档", 13, FontStyle.Italic, new Color(0.6f, 0.58f, 0.55f))
                .rectTransform.sizeDelta = new Vector2(0, 20);

            var listGo = new GameObject("List");
            listGo.transform.SetParent(panel.transform, false);
            var listRt = listGo.AddComponent<RectTransform>();
            listRt.sizeDelta = new Vector2(0, 280);
            var listVlg = listGo.AddComponent<VerticalLayoutGroup>();
            listVlg.spacing = 2;
            listVlg.childControlWidth = true;
            listVlg.childControlHeight = false;
            listVlg.childForceExpandWidth = true;
            listVlg.childForceExpandHeight = false;
            _listContent = listGo.transform;
        }

        private static Text CreateText(Transform parent, string content, int size, FontStyle style, Color color)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 24);
            var t = go.AddComponent<Text>();
            t.font = _font;
            t.fontSize = size;
            t.fontStyle = style;
            t.alignment = TextAnchor.MiddleLeft;
            t.color = color;
            t.text = content;
            t.raycastTarget = false;
            return t;
        }
    }
}
