using UnityEngine;
using UnityEngine.UI;
using PBV.Core;
using PBV.Dialogue;

namespace PBV.NPC
{
    /// <summary>
    /// v0.1.0 基础 NPC：碰撞体 + 对话触发 + 头顶名牌
    /// 挂载要求：Collider2D (设为 Trigger 并放在 Interact Layer)
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

        public string GetInteractPrompt() => $"与 {npcName} 交谈";

        private void Start()
        {
            if (_showNameTag) CreateNameTag();
        }

        public void OnInteract()
        {
            if (DialogueManager.Instance == null) return;
            if (DialogueManager.Instance.IsDialogueActive) return; // 对话中防止重复触发
            // 结束冷却：E 键同帧结束对话又重新触发会被拦下，给玩家一个退出窗口
            if (Time.time - DialogueManager.Instance.LastEndTime < 0.25f) return;

            DialogueManager.Instance.StartDialogue(npcName, dialogueLines);
        }

        /// <summary>头顶名牌：世界空间 Canvas + Text，子物体跟随 NPC</summary>
        private void CreateNameTag()
        {
            var tagGo = new GameObject($"NameTag_{npcName}");
            tagGo.transform.SetParent(transform, false);
            tagGo.transform.localPosition = new Vector3(0, _nameTagHeight, 0);

            var canvas = tagGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 60;

            var canvasRect = canvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(200f, 40f);
            canvasRect.localScale = new Vector3(0.01f, 0.01f, 0.01f);

            var bgGo = new GameObject("Bg");
            bgGo.transform.SetParent(tagGo.transform, false);
            var bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bg = bgGo.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.55f);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(tagGo.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(4, 2);
            textRect.offsetMax = new Vector2(-4, -2);
            var text = textGo.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 22;
            text.fontStyle = FontStyle.Bold;
            text.color = _nameTagColor;
            text.text = npcName;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
        }
    }
}
