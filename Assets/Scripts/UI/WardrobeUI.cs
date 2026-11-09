using UnityEngine;
using UnityEngine.UI;
using PBV.Systems;

namespace PBV.UI
{
    /// <summary>衣柜换装 UI：WardrobeStation 打开，点击切换服装颜色。</summary>
    public class WardrobeUI : MonoBehaviour
    {
        public static WardrobeUI Instance { get; private set; }

        private GameObject _canvasGo;
        private Transform _panel;
        private bool _isOpen;
        private static Font _font;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
            BuildUI();
            _canvasGo.SetActive(false);
        }

        private void Update()
        {
            if (_isOpen && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E)))
                Toggle();
        }

        public void Toggle()
        {
            _isOpen = !_isOpen;
            _canvasGo.SetActive(_isOpen);
            if (_isOpen) UIEventSystemBootstrap.RegisterTopPanel(Toggle);
            else UIEventSystemBootstrap.ClearTopPanel();
            if (_isOpen) Refresh();
        }

        private void Refresh()
        {
            // 重建服装按钮（标注当前穿着）
            if (_panel == null) return;
            var list = _panel.Find("List");
            if (list == null) return;
            for (int i = list.childCount - 1; i >= 0; i--) Destroy(list.GetChild(i).gameObject);

            int cur = WardrobeSystem.Instance != null ? WardrobeSystem.Instance.OutfitIndex : 0;
            for (int i = 0; i < WardrobeSystem.Outfits.Length; i++)
            {
                var (name, color) = WardrobeSystem.Outfits[i];
                bool active = i == cur;
                CreateOutfitButton(list, name, color, i, active);
            }
        }

        private void CreateOutfitButton(Transform parent, string name, Color color, int index, bool active)
        {
            var btnGo = new GameObject($"Outfit_{index}");
            btnGo.transform.SetParent(parent, false);
            var rt = btnGo.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 48);

            var img = btnGo.AddComponent<Image>();
            img.color = active ? new Color(0.35f, 0.30f, 0.15f, 1f) : new Color(0.16f, 0.14f, 0.10f, 0.95f);

            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = img;

            // 颜色色块
            var swatchGo = new GameObject("Swatch");
            swatchGo.transform.SetParent(btnGo.transform, false);
            var swatchRt = swatchGo.AddComponent<RectTransform>();
            swatchRt.anchorMin = new Vector2(0, 0.2f); swatchRt.anchorMax = new Vector2(0, 0.8f);
            swatchRt.offsetMin = new Vector2(10, 0); swatchRt.offsetMax = new Vector2(44, 0);
            swatchGo.AddComponent<Image>().color = color;

            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(btnGo.transform, false);
            var txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(54, 0); txtRt.offsetMax = new Vector2(-10, 0);
            var txt = txtGo.AddComponent<Text>();
            txt.font = _font; txt.fontSize = 17; txt.alignment = TextAnchor.MiddleLeft;
            txt.color = active ? new Color(1f, 0.9f, 0.6f) : new Color(0.9f, 0.9f, 0.85f);
            txt.text = active ? $"<b>{name}</b>（当前）" : name;
            txt.supportRichText = true; txt.raycastTarget = false;

            int captured = index;
            btn.onClick.AddListener(() => { WardrobeSystem.Instance?.SetOutfit(captured); Refresh(); });
        }

        private void BuildUI()
        {
            _canvasGo = new GameObject("WardrobeCanvas");
            _canvasGo.transform.SetParent(transform, false);
            var canvas = _canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 29;
            var scaler = _canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            _canvasGo.AddComponent<GraphicRaycaster>();

            var bg = new GameObject("BG");
            bg.transform.SetParent(_canvasGo.transform, false);
            var bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.sizeDelta = Vector2.zero;
            bg.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.4f);
            bg.AddComponent<Button>().onClick.AddListener(Toggle);

            var panel = new GameObject("Panel");
            panel.transform.SetParent(_canvasGo.transform, false);
            _panel = panel.transform;
            var prt = panel.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 0.5f); prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(420, 440); prt.anchoredPosition = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0.12f, 0.10f, 0.08f, 0.97f);

            var vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 16, 16);
            vlg.spacing = 8; vlg.childControlWidth = true; vlg.childForceExpandWidth = true;

            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var title = MakeText("衣 柜 · 换装 (E/Esc 关闭)", 22, FontStyle.Bold, new Color(0.85f, 0.75f, 0.9f));
            title.alignment = TextAnchor.MiddleCenter;

            var hint = MakeText("换装改变角色外观（灰盒阶段以色块区分）", 13, FontStyle.Normal, new Color(0.7f, 0.65f, 0.6f));
            hint.alignment = TextAnchor.MiddleCenter;

            var list = new GameObject("List");
            list.transform.SetParent(panel.transform, false);
            var listRt = list.AddComponent<RectTransform>();
            listRt.sizeDelta = new Vector2(0, 320);
            var lvlg = list.AddComponent<VerticalLayoutGroup>();
            lvlg.spacing = 6; lvlg.childControlWidth = true; lvlg.childForceExpandWidth = true;
        }

        private Text MakeText(string content, int size, FontStyle style, Color color)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(_panel, false);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 24);
            var t = go.AddComponent<Text>();
            t.font = _font; t.fontSize = size; t.fontStyle = style;
            t.alignment = TextAnchor.UpperLeft; t.color = color; t.text = content;
            t.raycastTarget = false;
            return t;
        }
    }
}
