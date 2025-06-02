using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Input;
using TadaLib.Extension;
using TadaLib.Dbg;

namespace App.Actor.Player
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
        public int CharaIdx => Ui.CharaSelect.CharaSelectUiManager.PlayerUseCharaIdList(PlayerIdx);
        public int PlayerIdx { get; set; } = 0;
        public Vector3 DummyPlayerPos { get; set; } = Vector3.zero;
        public bool IsValidDummyPlayerPos { get; set; } = false;
        public Vector2 MaxVelocity { get; set; } = Vector2.zero;
        public float JumpPower { get; set; } = 10.0f;
        public float Gravity { get; set; } = -10.0f;

        //public Bubble LastLandingBubble { get; set; } = null;
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
            //Bubble ridingBubble = null;
            //var ridingMover = GetComponent<TadaLib.ActionStd.TadaRigidbody2D>().RidingMover;
            //if (ridingMover != null)
            //{
            //    ridingMover.TryGetComponent<Bubble>(out ridingBubble);
            //}
            //LastLandingBubble = ridingBubble;

            if (GetComponent<TadaLib.ActionStd.TadaRigidbody2D>().IsGround)
            {
                NoGroundDurationSec = 0.0f;
                LastLandingPos = transform.position;
            }
            else
            {
                NoGroundDurationSec += gameObject.DeltaTime();
            }

            var moveCtrl = GetComponent<MoveCtrl>();
            Velocity = moveCtrl.Velocity;
            MaxVelocity = moveCtrl.MaxVelocity;
            Gravity = moveCtrl.Gravity;

            IsJumpStartFrame = false;

        }
        #endregion

        #region privateメソッド
        void Start()
        {
#if UNITY_EDITOR
            if (PlayerIdx == 0)
            {
                var elem = DebugTextManager.Display(this, 1);
                elem.AddRemoveTrigger(this);
            }
#endif
        }

        public override string ToString()
        {
            return $"IsGround: {GetComponent<TadaLib.ActionStd.TadaRigidbody2D>().IsGround}\nState: {GetComponent<TadaLib.ActionStd.StateMachine>().CurrentStateName}";
        }
        #endregion

        #region privateフィールド
        #endregion
    }
}