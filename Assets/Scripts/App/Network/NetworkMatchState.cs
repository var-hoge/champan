using Fusion;
using UnityEngine;

namespace App.Network
{
    /// <summary>
    /// 対戦の進行と勝敗を全員で共有する
    ///
    /// 勝敗の判定はバブルの破裂を検知した側で行われるが、
    /// その判定は権威を持つ側でしか走らない。
    /// そのままではゲストがラウンドの終了を知れないため、結果を配る。
    ///
    /// フェーズ (試合前 / 試合中 / 試合後) も、
    /// 台ごとにずれるとキャラの操作可否が食い違うため揃える。
    /// </summary>
    public class NetworkMatchState
        : NetworkBehaviour
    {
        #region プロパティ
        public static NetworkMatchState Instance { get; private set; }

        /// <summary>
        /// 試合の進行状況
        /// </summary>
        [Networked]
        public byte PhaseValue { get; set; }

        /// <summary>
        /// 各席の勝ち点
        /// </summary>
        [Networked]
        [Capacity(4)]
        public NetworkArray<int> WinCounts { get; }

        /// <summary>
        /// 直近のラウンドの勝者
        /// </summary>
        [Networked]
        public int WinnerSeatIdx { get; set; }

        /// <summary>
        /// ラウンドが終わるたびに増える
        /// 値の変化でラウンド終了を検知する
        /// </summary>
        [Networked]
        [OnChangedRender(nameof(OnRoundEndSignalChanged))]
        public int RoundEndSignal { get; set; }
        #endregion

        #region メソッド
        /// <summary>
        /// ラウンドの結果を全員に知らせる (権威側のみ)
        /// </summary>
        public void PublishRoundEnd(int winnerSeatIdx)
        {
            if (!HasStateAuthority)
            {
                return;
            }

            WinnerSeatIdx = winnerSeatIdx;
            PublishWinCounts();

            // 値を変えることでラウンド終了を伝える
            RoundEndSignal += 1;
        }
        #endregion

        #region Fusion.NetworkBehaviour の実装
        public override void Spawned()
        {
            Instance = this;

            // 途中から合流した場合に備え、現在の値を反映しておく
            if (!HasStateAuthority)
            {
                ApplyWinCounts();
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public override void FixedUpdateNetwork()
        {
            var sequenceManager = GameSequenceManager.Instance;
            if (sequenceManager == null)
            {
                // 対戦シーン以外
                return;
            }

            if (HasStateAuthority)
            {
                PhaseValue = (byte)sequenceManager.PhaseKind;
                PublishWinCounts();
                return;
            }

            // フェーズがずれるとキャラの操作可否が食い違うため、常に合わせる
            sequenceManager.PhaseKind = (GameSequenceManager.Phase)PhaseValue;
            ApplyWinCounts();
        }
        #endregion

        #region private メソッド
        void PublishWinCounts()
        {
            var gameMatchManager = GameMatchManager.Instance;
            if (gameMatchManager == null)
            {
                return;
            }

            var counts = gameMatchManager.WinCounts;
            for (int idx = 0; idx < WinCounts.Length && idx < counts.Count; ++idx)
            {
                if (WinCounts[idx] != counts[idx])
                {
                    WinCounts.Set(idx, counts[idx]);
                }
            }
        }

        void ApplyWinCounts()
        {
            var gameMatchManager = GameMatchManager.Instance;
            if (gameMatchManager == null)
            {
                return;
            }

            var counts = new int[WinCounts.Length];
            for (int idx = 0; idx < WinCounts.Length; ++idx)
            {
                counts[idx] = WinCounts[idx];
            }

            gameMatchManager.ApplyNetworkWinCounts(counts);
        }

        /// <summary>
        /// ホストがラウンド終了を知らせてきたとき
        /// </summary>
        void OnRoundEndSignalChanged()
        {
            if (HasStateAuthority)
            {
                // 判定した側は既に演出を再生している
                return;
            }

            var sequenceManager = GameSequenceManager.Instance;
            if (sequenceManager == null)
            {
                return;
            }

            // 勝ち点を先に合わせてから演出を出す (ゲーム終了かラウンド継続かの判定に使われる)
            ApplyWinCounts();

            Debug.Log($"[NetworkMatchState] ホストからラウンド終了を受け取りました: 勝者={WinnerSeatIdx}");

            sequenceManager.PlayRoundEndSequence(WinnerSeatIdx);
        }
        #endregion
    }
}
