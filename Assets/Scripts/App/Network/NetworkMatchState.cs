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
        /// 落下に添える追加のバブルの生成をホストに要求する
        ///
        /// 落下の検知は、そのキャラを動かしている台でしか行えない。
        /// (ホスト側では他の台のキャラの物理を止めているため検知できない)
        ///
        /// 復帰バブル本体は落ちた本人の台が持つ。
        /// ホストに持たせると、落ちた本人がバブルを左右に動かせなくなる。
        /// </summary>
        public void RequestExtraBubbles(float spawnPointX)
        {
            RPC_RequestExtraBubbles(spawnPointX);
        }

        /// <summary>
        /// 復帰したことを他の台に伝える
        ///
        /// 割れる判断と復帰位置は、そのキャラを動かしている台が決める。
        /// ホストの許可を待たないため、落ちた本人の手応えが遅れない。
        ///
        /// バブル自身に載せて伝えることはできない。
        /// 伝えた直後にバブルは破棄されるため、届く前に対象が消えてしまう。
        /// </summary>
        /// <summary>
        /// 落下したことを他の台に伝える
        ///
        /// 落下を検知できるのは、そのキャラを動かしている台だけ。
        /// 他の台では物理を止めているため、いつ落ちたか分からず、
        /// オノマトペや音が出ないままになる。
        /// </summary>
        public void NotifyFall(int seatIdx, Vector3 position, Vector3 velocity)
        {
            if (Object == null || !Object.IsValid)
            {
                return;
            }

            RPC_NotifyFall(seatIdx, position.x, position.y, velocity.x, velocity.y);
        }

        /// <summary>
        /// ジャンプしたことを他の台に伝える
        ///
        /// ジャンプできるのは、そのキャラを動かしている台だけ。
        /// 他の台では入力も物理も止めているため状態が切り替わらず、
        /// ジャンプの音が鳴らないままになる。
        /// </summary>
        public void NotifyJump(int seatIdx)
        {
            if (Object == null || !Object.IsValid)
            {
                return;
            }

            RPC_NotifyJump(seatIdx);
        }

        /// <summary>
        /// 踏まれたことを、そのキャラを動かしている台に伝える
        ///
        /// 踏まれた動きは、そのキャラを動かしている台が行う。
        /// 他の台で動かしても、持ち主が配る座標で上書きされてしまう。
        /// </summary>
        public void NotifyStepedOn(int seatIdx)
        {
            if (Object == null || !Object.IsValid)
            {
                return;
            }

            RPC_NotifyStepedOn(seatIdx);
        }

        /// <summary>
        /// 試合が決まったことを全員に知らせる (ホストのみ)
        ///
        /// 勝敗はホストが決めるが、終了演出は各台で再生する。
        /// ホストでしか動かない破裂処理の中に置いていたため、
        /// ゲスト側では演出が出ないままリザルト画面に飛んでいた。
        /// </summary>
        public void NotifyGameFinish(int winnerSeatIdx)
        {
            if (Object == null || !Object.IsValid || !HasStateAuthority)
            {
                return;
            }

            RPC_NotifyGameFinish(winnerSeatIdx);
        }

        public void NotifyRespawn(int seatIdx, Vector3 position)
        {
            if (Object == null || !Object.IsValid)
            {
                return;
            }

            RPC_NotifyRespawn(seatIdx, position.x, position.y);
        }
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

            // @memo: 調査用。原因が判明したら削除する
            if (Runner.Tick % 120 == 0)
            {
                Debug.Log(
                    $"[同期調査] 権威={HasStateAuthority}"
                    + $" 配られているフェーズ={(GameSequenceManager.Phase)PhaseValue}"
                    + $" この台のフェーズ={sequenceManager.PhaseKind}");
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
        /// <summary>
        /// 追加のバブルの生成 (ホスト上でのみ実行される)
        /// </summary>
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        void RPC_RequestExtraBubbles(float spawnPointX)
        {
            var spawner = FindAnyObjectByType<Actor.Gimmick.RespawnBubble.PlayerSpawner>();
            if (spawner == null)
            {
                Debug.LogError("[NetworkMatchState] PlayerSpawner が見つかりません");
                return;
            }

            spawner.SpawnExtraBubbles(spawnPointX);
        }

        /// <summary>
        /// 復帰したという知らせを受ける
        ///
        /// 送った台では既に済んでいるため、そこでは呼ばない。
        /// </summary>
        /// <summary>
        /// 落下したという知らせを受けて、この台でも見た目と音を出す
        /// </summary>
        [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = false)]
        void RPC_NotifyFall(int seatIdx, float posX, float posY, float velX, float velY)
        {
            Actor.Gimmick.RespawnBubble.PlayerSpawner.PlayFallVisual(
                seatIdx,
                new Vector3(posX, posY, 0f),
                new Vector3(velX, velY, 0f));
        }

        /// <summary>
        /// ジャンプしたという知らせを受けて、この台でも音を鳴らす
        ///
        /// 見た目は拡縮と座標が配られるため、音だけでよい。
        /// </summary>
        [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = false)]
        void RPC_NotifyJump(int seatIdx)
        {
            // この台でもそのキャラを動かしているなら、既に自分で鳴らしている。
            // バブルに飛ばされたときのように、
            // 持ち主以外の台からジャンプが始まる経路があるため念のため見る。
            if (SeatInput.IsMovableHere(seatIdx))
            {
                return;
            }

            Actor.Player.State.StateJump.PlayJumpSe();
        }

        /// <summary>
        /// 踏まれたという知らせを受けて、自分の担当なら踏まれた動きをする
        /// </summary>
        [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = false)]
        void RPC_NotifyStepedOn(int seatIdx)
        {
            var hitColliders = FindObjectsByType<Actor.Player.Hit.HitCollider>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (var hitCollider in hitColliders)
            {
                if (hitCollider.PlayerIdx != seatIdx)
                {
                    continue;
                }

                // 表情は配っていないため、見ている台それぞれで出す
                hitCollider.PlayStepedOnEmotion();

                // 拡縮と動きは、そのキャラを動かしている台だけ。
                // 拡縮の結果は他の台へ配られる。
                if (SeatInput.IsMovableHere(seatIdx))
                {
                    hitCollider.PlayStepedOnSquash();
                    hitCollider.PlayStepedOnMove();
                }

                return;
            }
        }

        /// <summary>
        /// 試合が決まったという知らせを受けて、この台でも終了演出を再生する
        /// </summary>
        [Rpc(RpcSources.StateAuthority, RpcTargets.All, InvokeLocal = false)]
        void RPC_NotifyGameFinish(int winnerSeatIdx)
        {
            var crownBubble = Actor.Gimmick.Crown.Manager.Instance?.CrownBubble;
            if (crownBubble == null)
            {
                Debug.LogWarning("[NetworkMatchState] 王冠バブルが見つからないため終了演出を出せません");
                return;
            }

            crownBubble.PlayFinishStaging(winnerSeatIdx);
        }

        [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = false)]
        void RPC_NotifyRespawn(int seatIdx, float posX, float posY)
        {
            NetworkRespawnBubbleBinder.ApplyRespawn(seatIdx, new Vector3(posX, posY, 0f));
        }

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
