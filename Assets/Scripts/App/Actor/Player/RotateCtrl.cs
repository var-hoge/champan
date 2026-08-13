using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.ActionStd;
using TadaLib.Input;
using UnityEngine.XR;

namespace App.Actor.Player
{
    /// <summary>
    /// 向き制御処理
    /// </summary>
    public class RotateCtrl
        : BaseProc
        , IProcPostMove
    {
        #region プロパティ
        /// <summary>
        /// 左を向いているか
        /// </summary>
        public bool IsFacingLeft => Mathf.Abs(Mathf.DeltaAngle(_mesh.transform.localEulerAngles.y, 180.0f)) < 90.0f;
        #endregion

        #region メソッド
        /// <summary>
        /// 向きを設定する
        ///
        /// 自分で向きを決められない場合 (ネットワーク越しの相手など) に使う。
        /// 速度から向きを決める処理は、速度が小さいときは現在の向きを保つため、
        /// ここで設定した向きは維持される。
        /// </summary>
        public void SetFacingLeft(bool isFacingLeft)
        {
            var rotVec3 = _mesh.transform.localEulerAngles;
            rotVec3.y = isFacingLeft ? 180.0f : 0.0f;

            _mesh.transform.localEulerAngles = rotVec3;
        }
        #endregion

        #region Monobehavior の実装
        /// <summary>
        /// 生成時の処理
        /// </summary>
        void Start()
        {
            UpdateRotate();
        }

        /// <summary>
        /// 移動後の更新処理
        /// </summary>
        public void OnPostMove()
        {
            UpdateRotate();
        }
        #endregion

        #region privateメソッド
        void UpdateRotate()
        {
            var dataHolder = GetComponent<DataHolder>();
            var rotVec3 = _mesh.transform.localEulerAngles;

            var velocityX = GetComponent<MoveCtrl>().Velocity.x;

            // 向きが変わるとみなす大きさ。
            // 速さで見るときは、歩き出しのわずかな速度で向きが暴れないようにする。
            var threshold = VelocityThreshold;

            // 試合開始前に方向を変更できるようにする
            if (GameSequenceManager.Instance != null)
            {
                if (GameSequenceManager.Instance.PhaseKind == GameSequenceManager.Phase.BeforeBattle)
                {
                    velocityX = InputUtil.GetAxis(gameObject).x;

                    // 入力は最大でも 1 のため、速さと同じ 1 で比べると
                    // 真横に倒し切ったときしか向きが変わらない。
                    // 少しでも斜めに入ると 1 に届かず、変えられなくなる。
                    //
                    // 他の場所で入力の遊びに使っている値に合わせる。
                    threshold = InputDeadZone;
                }
            }

            if (Mathf.Abs(velocityX) >= threshold)
            {
                rotVec3.y = velocityX < 0.0f ? 180.0f : 0.0f;
            }
            else if (dataHolder.PushedDir != 0)
            {
                rotVec3.y = dataHolder.PushedDir > 0 ? 0.0f : 180.0f;
                dataHolder.PushedDir = 0;
            }

            _mesh.transform.localEulerAngles = rotVec3;
            dataHolder.FaceVec = transform.right;
        }
        #endregion

        #region privateフィールド
        /// <summary>
        /// 速さで向きを決めるときのしきい値
        /// </summary>
        const float VelocityThreshold = 1.0f;

        /// <summary>
        /// 入力で向きを決めるときのしきい値 (試合開始前)
        /// PlayerInputProxy の遊びと同じ値
        /// </summary>
        const float InputDeadZone = 0.5f;

        [SerializeField]
        Transform _mesh;
        #endregion
    }
}