using UnityEngine;
using PBV.Core;

namespace PBV.Systems
{
    /// <summary>传送门：E 键穿越时空（古代⇄现代）。</summary>
    [RequireComponent(typeof(Collider2D))]
    public class WorldPortal : MonoBehaviour, IInteractable
    {
        public string GetInteractPrompt()
        {
            var ws = WorldState.Instance;
            if (ws == null) return "传送门 (E)";
            return ws.Current == WorldType.Ancient ? "传送门：前往现代 (E)" : "传送门：回到桃花谷 (E)";
        }

        public void OnInteract()
        {
            WorldState.Instance?.Toggle();
        }
    }
}
