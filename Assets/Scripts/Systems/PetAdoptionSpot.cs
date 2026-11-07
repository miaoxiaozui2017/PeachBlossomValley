using UnityEngine;
using PBV.Core;

namespace PBV.Systems
{
    /// <summary>灵兽祠：E 键领养灵兽猫，领养后猫跟随玩家。</summary>
    [RequireComponent(typeof(Collider2D))]
    public class PetAdoptionSpot : MonoBehaviour, IInteractable
    {
        public string GetInteractPrompt()
        {
            if (PetSystem.Instance != null && PetSystem.Instance.HasPet) return "（已有灵兽陪伴）";
            return "灵兽祠：领养灵兽 (E)";
        }

        public void OnInteract()
        {
            if (PetSystem.Instance == null || PetSystem.Instance.HasPet) return;
            PetSystem.Instance.AdoptPet();

            // 创建猫物体
            var petGo = new GameObject("Pet");
            var player = FindObjectOfType<PBV.Player.PlayerController>();
            if (player != null) petGo.transform.position = player.transform.position + new Vector3(-1.2f, -0.5f, 0);
            petGo.AddComponent<PetController>();
        }
    }
}
