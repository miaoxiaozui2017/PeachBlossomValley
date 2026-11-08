using UnityEngine;
using PBV.Core;

namespace PBV.Systems
{
    /// <summary>传送门：E 键穿越时空（古代⇄现代）；现代时按 T 可随时回桃花谷。</summary>
    [RequireComponent(typeof(Collider2D))]
    public class WorldPortal : MonoBehaviour, IInteractable
    {
        private SpriteRenderer _sr;

        private void Awake() => _sr = GetComponent<SpriteRenderer>();

        private void Update()
        {
            if (_sr == null) return;
            bool modern = WorldState.Instance != null && WorldState.Instance.Current == WorldType.Modern;
            _sr.color = modern ? new Color(0.95f, 0.55f, 0.3f) : new Color(0.6f, 0.4f, 0.8f);
        }

        public string GetInteractPrompt()
        {
            var ws = WorldState.Instance;
            if (ws == null) return "传送门 (E)";
            return ws.Current == WorldType.Ancient
                ? "传送门：前往现代（出货×2，农场功能不变） (E)"
                : "传送门：回到桃花谷 (E)  /  也可直接按 T";
        }

        public void OnInteract()
        {
            if (WorldState.Instance == null) return;
            WorldState.Instance.Toggle();
        }
    }
}
