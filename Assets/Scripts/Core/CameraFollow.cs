using UnityEngine;
using PBV.Player;

namespace PBV.Core
{
    /// <summary>
    /// 相机平滑跟随玩家（v0.1.0 灰盒版，后续升级为 CameraController 带边界锁定）
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private float _smoothTime = 0.15f;

        private Vector3 _velocity;

        private void LateUpdate()
        {
            if (_target == null)
            {
                var player = FindObjectOfType<PlayerController>();
                if (player == null) return;
                _target = player.transform;
            }

            var targetPos = new Vector3(_target.position.x, _target.position.y, transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _velocity, _smoothTime);
        }
    }
}
