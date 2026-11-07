using UnityEngine;
using PBV.Core;
using PBV.UI;

namespace PBV.Systems
{
    /// <summary>矿洞入口交互物体，E 键打开挖矿界面。</summary>
    [RequireComponent(typeof(Collider2D))]
    public class MineEntrance : MonoBehaviour, IInteractable
    {
        public string GetInteractPrompt() => "矿洞：挖矿 (E)";
        public void OnInteract() { MineUI.Instance?.Toggle(); }
    }
}
