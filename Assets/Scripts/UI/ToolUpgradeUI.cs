using UnityEngine;
using UnityEngine.UI;
using PBV.Player;

namespace PBV.UI
{
    /// <summary>工具升级 UI：U 键开关，显示4种工具等级+升级消耗+按钮。</summary>
    public class ToolUpgradeUI : MonoBehaviour
    {
        public static ToolUpgradeUI Instance { get; private set; }

        private GameObject _root;
        private GameObject _canvasGo;   // 整块 Canvas（含全屏 BG），关闭时一起隐藏，避免 BG 常驻吃点击
        private Transform _listContent;
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
            if (Input.GetKeyDown(KeyCode.U) || (_isOpen && Input.GetKeyDown(KeyCode.Escape))) Toggle();
        }

        public void Toggle()
        {
            _isOpen = !_isOpen;
            _canvasGo.SetActive(_isOpen);
            if (_isOpen) UIEventSystemBootstrap.RegisterTopPanel(Toggle); else UIEventSystemBootstrap.ClearTopPanel();
            if (_isOpen) Refresh();
        }

        private void Refresh()
        {
            if (ToolManager.Instance == null || _listContent == null) return;
            for (int i = _listContent.childCount - 1; i >= 0; i--) Destroy(_listContent.GetChild(i).gameObject);
            for (int i = 0; i < ToolManager.Instance.ToolCount; i++) CreateRow(i);
        }

        private void CreateRow(int toolIndex)
        {
            var tm = ToolManager.Instance;
            int level = tm.GetToolLevel(toolIndex);
            var cost = tm.GetUpgradeCost(toolIndex);
            bool canUpgrade = cost != null;

            var row = new GameObject($"Tool_{toolIndex}");
            row.transform.SetParent(_listContent, false);
            var rt = row.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 80);
            var img = row.AddComponent<Image>();
            img.color = canUpgrade ? new Color(0.15f, 0.18f, 0.12f, 0.95f) : new Color(0.1f, 0.1f, 0.1f, 0.8f);

            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(10, 10, 4, 4);
            hlg.spacing = 8; hlg.childControlWidth = false; hlg.childForceExpandWidth = false;

            // 信息
            var infoGo = new GameObject("Info");
            infoGo.transform.SetParent(row.transform, false);
            var infoRt = infoGo.AddComponent<RectTransform>();
            infoRt.sizeDelta = new Vector2(340, 72);
            var infoText = infoGo.AddComponent<Text>();
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            infoText.font = _font;
            infoText.fontSize = 15;
            infoText.alignment = TextAnchor.MiddleLeft;
            infoText.color = new Color(1f, 0.92f, 0.75f);
            infoText.supportRichText = true;

            string costText = level >= 3 ? "<color=#888>已满级</color>" : $"升级: {cost.Value.oreCount}个{OreName(cost.Value.oreId)} + {cost.Value.money} 文";
            string effect = $"现在: {tm.GetToolEffect(toolIndex)}";
            if (level < 3) effect += $"\n<color=#8f8>下级: {ToolManager.DescribeEffect(toolIndex, level + 1)}</color>";
            infoText.text = $"<b>{tm.GetToolName(toolIndex)}</b>  Lv.{level}\n{costText}\n{effect}";
            infoText.raycastTarget = false;

            // 按钮
            var btnGo = new GameObject("UpgradeBtn");
            btnGo.transform.SetParent(row.transform, false);
            var btnRt = btnGo.AddComponent<RectTransform>();
            btnRt.sizeDelta = new Vector2(80, 48);
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = canUpgrade ? new Color(0.3f, 0.4f, 0.6f, 1f) : new Color(0.3f, 0.3f, 0.3f, 0.5f);
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.interactable = canUpgrade;
            var btnTextGo = new GameObject("Text");
            btnTextGo.transform.SetParent(btnGo.transform, false);
            var btnTextRt = btnTextGo.AddComponent<RectTransform>();
            btnTextRt.anchorMin = Vector2.zero; btnTextRt.anchorMax = Vector2.one; btnTextRt.sizeDelta = Vector2.zero;
            var btnText = btnTextGo.AddComponent<Text>();
            btnText.font = _font; btnText.fontSize = 16; btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = Color.white; btnText.text = level >= 3 ? "满级" : "升级"; btnText.raycastTarget = false;
            if (canUpgrade) { int idx = toolIndex; btn.onClick.AddListener(() => { tm.TryUpgrade(idx); Refresh(); }); }
        }

        private static string OreName(string oreId)
        {
            if (oreId == "item_ore_copper") return "铜矿";
            if (oreId == "item_ore_iron") return "铁矿";
            if (oreId == "item_ore_gold") return "金矿";
            return oreId;
        }

        private void BuildUI()
        {
            _canvasGo = new GameObject("UpgradeCanvas");
            var canvasGo = _canvasGo;
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 29;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            var bg = new GameObject("BG");
            bg.transform.SetParent(canvasGo.transform, false);
            var bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.sizeDelta = Vector2.zero;
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0f, 0f, 0f, 0.4f);
            bg.AddComponent<Button>().onClick.AddListener(Toggle);

            _root = new GameObject("Panel");
            _root.transform.SetParent(canvasGo.transform, false);
            var panelRt = _root.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0.5f); panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(480, 440); panelRt.anchoredPosition = Vector2.zero;
            _root.AddComponent<Image>().color = new Color(0.1f, 0.09f, 0.07f, 0.96f);

            var vlg = _root.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(12, 12, 12, 12);
            vlg.spacing = 6; vlg.childControlWidth = true; vlg.childForceExpandWidth = true;

            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(_root.transform, false);
            var titleRt = titleGo.AddComponent<RectTransform>();
            titleRt.sizeDelta = new Vector2(0, 28);
            var title = titleGo.AddComponent<Text>();
            title.font = _font; title.fontSize = 22; title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.UpperLeft; title.color = new Color(0.7f, 0.8f, 1f);
            title.text = "铁匠铺 (U/Esc 关闭)"; title.raycastTarget = false;

            _listContent = _root.transform;
        }
    }
}
