using UnityEngine;
using PBV.Player;

namespace PBV.Systems
{
    /// <summary>
    /// 灵兽系统：领养猫后每日额外恢复20体力。
    /// 存档支持。
    /// </summary>
    public class PetSystem : MonoBehaviour
    {
        public static PetSystem Instance { get; private set; }

        public bool HasPet { get; private set; }
        public int DailyStaminaBonus => HasPet ? 20 : 0;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        private void Start()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnDayChanged += OnNewDay;
        }

        private void OnDestroy()
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.OnDayChanged -= OnNewDay;
        }

        private void OnNewDay()
        {
            if (HasPet && PlayerStats.Instance != null)
            {
                PlayerStats.Instance.Restore(DailyStaminaBonus);
                Debug.Log("[Pet] 猫陪伴你，额外恢复20体力");
            }
        }

        public void AdoptPet()
        {
            if (HasPet) return;
            HasPet = true;
            Debug.Log("[Pet] 领养了一只猫！");
        }

        public int Export() => HasPet ? 1 : 0;
        public void Import(int v)
        {
            HasPet = v == 1;
            if (HasPet)
            {
                var petGo = new GameObject("Pet");
                petGo.transform.position = Vector3.zero;
                petGo.AddComponent<PetController>();
            }
        }
    }
}
