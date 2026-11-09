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
            return "时空驿站：穿梭 / 兑换货币 (E)";
        }

        public void OnInteract()
        {
            if (PBV.UI.PortalUI.Instance != null) PBV.UI.PortalUI.Instance.Open();
        }
    }
}
