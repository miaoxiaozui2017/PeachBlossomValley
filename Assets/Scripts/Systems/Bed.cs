using UnityEngine;
using PBV.Core;
using PBV.Player;

namespace PBV.Systems
{
    /// <summary>
    /// 床：E 键睡觉，恢复满体力并跳到次日。
    /// 替代手动按 N 跳日，有玩法意义（恢复体力）。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Bed : MonoBehaviour, IInteractable
    {
        public string GetInteractPrompt() => "床：睡觉（恢复体力，跳到次日）";

        public void OnInteract()
        {
            // 恢复满体力
            PlayerStats.Instance?.RestoreFull();

            // 跳到次日
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.ForceNextDay();
                Debug.Log("[Bed] 睡了一觉，体力恢复，新的一天开始了");
            }
        }
    }
}
