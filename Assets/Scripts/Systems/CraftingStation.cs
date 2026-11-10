using UnityEngine;
using PBV.Core;
using PBV.UI;

namespace PBV.Systems
{
    /// <summary>
    /// 制作台：场景交互物体，E 键打开「本建筑专属」制作配方。
    /// 每个建筑按自身 goName 过滤配方，避免所有配方挤在同一个面板里。
    /// 挂载要求：Collider2D (Trigger, Interact Layer)
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CraftingStation : MonoBehaviour, IInteractable
    {
        /// <summary>本建筑 id（用于 UI 过滤配方）</summary>
        public string StationId => gameObject.name;

        public string GetInteractPrompt() => $"{CraftingSystem.StationName(StationId)}：打开本台制作配方 (E)";

        public void OnInteract()
        {
            // 只显示本建筑配方；从别的台切换过来时重新过滤（面板栈由 UI 层统一管理）
            CraftingUI.Instance?.OpenFor(StationId);
        }
    }
}
