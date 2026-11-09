using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PBV.Inventory;
using PBV.UI;

namespace PBV.Systems
{
    // =====================================================================
    // v0.6.0 内容填充：收集图鉴 / 鱼塘养殖 / 隐藏彩蛋 / 图鉴面板(Y)
    // 运行时由 V06Bootstrap 自动补挂，不要求重建场景。
    // =====================================================================

    /// <summary>收集图鉴：自动记录见过的物品，PlayerPrefs 持久化</summary>
    public class CollectionSystem : MonoBehaviour
    {
        public static CollectionSystem Instance { get; private set; }
        private const string PrefKey = "gzx_collection_v1";

        private readonly HashSet<string> _found = new HashSet<string>();
        private ItemData[] _catalog;
        private float _timer;

        public int TotalCount => _catalog != null ? _catalog.Length : 0;
        public int FoundCount => _found.Count;
        public bool Has(string id) => !string.IsNullOrEmpty(id) && _found.Contains(id);
        public System.Action OnChanged;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
            _catalog = Resources.LoadAll<ItemData>("Data");
            foreach (var s in PlayerPrefs.GetString(PrefKey, "").Split(','))
                if (!string.IsNullOrEmpty(s)) _found.Add(s);
        }

        private void Update()
        {
            _timer -= Time.unscaledDeltaTime;
            if (_timer > 0f) return;
            _timer = 1f;
            Scan();
            EasterEggSystem.Instance?.Check();
        }

        private void Scan()
        {
            var inv = InventorySystem.Instance;
            if (inv == null) return;
            bool changed = false;
            for (int i = 0; i < inv.Capacity; i++)
            {
                var s = inv.GetSlot(i);
                if (s == null || s.IsEmpty || s.item == null) continue;
                string id = s.item.id;
                if (!string.IsNullOrEmpty(id) && _found.Add(id)) changed = true;
            }
            if (changed) { Save(); OnChanged?.Invoke(); }
        }

        private void Save() => PlayerPrefs.SetString(PrefKey, string.Join(",", _found));

