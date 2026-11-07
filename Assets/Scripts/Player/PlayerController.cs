using UnityEngine;
using PBV.Core;

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

        private void Start()
        {
            // 订阅 InputHandler 统一输入分发（灰盒直读键盘 / PlayerInput 双通道兼容）
            if (InputHandler.Instance != null)
            {
                InputHandler.Instance.MoveEvent += OnMoveVector;
                InputHandler.Instance.InteractEvent += OnInteractInput;
            }
        }

        private void OnDestroy()
        {
            if (InputHandler.Instance != null)
            {
                InputHandler.Instance.MoveEvent -= OnMoveVector;
                InputHandler.Instance.InteractEvent -= OnInteractInput;
            }
        }

        /// <summary>
        /// InputHandler.MoveEvent 回调
        /// </summary>
        private void OnMoveVector(Vector2 value)
        {
            _moveInput = value;

            if (_moveInput.sqrMagnitude > 0.01f)
            {
                _lastLookDir = _moveInput.normalized;
            }
        }

        /// <summary>
        /// InputHandler.InteractEvent 回调
        /// </summary>
        private void OnInteractInput()
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
                var interactable = hit.GetComponent<PBV.Core.IInteractable>();
                if (interactable != null)
                {
                    interactable.OnInteract();
                    break; // 只交互一个
                }
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
}
