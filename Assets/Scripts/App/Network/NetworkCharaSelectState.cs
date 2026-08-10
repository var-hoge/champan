using Fusion;
using UnityEngine;

namespace App.Network
{
    /// <summary>
    /// キャラセレクトの状態を全員で共有する
    ///
    /// キャラセレクトの Player はプレイヤーが Entry したときにローカルで有効化されるため、
    /// 座標を同期しようとしても「全員で共有された 1 つのオブジェクト」にならない。
    /// そこで座標ではなく、席ごとの状態そのものを配る。
    ///
    /// 各台は自分の席だけを書き込み、他の席は受け取った状態を表示する。
    /// </summary>
    public class NetworkCharaSelectState
        : NetworkBehaviour
    {
        #region 型定義
        public enum Phase : byte
        {
            /// <summary>
            /// エントリー待ち
            /// </summary>
            WaitingForEntry = 0,

            /// <summary>
            /// キャラ選択中
            /// </summary>
            InSelection = 1,

            /// <summary>
            /// キャラ決定後
            /// </summary>
            Selected = 2,
        }

        public struct SeatState
            : INetworkStruct
        {
            public byte PhaseValue;

            /// <summary>
            /// カーソルが指している位置 (キャラ ID ではなく並び順)
            /// </summary>
            public int SelectIdx;

            /// <summary>
            /// 決定後のキャラの位置
            ///
            /// キャラセレクトのキャラは Fusion の権威では扱わない。
            /// シーン上のオブジェクトが決定時に有効化される作りのため、
            /// 権威の受け渡しが安定せず、何度もキャラが固まる原因になった。
            /// カーソルと同じく状態として配る。
            /// </summary>
            public Vector2 CharaPos;

            /// <summary>
            /// キャラが左を向いているか
            /// </summary>
            public NetworkBool IsFacingLeft;

            /// <summary>
            /// キャラの大きさ
            ///
            /// 座標と同じ理由で、権威ではなく状態として配る。
            /// z は常に 1 のため運ばない。
            /// </summary>
            public Vector2 Scale;

            /// <summary>
            /// キャラの見た目の大きさ
            /// </summary>
            public Vector2 ViewScale;

            /// <summary>
            /// どの回のキャラセレクトで書かれた値か
            ///
            /// 画面に入り直しても、前回の値が残ったまま届く。
            /// 古いものを読むと、相手が決定済みのまま即座に扉へ入ってしまう。
            /// </summary>
            public byte Generation;
        }
        #endregion

        #region プロパティ
        public static NetworkCharaSelectState Instance { get; private set; }

        [Networked]
        [Capacity(4)]
        public NetworkArray<SeatState> Seats { get; }

        /// <summary>
        /// 今が何回目のキャラセレクトか
        ///
        /// 画面に入り直すたびにホストが進める。
        /// これと一致しない席の値は前回の残りなので読まない。
        /// </summary>
        [Networked]
        public byte Generation { get; set; }
        #endregion

        #region メソッド
        public Phase GetPhase(int seatIdx)
        {
            return (Phase)Seats[seatIdx].PhaseValue;
        }

        public int GetSelectIdx(int seatIdx)
        {
            return Seats[seatIdx].SelectIdx;
        }

        public Vector2 GetCharaPos(int seatIdx)
        {
            return Seats[seatIdx].CharaPos;
        }

        public bool IsFacingLeft(int seatIdx)
        {
            return Seats[seatIdx].IsFacingLeft;
        }

        /// <summary>
        /// その席の値が、今回のキャラセレクトで書かれたものか
        ///
        /// 前回の残りを読むと、相手が決定済みのまま即座に扉へ入ってしまう。
        /// </summary>
        public bool IsSeatFresh(int seatIdx)
        {
            return Seats[seatIdx].Generation == Generation;
        }

        public Vector2 GetScale(int seatIdx)
        {
            return Seats[seatIdx].Scale;
        }

        public Vector2 GetViewScale(int seatIdx)
        {
            return Seats[seatIdx].ViewScale;
        }

        /// <summary>
        /// 自分の席のキャラの大きさを全員に知らせる
        ///
        /// キャラセレクトのキャラはシーンに置かれているため、権威はホストが持つ。
        /// ゲスト側では FixedUpdateNetwork が動かず、そちらの経路では配れない。
        /// 座標や向きと同じく、席の状態として配る。
        /// </summary>
        public void PublishScale(int seatIdx, Vector2 scale, Vector2 viewScale)
        {
            if (!SeatInput.IsLocalSeat(seatIdx))
            {
                return;
            }

            RPC_SetSeatScale(seatIdx, scale, viewScale);
        }

        /// <summary>
        /// 自分の席の状態を全員に知らせる
        /// </summary>
        public void Publish(int seatIdx, Phase phase, int selectIdx, Vector2 charaPos, bool isFacingLeft)
        {
            if (!SeatInput.IsLocalSeat(seatIdx))
            {
                return;
            }

            RPC_SetSeatState(seatIdx, (byte)phase, selectIdx, charaPos, isFacingLeft);
        }

        /// <summary>
        /// キャラセレクトをやり直すときに使う
        /// </summary>
        public void ResetAll()
        {
            if (!HasStateAuthority)
            {
                return;
            }

            // 消すだけでは足りない。
            // 消したことが他の台に届く前に読まれてしまうため、
            // 回を進めて「前回の値」と区別できるようにする。
            Generation = (byte)(Generation + 1);

            for (int idx = 0; idx < Seats.Length; ++idx)
            {
                Seats.Set(idx, default);
            }
        }
        #endregion

        #region Fusion.NetworkBehaviour の実装
        public override void Spawned()
        {
            Instance = this;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
        #endregion

        #region private メソッド
        /// <summary>
        /// 状態の書き込みは MasterClient に集約する
        /// 他人の席を勝手に書き換えないよう、送り主が担当している席かを確認する
        /// </summary>
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        void RPC_SetSeatState(
            int seatIdx,
            byte phaseValue,
            int selectIdx,
            Vector2 charaPos,
            NetworkBool isFacingLeft,
            RpcInfo info = default)
        {
            if (NetworkSeatTable.Instance == null)
            {
                return;
            }

            // 自分自身が呼んだ場合、送り主が None になることがある
            var source = info.Source.IsRealPlayer ? info.Source : Runner.LocalPlayer;

            var owner = NetworkSeatTable.Instance.Seats[seatIdx].Owner;
            if (owner != source)
            {
                Debug.LogWarning(
                    $"[NetworkCharaSelectState] 担当外の席への書き込みを無視しました:"
                    + $" seatIdx={seatIdx} 送り主={source} 担当={owner}");
                return;
            }

            // 大きさは別に配られるため、ここで消さないように引き継ぐ
            var current = Seats[seatIdx];

            Seats.Set(seatIdx, new SeatState
            {
                PhaseValue = phaseValue,
                SelectIdx = selectIdx,
                CharaPos = charaPos,
                IsFacingLeft = isFacingLeft,
                Scale = current.Scale,
                ViewScale = current.ViewScale,

                // 今回のキャラセレクトで書いたことを残す
                Generation = Generation,
            });
        }

        /// <summary>
        /// 大きさの書き込み (ホスト上でのみ実行される)
        /// </summary>
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        void RPC_SetSeatScale(
            int seatIdx,
            Vector2 scale,
            Vector2 viewScale,
            RpcInfo info = default)
        {
            if (NetworkSeatTable.Instance == null)
            {
                return;
            }

            var source = info.Source.IsRealPlayer ? info.Source : Runner.LocalPlayer;

            var owner = NetworkSeatTable.Instance.Seats[seatIdx].Owner;
            if (owner != source)
            {
                return;
            }

            var current = Seats[seatIdx];
            current.Scale = scale;
            current.ViewScale = viewScale;
            Seats.Set(seatIdx, current);
        }
        #endregion
    }
}
