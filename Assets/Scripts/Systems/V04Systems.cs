using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PBV.Inventory;
using PBV.UI;

namespace PBV.Systems
{
    // =====================================================================
    // v0.4.0 系统完善：宗族声望 / 菜品 Buff / 节气随机事件 /
    //                  餐厅订单 / 穿梭贸易「三出三进」 / 两个管理面板
    // 全部由 V04Bootstrap 运行时补挂，不要求重建场景。
    // =====================================================================

    /// <summary>菜品临时增益类型</summary>
    public enum BuffType { None, Fishing, Mining, Farming, Trade }

    /// <summary>宗族声望：影响售价与主线解锁</summary>
    public class ReputationSystem : MonoBehaviour
    {
        public static ReputationSystem Instance { get; private set; }
        public int Reputation { get; private set; }
        public float SellMultiplier => 1f + Mathf.Min(0.5f, Reputation / 1000f);
        public event Action<int> OnChanged;

        private void Awake() { if (Instance == null) Instance = this; else { Destroy(gameObject); return; } }

        public void Add(int n)
        {
            if (n == 0) return;
            Reputation = Mathf.Max(0, Reputation + n);
            OnChanged?.Invoke(Reputation);
            PBV.UI.HUD.Instance?.ShowNotice($"宗族声望 {(n > 0 ? "+" : "")}{n} → {Reputation}（售价 ×{SellMultiplier:0.00}）");
        }
        public void Restore(int v) { Reputation = Mathf.Max(0, v); OnChanged?.Invoke(Reputation); }
    }

    /// <summary>菜品 Buff：吃下对应菜系料理后生效，持续到次日</summary>
    public class BuffSystem : MonoBehaviour
    {
        public static BuffSystem Instance { get; private set; }
        public BuffType Active { get; private set; } = BuffType.None;
        public int DaysLeft { get; private set; }

        public float FishingBonus => Active == BuffType.Fishing ? 0.25f : 0f;
        public int MiningBonus => Active == BuffType.Mining ? 1 : 0;
        public int FarmingBonus => Active == BuffType.Farming ? 1 : 0;
        public float TradeBonus => Active == BuffType.Trade ? 0.2f : 0f;

        private void Awake() { if (Instance == null) Instance = this; else { Destroy(gameObject); return; } }

        private void Start()
        {
            if (TimeManager.Instance != null) TimeManager.Instance.OnDayChanged += OnNewDay;
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null) TimeManager.Instance.OnDayChanged -= OnNewDay;
        }

        private void OnNewDay()
        {
            if (DaysLeft <= 0) return;
            DaysLeft--;
            if (DaysLeft <= 0) { Active = BuffType.None; Debug.Log("[Buff] 菜品增益结束"); }
        }

        public void Apply(BuffType type, int days = 1)
        {
            if (type == BuffType.None) return;
            Active = type;
            DaysLeft = Mathf.Max(DaysLeft, days);
            PBV.UI.HUD.Instance?.ShowNotice($"获得「{BuffName(type)}」增益，持续 {DaysLeft} 天");
        }

        public void Restore(int type, int days) { Active = (BuffType)Mathf.Clamp(type, 0, 4); DaysLeft = Mathf.Max(0, days); }
        public int ExportType() => (int)Active;