        public void ResetAll()
        {
            _found.Clear();
            Save();
            OnChanged?.Invoke();
            HUD.Instance?.ShowNotice("收集图鉴已清空");
        }
    }

    /// <summary>鱼塘：投入 1 条鱼，次日可收 2 条同种鱼</summary>
    public class FishPondSystem : MonoBehaviour
    {
        public static FishPondSystem Instance { get; private set; }

        private string _fishId = "";
        private int _daysLeft;

        private void Awake() { if (Instance == null) Instance = this; else { Destroy(gameObject); return; } }

        private void Start()
        {
            if (TimeManager.Instance != null) TimeManager.Instance.OnDayChanged += OnNewDay;
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null) TimeManager.Instance.OnDayChanged -= OnNewDay;
        }

        private void OnNewDay() { if (_daysLeft > 0) _daysLeft--; }

        public bool HasFish => !string.IsNullOrEmpty(_fishId);
        public bool Ready => HasFish && _daysLeft <= 0;

        public string Status
        {
            get
            {
                if (!HasFish) return "鱼塘空着 · 选中背包里的鱼后点「投入 1 条鱼」";
                var item = Resources.Load<ItemData>($"Data/{_fishId}");
                string name = item != null ? item.itemName : _fishId;
                return Ready ? $"{name} 已可收获（可收 2 条）" : $"{name} 养殖中，还需 {_daysLeft} 天";
            }
        }

        public bool Deposit()
        {
            if (HasFish) { HUD.Instance?.ShowNotice("鱼塘里已有鱼，先收获"); return false; }
            var inv = InventorySystem.Instance;
            if (inv == null) return false;
            int idx = InventoryUI.SelectedSlotIndex;
            var s = idx >= 0 && idx < inv.Capacity ? inv.GetSlot(idx) : null;
            if (s == null || s.IsEmpty || s.item == null || s.item.id == null || !s.item.id.StartsWith("item_fish_"))
            {
                HUD.Instance?.ShowNotice("先用数字键选中背包里的一条鱼");
                return false;
            }
            _fishId = s.item.id;
            inv.RemoveItem(s.item, 1);
            _daysLeft = 1;
            HUD.Instance?.ShowNotice($"投入 {s.item.itemName}，次日可收 2 条");
            return true;
        }

        public bool Collect()
        {
            if (!Ready) { HUD.Instance?.ShowNotice("鱼还没养好，或鱼塘是空的"); return false; }
            var inv = InventorySystem.Instance;
            var item = Resources.Load<ItemData>($"Data/{_fishId}");
            if (inv == null || item == null) return false;
            string name = item.itemName;
            if (!inv.AddItem(item, 2)) { HUD.Instance?.ShowNotice("背包放不下，先清出格子"); return false; }
            _fishId = "";
            HUD.Instance?.ShowNotice($"鱼塘收获 {name} ×2");
            return true;
        }
    }

    /// <summary>隐藏彩蛋：见闻 20 种 + 声望 200 + 成就 4 个 → 古画重现，奖励 ¥5000</summary>
    public class EasterEggSystem : MonoBehaviour
    {
        public static EasterEggSystem Instance { get; private set; }
        private bool _triggered;

        private void Awake() { if (Instance == null) Instance = this; else { Destroy(gameObject); return; } }

        public string Progress
        {
            get
            {
                var c = CollectionSystem.Instance;
                var r = ReputationSystem.Instance;
                var a = AchievementSystem.Instance;
                return $"见闻 {c?.FoundCount ?? 0}/20 · 声望 {r?.Reputation ?? 0}/200 · 成就 {a?.UnlockedCount ?? 0}/4" +
                       (_triggered ? "  ✓ 已触发「谷中仙」" : "");
            }
        }

        public void Check()
        {
            if (_triggered) return;
            var c = CollectionSystem.Instance;
            var r = ReputationSystem.Instance;
            var a = AchievementSystem.Instance;
            if (c == null || c.FoundCount < 20) return;
            if (r != null && r.Reputation < 200) return;
            if (a != null && a.UnlockedCount < 4) return;
            _triggered = true;
            if (PlayerWallet.Instance != null) PlayerWallet.Instance.AddSong(5000);
            HUD.Instance?.ShowNotice("【隐藏彩蛋 · 谷中仙】古画重现光彩，赠 5000文", 6f);
            Debug.Log("[EasterEgg] 隐藏彩蛋触发");
        }
    }

    /// <summary>图鉴面板：Y 键开关，显示收集进度、鱼塘状态与彩蛋进度</summary>
    public class V06Panel : MonoBehaviour
    {
        private GameObject _canvasGo;
        private Text _text;
        private static Font _font;

        private void Awake()
        {
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildUI();
            _canvasGo.SetActive(false);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Y)) Toggle();
            if (_canvasGo.activeSelf && Input.GetKeyDown(KeyCode.Escape)) { _canvasGo.SetActive(false); UIEventSystemBootstrap.ClearTopPanel(); }
        }

        private void Toggle()
        {
            bool open = !_canvasGo.activeSelf;
            _canvasGo.SetActive(open);
            if (open)
            {
                UIEventSystemBootstrap.RegisterTopPanel(() => { _canvasGo.SetActive(false); UIEventSystemBootstrap.ClearTopPanel(); });
                Refresh();
            }
            else UIEventSystemBootstrap.ClearTopPanel();
        }

        private void Refresh()
        {
            if (_text == null) return;
            var c = CollectionSystem.Instance;
            var pond = FishPondSystem.Instance;
            var egg = EasterEggSystem.Instance;
            _text.text =
                "收集图鉴 (Y / Esc 关闭)\n" +
                $"【见闻】已记录 {c?.FoundCount ?? 0} / {c?.TotalCount ?? 0} 种物品（自动记录，永久保存）\n" +
                $"【鱼塘】{(pond != null ? pond.Status : "未就绪")}\n" +
                $"【隐藏彩蛋】{(egg != null ? egg.Progress : "未就绪")}\n\n" +
                "鱼塘玩法：数字键选中背包里的鱼 → 点「投入」→ 跳到次日 → 点「收获」得 2 条。";
        }

        private void BuildUI()
        {
            _canvasGo = new GameObject("V06CollectionCanvas");
            _canvasGo.transform.SetParent(transform, false);
            var canvas = _canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 46;
            var scaler = _canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            _canvasGo.AddComponent<GraphicRaycaster>();

            var bg = new GameObject("BG");
            bg.transform.SetParent(_canvasGo.transform, false);
            var bgRt = bg.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.sizeDelta = Vector2.zero;
            bg.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
            bg.AddComponent<Button>().onClick.AddListener(() => { _canvasGo.SetActive(false); UIEventSystemBootstrap.ClearTopPanel(); });

            var panel = new GameObject("Panel");
            panel.transform.SetParent(_canvasGo.transform, false);
            var rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(560, 360);
            panel.AddComponent<Image>().color = new Color(0.1f, 0.08f, 0.06f, 0.97f);
            var vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(18, 18, 16, 16);
            vlg.spacing = 8; vlg.childControlWidth = true; vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

            _text = MakeText(panel.transform, "收集图鉴 (Y/Esc 关闭)", 15, FontStyle.Normal, new Color(0.92f, 0.9f, 0.82f), 150);
            MakeButton(panel.transform, "投入 1 条鱼（选中背包里的鱼）", 40, () => { FishPondSystem.Instance?.Deposit(); Refresh(); });
            MakeButton(panel.transform, "收获鱼塘", 40, () => { FishPondSystem.Instance?.Collect(); Refresh(); });
            MakeButton(panel.transform, "清空图鉴记录（调试）", 34, () => { CollectionSystem.Instance?.ResetAll(); Refresh(); });
        }

        private Text MakeText(Transform parent, string content, int size, FontStyle style, Color color, float height)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, height);
            var t = go.AddComponent<Text>();
            t.font = _font; t.fontSize = size; t.fontStyle = style; t.color = color;
            t.text = content; t.alignment = TextAnchor.UpperLeft;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        private void MakeButton(Transform parent, string label, float height, System.Action onClick)
        {
            var go = new GameObject("Btn");
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>().sizeDelta = new Vector2(0, height);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.36f, 0.42f, 1f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(go.transform, false);
            var r = txtGo.AddComponent<RectTransform>();
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.sizeDelta = Vector2.zero;
            var txt = txtGo.AddComponent<Text>();
            txt.font = _font; txt.fontSize = 16; txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white; txt.text = label; txt.raycastTarget = false;
            btn.onClick.AddListener(() => { onClick(); });
        }
    }

    /// <summary>运行时补挂 v0.6.0 系统</summary>
    public class V06Bootstrap : MonoBehaviour
    {
        private void Awake()
        {
            Ensure<CollectionSystem>();
            Ensure<FishPondSystem>();
            Ensure<EasterEggSystem>();
            Ensure<V06Panel>();
        }

        private static void Ensure<T>() where T : MonoBehaviour
        {
            if (FindObjectOfType<T>() != null) return;
            new GameObject(typeof(T).Name + " (Auto)").AddComponent<T>();
        }
    }
}
