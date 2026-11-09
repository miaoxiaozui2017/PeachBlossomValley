using UnityEngine;
using PBV.Player;

namespace PBV.Systems
{
    /// <summary>
    /// 晕倒系统（单例）：体力归零时晕倒，自动传送到药婆处。
    /// 药婆收诊费 200 文清醒并恢复 50% 体力；钱不够则背债（欠账累计，下次有钱先还）。
    /// </summary>
    public class FaintSystem : MonoBehaviour
    {
        public static FaintSystem Instance { get; private set; }

        public const int ClinicFee = 200;         // 诊费（宋币/文）
        public int Debt { get; private set; }     // 欠账（背债累计）

        private bool _fainting;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        /// <summary>体力归零时调用：晕倒并送医婆</summary>
        public void OnExhausted()
        {
            if (_fainting) return;
            _fainting = true;

            PBV.UI.HUD.Instance?.ShowNotice("你体力耗尽，眼前一黑晕了过去……", 4f);
            Debug.Log("[Faint] 玩家晕倒，送往药婆");

            // 传送到药婆处
            TeleportToYaoPo();

            // 延迟结算（等传送完成、给玩家一点反馈时间）
            Invoke(nameof(ResolveClinic), 1.5f);
        }

        private void TeleportToYaoPo()
        {
            var player = FindObjectOfType<PlayerController>();
            if (player == null) return;
            var yaoPo = GameObject.Find("NPC_YaoPo");
            Vector2 target = yaoPo != null ? (Vector2)yaoPo.transform.position : new Vector2(8f, 6f);
            player.transform.position = new Vector3(target.x, target.y + 1f, 0f);
        }

        private void ResolveClinic()
        {
            var ps = PlayerStats.Instance;
            var wallet = PlayerWallet.Instance;

            int toPay = ClinicFee + Debt; // 先还旧债 + 本次诊费
            if (wallet != null && wallet.SongCoins >= toPay)
            {
                wallet.SpendSong(toPay);
                Debt = 0;
                PBV.UI.HUD.Instance?.ShowNotice($"药婆为你施针，收诊费 {ClinicFee} 文，你清醒了过来", 4f);
            }
            else
            {
                // 钱不够：有多少还多少，剩下的背债
                int paid = wallet != null ? wallet.SongCoins : 0;
                if (wallet != null) wallet.SpendSong(paid);
                Debt = toPay - paid;
                PBV.UI.HUD.Instance?.ShowNotice($"药婆为你施针（诊费 {ClinicFee} 文）。你钱不够，欠账 {Debt} 文，先记下了", 4f);
            }

            // 清醒：恢复 50% 体力
            if (ps != null) ps.Restore(Mathf.RoundToInt(ps.MaxStamina * 0.5f));

            _fainting = false;
        }

        /// <summary>还款（玩家主动还债，可在别处调用）</summary>
        public bool RepayDebt()
        {
            var wallet = PlayerWallet.Instance;
            if (Debt <= 0) return true;
            if (wallet == null) return false;
            int pay = Mathf.Min(Debt, wallet.SongCoins);
            if (pay <= 0) return false;
            wallet.SpendSong(pay);
            Debt -= pay;
            PBV.UI.HUD.Instance?.ShowNotice($"还债 {pay} 文，还剩 {Debt} 文");
            return true;
        }

        // ===== 存档 =====
        public void RestoreDebt(int debt) => Debt = Mathf.Max(0, debt);
    }
}
