using UnityEngine;
using UnityEngine.UI;
using PBV.Systems;
using PBV.Player;

namespace PBV.UI
{
    /// <summary>
    /// 战斗面板（回合制简化）：显示敌我血量，按钮「攻击 / 用药 / 逃跑」。
    /// 遇怪时自动弹出，战斗结束自动关闭。
    /// </summary>
    public class CombatUI : MonoBehaviour
    {
        public static CombatUI Instance { get; private set; }

        private GameObject _canvasGo;
        private Text _enemyText;
        private Text _playerText;
        private Text _logText;
        private static Font _font;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
            BuildUI();
            _canvasGo.SetActive(false);
        }

        private void Start()
        {
            if (CombatSystem.Instance != null)
                CombatSystem.Instance.OnCombatChanged += RefreshOrClose;
        }

        private void OnDestroy()
        {
            if (CombatSystem.Instance != null)
                CombatSystem.Instance.OnCombatChanged -= RefreshOrClose;
        }

        private void RefreshOrClose()
        {
            var cs = CombatSystem.Instance;
            if (cs == null) return;
            if (cs.InCombat)
            {
                _canvasGo.SetActive(true);
                Refresh();
            }
            else
            {
                _canvasGo.SetActive(false);
            }
        }

        private void Refresh()
        {
            var cs = CombatSystem.Instance;
            var ps = PlayerStats.Instance;
            if (_enemyText != null && cs.CurrentMonster != null)
                _enemyText.text = $"{cs.CurrentMonster.name}　HP {Mathf.Max(0, cs.MonsterHp)} / {cs.CurrentMonster.hp}";
            if (_playerText != null && ps != null)
                _playerText.text = $"你　HP {ps.Hp} / {ps.MaxHp}";
        }

        private void DoAttack()
        {
            var cs = CombatSystem.Instance;
            if (cs == null) return;
            string log;
            cs.PlayerAttack(out log);
            if (_logText != null) _logText.text = log;
            Refresh();
            if (!cs.InCombat) RefreshOrClose();
        }

        private void DoUsePotion()
        {
            // 用药：吃「跌打膏/药酒」等回血（直接回血，简化）
            var ps = PlayerStats.Instance;
            if (ps != null)
            {
                // 有跌打膏则消耗并回血
                var inv = PBV.Inventory.InventorySystem.Instance;
                var plaster = Find("item_plaster");
                if (inv != null && plaster != null && inv.CountItem(plaster) > 0)
                {
                    inv.RemoveItem(plaster, 1);
                    ps.Heal(25);
                    if (_logText != null) _logText.text = "使用跌打膏，HP +25";
                }
                else
                {
                    if (_logText != null) _logText.text = "没有跌打膏（药庐可制）";
                }
            }
            Refresh();
        }

        private void DoFlee()
        {
            CombatSystem.Instance?.Flee();
            Refresh();
        }

        private static PBV.Inventory.ItemData Find(string id)
        {
            foreach (var item in Resources.LoadAll<PBV.Inventory.ItemData>("Data"))
                if (item != null && item.id == id) return item;
            return null;
        }

        private void BuildUI()
        {
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            _canvasGo = new GameObject("CombatCanvas");
            _canvasGo.transform.SetParent(transform, false);
            var canvas = _canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 45;
            var scaler = _canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            _canvasGo.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("Panel");
            panel.transform.SetParent(_canvasGo.transform, false);
            var prt = panel.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 0.5f); prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(460, 360); prt.anchoredPosition = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0.16f, 0.06f, 0.06f, 0.97f);

            var vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(20, 20, 20, 20);
            vlg.spacing = 12; vlg.childControlWidth = true; vlg.childForceExpandWidth = true;

            var title = MakeText(panel.transform, "战 斗", 24, FontStyle.Bold, new Color(1f, 0.6f, 0.5f), 30);
            _enemyText = MakeText(panel.transform, "怪物 HP", 20, FontStyle.Bold, new Color(1f, 0.7f, 0.6f), 30);
            _playerText = MakeText(panel.transform, "你 HP", 18, FontStyle.Normal, new Color(0.5f, 0.9f, 0.5f), 26);

            // 按钮行
            var row = new GameObject("Buttons");
            row.transform.SetParent(panel.transform, false);
            row.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 56);
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10; hlg.childControlWidth = true; hlg.childForceExpandWidth = true;
            hlg.childControlHeight = true; hlg.childForceExpandHeight = true;
            MakeButton(row.transform, "攻击", new Color(0.7f, 0.3f, 0.25f), DoAttack);
            MakeButton(row.transform, "用药", new Color(0.3f, 0.55f, 0.3f), DoUsePotion);
            MakeButton(row.transform, "逃跑", new Color(0.4f, 0.4f, 0.45f), DoFlee);

            _logText = MakeText(panel.transform, "遭遇了怪物！", 15, FontStyle.Normal, new Color(1f, 0.9f, 0.6f), 40);
            _logText.supportRichText = true;
        }

        private Text MakeText(Transform parent, string content, int size, FontStyle style, Color color, int height)
        {
            var go = new GameObject("Txt");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, height);
            var t = go.AddComponent<Text>();
            t.font = _font; t.fontSize = size; t.fontStyle = style;
            t.alignment = TextAnchor.MiddleCenter; t.color = color; t.text = content;
            t.raycastTarget = false;
            return t;
        }

        private void MakeButton(Transform parent, string label, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            var tgo = new GameObject("Text");
            tgo.transform.SetParent(go.transform, false);
            var trt = tgo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.sizeDelta = Vector2.zero;
            var t = tgo.AddComponent<Text>();
            t.font = _font; t.fontSize = 18; t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter; t.color = Color.white; t.text = label;
            t.raycastTarget = false;
        }
    }
}
