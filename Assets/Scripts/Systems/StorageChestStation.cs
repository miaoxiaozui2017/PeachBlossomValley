using UnityEngine;
using UnityEngine.UI;
using PBV.Core;

namespace PBV.Systems
{
    /// <summary>
    /// 场景中的单个储物箱交互点：挂在运行时生成的箱子 GameObject 上。
    /// E 键打开该箱子的存取面板（StorageChestUI）。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class StorageChestStation : MonoBehaviour, IInteractable
    {
        public string ChestUid { get; private set; }

        private SpriteRenderer _sr;

        public void Init(string uid)
        {
            ChestUid = uid;
            RefreshVisual();
        }

        public void RefreshVisual()
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr == null) return;
            var chest = StorageSystem.Instance != null ? StorageSystem.Instance.GetChest(ChestUid) : null;
            if (chest != null)
            {
                int ci = Mathf.Clamp(chest.colorIndex, 0, StorageSystem.ChestColors.Length - 1);
                _sr.color = StorageSystem.ChestColors[ci];
            }
        }

        /// <summary>更新头顶名牌文本（改名后调用）</summary>
        public void RefreshLabel(string text)
        {
            var label = transform.Find("ChestLabel");
            if (label == null) return;
            var t = label.GetComponent<Text>();
            if (t != null) t.text = text;
        }

        public string GetInteractPrompt()
        {
            var chest = StorageSystem.Instance != null ? StorageSystem.Instance.GetChest(ChestUid) : null;
            string name = chest != null ? chest.label : "储物箱";
            return $"{name}（E 打开）";
        }

        public void OnInteract()
        {
            if (PBV.UI.StorageChestUI.Instance != null)
                PBV.UI.StorageChestUI.Instance.Open(ChestUid);
        }
    }
}
