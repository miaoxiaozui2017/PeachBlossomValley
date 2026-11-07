using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using PBV.Player;

namespace PBV.Core
{
    /// <summary>
    /// 项目输入分发中枢：将 Input System 的底层事件转化为逻辑层 UnityEvents
    /// 并负责计算九宫格选择逻辑 (Smart Selection)
    /// 遵循 Docs/v0_1_0/03_PROGRAM_TODO.md
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        public static InputHandler Instance { get; private set; }

        [Header("Player Actions")]
        public UnityAction<Vector2> MoveEvent;
        public UnityAction InteractEvent;
        public UnityAction UseToolEvent;
        public UnityAction ToolCycleEvent; // Q 键循环切换工具
        public UnityAction GiftEvent;     // G 键送礼

        [Header("Selection Settings")]
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private bool _showDebugGizmos = true;

        /// <summary>
        /// 当前选中的网格坐标
        /// </summary>
        public Vector3Int SelectedGridPosition { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Update()
        {
            PollKeyboard();
            CalculateSelection();
        }

        /// <summary>
        /// 灰盒模式：无 PlayerInput 资产时直读键盘（WASD/E/Space）
        /// </summary>
        private void PollKeyboard()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return;

            Vector2 move = Vector2.zero;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) move.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) move.y -= 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) move.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) move.x += 1f;
            _lastMoveInput = move;
            MoveEvent?.Invoke(move);

            if (kb.eKey.wasPressedThisFrame) InteractEvent?.Invoke();
            if (kb.spaceKey.wasPressedThisFrame) UseToolEvent?.Invoke();
            if (kb.qKey.wasPressedThisFrame) ToolCycleEvent?.Invoke();
            if (kb.gKey.wasPressedThisFrame) GiftEvent?.Invoke();
        }

        private Vector2 _lastMoveInput;

        /// <summary>
        /// Input System "Move" 映射
        /// </summary>
        public void OnMove(InputValue value)
        {
            MoveEvent?.Invoke(value.Get<Vector2>());
        }

        /// <summary>
        /// Input System "Interact" (E) 映射
        /// </summary>
        public void OnInteract()
        {
            InteractEvent?.Invoke();
        }

        /// <summary>
        /// Input System "UseTool" (Space) 映射
        /// </summary>
        public void OnUseTool()
        {
            UseToolEvent?.Invoke();
            // 广播工具使用事件，具体的工具系统（如 Farming）会监听此事件并获取 SelectedGridPosition
        }

        /// <summary>
        /// 计算九宫格选择逻辑：根据玩家面向和位置，确定最合适的操作目标地块
        /// </summary>
        private void CalculateSelection()
        {
            if (_playerController == null)
            {
                _playerController = FindObjectOfType<PlayerController>();
                if (_playerController == null) return;
            }

            Vector2 playerPos = _playerController.transform.position;
            Vector2 lookDir = _playerController.GetLookDirection();

            // 基础逻辑：玩家中心位置 + 面向偏移 (1.0f)
            // 改进逻辑：根据 Input 向量微调，或者在九宫格内寻找优先级最高的目标（例如已有作物的地块）
            // 目前实现 V1：选取正前方一格
            
            // 偏移 0.5f 是为了从脚底中心算起
            Vector2 targetWorldPos = playerPos + lookDir * 1.0f + new Vector2(0, 0.5f); 

            SelectedGridPosition = new Vector3Int(
                Mathf.RoundToInt(targetWorldPos.x),
                Mathf.RoundToInt(targetWorldPos.y),
                0
            );
        }
        
        private void OnDrawGizmos()
        {
            if (_showDebugGizmos)
            {
                Gizmos.color = new Color(0, 1, 0, 0.5f);
                Gizmos.DrawWireCube(SelectedGridPosition, new Vector3(1, 1, 0));
            }
        }
    }
}
