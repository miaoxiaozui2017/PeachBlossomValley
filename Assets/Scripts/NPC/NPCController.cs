using UnityEngine;
using PBV.Core;
using PBV.Dialogue;

namespace PBV.NPC
{
    /// <summary>
    /// v0.1.0 基础 NPC：碰撞体 + 对话触发
    /// 挂载要求：Collider2D (设为 Trigger 并放在 Interact Layer)
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class NPCController : MonoBehaviour, IInteractable
    {
        [Header("NPC Info")]
        [SerializeField] private string npcName = "村民";

        [Header("Dialogue Lines")]
        [TextArea(3, 10)]
        [SerializeField] private string[] dialogueLines = new string[]
        {
            "……桃花谷好久没有外乡人来了。",
            "地里的活儿不等人，早些开垦吧。"
        };

        public string NpcName => npcName;

        public string GetInteractPrompt() => $"与 {npcName} 交谈";

        public void OnInteract()
        {
            if (DialogueManager.Instance == null) return;
            if (DialogueManager.Instance.IsDialogueActive) return; // 对话中防止重复触发

            DialogueManager.Instance.StartDialogue(npcName, dialogueLines);
        }
    }
}
