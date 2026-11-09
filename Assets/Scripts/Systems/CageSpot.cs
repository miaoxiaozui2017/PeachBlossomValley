using UnityEngine;
using PBV.Core;
using PBV.Inventory;

namespace PBV.Systems
{
    /// <summary>
    /// 笼箱交互点：E 键在水域放笼箱，委托 CageSystem 处理布置与收获。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CageSpot : MonoBehaviour, IInteractable
    {
        public string GetInteractPrompt()
        {
            if (CageSystem.Instance != null && CageSystem.Instance.IsBaited)
                return "笼箱已放（次日来收水产）(E)";
            return "放笼箱抓水产（需鱼饵/食材）(E)";
        }

        public void OnInteract()
        {
            if (CageSystem.Instance == null)
            {
                PBV.UI.HUD.Instance?.ShowNotice("笼箱系统未就绪 · 稍后再试");
                return;
            }
            if (CageSystem.Instance.IsBaited)
            {
                PBV.UI.HUD.Instance?.ShowNotice("笼箱已放在水里 · 睡一觉明早来收水产");
                return;
            }
            CageSystem.Instance.SetCage();
        }
    }
}
