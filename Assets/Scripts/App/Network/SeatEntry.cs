using Fusion;

namespace App.Network
{
    /// <summary>
    /// 席テーブルの 1 席分の情報
    /// </summary>
    public struct SeatEntry
        : INetworkStruct
    {
        #region プロパティ
        /// <summary>
        /// 空席かどうか
        /// </summary>
        public bool IsEmpty => Owner == PlayerRef.None;
        #endregion

        #region メソッド
        public static SeatEntry Empty => new SeatEntry
        {
            Owner = PlayerRef.None,
            LocalSlot = 0,
        };

        /// <summary>
        /// 指定したピアのローカル枠と一致するか
        /// </summary>
        public bool Matches(PlayerRef owner, int localSlot)
        {
            return Owner == owner && LocalSlot == localSlot;
        }
        #endregion

        #region フィールド
        /// <summary>
        /// この席を担当するピア
        /// 空席なら PlayerRef.None
        /// </summary>
        public PlayerRef Owner;

        /// <summary>
        /// Owner のピアの中での何人目か (0 始まり)
        ///
        /// 「1 台にローカル 2 人」の 2 人目を表すための番号で、
        /// ゲームロジックが使う席番号 (playerIdx) とは別物。
        /// ローカルの入力順スロットに対応する。
        /// </summary>
        public int LocalSlot;
        #endregion
    }
}
