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

        /// <summary>
        /// 見た目の種類
        ///
        /// バブルの色は各台が乱数で選ぶため、そのままでは食い違う。
        /// 権威側が決めた値を配る。
        /// </summary>
        [Networked]
        public int VisualIdx { get; set; }

        /// <summary>
        /// 見た目の種類が決まっているか
        /// </summary>
        public bool HasVisualIdx => Object != null && Object.IsValid && VisualIdx > 0;

        /// <summary>
        /// 基準となる大きさ
        ///
        /// バブルは Start で乱数から大きさを決めるため、そのままでは台ごとに違う。
        ///
        /// 拡縮アニメの途中の値は配らない。
        /// 毎フレーム上書きすると、各台で再生している演出が打ち消されてしまう。
        /// </summary>
        [Networked]
        public Vector3 BaseScale { get; set; }
        #endregion

        #region Fusion.NetworkBehaviour の実装
        public override void Spawned()
        {
            if (HasStateAuthority && VisualIdx == 0)
            {
                // 0 は「未設定」を表すため 1 以上にする
                VisualIdx = Random.Range(1, 1000);
            }

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

        // @memo: 配られた座標の反映は NetworkGimmickPositionApplier が行う。
        //        乗り物の移動差分を正しく出すために、反映する位相が重要になる。
        #endregion

        #region メソッド
        /// <summary>
        /// 破裂をホストに要求する
        ///
        /// 破裂の見た目は要求した台がその場で出し、
        /// 実際の破棄やシールドの増減はホストが行う。
        /// ホストの返答を待ってから見た目を出すと、手応えが遅れて感じられるため。
        /// </summary>
        /// <summary>
        /// 基準となる大きさを配る (権威を持つ側のみ)
        ///
        /// 大きさは Start で乱数から決まるため、
        /// それより前に配ると既定値を配ってしまう。
        /// 決まった時点で呼ぶこと。
        /// </summary>
        public void PublishBaseScale(Vector3 scale)
        {
            if (Object == null || !Object.IsValid || !HasStateAuthority)
            {
                return;
            }

            BaseScale = scale;
        }

        public void RequestBurst(int playerIdx)
        {
            if (Object == null || !Object.IsValid)
            {
                return;
            }

            RPC_RequestBurst(playerIdx);
        }
        #endregion

        #region Fusion.IStateAuthorityChanged の実装
        public void StateAuthorityChanged()
        {
            ApplyAuthorityState();
        }
        #endregion

        #region private メソッド
        /// <summary>
        /// 破裂の要求を受けて、実際の処理を行う (ホスト上でのみ実行される)
        /// </summary>
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        void RPC_RequestBurst(int playerIdx)
        {
            var bubble = GetComponent<Actor.Gimmick.Bubble.Bubble>();
            if (bubble == null)
            {
                return;
            }

            // 二重に要求されても Bubble 側で無視される
            bubble.DoBurst(playerIdx);
        }

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

            // 種類が変わるときだけ代入する。
            // 2D 物理は種類を入れ直すと速度を失う。
            // 権威が移るたびに代入していると、動き出した直後に止まってしまう。
            if (HasStateAuthority)
            {
                if (rigidbody.bodyType != _originalBodyType)
                {
                    rigidbody.bodyType = _originalBodyType;
                }

                return;
            }

            if (rigidbody.bodyType == RigidbodyType2D.Kinematic)
            {
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
        RigidbodyType2D _originalBodyType = RigidbodyType2D.Dynamic;
        bool _hasOriginalBodyType = false;
        #endregion
    }
}
