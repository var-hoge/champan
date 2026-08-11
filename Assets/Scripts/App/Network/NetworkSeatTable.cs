using Fusion;
using UnityEngine;

namespace App.Network
{
    /// <summary>
    /// 席テーブル
    ///
    /// ネットワーク全体で一意な席番号 (= 既存コードが使っている playerIdx) を
    /// MasterClient が割り当てて全員に配る。
    ///
    /// 席番号を各ピアがローカルに計算してはいけない。
    /// 「1 台にローカル 2 人 × 2 台」を許容するため、Fusion の PlayerRef と
    /// 席番号は 1 対 1 に対応しないため。
    /// </summary>
    public class NetworkSeatTable
        : NetworkBehaviour
    {
        #region プロパティ
        public static NetworkSeatTable Instance { get; private set; }

        /// <summary>
        /// 席の総数
        /// 既存のローカル対戦と同じ上限に合わせる
        /// </summary>
        public static int SeatCountMax => Actor.Player.Constant.PlayerCountMax;

        /// <summary>
        /// 席の割り当て状況
        /// 配列の添字がそのまま席番号 (playerIdx) になる
        /// </summary>
        [Networked]
        [Capacity(4)]
        public NetworkArray<SeatEntry> Seats { get; }
        #endregion

        #region メソッド
        /// <summary>
        /// 自分のピアのローカル枠に対応する席番号を取得する
        /// </summary>
        /// <returns>まだ割り当てられていないなら false</returns>
        public bool TryGetSeatIdx(int localSlot, out int seatIdx)
        {
            return TryGetSeatIdx(Runner.LocalPlayer, localSlot, out seatIdx);
        }

        /// <summary>
        /// 指定したピアのローカル枠に対応する席番号を取得する
        /// </summary>
        /// <returns>まだ割り当てられていないなら false</returns>
        public bool TryGetSeatIdx(PlayerRef owner, int localSlot, out int seatIdx)
        {
            for (int idx = 0; idx < Seats.Length; ++idx)
            {
                if (Seats[idx].Matches(owner, localSlot))
                {
                    seatIdx = idx;
                    return true;
                }
            }

            seatIdx = -1;
            return false;
        }

        /// <summary>
        /// 指定した席が自分のピアの担当かどうか
        /// </summary>
        public bool IsLocalSeat(int seatIdx)
        {
            return Seats[seatIdx].Owner == Runner.LocalPlayer;
        }

        /// <summary>
        /// CPU が担当する席かどうか
        ///
        /// 人間が着いていない席が CPU 席になる。
        /// 席テーブルは全員に複製されているので、どのピアでも同じ結論になり、
        /// これ自体を別途同期する必要はない。
        /// </summary>
        public bool IsCpuSeat(int seatIdx)
        {
            return Seats[seatIdx].IsEmpty;
        }

        /// <summary>
        /// 使用中の席数
        /// </summary>
        public int OccupiedSeatCount
        {
            get
            {
                var count = 0;
                for (int idx = 0; idx < Seats.Length; ++idx)
                {
                    if (!Seats[idx].IsEmpty)
                    {
                        ++count;
                    }
                }

                return count;
            }
        }

        /// <summary>
        /// 自分のピアの人数分の席を要求する
        /// 実際の割り当ては MasterClient が行うため、戻ってくるまでに 1 往復かかる
        /// </summary>
        public void RequestSeats(int localPlayerCount, string nickname)
        {
            for (int slot = 0; slot < localPlayerCount; ++slot)
            {
                RPC_RequestSeat(Runner.LocalPlayer, slot, nickname);
            }
        }

        /// <summary>
        /// 指定したピアの席をすべて解放する
        /// 退出時に MasterClient が呼ぶ
        /// </summary>
        public void ReleaseSeats(PlayerRef owner)
        {
            if (!HasStateAuthority)
            {
                return;
            }

            for (int idx = 0; idx < Seats.Length; ++idx)
            {
                if (Seats[idx].Owner == owner)
                {
                    Seats.Set(idx, SeatEntry.Empty);
                    Debug.Log($"[NetworkSeatTable] 席を解放しました: seatIdx={idx} owner={owner}");
                }
            }
        }
        #endregion

        #region Fusion.NetworkBehaviour の実装
        public override void Spawned()
        {
            Instance = this;

            // MasterClient だけがテーブルを初期化する
            if (!HasStateAuthority)
            {
                return;
            }

            for (int idx = 0; idx < Seats.Length; ++idx)
            {
                Seats.Set(idx, SeatEntry.Empty);
            }
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
        /// 席の割り当て要求
        /// 権威を持つ MasterClient 上でのみ実行される
        /// </summary>
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        void RPC_RequestSeat(PlayerRef owner, int localSlot, NetworkString<_16> nickname)
        {
            // 同じ要求が二重に届いても席を増やさない。
            // ただし名前は入れ直す。
            // 名前を持たずに割り当てられた後で届いた要求を捨てると、
            // その席の名前が空のまま埋まらなくなる。
            if (TryGetSeatIdx(owner, localSlot, out var assigned))
            {
                var current = Seats[assigned];
                if (current.Nickname != nickname)
                {
                    current.Nickname = nickname;
                    Seats.Set(assigned, current);

                    Debug.Log($"[NetworkSeatTable] 名前を入れ直しました: seatIdx={assigned} 名前={nickname}");
                }

                return;
            }

            for (int idx = 0; idx < Seats.Length; ++idx)
            {
                if (!Seats[idx].IsEmpty)
                {
                    continue;
                }

                Seats.Set(idx, new SeatEntry
                {
                    Owner = owner,
                    LocalSlot = localSlot,
                    Nickname = nickname,
                });

                Debug.Log($"[NetworkSeatTable] 席を割り当てました: seatIdx={idx} owner={owner} localSlot={localSlot}");
                return;
            }

            Debug.LogWarning($"[NetworkSeatTable] 空席がないため割り当てできません: owner={owner} localSlot={localSlot}");
        }
        #endregion
    }
}
