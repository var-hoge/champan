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
        , TadaLib.ProcSystem.IProcUpdate
        , TadaLib.ProcSystem.IProcMove
        , TadaLib.ProcSystem.IProcPhysicsMove
        , TadaLib.ProcSystem.IProcPostMove
    {
        #region TadaLib.ProcSystem.IProcUpdate の実装
        /// <summary>
        /// 見た目の計算に使う元の値を、配られたもので埋める
        ///
        /// 権威を持たない側では MoveCtrl と TadaRigidbody2D を止めているため、
        /// 速度も接地状態も変化しない。
        /// そのままでは、そこから決まる拡縮アニメや表情、オノマトペが何も出ない。
        ///
        /// 演出を一つずつ配るのではなく元の値を配ることで、
        /// 各台が同じ計算をして同じ見た目になる。
        /// </summary>
        public void OnUpdate()
        {
            if (!NetworkSession.IsOnline)
            {
                return;
            }

            var binder = GetComponent<NetworkPlayerBinder>();
            if (binder == null || binder.Object == null || !binder.Object.IsValid)
            {
                return;
            }

            // キャラセレクトのキャラはシーンに置かれていて、権威はホストが持つ。
            // 権威の有無では「誰が動かしているか」を判断できないため、席で見る。
            if (!NetworkSession.IsInMatchScene(gameObject))
            {
                ApplyCharaSelectScale(binder.SeatIdx);
                return;
            }

            if (binder.Object.HasStateAuthority)
            {
                // 権威を持つ側は自分で計算している
                return;
            }

            var moveCtrl = GetComponent<Actor.Player.MoveCtrl>();
            if (moveCtrl != null)
            {
                moveCtrl.SetVelocityForce(binder.SyncVelocity);
            }

            var rigidbody = GetComponent<TadaLib.ActionStd.TadaRigidbody2D>();
            if (rigidbody != null)
            {
                rigidbody.SetIsGroundForcibly(binder.IsGrounded);
            }

            // 拡縮は元の値からは再現しきれない。
            // 踏まれたときの拡縮のように、時間で動く演出が混ざっているため、
            // 通り道の結果をそのまま映す。
            var totalScaleCtrl = GetComponentInChildren<TadaLib.ActionStd.TotalScaleCtrl>(true);
            if (totalScaleCtrl != null && binder.SyncScale != Vector2.zero)
            {
                totalScaleCtrl.SetScaleForcibly(
                    new Vector3(binder.SyncScale.x, binder.SyncScale.y, 1.0f),
                    new Vector3(binder.SyncViewScale.x, binder.SyncViewScale.y, 1.0f));
            }
        }
        #endregion

        #region TadaLib.ProcSystem.IProcPostMove の実装
        /// <summary>
        /// 配られた向きを反映する
        ///
        /// 自分のキャラの向きは RotateCtrl が IProcPostMove で決めている。
        /// 同じ位相で反映して、自分と相手で処理の流れをそろえる。
        /// </summary>
        public void OnPostMove()
        {
            if (!NetworkSession.IsOnline || !NetworkSession.IsInMatchScene(gameObject))
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
                // 権威を持つ側は RotateCtrl が決める
                return;
            }

            var rotateCtrl = GetComponent<Actor.Player.RotateCtrl>();
            if (rotateCtrl == null)
            {
                return;
            }

            rotateCtrl.SetFacingLeft(binder.IsFacingLeft);
        }
        #endregion

        #region private メソッド
        /// <summary>
        /// キャラセレクトの大きさをやりとりする
        ///
        /// 自分の席なら配り、他人の席なら配られたものを映す。
        /// </summary>
        void ApplyCharaSelectScale(int seatIdx)
        {
            var charaSelectState = NetworkCharaSelectState.Instance;
            if (charaSelectState == null)
            {
                return;
            }

            var totalScaleCtrl = GetComponentInChildren<TadaLib.ActionStd.TotalScaleCtrl>(true);
            if (totalScaleCtrl == null)
            {
                return;
            }

            if (SeatInput.IsMovableHere(seatIdx))
            {
                charaSelectState.PublishScale(
                    seatIdx,
                    totalScaleCtrl.CurrentScale,
                    totalScaleCtrl.CurrentViewScale);

                return;
            }

            var scale = charaSelectState.GetScale(seatIdx);
            if (scale == Vector2.zero)
            {
                // まだ配られていない
                return;
            }

            var viewScale = charaSelectState.GetViewScale(seatIdx);
            totalScaleCtrl.SetScaleForcibly(
                new Vector3(scale.x, scale.y, 1.0f),
                new Vector3(viewScale.x, viewScale.y, 1.0f));
        }
        #endregion

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

        #region TadaLib.ProcSystem.IProcPhysicsMove の実装
        /// <summary>
        /// 権威を持たない側でも、乗っている足場に自分を登録する
        ///
        /// 本来は TadaRigidbody2D が行うが、権威を持たない側では止めているため、
        /// 配られた情報をもとにここで登録する。
        /// 登録しないと、その台では足場が「踏まれていない」ことになり、
        /// バブルがはじけず、乗ったときのアニメも出ない。
        ///
        /// ローカルの登録と同じ位相で行う (MoveInfoCtrl は IProcUpdate で一覧を空にする)。
        /// </summary>
        public void OnPhysicsMove()
        {
            if (!NetworkSession.IsOnline || !NetworkSession.IsInMatchScene(gameObject))
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
                // 権威を持つ側は TadaRigidbody2D が登録する
                return;
            }

            var ridingObject = binder.RidingObject;
            if (ridingObject == null)
            {
                return;
            }

            // MoveInfoCtrl は子オブジェクトに付いていることがある
            var moveInfoCtrl = ridingObject.GetComponentInChildren<TadaLib.ActionStd.MoveInfoCtrl>(true);
            if (moveInfoCtrl == null)
            {
                return;
            }

            moveInfoCtrl.RegisterRidedFrame(gameObject);
        }
        #endregion

        #region private フィールド
        const float PosFollowSpeed = 25.0f;
        const float PosSnapDistanceSqr = 25.0f;
        #endregion
    }
}
