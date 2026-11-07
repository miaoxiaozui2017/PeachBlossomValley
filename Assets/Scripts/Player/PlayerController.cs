using System;
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

        /// <summary>当前附近检测到的可交互对象（null 表示无）</summary>
        public IInteractable CurrentNearby { get; private set; }
        /// <summary>附近可交互对象变化时触发（参数可为 null）</summary>
        public event Action<IInteractable> OnNearbyChanged;

        private float _detectTimer;
        private const float DetectInterval = 0.1f;
        
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
                InputHandler.Instance.GiftEvent += OnGiftInput;
            }
        }

        private void OnDestroy()
        {
            if (InputHandler.Instance != null)
            {
                InputHandler.Instance.MoveEvent -= OnMoveVector;
                InputHandler.Instance.InteractEvent -= OnInteractInput;
                InputHandler.Instance.GiftEvent -= OnGiftInput;
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

        /// <summary>G 键送礼：向附近 NPC 送当前选中槽位物品</summary>
        private void OnGiftInput()
        {
            if (CurrentNearby is PBV.NPC.NPCController npc)
            {
                npc.Gift();
            }
        }

        private void FixedUpdate()
        {
            ApplyMovement();
        }

        private void Update()
        {
            UpdateAnimations();

            // 周期检测附近可交互对象（供交互提示 UI 显示）
            _detectTimer += Time.deltaTime;
            if (_detectTimer >= DetectInterval)
            {
                _detectTimer = 0f;
                DetectNearby();
            }
        }

        private void DetectNearby()
        {
            Vector2 origin = _rb.position + _interactOffset;
            Vector2 interactPos = origin + _lastLookDir * 0.5f;
            var hits = Physics2D.OverlapCircleAll(interactPos, _interactRadius, _interactLayer);

            IInteractable found = null;
            foreach (var hit in hits)
            {
                var interactable = hit.GetComponent<IInteractable>();
                if (interactable != null) { found = interactable; break; }
            }

            if (found != CurrentNearby)
            {
                CurrentNearby = found;
                OnNearbyChanged?.Invoke(found);
            }
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
            // 优先使用持续检测的结果；检测未命中时再做一次即时检测（兜底）
            IInteractable target = CurrentNearby;
            if (target == null)
            {
                DetectNearby();
                target = CurrentNearby;
            }
            target?.OnInteract();
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
