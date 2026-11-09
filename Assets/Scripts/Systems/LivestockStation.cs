using UnityEngine;
using PBV.Core;

namespace PBV.Systems
{
    /// <summary>畜栏交互点：E 键打开养殖面板。</summary>
    [RequireComponent(typeof(Collider2D))]
    public class LivestockStation : MonoBehaviour, IInteractable
    {
        public string GetInteractPrompt() => "畜栏：养殖鸡鸭牛羊猪 (E)";

        public void OnInteract()
        {
            if (PBV.UI.LivestockUI.Instance != null)
                PBV.UI.LivestockUI.Instance.Open();
        }
    }
}
