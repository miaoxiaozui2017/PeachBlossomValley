using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using PBV.Core;
using PBV.Inventory;
using PBV.Player;

namespace PBV.Systems
{
    /// <summary>
    /// 钓鱼点：E 键开始钓鱼小游戏。
    /// 随机等待→提示按Space→成功钓到鱼/失败。
    /// 消耗 5 体力。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class FishingSpot : MonoBehaviour, IInteractable
    {
        private bool _isFishing;
        private Text _promptText;
        private Canvas _canvas;

        /// <summary>全局：有任一钓鱼 QTE 进行中时，禁用鼠标左键农事操作</summary>
        public static bool IsFishingActive { get; private set; }

        public string GetInteractPrompt() => _isFishing ? "钓鱼中（Space 收竿）" : "钓鱼 (E)";

        private void Start()
        {
            CreatePromptUI();
        }

        public void OnInteract()
        {
            if (_isFishing) return;
            if (PlayerStats.Instance != null && PlayerStats.Instance.Stamina < 5)
            {
                ShowPrompt("体力不足！");
                StartCoroutine(HidePromptAfter(1.5f));
                return;
            }
            StartCoroutine(FishRoutine());
        }

        private IEnumerator FishRoutine()
        {
            _isFishing = true;
            IsFishingActive = true;
            PlayerStats.Instance?.TrySpend(5);

            // 阶段1：等待鱼上钩
            ShowPrompt("等待鱼上钩...");
            float waitTime = Random.Range(1f, 3f);
            yield return new WaitForSeconds(waitTime);

            // 阶段2：鱼上钩，等玩家按Space
            ShowPrompt("！快按 Space！");
            float timer = 0f;
            bool caught = false;
            while (timer < 1.5f)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    caught = true;
                    break;
                }
                timer += Time.deltaTime;
                yield return null;
            }

            // 阶段3：结果
            if (caught && FishingSystem.Instance != null)
            {
                var fish = FishingSystem.Instance.GetRandomFish();
                if (fish != null && InventorySystem.Instance != null)
                {
                    bool added = InventorySystem.Instance.AddItem(fish, 1);
                    if (added)
                    {
                        ShowPrompt($"钓到了 {fish.itemName}！");
                        Debug.Log($"[Fishing] 钓到 {fish.itemName}");
                        PBV.Systems.SkillSystem.Instance?.AddExp(PBV.Systems.SkillType.Fishing, 15);
                    }
                    else
                    {
                        ShowPrompt("背包满了！");
                    }
                }
                else
                {
                    ShowPrompt("钓到了...但鱼跑了！");
                }
            }
            else
            {
                ShowPrompt("鱼跑了...");
            }

            yield return new WaitForSeconds(2f);
            HidePrompt();
            _isFishing = false;
            IsFishingActive = false;
        }

        private void OnDestroy()
        {
            // 场景卸载/退出 Play 时清掉全局标志，避免卡住左键
            if (_isFishing) IsFishingActive = false;
        }

        private void CreatePromptUI()
        {
            var canvasGo = new GameObject("FishingCanvas");
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 35;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            var textGo = new GameObject("Prompt");
            textGo.transform.SetParent(canvasGo.transform, false);
            var rt = textGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(600, 60);
            rt.anchoredPosition = new Vector2(0, 100);
            _promptText = textGo.AddComponent<Text>();
            _promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _promptText.fontSize = 28;
            _promptText.fontStyle = FontStyle.Bold;
            _promptText.alignment = TextAnchor.MiddleCenter;
            _promptText.color = new Color(1f, 0.9f, 0.5f);
            _promptText.text = "";
            _promptText.raycastTarget = false;

            _canvas.gameObject.SetActive(false);
        }

        private void ShowPrompt(string text)
        {
            if (_promptText != null)
            {
                _promptText.text = text;
                _canvas.gameObject.SetActive(true);
            }
        }

        private void HidePrompt()
        {
            if (_canvas != null) _canvas.gameObject.SetActive(false);
        }

        private IEnumerator HidePromptAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            HidePrompt();
        }
    }
}
