using UnityEngine;
using PBV.Core;

namespace PBV.Farming
{
    /// <summary>
    /// 地块选中高亮指示器：每帧跟随 InputHandler.SelectedGridPosition，
    /// 在游戏视图显示一个半透明高亮框，让玩家清楚知道当前要操作哪块地。
    /// 修复 playtest bug: 原 OnDrawGizmos 只在 Scene 视图显示，运行时看不到。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SelectionIndicator : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InputHandler _inputHandler;
        [SerializeField] private FarmGrid _farmGrid;

        [Header("Visual")]
        [Tooltip("高亮颜色（按当前可执行动作变色）")]
        [SerializeField] private Color _idleColor = new Color(1f, 1f, 1f, 0.35f);
        [SerializeField] private Color _tillableColor = new Color(1f, 0.85f, 0.2f, 0.55f);   // 黄：可锄地
        [SerializeField] private Color _plantableColor = new Color(0.4f, 0.9f, 1f, 0.55f);   // 青：可播种
        [SerializeField] private Color _waterableColor = new Color(0.35f, 0.6f, 1f, 0.55f);  // 蓝：可浇水
        [SerializeField] private Color _harvestColor = new Color(0.5f, 1f, 0.4f, 0.6f);      // 绿：可收获

        private SpriteRenderer _sr;
        private Vector3Int _lastGridPos = new Vector3Int(int.MinValue, 0, 0);

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _sr.sprite = CreateFrameSprite();
            _sr.sortingOrder = 50;
            _sr.color = _idleColor;
            _sr.drawMode = SpriteDrawMode.Simple;
        }

        private void Start()
        {
            if (_inputHandler == null) _inputHandler = InputHandler.Instance;
            if (_farmGrid == null) _farmGrid = FindObjectOfType<FarmGrid>();
        }

        private void LateUpdate()
        {
            if (_inputHandler == null) return;

            Vector3Int gridPos = _inputHandler.SelectedGridPosition;
            if (gridPos != _lastGridPos)
            {
                // Tilemap cell 中心 = cell + (0.5, 0.5, 0)（cell size 1）
                transform.position = new Vector3(gridPos.x + 0.5f, gridPos.y + 0.5f, 0f);
                _lastGridPos = gridPos;
            }

            _sr.color = GetActionColor(gridPos);
        }

        private Color GetActionColor(Vector3Int gridPos)
        {
            if (_farmGrid == null) return _idleColor;
            var plot = _farmGrid.GetPlot(gridPos);

            if (plot != null && plot.plantedCrop != null && plot.growthDays >= plot.plantedCrop.daysToMature)
                return _harvestColor;
            if (plot == null || plot.status == TileStatus.Empty)
                return _tillableColor;
            if (plot.plantedCrop == null)
            {
                // 有种子优先显示播种色
                var inv = PBV.Inventory.InventorySystem.Instance;
                return (inv != null && inv.FindSeedSlot() != null) ? _plantableColor : _waterableColor;
            }
            // 有作物未成熟
            return plot.status == TileStatus.Tilled ? _waterableColor : _idleColor;
        }

        /// <summary>运行时生成一个 32x32 白色边框 Sprite（中空，只画 2px 边）</summary>
        private static Sprite CreateFrameSprite()
        {
            const int size = 32;
            const int t = 3; // 边框厚度
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var clear = new Color(0, 0, 0, 0);
            var white = Color.white;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isEdge = x < t || x >= size - t || y < t || y >= size - t;
                    tex.SetPixel(x, y, isEdge ? white : clear);
                }
            }
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
        }
    }
}
