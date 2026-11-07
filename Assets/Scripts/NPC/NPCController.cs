using UnityEngine;
using UnityEngine.UI;
using PBV.Core;
using PBV.Dialogue;
using PBV.Inventory;
using PBV.Systems;

namespace PBV.NPC
{
    /// <summary>
    /// NPC 控制器：对话 + 送礼 + 头顶名牌(含好感等级)。
    /// E=对话，G=送礼(送当前选中槽位物品)。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class NPCController : MonoBehaviour, IInteractable
    {
        [Header("NPC Info")]
        [SerializeField] private string npcName = "村民";

        [Header("Name Tag")]
        [SerializeField] private bool _showNameTag = true;
        [SerializeField] private float _nameTagHeight = 1.6f;
        [SerializeField] private Color _nameTagColor = new Color(1f, 0.95f, 0.8f);

        [Header("Dialogue Lines")]
        [TextArea(3, 10)]
        [SerializeField] private string[] dialogueLines = new string[]
        {
            "……桃花谷好久没有外乡人来了。",
            "地里的活儿不等人，早些开垦吧。"
        };

        public string NpcName => npcName;

        // 名牌引用（供好感更新刷新）
        private Text _nameTagText;

        public string GetInteractPrompt()
        {
            string gift = " / G 送礼";
            return $"与 {npcName} 交谈{gift}";
        }

        private void Start()
        {
            if (_showNameTag) CreateNameTag();
            UpdateNameTagAffection();
        }

        public void OnInteract()
        {
            if (DialogueManager.Instance == null) return;
            if (DialogueManager.Instance.IsDialogueActive) return;
            if (Time.time - DialogueManager.Instance.LastEndTime < 0.25f) return;
            DialogueManager.Instance.StartDialogue(npcName, dialogueLines);
        }

        /// <summary>送礼：送当前选中槽位物品给此 NPC</summary>
        public void Gift()
        {
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive) return;

            var inv = InventorySystem.Instance;
            if (inv == null) return;

            int slotIndex = UI.InventoryUI.SelectedSlotIndex;
            var slot = inv.GetSlot(slotIndex);
            if (slot == null || slot.IsEmpty)
            {
                Debug.Log($"[Gift] 槽位 {slotIndex} 无物品，无法送礼");
                return;
            }

            var item = slot.item;
            if (item.affectionValue <= 0)
            {
                Debug.Log($"[Gift] {item.itemName} 不适合送礼");
                return;
            }

            if (RelationshipSystem.Instance != null)
                RelationshipSystem.Instance.TryGift(npcName, item);
            inv.RemoveItem(item, 1);
            UpdateNameTagAffection();
        }

        /// <summary>头顶名牌：世界空间 Canvas + Text</summary>
        private void CreateNameTag()
        {
            var tagGo = new GameObject($"NameTag_{npcName}");
            tagGo.transform.SetParent(transform, false);
            tagGo.transform.localPosition = new Vector3(0, _nameTagHeight, 0);

            var canvas = tagGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 60;

            var canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(240f, 40f);
            canvasRect.localScale = new Vector3(0.01f, 0.01f, 0.01f);

            var bgGo = new GameObject("Bg");
            bgGo.transform.SetParent(tagGo.transform, false);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;
            bgGo.AddComponent<Image>().color = new Color(0, 0, 0, 0.55f);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(tagGo.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(4, 2); textRect.offsetMax = new Vector2(-4, -2);
            _nameTagText = textGo.AddComponent<Text>();
            _nameTagText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _nameTagText.fontSize = 22;
            _nameTagText.fontStyle = FontStyle.Bold;
            _nameTagText.color = _nameTagColor;
            _nameTagText.alignment = TextAnchor.MiddleCenter;
            _nameTagText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _nameTagText.verticalOverflow = VerticalWrapMode.Overflow;
        }

        /// <summary>刷新名牌文字（NPC名 + 好感等级）</summary>
        private void UpdateNameTagAffection()
        {
            if (_nameTagText == null) return;
            string levelText = "";
            if (RelationshipSystem.Instance != null)
            {
                var level = RelationshipSystem.Instance.GetLevel(npcName);
                levelText = $" [{RelationshipSystem.LevelToName(level)}]";
            }
            _nameTagText.text = npcName + levelText;
        }
    }
}
