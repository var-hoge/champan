using Fusion;
using UnityEngine;

namespace App.Network
{
    /// <summary>
    /// ネットワーク対戦時に、権威を持たない側でギミックの自前シミュレーションを止める
    ///
    /// バブルは Rigidbody2D で動くため、リモート側でも物理が回ると
    /// NetworkTransform が配ってくる座標と二重に動いて位置がずれる。
    ///
    /// Player 用の NetworkPlayerBinder と同じ考え方だが、
    /// ギミックは権威が移らない (MasterClient が持ち続ける) 点が異なる。
    /// </summary>
    public class NetworkGimmickBinder
        : NetworkBehaviour
        , IStateAuthorityChanged
    {
        #region Fusion.NetworkBehaviour の実装
        public override void Spawned()
        {
            ApplyAuthorityState();
        }
        #endregion

        #region Fusion.IStateAuthorityChanged の実装
        public void StateAuthorityChanged()
        {
            ApplyAuthorityState();
        }
        #endregion

        #region private メソッド
        void ApplyAuthorityState()
        {
            var rigidbody = GetComponent<Rigidbody2D>();
            if (rigidbody == null)
            {
                return;
            }

            // 権威を持たない側では物理を止め、位置は NetworkTransform に任せる
            rigidbody.simulated = HasStateAuthority;
        }
        #endregion
    }
}
