using UnityEngine;

namespace App.Ui.CharaSelect
{
    /// <summary>
    /// ネットワーク対戦のエントリーを受け付ける
    ///
    /// 席は部屋に入った時点では持たず、ここで押されたときに初めて要求する。
    /// 部屋には席の数より多くの人が居られるため、
    /// 入った順に席を配ると、後から入った人が永久に入れなくなる。
    ///
    /// 席が無い状態で押すことになるので、
    /// 「この席を担当するコントローラ」を見ることができない。
    /// 席を持っていないコントローラをすべて見る。
    /// </summary>
    public class NetworkEntryWatcher
        : MonoBehaviour
    {
        #region MonoBehaviour の実装
        void Update()
        {
            if (!Network.NetworkSession.IsOnline)
            {
                return;
            }

            var seatTable = Network.NetworkSeatTable.Instance;
            if (seatTable == null)
            {
                return;
            }

            var launcher = Network.NetworkGameLauncher.Instance;
            if (launcher == null)
            {
                return;
            }

            for (int localSlot = 0; localSlot < launcher.LocalPlayerCount; ++localSlot)
            {
                // 既に席を持っているなら、その席の枠が受け持つ
                if (seatTable.HasSeat(localSlot))
                {
                    continue;
                }

                var inputProxy = TadaLib.Input.PlayerInputManager.Instance.InputProxy(localSlot);
                if (inputProxy == null)
                {
                    continue;
                }

                if (!inputProxy.IsPressedTrigger(TadaLib.Input.ButtonCode.Action))
                {
                    continue;
                }

                // 空きが無ければ何も起きない。
                // 席が付いたかどうかは、席テーブルを見て各自が判断する。
                seatTable.RequestSeat(localSlot, launcher.Nickname);
            }
        }
        #endregion
    }
}
