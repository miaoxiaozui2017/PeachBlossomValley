using UnityEngine;
using PBV.Core;
using PBV.UI;

namespace PBV.Player
{
    /// <summary>铁匠铺：工具升级交互物体，E 键打开升级界面。</summary>
    [RequireComponent(typeof(Collider2D))]
    public class ToolUpgradeStation : MonoBehaviour, IInteractable
    {
        public string GetInteractPrompt() => "铁匠铺：升级工具 (E)";
        public void OnInteract() { ToolUpgradeUI.Instance?.Toggle(); }
    }
}
