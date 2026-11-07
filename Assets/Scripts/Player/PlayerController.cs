using UnityEngine;
using UnityEngine.InputSystem;

namespace PBV.Player
{
    /// <summary>
    /// 玩家控制核心：处理 8 方向移动与基础状态切换
    /// 遵循 Docs/v0_1_0/03_PROGRAM_TODO.md
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float _moveSpeed = 5f;
        
        [Header("Interaction Settings")]
        [SerializeField] private float _interactRadius = 1.0f;
        [SerializeField] private LayerMask _interactLayer;
        [SerializeField] private Vector2 _interactOffset = new Vector2(0, 0.5f);

        [Header("Animation Settings")]
        [SerializeField] private Animator _animator;

        private Rigidbody2D _rb;
        private Vector2 _moveInput;
        private Vector2 _lastLookDir = Vector2.down;
        
        private static readonly int AnimDirX = Animator.StringToHash("DirX");
        private static readonly int AnimDirY = Animator.StringToHash("DirY");
        private static readonly int AnimIsMoving = Animator.StringToHash("IsMoving");
        private static readonly int AnimIsAction = Animator.StringToHash("IsAction");

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0;
            _rb.freezeRotation = true;
            
            if (_animator == null) _animator = GetComponent<Animator>();
        }

        /// <summary>
        /// 由 Input System 事件调用 (SendMessage 或 PlayerInput 组件)
        /// </summary>
        /// <param name="value">输入向量</param>
        public void OnMove(InputValue value)
        {
            _moveInput = value.Get<Vector2>();
            
            if (_moveInput.sqrMagnitude > 0.01f)
            {
                _lastLookDir = _moveInput.normalized;
                // 规范化到 8 方向 (或者 4 方向，取决于美术资源，这里保留原始向量供混合树使用)
            }
        }

        /// <summary>
        /// 由 Input System 事件调用 (SendMessage - Interact)
        /// </summary>
        public void OnInteract()
        {
            TryInteract();
        }

        private void FixedUpdate()
        {
            ApplyMovement();
        }

        private void Update()
        {
            UpdateAnimations();
        }

        private void ApplyMovement()
        {
            // 物理移动
            Vector2 targetPos = _rb.position + _moveInput * (_moveSpeed * Time.fixedDeltaTime);
            _rb.MovePosition(targetPos);
        }

        private void UpdateAnimations()
        {
            if (_animator == null) return;

            _animator.SetFloat(AnimDirX, _lastLookDir.x);
            _animator.SetFloat(AnimDirY, _lastLookDir.y);
            _animator.SetBool(AnimIsMoving, IsMoving());
        }

        private void TryInteract()
        {
            // 计算交互中心点 (玩家位置 + 偏移 + 面向 * 距离)
            Vector2 origin = _rb.position + _interactOffset;
            Vector2 interactPos = origin + _lastLookDir * 0.5f;

            Collider2D[] hits = Physics2D.OverlapCircleAll(interactPos, _interactRadius, _interactLayer);
            
            foreach (var hit in hits)
            {
                // 优先处理最近的或者特定组件
                // TODO: 接入实际的交互接口，例如 IInteractable
                Debug.Log($"[Player] Interacted with {hit.name}");
                hit.GetComponent<IInteractable>()?.OnInteract();
                break; // 只交互一个
            }
        }

        /// <summary>
        /// 获取九宫格逻辑下的目标地块坐标
        /// </summary>
        public Vector3Int GetTargetGridPosition()
        {
            Vector2 currentPos = _rb.position;
            // 简单逻辑：当前位置 + 面向方向 * 1格距离，然后取整
            Vector2 targetWorldPos = currentPos + _lastLookDir * 1.0f;
            
            return new Vector3Int(
                Mathf.RoundToInt(targetWorldPos.x),
                Mathf.RoundToInt(targetWorldPos.y),
                0
            );
        }

        /// <summary>
        /// 获取当前面向，用于九宫格交互逻辑
        /// </summary>
        public Vector2 GetLookDirection() => _lastLookDir;

        public bool IsMoving() => _moveInput.sqrMagnitude > 0.01f;
        
        private void OnDrawGizmosSelected()
        {
            if (_rb == null) return;
            Vector2 origin = _rb.position + _interactOffset;
            Vector2 interactPos = origin + _lastLookDir * 0.5f;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(interactPos, _interactRadius);
            
            // Draw Target Grid
            Vector3Int gridPos = GetTargetGridPosition();
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(gridPos, Vector3.one * 0.9f);
        }
    }
    
    // 临时接口，确保编译通过，实际应在 Core 或 Interaction 命名空间定义
    public interface IInteractable
    {
        void OnInteract();
    }
}
