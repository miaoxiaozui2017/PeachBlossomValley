using System.Collections.Generic;
using UnityEngine;
using PBV.Inventory;

namespace PBV.Systems
{
    public class AchievementSystem : MonoBehaviour
    {
        public static AchievementSystem Instance { get; private set; }

        private readonly string[] _names = { "初出茅庐(锄地)", "初次收获", "渔夫(钓鱼)", "矿工(挖矿)", "造纸匠(制纸)", "跨时空旅人", "万元户" };
        private readonly bool[] _unlocked = new bool[7];
        private float _timer;

        public event System.Action<string> OnAchievementUnlocked;
        public IReadOnlyList<string> Names => _names;
        public bool IsUnlocked(int i) => i >= 0 && i < _unlocked.Length && _unlocked[i];
        public int UnlockedCount { get; private set; }

        private void Awake() { if (Instance == null) Instance = this; else { Destroy(gameObject); return; } }

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer < 2f) return;
            _timer = 0;
            CheckAll();
        }

        private void CheckAll()
        {
            if (!_unlocked[0]) TryUnlock(0, AnyTilled());
            if (!_unlocked[1]) TryUnlock(1, HasItem("item_radish") || HasItem("item_cabbage") || HasItem("item_tomato"));
            if (!_unlocked[2]) TryUnlock(2, HasItemPrefix("item_fish_"));
            if (!_unlocked[3]) TryUnlock(3, HasItemPrefix("item_ore_"));
            if (!_unlocked[4]) TryUnlock(4, HasItem("item_paper") || HasItem("item_pulp"));
            if (!_unlocked[5]) TryUnlock(5, WorldState.Instance != null && WorldState.Instance.Current == WorldType.Modern);
            if (!_unlocked[6]) TryUnlock(6, PlayerWallet.Instance != null && PlayerWallet.Instance.SongCoins >= 10000);
        }

        private bool AnyTilled() { var fg = FindObjectOfType<PBV.Farming.FarmGrid>(); return fg != null && fg.GetAllPlots().Count > 0; }
        private bool HasItem(string id) { var inv = InventorySystem.Instance; if (inv == null) return false; for (int i = 0; i < inv.Capacity; i++) { var s = inv.GetSlot(i); if (s != null && !s.IsEmpty && s.item.id == id) return true; } return false; }
        private bool HasItemPrefix(string prefix) { var inv = InventorySystem.Instance; if (inv == null) return false; for (int i = 0; i < inv.Capacity; i++) { var s = inv.GetSlot(i); if (s != null && !s.IsEmpty && s.item.id != null && s.item.id.StartsWith(prefix)) return true; } return false; }

        private void TryUnlock(int idx, bool condition)
        {
            if (!condition) return;
            _unlocked[idx] = true;
            UnlockedCount++;
            Debug.Log($"[Achievement] 解锁成就: {_names[idx]}");
            OnAchievementUnlocked?.Invoke(_names[idx]);
        }

        public int Export() { int v = 0; for (int i = 0; i < 7; i++) if (_unlocked[i]) v |= (1 << i); return v; }
        public void Import(int v) { for (int i = 0; i < 7; i++) { _unlocked[i] = (v & (1 << i)) != 0; if (_unlocked[i]) UnlockedCount++; } }
    }
}