        public static string BuffName(BuffType t)
        {
            switch (t)
            {
                case BuffType.Fishing: return "鲜味·钓鱼稀有率+25%";
                case BuffType.Mining: return "扎实·挖矿产出+1";
                case BuffType.Farming: return "家常·收获产量+1";
                case BuffType.Trade: return "雅致·贸易收益+20%";
                default: return "无";
            }
        }
    }

    /// <summary>节气随机事件：跨日时roll一个，给当日一个可感知的临时效果</summary>
    public class RandomEventSystem : MonoBehaviour
    {
        public static RandomEventSystem Instance { get; private set; }

        private readonly string[] _events =
        {
            "货商过境：今日商店回收价 +20%",
            "山雨欲来：今日钓鱼稀有率 +20%",
            "乡邻赠礼：随机获得 1 件作物",
            "古画微光：宗族声望 +15",
            "矿脉显露：今日挖矿产出 +1"
        };
        public string TodayEvent { get; private set; } = "今日无事";
        public float ShopBonus { get; private set; }
        public float FishBonus { get; private set; }
        public int OreBonus { get; private set; }

        private void Awake() { if (Instance == null) Instance = this; else { Destroy(gameObject); return; } }

        private void Start()
        {
            if (TimeManager.Instance != null) TimeManager.Instance.OnDayChanged += Roll;
            Roll();
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null) TimeManager.Instance.OnDayChanged -= Roll;
        }

        private void Roll()
        {
            ShopBonus = 0f; FishBonus = 0f; OreBonus = 0;
            int idx = UnityEngine.Random.Range(0, _events.Length);
            TodayEvent = _events[idx];
            switch (idx)
            {
                case 0: ShopBonus = 0.2f; break;
                case 1: FishBonus = 0.2f; break;
                case 2: GrantRandomCrop(); break;
                case 3: ReputationSystem.Instance?.Add(15); break;
                case 4: OreBonus = 1; break;
            }
            PBV.UI.HUD.Instance?.ShowNotice($"【今日事件】{TodayEvent}", 3.5f);
            Debug.Log($"[Event] {TodayEvent}");
        }

        private static void GrantRandomCrop()
        {
            var inv = InventorySystem.Instance;
            if (inv == null) return;
            foreach (var item in Resources.LoadAll<ItemData>("Data"))
            {
                if (item == null || item.id == null || !item.id.StartsWith("item_")) continue;
                if (item.sellPrice > 0 && item.type == ItemType.Resource && item.staminaRestore == 0)
                {
                    inv.AddItem(item, 1);
                    break;
                }
            }
        }
    }

    /// <summary>餐厅订单：每天一位 NPC 点一道简单菜，交付得金钱 + 声望 + Buff</summary>
    public class RestaurantOrderSystem : MonoBehaviour
    {
        public static RestaurantOrderSystem Instance { get; private set; }

        private static readonly string[] Customers = { "老农", "药婆", "书生", "柳娘", "阿蓝", "老窑" };
        private static readonly (string dishId, string dishName, BuffType buff)[] Dishes =
        {
            ("item_dish_radish_soup", "萝卜汤", BuffType.Farming),
            ("item_dish_cabbage_soup", "白菜汤", BuffType.Mining),
            ("item_dish_salad", "蔬菜沙拉", BuffType.Fishing),
            ("item_dish_steamed_fish", "清蒸鱼", BuffType.Fishing),
        };

        public string Customer { get; private set; } = "";
        public string DishId { get; private set; } = "";
        public string DishName { get; private set; } = "";
        public int RewardMoney { get; private set; }
        public int RewardRep { get; private set; }
        public BuffType RewardBuff { get; private set; }

        private int _lastOrderDay = -1;

        private void Awake() { if (Instance == null) Instance = this; else { Destroy(gameObject); return; } }

        private void Start()
        {
            if (TimeManager.Instance != null) TimeManager.Instance.OnDayChanged += NewOrder;
            NewOrder();
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null) TimeManager.Instance.OnDayChanged -= NewOrder;
        }

        private void NewOrder()
        {
            int day = TimeManager.Instance != null ? TimeManager.Instance.CurrentDay : 1;
            if (day == _lastOrderDay) return;
            _lastOrderDay = day;
            var d = Dishes[UnityEngine.Random.Range(0, Dishes.Length)];
            Customer = Customers[UnityEngine.Random.Range(0, Customers.Length)];
            DishId = d.dishId; DishName = d.dishName; RewardBuff = d.buff;
            RewardMoney = 80 + UnityEngine.Random.Range(0, 5) * 20;
            RewardRep = 10;
        }

        public bool TryDeliver()
        {
            var inv = InventorySystem.Instance;
            if (inv == null) return false;
            ItemData dish = null;
            foreach (var i in Resources.LoadAll<ItemData>("Data"))
                if (i != null && i.id == DishId) { dish = i; break; }
            if (dish == null || inv.CountItem(dish) < 1)
            {
                PBV.UI.HUD.Instance?.ShowNotice($"订单未完成：背包里没有「{DishName}」");
                return false;
            }
            inv.RemoveItem(dish, 1);
            if (PlayerWallet.Instance != null) PlayerWallet.Instance.AddSong(RewardMoney);
            ReputationSystem.Instance?.Add(RewardRep);
            BuffSystem.Instance?.Apply(RewardBuff);
            PBV.UI.HUD.Instance?.ShowNotice($"{Customer} 的订单完成：+{RewardMoney}文 · 声望 +{RewardRep}");
            QuestManager.Instance?.RecordOrder();
            NewOrder();
            return true;
        }

        public string Summary => $"{Customer} 想要「{DishName}」 · {RewardMoney}文 / 声望+{RewardRep} / {BuffSystem.BuffName(RewardBuff)}";
    }

    /// <summary>时代价值标签</summary>
    public enum EraTag { Common, AncientCraft, Relic, Contraband }

    /// <summary>
    /// 穿梭带货「三出三进」：两界背包独立，只有这 3 个槽位能跨境。
    /// 穿越时货物**实物**进对岸背包，不再自动结算成钱；EraTag 仅作物品分类标注。
    /// </summary>
    public class TradeCarrySystem : MonoBehaviour
    {
        public static TradeCarrySystem Instance { get; private set; }
        public const int Slots = 3;

        private readonly ItemData[] _items = new ItemData[Slots];

        private void Awake() { if (Instance == null) Instance = this; else { Destroy(gameObject); return; } }

        public ItemData Get(int slot) => slot >= 0 && slot < Slots ? _items[slot] : null;

        public EraTag TagOf(ItemData item)
        {
            if (item == null) return EraTag.Common;
            string id = item.id;
            if (id.StartsWith("item_antique_") || id.StartsWith("item_art_")) return EraTag.Relic;
            if (id == "item_ore_gold" || id == "item_fish_dragoncarp" || id == "item_garment") return EraTag.Relic;
            if (id == "item_xuanpaper" || id == "item_cloth_pattern" || id == "item_fish_koi") return EraTag.AncientCraft;
            if (id == "item_paper" || id == "item_pulp" || id == "item_cloth_plain") return EraTag.Contraband;
            return EraTag.Common;
        }

        public static string TagName(EraTag t)
        {
            switch (t)
            {
                case EraTag.Relic: return "珍贵文物";
                case EraTag.AncientCraft: return "古代珍品";
                case EraTag.Contraband: return "违禁品";
                default: return "普通货";
            }
        }

        /// <summary>把当前选中的背包物品装入槽位（每槽 1 件）</summary>
        public bool Load(int slot)
        {
            if (slot < 0 || slot >= Slots || _items[slot] != null) return false;
            var inv = InventorySystem.Instance;
            if (inv == null) return false;
            int idx = PBV.UI.InventoryUI.SelectedSlotIndex;
            var s = idx >= 0 && idx < inv.Capacity ? inv.GetSlot(idx) : null;
            if (s == null || s.IsEmpty || s.item == null) return false;
            inv.RemoveItem(s.item, 1);
            _items[slot] = s.item;
            PBV.UI.HUD.Instance?.ShowNotice($"槽位 {slot + 1} 装入 {s.item.itemName}（{TagName(TagOf(s.item))}）");
            return true;
        }

        public bool Unload(int slot)
        {
            if (slot < 0 || slot >= Slots || _items[slot] == null) return false;
            var inv = InventorySystem.Instance;
            var item = _items[slot];
            if (inv == null || !inv.AddItem(item, 1)) return false;
            _items[slot] = null;
            PBV.UI.HUD.Instance?.ShowNotice($"槽位 {slot + 1} 取回 {item.itemName}");
            return true;
        }

        /// <summary>
        /// 参考价（仅用于 UI 展示物品大致价值，不再结算成钱）。
        /// 穿梭贸易已改为「实物带货」：货物到对岸进背包，玩家自己决定用掉或卖给当地商店。
        /// </summary>
        public int Estimate(int slot, WorldType to)
        {
            var item = Get(slot);
            if (item == null) return 0;
            float mult = TagOf(item) == EraTag.Relic ? 5f :
                         TagOf(item) == EraTag.AncientCraft ? 3f :
                         TagOf(item) == EraTag.Contraband ? 0.5f : 1.5f;
            if (to == WorldType.Modern) mult *= 2f;
            return Mathf.RoundToInt(item.sellPrice * mult);
        }

        /// <summary>
        /// 穿越到新世界时把 3 槽货物**实物**放进对岸背包。
        /// 调用时序由 WorldState.SetWorld 保证：OnWorldChanged 已先把活动背包切到对岸。
        /// 背包放不下的货物留在槽里（下次穿越再带），不丢物品。
        /// </summary>
        public int Settle(WorldType to)
        {
            var inv = PBV.Inventory.InventorySystem.Instance;
            int delivered = 0;
            for (int i = 0; i < Slots; i++)
            {
                var item = _items[i];
                if (item == null) continue;
                if (inv != null && inv.AddItem(item, 1))
                {
                    _items[i] = null;
                    delivered++;
                }
                else
                {
                    PBV.UI.HUD.Instance?.ShowNotice($"{item.itemName} 放不进对岸背包，留在槽位 {i + 1}", 4f);
                }
            }
            if (delivered > 0)
            {
                PBV.UI.HUD.Instance?.ShowNotice(
                    $"带入{(to == WorldType.Modern ? "现代" : "桃花谷")} {delivered} 件货物 → 已进当地背包", 4f);
                ReputationSystem.Instance?.Add(delivered * 3);
                Debug.Log($"[TradeCarry] 实物落地 {delivered} 件到 {(to == WorldType.Modern ? "现代" : "桃花谷")} 背包");
            }
            return delivered;
        }

        public string[] Export()
        {
            var r = new string[Slots];
            for (int i = 0; i < Slots; i++) r[i] = _items[i] != null ? _items[i].id : "";
            return r;
        }

        public void Import(string[] ids)
        {
            if (ids == null) return;
            var all = Resources.LoadAll<ItemData>("Data");
            for (int i = 0; i < Slots && i < ids.Length; i++)
            {
                _items[i] = null;
                if (string.IsNullOrEmpty(ids[i])) continue;
                foreach (var it in all) if (it.id == ids[i]) { _items[i] = it; break; }
            }
        }
    }

    /// <summary>
    /// v0.4.0 两个管理面板：O = 订单/声望/Buff/事件，R = 穿梭贸易槽位。
    /// 自建 Canvas，整块随面板开关，避免背景常驻吃点击。
    /// </summary>
    public class V04Panel : MonoBehaviour
    {
        private GameObject _infoCanvas, _tradeCanvas;
        private Text _infoText;
        private readonly Text[] _slotTexts = new Text[TradeCarrySystem.Slots];
        private static Font _font;

        private void Awake()
        {
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildInfoPanel();
            BuildTradePanel();
            _infoCanvas.SetActive(false);
            _tradeCanvas.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.O)) Toggle(_infoCanvas, _tradeCanvas);
            if (Input.GetKeyDown(KeyCode.R)) Toggle(_tradeCanvas, _infoCanvas);
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_infoCanvas.activeSelf) { _infoCanvas.SetActive(false); UIEventSystemBootstrap.ClearTopPanel(); }
                else if (_tradeCanvas.activeSelf) { _tradeCanvas.SetActive(false); UIEventSystemBootstrap.ClearTopPanel(); }
            }
        }

        private void Toggle(GameObject target, GameObject other)
        {
            bool open = !target.activeSelf;
            if (other.activeSelf) other.SetActive(false);
            target.SetActive(open);
            if (open)
            {
                UIEventSystemBootstrap.RegisterTopPanel(() => { target.SetActive(false); UIEventSystemBootstrap.ClearTopPanel(); });
                Refresh();
            }
            else UIEventSystemBootstrap.ClearTopPanel();
        }

        private void Refresh()
        {
            var rep = ReputationSystem.Instance;
            var buff = BuffSystem.Instance;
            var ev = RandomEventSystem.Instance;
            var order = RestaurantOrderSystem.Instance;
            if (_infoText != null)
            {
                _infoText.text =
                    $"【宗族声望】{(rep != null ? rep.Reputation : 0)}  ·  出货售价 ×{(rep != null ? rep.SellMultiplier : 1f):0.00}\n" +
                    $"【当前增益】{(buff != null ? BuffSystem.BuffName(buff.Active) : "无")}（剩余 {(buff != null ? buff.DaysLeft : 0)} 天）\n" +
                    $"【今日事件】{(ev != null ? ev.TodayEvent : "今日无事")}\n" +
                    $"【餐厅订单】{(order != null ? order.Summary : "暂无")}\n\n" +
                    "做出订单要求的菜后，点下方「交付订单」即可领奖。\n" +
                    "菜品 Buff 在按 F 食用时生效，持续到次日。";
            }
            var carry = TradeCarrySystem.Instance;
            var ws = WorldState.Instance;
            WorldType to = ws != null && ws.Current == WorldType.Ancient ? WorldType.Modern : WorldType.Ancient;
            string dest = to == WorldType.Modern ? "现代" : "桃花谷";
            for (int i = 0; i < _slotTexts.Length; i++)
            {
                if (_slotTexts[i] == null || carry == null) continue;
                var item = carry.Get(i);
                _slotTexts[i].text = item == null
                    ? $"槽位 {i + 1}：空（点击装入选中物品）"
                    : $"槽位 {i + 1}：{item.itemName} [{TradeCarrySystem.TagName(carry.TagOf(item))}]  穿越后带入{dest}背包";
            }
        }

        // ---------- UI 构建 ----------

        private GameObject MakeCanvas(string name, out GameObject panel)
        {
            var canvasGo = new GameObject(name);
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 45;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            var bg = new GameObject("BG");
            bg.transform.SetParent(canvasGo.transform, false);
            var bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.sizeDelta = Vector2.zero;
            bg.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
            bg.AddComponent<Button>().onClick.AddListener(() => { canvasGo.SetActive(false); UIEventSystemBootstrap.ClearTopPanel(); });

            panel = new GameObject("Panel");
            panel.transform.SetParent(canvasGo.transform, false);
            var rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(560, 400);
            panel.AddComponent<Image>().color = new Color(0.1f, 0.08f, 0.06f, 0.97f);
            var vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(18, 18, 16, 16);
            vlg.spacing = 8; vlg.childControlWidth = true; vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            return canvasGo;
        }

        private void BuildInfoPanel()
        {
            GameObject panel;
            _infoCanvas = MakeCanvas("V04InfoCanvas", out panel);
            MakeText(panel.transform, "声望 · 订单 · 增益 (O/Esc 关闭)", 20, FontStyle.Bold, new Color(1f, 0.88f, 0.5f), 28);
            _infoText = MakeText(panel.transform, "", 15, FontStyle.Normal, new Color(0.92f, 0.9f, 0.82f), 190);
            MakeButton(panel.transform, "交付订单", 46, () =>
            {
                RestaurantOrderSystem.Instance?.TryDeliver();
                Refresh();
            });
        }

        private void BuildTradePanel()
        {
            GameObject panel;
            _tradeCanvas = MakeCanvas("V04TradeCanvas", out panel);
            MakeText(panel.transform, "穿梭带货 · 每程最多三样 (R/Esc 关闭)", 20, FontStyle.Bold, new Color(0.85f, 0.75f, 1f), 28);
            MakeText(panel.transform, "两边背包互不通用：只有装入槽位的 3 件能跨境，穿越后实物进对岸背包。\n出谷通行费 50 文 / 回谷 ¥5；货币按 X 打开驿站随时兑换。",
                14, FontStyle.Normal, new Color(0.8f, 0.78f, 0.72f), 60);
            for (int i = 0; i < TradeCarrySystem.Slots; i++)
            {
                int slot = i;
                _slotTexts[i] = MakeText(panel.transform, $"槽位 {i + 1}：空", 16, FontStyle.Bold, new Color(1f, 0.9f, 0.65f), 34);
                MakeButton(panel.transform, $"槽位 {i + 1}：装入 / 取出", 40, () =>
                {
                    var carry = TradeCarrySystem.Instance;
                    if (carry == null) return;
                    if (carry.Get(slot) != null) carry.Unload(slot);
                    else carry.Load(slot);
                    Refresh();
                });
            }
            MakeButton(panel.transform, "清空全部槽位（取回背包）", 38, () =>
            {
                var carry = TradeCarrySystem.Instance;
                if (carry == null) return;
                for (int i = 0; i < TradeCarrySystem.Slots; i++) carry.Unload(i);
                Refresh();
            });
        }

        private Text MakeText(Transform parent, string content, int size, FontStyle style, Color color, float height)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, height);
            var t = go.AddComponent<Text>();
            t.font = _font; t.fontSize = size; t.fontStyle = style; t.color = color;
            t.text = content; t.alignment = TextAnchor.UpperLeft;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        private void MakeButton(Transform parent, string label, float height, Action onClick)
        {
            var go = new GameObject("Btn");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, height);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.38f, 0.3f, 1f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(go.transform, false);
            var rt = txtGo.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
            var txt = txtGo.AddComponent<Text>();
            txt.font = _font; txt.fontSize = 16; txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white; txt.text = label; txt.raycastTarget = false;
            btn.onClick.AddListener(() => onClick());
        }
    }

    /// <summary>运行时补挂 v0.4.0 全部系统</summary>
    public class V04Bootstrap : MonoBehaviour
    {
        private void Awake()
        {
            Ensure<ReputationSystem>();
            Ensure<BuffSystem>();
            Ensure<RandomEventSystem>();
            Ensure<RestaurantOrderSystem>();
            Ensure<TradeCarrySystem>();
            Ensure<V04Panel>();
        }

        private static void Ensure<T>() where T : MonoBehaviour
        {
            if (FindObjectOfType<T>() != null) return;
            new GameObject(typeof(T).Name + " (Auto)").AddComponent<T>();
        }
    }
}
