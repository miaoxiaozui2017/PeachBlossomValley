using UnityEngine;
using PBV.Core;
using PBV.UI;

namespace PBV.Systems
{
    /// <summary>采山入口交互物体，E 键打开采山界面。</summary>
    [RequireComponent(typeof(Collider2D))]
    public class MountainEntrance : MonoBehaviour, IInteractable
    {
        public string GetInteractPrompt() => "采山入口：采山货 / 药材 / 野味 (E)";
        public void OnInteract() { MountainUI.Instance?.Toggle(); }
    }
}
