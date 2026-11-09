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
        public string GetInteractPrompt() => $"{StationLabel()}：打开制作配方 (E)";

        /// <summary>按物体名给出正确的台名，避免陶坊/药庐/木工坊都自称「造纸台」</summary>
        private string StationLabel()
        {
            switch (gameObject.name)
            {
                case "PotteryStation": return "陶坊";
                case "MedicineStation": return "药庐";
                case "CarpentryStation": return "木工坊";
                case "DyeingStation": return "染坊";
                default: return "造纸台";
            }
        }

        public void OnInteract()
        {
            if (CraftingUI.Instance != null)
            {
                CraftingUI.Instance.Toggle();
            }
        }
    }
}
