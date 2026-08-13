using Fusion;
using UnityEngine;

namespace App.Network
{
    /// <summary>
    /// リスポーンバブルを、各台で対応する Player に結び付ける
    ///
    /// 生成はホストだけが行うため、ホスト以外では対象のプレイヤーが設定されず、
    /// 復帰処理そのものが動かない (落ちたまま戻ってこない)。
    ///
    /// どの席のバブルかを配り、各台が自分のシーンにある同じ席の Player を結び付ける。
    /// </summary>
    public class NetworkRespawnBubbleBinder
        : NetworkBehaviour
    {
        #region プロパティ
        /// <summary>
        /// このバブルが復帰させる席
        /// </summary>
        [Networked]
        public int SeatIdx { get; set; }
        #endregion

        #region メソッド
        /// <summary>
        /// 席を設定する
        ///
        /// オフラインでは Networked なプロパティを扱えないため、何もしない。
        /// (オフラインは PlayerSpawner が直接 Player を結び付ける)
        /// </summary>
        /// <summary>
        /// 他の台が復帰したという知らせを受けて、この台の見た目を合わせる
        ///
        /// キャラの座標はここでは動かさない。
        /// 動かしている台が配る座標で、どのみち上書きされる。
        /// </summary>
        public static void ApplyRespawn(int seatIdx, Vector3 position)
        {
            var binders = FindObjectsByType<NetworkRespawnBubbleBinder>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (var binder in binders)
            {
                if (binder.SeatIdx != seatIdx)
                {
                    continue;
                }

                var target = binder.GetComponent<Actor.Gimmick.RespawnBubble.RespawnBubble>();
                if (target != null)
                {
                    target.transform.position = position;
                    target.PlayBurstVisual();
                }

                // 破棄できるのは、このバブルを持っている台だけ。
                //
                // 持ち主はホストとは限らない。
                // 復帰バブルは落ちた本人の台が生成して持つ。
                var isOwner = binder.Object != null
                    && binder.Object.IsValid
                    && binder.Object.HasStateAuthority;

                if (isOwner)
                {
                    NetworkSession.Despawn(binder.gameObject);
                }
                else
                {
                    binder.gameObject.SetActive(false);
                }

                return;
            }

            Debug.LogWarning($"[復帰] 対象の復帰用バブルが見つかりません: seatIdx={seatIdx}");
        }

        public void SetSeatIdx(int seatIdx)
        {
            // オフラインでは Networked なプロパティを扱えない
            // (オフラインは PlayerSpawner が直接 Player を結び付ける)
            if (!NetworkSession.IsOnline)
            {
                return;
            }

            try
            {
                SeatIdx = seatIdx;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[復帰用バブル] 席を設定できませんでした: seatIdx={seatIdx} {e.Message}");
            }
        }
        #endregion

        #region Fusion.NetworkBehaviour の実装
        public override void Spawned()
        {
            _isSpawned = true;

            TryBind();
        }

        public override void Render()
        {
            // Spawned より前に呼ばれることがある。
            // その状態では Networked なプロパティを読めない。
            if (!_isSpawned)
            {
                return;
            }

            // 席が配られるのが遅れることがあるため、結び付くまで試す
            TryBind();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _isSpawned = false;
        }
        #endregion

        #region private メソッド
        void TryBind()
        {
            if (_isBound)
            {
                return;
            }

            // オフラインでは PlayerSpawner が直接結び付けるため、ここでは何もしない
            if (!_isSpawned || Object == null || !Object.IsValid)
            {
                return;
            }

            var respawnBubble = GetComponent<Actor.Gimmick.RespawnBubble.RespawnBubble>();
            if (respawnBubble == null)
            {
                _isBound = true;
                return;
            }

            // 席が配られるより前に生成されることがあるため、見つかるまで待つ
            var player = FindPlayerBySeat(SeatIdx);
            if (player == null)
            {
                return;
            }

            _isBound = true;
            respawnBubble.Bind(player);
        }

        static GameObject FindPlayerBySeat(int seatIdx)
        {
            var binders = FindObjectsByType<NetworkPlayerBinder>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (var binder in binders)
            {
                if (binder.SeatIdx == seatIdx)
                {
                    return binder.gameObject;
                }
            }

            return null;
        }
        #endregion

        #region private フィールド
        bool _isBound = false;
        bool _isSpawned = false;
        #endregion
    }
}
