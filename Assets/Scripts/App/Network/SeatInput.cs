namespace App.Network
{
    /// <summary>
    /// 席番号から、この台の入力を引くための変換
    ///
    /// 席番号 (playerIdx) とローカルのコントローラ番号は一致しない。
    /// 「2 台目の席 2」は、その台のローカル 1 人目が操作する。
    /// 同じ変換を各所で書くと食い違うため、ここに集約する。
    /// </summary>
    public static class SeatInput
    {
        #region メソッド
        /// <summary>
        /// この台が担当する席かどうか
        /// オフラインでは常に true
        /// </summary>
        public static bool IsLocalSeat(int seatIdx)
        {
            if (!NetworkSession.IsOnline)
            {
                return true;
            }

            if (NetworkSeatTable.Instance == null)
            {
                // 席が配られるまでは、どの席も自分のものと見なさない
                return false;
            }

            return NetworkSeatTable.Instance.IsLocalSeat(seatIdx);
        }

        /// <summary>
        /// 席に対応するこの台のコントローラ番号を取得する
        /// 他の台が担当する席なら -1
        /// </summary>
        public static int GetLocalInputIdx(int seatIdx)
        {
            if (!NetworkSession.IsOnline)
            {
                return seatIdx;
            }

            if (!IsLocalSeat(seatIdx))
            {
                return -1;
            }

            return NetworkSeatTable.Instance.Seats[seatIdx].LocalSlot;
        }

        /// <summary>
        /// 席に対応するこの台の入力を取得する
        /// 他の台が担当する席なら null
        /// </summary>
        public static TadaLib.Input.PlayerInputProxy GetProxyOrNull(int seatIdx)
        {
            if (!NetworkSession.IsOnline)
            {
                return TadaLib.Input.PlayerInputManager.Instance.InputProxy(seatIdx);
            }

            if (!IsLocalSeat(seatIdx))
            {
                return null;
            }

            var localSlot = NetworkSeatTable.Instance.Seats[seatIdx].LocalSlot;

            return TadaLib.Input.PlayerInputManager.Instance.InputProxy(localSlot);
        }
        #endregion
    }
}
