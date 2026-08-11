using Fusion;

namespace App.Network
{
    /// <summary>
    /// 部屋にいる 1 台分の情報
    ///
    /// 席とは別に持つ。
    /// 席は対戦に出る 4 枠だけだが、部屋にはそれより多くの人が居られる。
    /// 席が空くのを待っている人も、部屋のメンバーとして数える。
    /// </summary>
    public struct MemberEntry
        : INetworkStruct
    {
        #region プロパティ
        public bool IsEmpty => Owner == PlayerRef.None;
        #endregion

        #region メソッド
        public static MemberEntry Empty => new MemberEntry
        {
            Owner = PlayerRef.None,
            LocalPlayerCount = 0,
            Nickname = default,
        };
        #endregion

        #region フィールド
        /// <summary>
        /// この台
        /// </summary>
        public PlayerRef Owner;

        /// <summary>
        /// この台で遊ぶ人数
        ///
        /// 定員はここの合計で数える。
        /// Fusion が数えるのは台の数であって人数ではないため、自分で持つ。
        /// </summary>
        public int LocalPlayerCount;

        /// <summary>
        /// この台のニックネーム
        /// </summary>
        public NetworkString<_16> Nickname;
        #endregion
    }
}
