using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Input;
using TadaLib.Extension;
using Ui;

namespace Scripts.Actor.Player
{
    /// <summary>
    /// Data保持
    /// </summary>
    public class DataHolder : BaseProc, IProcUpdate, IProcPostMove
    {
        #region プロパティ
        public float NoGroundDurationSec { get; private set; } = 0.0f;
        public bool IsJumpStartFrame { get; set; } = false;
        public bool IsDead { get; set; } = false;
        public Vector3 LastLandingPos { get; set; } = Vector3.zero;
        public Vector3 FaceVec { get; set; } = Vector3.right;
        public Vector2 Velocity { get; set; } = Vector3.zero;
        public int CharaIdx => CharaSelectUiManager.PlayerUseCharaIdList(PlayerIdx);
        public int PlayerIdx { get; set; } = 0;
        #endregion

        #region メソッド
        #endregion

        public void OnUpdate()
        {
        }

        #region TadaLib.ProcSystem.IProcPostMove の実装
        /// <summary>
        /// 移動後の更新処理
        /// </summary>
        public void OnPostMove()
        {
            if (GetComponent<TadaLib.ActionStd.TadaRigidbody2D>().IsGround)
            {
                NoGroundDurationSec = 0.0f;
                LastLandingPos = transform.position;
            }
            else
            {
                NoGroundDurationSec += gameObject.DeltaTime();
            }

            Velocity = GetComponent<MoveCtrl>().Velocity;

            IsJumpStartFrame = false;
        }
        #endregion

        #region privateメソッド
        #endregion

        #region privateフィールド
        #endregion
    }
}