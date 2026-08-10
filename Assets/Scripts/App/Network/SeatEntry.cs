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
            Nickname = default,
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

        /// <summary>
        /// この席のニックネーム
        ///
        /// 部屋のメンバー一覧のために、席と一緒に運ぶ。
        /// 別の名簿を作ると同期経路が一本増えるため、席に載せる。
        /// 同じピアの 2 人目以降は、表示側で「〇〇2」と添字を付ける。
        /// </summary>
        public NetworkString<_16> Nickname;
        #endregion
    }
}
