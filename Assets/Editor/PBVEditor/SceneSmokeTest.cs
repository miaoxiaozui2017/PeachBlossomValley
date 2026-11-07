using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PBVEditor
{
    /// <summary>
    /// 运行时冒烟测试入口：打开灰盒场景 -> 注入 SmokeTestRunner -> 进入 Play 模式
    /// Runner 自动验证核心循环并把结果写入 Logs/smoke_result.txt，然后退出编辑器进程。
    /// 用法：菜单 PBV -> 运行冒烟测试
    /// 命令行：-batchmode -executeMethod PBVEditor.SceneSmokeTest.RunFromCommandLine
    /// </summary>
    public static class SceneSmokeTest
    {
        private const string ScenePath = "Assets/Scenes/01_FarmGreyBox.unity";

        [MenuItem("PBV/运行冒烟测试")]
        public static void RunFromMenu() => Begin();

        public static void RunFromCommandLine() => Begin();

        private static void Begin()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath);

            var runnerGo = new GameObject("SmokeTestRunner");
            runnerGo.AddComponent<PBV.DebugTools.SmokeTestRunner>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorApplication.isPlaying = true;

            Debug.Log("[SmokeTest] 已进入 Play 模式，等待 Runner 执行检查...");
        }
    }
}
