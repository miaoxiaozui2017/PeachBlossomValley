using UnityEngine;
using PBV.Core;
using PBV.UI;

namespace PBV.Systems
{
    /// <summary>衣柜交互物体，E 键打开换装界面。</summary>
    [RequireComponent(typeof(Collider2D))]
    public class WardrobeStation : MonoBehaviour, IInteractable
    {
        public string GetInteractPrompt() => "衣柜：换装 (E)";
        public void OnInteract() { WardrobeUI.Instance?.Toggle(); }
    }
}
