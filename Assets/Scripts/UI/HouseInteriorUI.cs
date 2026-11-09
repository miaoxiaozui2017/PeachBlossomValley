using UnityEngine;
using UnityEngine.UI;
using PBV.Systems;

namespace PBV.UI
{
    /// <summary>
    /// 屋内面板：房屋入口进入后显示，只做「装修」展示区。
    /// 储物箱已独立（木工坊制作后手动放置），不再由房屋等级决定。
    /// 装修格子数随房屋等级增加：0 级 4 格、1 级 6 格、2 级 8 格。
    /// </summary>
    public class HouseInteriorUI : MonoBehaviour
    {
        public static HouseInteriorUI Instance { get; private set; }

        private GameObject _canvasGo;
        private bool _isOpen;
        private Text _titleText;
        private Text _descText;
        private Transform _decorGrid;
        private static Font _font;

        private const int MaxDecor = 8;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
            BuildUI();
            _canvasGo.SetActive(false);
        }

        private void Update()
        {
            if (_isOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
        }

        public void Open()
        {
            _isOpen = true;
            _canvasGo.SetActive(true);
            UIEventSystemBootstrap.RegisterTopPanel(Close);
            Refresh();
        }

        public void Close()
        {
            if (!_isOpen) return;
            _isOpen = false;
            _canvasGo.SetActive(false);
            UIEventSystemBootstrap.ClearTopPanel();
        }

        private void Refresh()
        {
            int houseLv = BuildSystem.Instance != null ? BuildSystem.Instance.HouseLevel : 0;
            int slots = 4 + houseLv * 2; // 0级4格、1级6格、2级8格
            if (_titleText != null)
                _titleText.text = $"房屋 · 屋内装修（房屋 {houseLv} 级）";
            if (_descText != null)
                _descText.text = $"可摆放装饰 {slots} 格 · 房屋扩建解锁更多格子\n（储物箱已独立：木工坊制作「储物箱」后放置，每个 36 格可改名改色）";

            for (int i = 0; i < MaxDecor; i++)
            {
                var slotGo = _decorGrid.Find($"Decor_{i}");
                if (slotGo == null) continue;
                bool active = i < slots;
                slotGo.gameObject.SetActive(active);
                if (active)
                {
                    var img = slotGo.GetComponent<Image>();
                    if (img != null) img.color = (i < houseLv) ? new Color(0.45f, 0.4f, 0.3f, 1f) : new Color(0.15f, 0.15f, 0.13f, 1f);
                }
            }
        }

        private void BuildUI()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            _canvasGo = new GameObject("HouseInteriorCanvas");
            _canvasGo.transform.SetParent(transform, false);
            var canvas = _canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 28;
            var scaler = _canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            _canvasGo.AddComponent<GraphicRaycaster>();

            var bg = new GameObject("BG");
            bg.transform.SetParent(_canvasGo.transform, false);
            var bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.sizeDelta = Vector2.zero;
            bg.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
            bg.AddComponent<Button>().onClick.AddListener(Close);

            var panel = new GameObject("Panel");
            panel.transform.SetParent(_canvasGo.transform, false);
            var rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(480, 360); rt.anchoredPosition = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0.12f, 0.10f, 0.08f, 0.97f);

            var vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 16, 16);
            vlg.spacing = 8; vlg.childControlWidth = true; vlg.childForceExpandWidth = true;

            _titleText = MakeText(panel.transform, "Title", "房屋 · 屋内装修（Esc 关闭）", 22, FontStyle.Bold, new Color(0.85f, 0.9f, 1f));
            _titleText.rectTransform.sizeDelta = new Vector2(0, 30);

            _descText = MakeText(panel.transform, "Desc", "", 15, FontStyle.Normal, new Color(0.75f, 0.75f, 0.7f));
            _descText.rectTransform.sizeDelta = new Vector2(0, 56);
            _descText.horizontalOverflow = HorizontalWrapMode.Wrap;

            // 装修格子区（4 列）
            var gridGo = new GameObject("DecorGrid");
            gridGo.transform.SetParent(panel.transform, false);
            var gridRect = gridGo.AddComponent<RectTransform>();
            gridRect.sizeDelta = new Vector2(0, 140);
            var grid = gridGo.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(70, 70);
            grid.spacing = new Vector2(6, 6);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;
            _decorGrid = gridGo.transform;

            for (int i = 0; i < MaxDecor; i++)
            {
                var slotGo = new GameObject($"Decor_{i}");
                slotGo.transform.SetParent(_decorGrid, false);
                slotGo.AddComponent<RectTransform>().sizeDelta = new Vector2(70, 70);
                var img = slotGo.AddComponent<Image>();
                img.color = new Color(0.15f, 0.15f, 0.13f, 1f);
                var t = MakeText(slotGo.transform, "Text", "空", 16, FontStyle.Normal, new Color(0.5f, 0.5f, 0.5f));
                var trt = t.GetComponent<RectTransform>();
                trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
                t.alignment = TextAnchor.MiddleCenter;
                slotGo.SetActive(false);
            }
        }

        private Text MakeText(Transform parent, string name, string content, int fontSize, FontStyle style, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 20);
            var t = go.AddComponent<Text>();
            t.font = _font; t.fontSize = fontSize; t.fontStyle = style;
            t.alignment = TextAnchor.MiddleLeft; t.color = color; t.text = content;
            t.raycastTarget = false;
            return t;
        }
    }
}
