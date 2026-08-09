using Fusion;
using UnityEngine;

namespace App.Network
{
    /// <summary>
    /// ネットワーク対戦時に、ギミックの座標を配る
    ///
    /// NetworkTransform は描画時に自分の持つ座標で上書きするため、
    /// 物理や自作の移動処理と噛み合わない。
    /// Player と同じく、権威を持つ側が座標を配り、
    /// 持たない側は自前で追従する。
    /// </summary>
    public class NetworkGimmickBinder
        : NetworkBehaviour
        , IStateAuthorityChanged
    {
        #region プロパティ
        /// <summary>
        /// 権威を持つ側の座標
        /// </summary>
        [Networked]
        public Vector3 SyncPosition { get; set; }
        #endregion

        #region Fusion.NetworkBehaviour の実装
        public override void Spawned()
        {
            // 座標は自前で配るため、NetworkTransform は使わない
            var networkTransform = GetComponent<NetworkTransform>();
            if (networkTransform != null && networkTransform.enabled)
            {
                networkTransform.enabled = false;
            }

            ApplyAuthorityState();
        }

        public override void FixedUpdateNetwork()
        {
            SyncPosition = transform.position;
        }

        /// <summary>
        /// 権威を持たない側は、配られた座標へ追従する
        /// Render は権威の有無に関わらず呼ばれる
        /// </summary>
        public override void Render()
        {
            if (HasStateAuthority)
            {
                return;
            }

            if (SyncPosition == Vector3.zero)
            {
                // まだ配られていない
                return;
            }

            var current = transform.position;

            // フレームレートに依存しない追従
            var rate = 1.0f - Mathf.Exp(-PosFollowSpeed * Time.deltaTime);
            var next = Vector3.Lerp(current, SyncPosition, rate);

            // 離れすぎたら補間せずに合わせる
            if ((current - SyncPosition).sqrMagnitude > PosSnapDistanceSqr)
            {
                next = SyncPosition;
            }

            transform.position = next;
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

            if (!_hasOriginalBodyType)
            {
                _originalBodyType = rigidbody.bodyType;
                _hasOriginalBodyType = true;
            }

            if (HasStateAuthority)
            {
                rigidbody.bodyType = _originalBodyType;
                return;
            }

            // 権威を持たない側では物理で動かさず、配られた座標に従う。
            //
            // ここで simulated = false にしてはいけない。
            // コライダーごと物理世界から外れてしまい、
            // その台ではバブルに当たり判定が無くなって、
            // 自分のキャラが乗れず落下し続ける。
            rigidbody.bodyType = RigidbodyType2D.Kinematic;
        }
        #endregion

        #region private フィールド
        /// <summary>
        /// 配られた座標への追従の速さ
        /// </summary>
        const float PosFollowSpeed = 25.0f;

        /// <summary>
        /// これ以上離れていたら補間せずに合わせる
        /// </summary>
        const float PosSnapDistanceSqr = 25.0f;

        RigidbodyType2D _originalBodyType = RigidbodyType2D.Dynamic;
        bool _hasOriginalBodyType = false;
        #endregion
    }
}
