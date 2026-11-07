using UnityEngine;
using PBV.Core;
using PBV.UI;

namespace PBV.Systems
{
    /// <summary>
    /// 制作台：场景交互物体，E 键打开制作 UI。
    /// 挂载要求：Collider2D (Trigger, Interact Layer)
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CraftingStation : MonoBehaviour, IInteractable
    {
        public string GetInteractPrompt() => "造纸台：打开制作";

        public void OnInteract()
        {
            if (CraftingUI.Instance != null)
            {
                CraftingUI.Instance.Toggle();
            }
        }
    }
}
