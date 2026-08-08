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
        }
        #endregion

        #region プロパティ
        public static NetworkCharaSelectState Instance { get; private set; }

        [Networked]
        [Capacity(4)]
        public NetworkArray<SeatState> Seats { get; }
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

        /// <summary>
        /// 自分の席の状態を全員に知らせる
        /// </summary>
        public void Publish(int seatIdx, Phase phase, int selectIdx, Vector2 charaPos)
        {
            if (!SeatInput.IsLocalSeat(seatIdx))
            {
                return;
            }

            RPC_SetSeatState(seatIdx, (byte)phase, selectIdx, charaPos);
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
        void RPC_SetSeatState(int seatIdx, byte phaseValue, int selectIdx, Vector2 charaPos, RpcInfo info = default)
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

            Seats.Set(seatIdx, new SeatState
            {
                PhaseValue = phaseValue,
                SelectIdx = selectIdx,
                CharaPos = charaPos,
            });
        }
        #endregion
    }
}
