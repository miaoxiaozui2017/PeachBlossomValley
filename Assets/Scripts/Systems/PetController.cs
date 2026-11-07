using UnityEngine;

namespace PBV.Systems
{
    /// <summary>
    /// 宠物跟随：猫跟随玩家移动（插值平滑）。
    /// 运行时生成色块贴图，不依赖美术资产。
    /// </summary>
    public class PetController : MonoBehaviour
    {
        private Transform _player;
        private SpriteRenderer _sr;

        private void Start()
        {
            var player = FindObjectOfType<PBV.Player.PlayerController>();
            if (player != null) _player = player.transform;

            _sr = GetComponent<SpriteRenderer>();
            if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sortingOrder = 9;
            _sr.color = new Color(0.9f, 0.6f, 0.2f);

            // 运行时生成猫色块贴图（小圆点）
            var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            var clear = new Color(0, 0, 0, 0);
            var body = new Color(0.9f, 0.6f, 0.2f);
            var ear = new Color(0.85f, 0.5f, 0.15f);
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                    tex.SetPixel(x, y, clear);
            // 身体（圆）
            for (int y = 8; y < 22; y++)
                for (int x = 10; x < 22; x++)
                {
                    float dx = x - 16, dy = y - 15;
                    if (dx * dx + dy * dy < 36) tex.SetPixel(x, y, body);
                }
            // 耳朵（三角）
            for (int y = 20; y < 28; y++)
                for (int x = 11; x < 15; x++)
                    if (y - 20 >= x - 14 && y - 20 <= 14 - x) tex.SetPixel(x, y, ear);
            for (int y = 20; y < 28; y++)
                for (int x = 18; x < 22; x++)
                    if (y - 20 >= 18 - x && y - 20 <= x - 11) tex.SetPixel(x, y, ear);
            // 眼睛
            tex.SetPixel(13, 16, Color.black);
            tex.SetPixel(19, 16, Color.black);
            tex.Apply();
            tex.filterMode = FilterMode.Point;

            _sr.sprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
        }

        private void Update()
        {
            if (_player == null) return;
            Vector3 target = _player.position + new Vector3(-1.2f, -0.5f, 0);
            transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * 3f);
        }
    }
}
