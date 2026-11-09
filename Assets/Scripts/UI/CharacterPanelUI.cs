using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using PBV.Inventory;
using PBV.NPC;
using PBV.Player;
using PBV.Systems;

namespace PBV.UI
{
    /// <summary>
    /// 人物属性面板（V 键开关，Esc 关闭）。
    /// 一屏交代「我是谁、我在哪天、我有多少钱、我练了什么、我把哪几门非遗做起来了、村里谁跟我亲」。
    /// 所有系统访问都做 null 保护，缺系统时只显示「未就绪」，不会报错。
    /// </summary>
    public class CharacterPanelUI : MonoBehaviour
    {
        public static CharacterPanelUI Instance { get; private set; }

        private GameObject _canvasGo;
        private bool _isOpen;
        private static Font _font;
        private Text _leftText;
        private Text _rightText;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
            BuildUI();
            _canvasGo.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.V))
            {
                if (_isOpen) Close(); else Open();
                return;
            }
            if (_isOpen && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E)))
                Close();
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
            _isOpen = false;
            _canvasGo.SetActive(false);
            UIEventSystemBootstrap.ClearTopPanel();
        }

        // ================= 内容刷新 =================

        private void Refresh()
        {
            if (_leftText != null) _leftText.text = BuildLeft();
            if (_rightText != null) _rightText.text = BuildRight();
        }

        private static string BuildLeft()
        {
            var sb = new StringBuilder();

            // 时日
            int day = TimeManager.Instance != null ? TimeManager.Instance.CurrentDay : 1;
            string season = SeasonSystem.Instance != null ? SeasonSystem.Instance.CurrentSeasonName : "春";
            int dayOfSeason = SeasonSystem.Instance != null ? SeasonSystem.Instance.DayOfSeason : 1;
            string weather = WeatherSystem.Instance != null ? WeatherSystem.Instance.WeatherName : "晴";
            string world = WorldState.Instance != null ? WorldState.Instance.WorldName : "桃花谷";
            sb.AppendLine("<b>【时 日】</b>");
            sb.AppendLine($"第 {day} 天 · {season} 第 {dayOfSeason} 日 · {weather} · {world}");
            string evt = RandomEventSystem.Instance != null ? RandomEventSystem.Instance.TodayEvent : "";
            if (!string.IsNullOrEmpty(evt)) sb.AppendLine($"今日节气：{evt}");
            sb.AppendLine();

            // 荷包
            var wallet = PlayerWallet.Instance;
            int song = wallet != null ? wallet.SongCoins : 0;
            int rmb = wallet != null ? wallet.RMB : 0;
            sb.AppendLine("<b>【荷 包】</b>");
            sb.AppendLine($"宋币　{song:N0} 文");
            sb.AppendLine($"人民币　¥{rmb:N0}");
            sb.AppendLine($"汇率　{ExchangeSystem.RateText()}（约合 ¥{ExchangeSystem.SongToRMB(song)}）");
            sb.AppendLine();

            // 状态
            var ps = PlayerStats.Instance;
            int st = ps != null ? ps.Stamina : 0;
            int stMax = ps != null ? ps.MaxStamina : 0;
            var rep = ReputationSystem.Instance;
            sb.AppendLine("<b>【状 态】</b>");
            sb.AppendLine($"体力　{st} / {stMax}");
            sb.AppendLine(rep != null
                ? $"声望　{rep.Reputation}（出售价 ×{rep.SellMultiplier:0.00}）"
                : "声望　未就绪");
            var col = CollectionSystem.Instance;
            sb.AppendLine(col != null
                ? $"见闻　{col.FoundCount} / {col.TotalCount}"
                : "见闻　未就绪");
            var ach = AchievementSystem.Instance;
            sb.AppendLine(ach != null ? $"成就　{ach.UnlockedCount} 个" : "成就　未就绪");

            var buff = BuffSystem.Instance;
            if (buff != null && buff.Active != BuffType.None)
                sb.AppendLine($"增益　{BuffSystem.BuffName(buff.Active)}（剩 {buff.DaysLeft} 天）");
            else
                sb.AppendLine("增益　无");

            return sb.ToString();
        }

        private static string BuildRight()
        {
            var sb = new StringBuilder();

            // 技艺
            sb.AppendLine("<b>【技 艺】</b>");
            var sk = SkillSystem.Instance;
            if (sk == null)
            {
                sb.AppendLine("未就绪");
            }
            else
            {
                AppendSkill(sb, sk, SkillType.Farming, BonusText("收获 +1", sk.FarmingYieldBonus));
                AppendSkill(sb, sk, SkillType.Mining, BonusText("挖矿 +1", sk.MiningYieldBonus));
                AppendSkill(sb, sk, SkillType.Fishing, PercentText("稀有率", sk.FishingRareBonus));
                AppendSkill(sb, sk, SkillType.Cooking, PercentText("菜品恢复", sk.CookingBonus));
            }
            sb.AppendLine();

            // 非遗产业链
            var inv = InventorySystem.Instance;
            sb.AppendLine("<b>【非遗产业】</b>");
            sb.AppendLine($"造纸　宣纸 ×{Count(inv, "item_xuanpaper")}");
            sb.AppendLine($"染织　成衣 ×{Count(inv, "item_garment")}");
            sb.AppendLine($"金银　珠宝 ×{Count(inv, "item_jewelry")}");
            sb.AppendLine($"陶瓷　瓷器件 ×{Count(inv, "item_pottery_bowl") + Count(inv, "item_pottery_vase")}");
            sb.AppendLine($"药墨　药墨 ×{Count(inv, "item_ink_medicine")}　药酒 ×{Count(inv, "item_medicine_wine")}");
            sb.AppendLine();

            // 好感
            sb.AppendLine("<b>【村 中 好 感】</b>");
            var rel = RelationshipSystem.Instance;
            if (rel == null)
            {
                sb.AppendLine("未就绪");
            }
            else
            {
                var list = new List<(string name, int aff, AffectionLevel lv)>();
                foreach (var npc in FindObjectsOfType<NPCController>())
                {
                    if (npc == null) continue;
                    string name = npc.NpcName;
                    if (string.IsNullOrEmpty(name)) continue;
                    list.Add((name, rel.GetAffection(name), rel.GetLevel(name)));
                }
                list.Sort((a, b) => b.aff.CompareTo(a.aff));
                if (list.Count == 0) sb.AppendLine("（还没遇到村里人）");
                foreach (var e in list)
                    sb.AppendLine($"{ShortName(e.name)}　{RelationshipSystem.LevelToName(e.lv)} {e.aff}");
            }

            return sb.ToString();
        }

        private static void AppendSkill(StringBuilder sb, SkillSystem sk, SkillType t, string bonus)
        {
            int lv = sk.GetLevel(t);
            int exp = sk.GetExp(t);
            int next = sk.GetExpToNext(t);
            sb.AppendLine($"{SkillSystem.SkillName(t)} Lv.{lv}　{exp}/{next}{(string.IsNullOrEmpty(bonus) ? "" : "　" + bonus)}");
        }

        private static string BonusText(string label, int v) => v > 0 ? label : "";
        private static string PercentText(string label, float v) => v > 0f ? $"{label} +{Mathf.RoundToInt(v * 100)}%" : "";

        /// <summary>NPC 名牌里常带括号身份，面板只显示姓名</summary>
        private static string ShortName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            int i = name.IndexOfAny(new[] { '（', '(' });
            return i > 0 ? name.Substring(0, i) : name;
        }

        private static int Count(InventorySystem inv, string id)
        {
            if (inv == null) return 0;
            var item = Find(id);
            return item != null ? inv.CountItem(item) : 0;
        }

        private static ItemData Find(string id)
        {
            foreach (var item in Resources.LoadAll<ItemData>("Data"))
                if (item != null && item.id == id) return item;
            return null;
        }

        // ================= 界面搭建 =================

        private void BuildUI()
        {
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            _canvasGo = new GameObject("CharacterCanvas");
            _canvasGo.transform.SetParent(transform, false);
            var canvas = _canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 34;
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
            var prt = panel.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 0.5f); prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(1060, 664); prt.anchoredPosition = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0.12f, 0.10f, 0.06f, 0.97f);

            var title = CreateText(panel.transform, "Title", 24, FontStyle.Bold, new Color(1f, 0.85f, 0.4f), "人 物 · 谷中仙");
            var trt = title.rectTransform;
            trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = new Vector2(24, -56); trt.offsetMax = new Vector2(-24, -16);
            title.alignment = TextAnchor.MiddleCenter;

            _leftText = CreateText(panel.transform, "Left", 15, FontStyle.Normal, new Color(0.92f, 0.90f, 0.82f), "");
            var lrt = _leftText.rectTransform;
            lrt.anchorMin = new Vector2(0f, 0f); lrt.anchorMax = new Vector2(0.5f, 1f);
            lrt.offsetMin = new Vector2(28, 46); lrt.offsetMax = new Vector2(-8, -62);
            _leftText.alignment = TextAnchor.UpperLeft;
            _leftText.lineSpacing = 1.25f;

            _rightText = CreateText(panel.transform, "Right", 15, FontStyle.Normal, new Color(0.92f, 0.90f, 0.82f), "");
            var rrt = _rightText.rectTransform;
            rrt.anchorMin = new Vector2(0.5f, 0f); rrt.anchorMax = new Vector2(1f, 1f);
            rrt.offsetMin = new Vector2(8, 46); rrt.offsetMax = new Vector2(-28, -62);
            _rightText.alignment = TextAnchor.UpperLeft;
            _rightText.lineSpacing = 1.25f;

            var hint = CreateText(panel.transform, "Hint", 12, FontStyle.Normal, new Color(0.7f, 0.65f, 0.6f), "V 开关 · E / Esc 关闭 · 技艺靠干活长，非遗靠亲手做出成品");
            var hrt = hint.rectTransform;
            hrt.anchorMin = new Vector2(0f, 0f); hrt.anchorMax = new Vector2(1f, 0f);
            hrt.offsetMin = new Vector2(28, 14); hrt.offsetMax = new Vector2(-28, 40);
            hint.alignment = TextAnchor.MiddleCenter;
        }

        private static Text CreateText(Transform parent, string name, int fontSize, FontStyle style, Color color, string content)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var t = go.AddComponent<Text>();
            t.font = _font; t.fontSize = fontSize; t.fontStyle = style;
            t.alignment = TextAnchor.UpperLeft; t.color = color; t.text = content;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            return t;
        }
    }
}
