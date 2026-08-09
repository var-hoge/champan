using TadaLib.Extension;
using UnityEngine;

namespace App.Network
{
    /// <summary>
    /// 配られた座標を Player に反映する
    ///
    /// ギミックと同じく、反映する位相を合わせる必要がある。
    /// 描画時に反映すると、その座標は当たり判定の計算より後になり、
    /// 他のキャラとの押し合いや踏み合いの判定が一フレームずれる。
    ///
    /// 詳細は NetworkGimmickPositionApplier のコメントを参照。
    /// </summary>
    public class NetworkPlayerPositionApplier
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

            // キャラセレクトの座標は NetworkCharaSelectState が配る
            if (!NetworkSession.IsInMatchScene(gameObject))
            {
                return;
            }

            var binder = GetComponent<NetworkPlayerBinder>();
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

            var rate = 1.0f - Mathf.Exp(-PosFollowSpeed * gameObject.DeltaTime());
            var next = Vector3.Lerp(current, targetPos, rate);

            // 離れすぎたら補間せずに合わせる (リスポーンなど)
            if ((current - targetPos).sqrMagnitude > PosSnapDistanceSqr)
            {
                next = targetPos;
            }

            transform.position = next;
        }
        #endregion

        #region private フィールド
        const float PosFollowSpeed = 25.0f;
        const float PosSnapDistanceSqr = 25.0f;
        #endregion
    }
}
