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
        #region private フィールド
        /// <summary>
        /// 分かっている席の表示名
        ///
        /// 席の情報が一時的に途切れても名前を出し続けるために覚えておく。
        /// </summary>
        static readonly string[] _seatNames =
            CreateEmptyNames();

        /// <summary>
        /// 既定は null になるため、空文字で埋めておく
        /// </summary>
        static string[] CreateEmptyNames()
        {
            var names = new string[Actor.Player.Constant.PlayerCountMax];
            for (int idx = 0; idx < names.Length; ++idx)
            {
                names[idx] = "";
            }

            return names;
        }
        #endregion

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
        /// その席の表示名を取得する
        ///
        /// ネットワーク対戦の人間の席だけ名前を持つ。
        /// オフラインや CPU 席では空を返す (呼び出し側が元の表示を使う)。
        ///
        /// 同じ台から 2 人以上参加している席には添字を付ける。
        /// 同じ名前が並ぶと、どちらが自分か分からないため。
        /// </summary>
        public static string GetSeatName(int seatIdx)
        {
            if (!NetworkSession.IsOnline)
            {
                _seatNames[seatIdx] = "";
                return "";
            }

            // 席の情報は画面の切り替わりで一時的に途切れる。
            // 分からない間は、前に分かっていた名前を返す。
            //
            // 覚える場所を画面の外に置くのが要点。
            // 画面ごとに作り直される場所に置くと、
            // 対戦の開始時に空から始まり、名前が出るまで待たされる。
            if (NetworkSeatTable.Instance == null)
            {
                return _seatNames[seatIdx];
            }

            var seats = NetworkSeatTable.Instance.Seats;
            var seat = seats[seatIdx];
            if (seat.IsEmpty)
            {
                // 席が空いたと分かったときは覚えた名前も捨てる
                _seatNames[seatIdx] = "";
                return "";
            }

            var name = seat.Nickname.ToString();
            if (name.Length == 0)
            {
                return _seatNames[seatIdx];
            }

            var sameOwnerCount = 0;
            for (int idx = 0; idx < seats.Length; ++idx)
            {
                if (!seats[idx].IsEmpty && seats[idx].Owner == seat.Owner)
                {
                    ++sameOwnerCount;
                }
            }

            _seatNames[seatIdx] = sameOwnerCount >= 2 ? $"{name}{seat.LocalSlot + 1}" : name;

            return _seatNames[seatIdx];
        }

        /// <summary>
        /// この台でそのキャラを動かしてよいか
        ///
        /// IsLocalSeat とは別物。
        /// CPU 席は誰も着いていないため、どの台から見ても「自分の席」にならない。
        /// そのままだと CPU のキャラを誰も動かさず、
        /// バブルを踏んでも吹き飛ばず、割れないままになる。
        ///
        /// CPU はホストが動かす。
        /// </summary>
        public static bool IsMovableHere(int seatIdx)
        {
            if (!NetworkSession.IsOnline)
            {
                return true;
            }

            if (IsLocalSeat(seatIdx))
            {
                return true;
            }

            if (NetworkSeatTable.Instance == null)
            {
                return false;
            }

            return NetworkSession.HasAuthority
                && NetworkSeatTable.Instance.IsCpuSeat(seatIdx);
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
