using UnityEngine;
using UnityEngine.SceneManagement;

namespace PBV.Systems
{
    /// <summary>
    /// 现代世界场景加载器（additive 叠加，不卸载桃花谷）。
    ///
    /// 设计取舍：
    /// - 桃花谷场景**永不卸载**，所有系统、地块、牲畜、储物箱状态原地保留，零迁移风险。
    /// - 现代做成独立场景 `02_Modern.unity` 叠加进来，美术可以单独进去画、单独迭代。
    /// - 若工程里没有该场景（或未加入 Build Settings），自动退回「运行时补建现代店」的旧行为，
    ///   所以没有生成场景时一切照旧，不会白屏。
    ///
    /// 执行顺序最靠前：保证 ShopWorldBootstrap.Start 补货架时，场景里的两家店已经就位。
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public class WorldSceneBootstrap : MonoBehaviour
    {
        public const string ModernSceneName = "02_Modern";
        public const string ModernRootName = "ModernRoot";

        public static WorldSceneBootstrap Instance { get; private set; }

        /// <summary>工程里是否真的有可加载的现代场景（决定走场景还是走运行时补建）</summary>
        public static bool HasModernScene
        {
            get
            {
                try { return Application.CanStreamedLevelBeLoaded(ModernSceneName); }
                catch { return false; }
            }
        }

        public static bool ModernSceneLoaded { get; private set; }

        private GameObject _modernRoot;
        private Vector3 _valleyPos;
        private bool _hasValleyPos;

        private static readonly Vector3 ModernSpawn = new Vector3(0f, 0f, 0f);

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void Start()
        {
            LoadModernSceneIfAvailable();
            if (WorldState.Instance != null)
                WorldState.Instance.OnWorldChanged += OnWorldChanged;
        }

        private void OnDestroy()
        {
            if (WorldState.Instance != null)
                WorldState.Instance.OnWorldChanged -= OnWorldChanged;
        }

        /// <summary>
        /// 穿越时挪一下玩家：现代街道在原点附近，谷内原地返回。
        /// 谷内场景没卸载，所以回程能精确还原到离开时的位置。
        /// </summary>
        private void OnWorldChanged(WorldType world)
        {
            if (!ModernSceneLoaded) return;
            // 读档恢复：玩家坐标由 SaveManager 从存档恢复，这里不能再挪
            if (WorldState.Instance != null && WorldState.Instance.LastChangeWasRestore) return;
            var pc = FindObjectOfType<PBV.Player.PlayerController>();
            if (pc == null) return;

            if (world == WorldType.Modern)
            {
                if (!_hasValleyPos)
                {
                    _valleyPos = pc.transform.position;
                    _hasValleyPos = true;
                }
                MovePlayer(pc, ModernSpawn);
            }
            else
            {
                // 回谷：优先还原离开时的位置；没有记录（例如读档直接进现代）就落在农场中心
                MovePlayer(pc, _hasValleyPos ? _valleyPos : Vector3.zero);
            }
        }

        private static void MovePlayer(PBV.Player.PlayerController pc, Vector3 pos)
        {
            var rb = pc.GetComponent<Rigidbody2D>();
            if (rb != null) rb.position = pos;
            pc.transform.position = pos;
        }

        private void LoadModernSceneIfAvailable()
        {
            if (!HasModernScene) return;

            var scene = SceneManager.GetSceneByName(ModernSceneName);
            if (!scene.isLoaded)
            {
                SceneManager.LoadScene(ModernSceneName, LoadSceneMode.Additive);
                scene = SceneManager.GetSceneByName(ModernSceneName);
            }
            if (!scene.isLoaded)
            {
                Debug.LogWarning($"[WorldScene] {ModernSceneName} 加载失败，退回运行时补建现代店");
                return;
            }

            ModernSceneLoaded = true;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root != null && root.name == ModernRootName) { _modernRoot = root; break; }
            }
            if (_modernRoot == null)
                Debug.LogWarning($"[WorldScene] {ModernSceneName} 里没找到 {ModernRootName} 根节点，现代内容可能不显示");
            else
                Debug.Log($"[WorldScene] 已叠加加载 {ModernSceneName}（美术可单独在此场景工作）");
        }

        /// <summary>桃花谷时关掉整个现代根节点；现代时打开。整个根节点开关，不影响任何逻辑组件。</summary>
        public void SetModernVisible(bool visible)
        {
            if (_modernRoot == null) return;
            if (_modernRoot.activeSelf != visible) _modernRoot.SetActive(visible);
        }
    }
}
