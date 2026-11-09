using System;
using UnityEngine;
using PBV.Player;

namespace PBV.Systems
{
    /// <summary>
    /// 衣柜换装系统：玩家持有成衣/木家具等服饰后可换装。
    /// 简洁实现：换装 = 切换玩家 SpriteRenderer 的主色（灰盒阶段用颜色代表不同服装）。
    /// 换装槽：素衣(默认蓝)/成衣(靛蓝)/花丝衣(金)/夜行衣(玄)/道袍(灰)/罗裙(红)。
    /// </summary>
    public class WardrobeSystem : MonoBehaviour
    {
        public static WardrobeSystem Instance { get; private set; }

        /// <summary>服装索引</summary>
        public int OutfitIndex { get; private set; }

        public static readonly (string name, Color color)[] Outfits =
        {
            ("素衣",      new Color(0.30f, 0.45f, 0.75f)), // 默认蓝
            ("靛蓝成衣",  new Color(0.35f, 0.45f, 0.80f)), // 染织成衣
            ("花丝锦衣",  new Color(0.85f, 0.70f, 0.20f)), // 金
            ("夜行衣",    new Color(0.25f, 0.25f, 0.35f)), // 玄
            ("青灰道袍",  new Color(0.50f, 0.55f, 0.60f)), // 灰
            ("绛红罗裙",  new Color(0.80f, 0.30f, 0.35f)), // 红
        };

        public event Action<int> OnOutfitChanged;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        /// <summary>切换服装并应用到玩家 SpriteRenderer</summary>
        public void SetOutfit(int index)
        {
            index = Mathf.Clamp(index, 0, Outfits.Length - 1);
            OutfitIndex = index;
            ApplyToPlayer();
            OnOutfitChanged?.Invoke(index);
            Debug.Log($"[Wardrobe] 换上 {Outfits[index].name}");
        }

        private void ApplyToPlayer()
        {
            var player = FindObjectOfType<PlayerController>();
            if (player == null) return;
            var sr = player.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = Outfits[OutfitIndex].color;
        }

        // 存档
        public int Export() => OutfitIndex;
        public void Restore(int idx) => SetOutfit(idx);
    }
}
