using UnityEngine;
using PBV.Core;
using PBV.UI;

namespace PBV.Systems
{
    /// <summary>建造台交互物体，E 键打开房屋扩建界面。</summary>
    [RequireComponent(typeof(Collider2D))]
    public class BuildStation : MonoBehaviour, IInteractable
    {
        public string GetInteractPrompt() => "建造台：房屋扩建 (E)";
        public void OnInteract() { BuildUI.Instance?.Toggle(); }
    }
}
