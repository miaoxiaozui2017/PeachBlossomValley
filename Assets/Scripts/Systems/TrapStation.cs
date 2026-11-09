using UnityEngine;
using PBV.Core;
using PBV.UI;

namespace PBV.Systems
{
    /// <summary>陷阱交互物体：E 键布置陷阱。</summary>
    [RequireComponent(typeof(Collider2D))]
    public class TrapStation : MonoBehaviour, IInteractable
    {
        public string GetInteractPrompt()
            => TrapSystem.Instance != null && TrapSystem.Instance.IsBaited
                ? "陷阱：已布饵，静候猎物 (E)"
                : "陷阱：布置毒饵捉野味 (E)";

        public void OnInteract()
        {
            if (TrapSystem.Instance == null) return;
            if (TrapSystem.Instance.IsBaited)
            {
                PBV.UI.HUD.Instance?.ShowNotice($"陷阱已布置（第 {TrapSystem.Instance.BaitedDays} 天），静候野味");
                return;
            }
            TrapSystem.Instance.SetTrap();
        }
    }
}
