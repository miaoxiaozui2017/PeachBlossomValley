using UnityEngine;
using PBV.Core;

namespace PBV.Systems
{
    /// <summary>
    /// 房屋入口交互点：按 E 进入屋内，打开「屋内装修」面板。
    /// 房屋等级决定装修格子数（见 BuildSystem）。储物箱已独立（StorageSystem）。
    /// </summary>
    public class HouseStation : MonoBehaviour, IInteractable
    {
        public string GetInteractPrompt() => "房屋：进入屋内装修 (E)";

        public void OnInteract()
        {
            if (PBV.UI.HouseInteriorUI.Instance != null)
                PBV.UI.HouseInteriorUI.Instance.Open();
        }
    }
}
