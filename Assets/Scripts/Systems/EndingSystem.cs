using UnityEngine;
using UnityEngine.UI;
using PBV.Inventory;

namespace PBV.Systems
{
    /// <summary>
    /// 多结局系统：第112天（1年）触发结局。
    /// 归隐结局：3个NPC好感>=亲密 + 背包有宣纸
    /// 富商结局：金钱>=10000
    /// 凡人结局：以上都不满足
    /// </summary>
    public class EndingSystem : MonoBehaviour
    {
        public static EndingSystem Instance { get; private set; }

        private bool _ended;
        private GameObject _endingCanvas;
        private float _keyDelay = 1f;

        private void Update()
        {
            if (!_ended) return;
            _keyDelay -= Time.deltaTime;
            if (_keyDelay > 0) return;
            if (Input.anyKeyDown && _endingCanvas != null)
            {
                Destroy(_endingCanvas);
                _ended = false;
            }
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        private void Start()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnDayChanged += CheckEnding;
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnDayChanged -= CheckEnding;
        }

        private void CheckEnding()
        {
            if (_ended) return;
            if (TimeManager.Instance == null) return;
            if (TimeManager.Instance.CurrentDay >= 112) TriggerEnding();
        }

        private void TriggerEnding()
        {
            _ended = true;
            string ending = DetermineEnding();
            ShowEnding(ending);
        }

        private string DetermineEnding()
        {
            // 检查归隐条件
            bool allClose = true;
            if (RelationshipSystem.Instance != null)
            {
                string[] npcs = { "老农", "药婆", "书生" };
                foreach (var npc in npcs)
                {
                    if (RelationshipSystem.Instance.GetLevel(npc) < AffectionLevel.Close)
                    {
                        allClose = false;
                        break;
                    }
                }
            }
            else allClose = false;

            bool hasXuanpaper = false;
            if (InventorySystem.Instance != null)
            {
                for (int i = 0; i < InventorySystem.Instance.Capacity; i++)
                {
                    var s = InventorySystem.Instance.GetSlot(i);
                    if (s != null && !s.IsEmpty && s.item.id == "item_xuanpaper")
                    {
                        hasXuanpaper = true;
                        break;
                    }
                }
            }

            if (allClose && hasXuanpaper)
                return "归隐结局\n\n你与桃花谷众人结为至交，\n手制的宣纸名扬天下。\n你选择留在这片山谷，\n做一辈子的匠人。\n\n桃花灼灼，岁月静好。";

            if (PlayerWallet.Instance != null && PlayerWallet.Instance.SongCoins >= 10000)
                return "富商结局\n\n你凭借穿梭贸易与勤劳耕作，\n积累了万贯家财。\n桃花谷因你而繁荣，\n你成为了远近闻名的富商。\n\n金银满仓，名动一方。";

            return "凡人结局\n\n一年时光转瞬即逝。\n你在桃花谷度过了平凡而充实的一年，\n种地、造纸、钓鱼、交友……\n没有惊天动地的成就，\n但每一天都过得踏实。\n\n平凡，也是一种幸福。";
        }

        private void ShowEnding(string text)
        {
            _endingCanvas = new GameObject("EndingCanvas");
            var canvasGo = _endingCanvas;
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 200;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("Panel");
            panel.transform.SetParent(canvasGo.transform, false);
            var rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
            var img = panel.AddComponent<Image>();
            img.color = new Color(0.02f, 0.02f, 0.04f, 0.98f);

            var vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(200, 200, 100, 100);
            vlg.spacing = 30;
            vlg.childAlignment = TextAnchor.MiddleCenter;

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(panel.transform, false);
            titleGo.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 40);
            var title = titleGo.AddComponent<Text>();
            title.font = font; title.fontSize = 28; title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color(1f, 0.85f, 0.5f);
            title.text = "—— 结局 ——";
            title.raycastTarget = false;

            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(panel.transform, false);
            bodyGo.AddComponent<RectTransform>().sizeDelta = new Vector2(0, 300);
            var body = bodyGo.AddComponent<Text>();
            body.font = font; body.fontSize = 22;
            body.alignment = TextAnchor.MiddleCenter;
            body.color = new Color(0.9f, 0.88f, 0.8f);
            body.lineSpacing = 1.8f;
            body.text = text + "\n\n（按任意键退出）";
            body.raycastTarget = false;

            Debug.Log($"[Ending] 触发结局:\n{text}");
        }
    }
}
