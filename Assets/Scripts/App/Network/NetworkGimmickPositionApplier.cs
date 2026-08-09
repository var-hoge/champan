using TadaLib.Extension;
using UnityEngine;

namespace App.Network
{
    /// <summary>
    /// 配られた座標をギミックに反映する
    ///
    /// 反映する位相が重要になる。
    /// 乗り物の処理は次の順で動く。
    ///
    ///   IProcUpdate      : MoveInfoCtrl が「移動前の座標」を記録する
    ///   IProcMove        : ここで足場が動く
    ///   IProcPhysicsMove : TadaRigidbody2D が足場の移動差分を加えて乗客を運ぶ
    ///
    /// 描画時に座標を反映すると、この記録より後になるため移動差分が常にゼロになり、
    /// 足場だけが動いて乗っているキャラが取り残される (滑って落ちる)。
    /// そのため IProcMove で反映する。
    /// </summary>
    public class NetworkGimmickPositionApplier
        : TadaLib.ProcSystem.BaseProc
        , TadaLib.ProcSystem.IProcMove
    {
        #region TadaLib.ProcSystem.IProcMove の実装
        public void OnMove()
        {
            if (!NetworkSession.IsOnline)
            {
                return;
            }

            var binder = GetComponent<NetworkGimmickBinder>();
            if (binder == null || binder.Object == null || !binder.Object.IsValid)
            {
                return;
            }

            if (binder.Object.HasStateAuthority)
            {
                // 権威を持つ側は自分で動く
                return;
            }

            var targetPos = binder.SyncPosition;
            if (targetPos == Vector3.zero)
            {
                // まだ配られていない
                return;
            }

            var current = transform.position;

            // フレームレートに依存しない追従
            var rate = 1.0f - Mathf.Exp(-PosFollowSpeed * gameObject.DeltaTime());
            var next = Vector3.Lerp(current, targetPos, rate);

            // 離れすぎたら補間せずに合わせる
            if ((current - targetPos).sqrMagnitude > PosSnapDistanceSqr)
            {
                next = targetPos;
            }

            transform.position = next;
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
        #endregion
    }
}
