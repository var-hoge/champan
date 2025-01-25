using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Input;

namespace Scripts.Actor.Player.State
{
    /// <summary>
    /// State処理
    /// </summary>
    [System.Serializable]
    public class StateFall : TadaLib.ActionStd.StateMachine.StateBase
    {
        #region static関数
        public static void ChangeState(GameObject obj)
        {
            var state = obj.GetComponent<TadaLib.ActionStd.StateMachine>().GetStateInstance<StateFall>();
            state.ChangeState(typeof(StateFall));
        }
        #endregion

        #region プロパティ
        #endregion

        #region StateMachine.StateBaseの実装
        // ステートが始まった時に呼ばれるメソッド
        public override void OnStart()
        {
            //obj.GetComponent<Animator>().SetBool("Fall", true);
        }

        // ステートが終了したときに呼ばれるメソッド
        public override void OnEnd()
        {
            //obj.GetComponent<Animator>().SetBool("Fall", false);
        }

        // 毎フレーム呼ばれる関数
        public override void OnUpdate()
        {
            var moveCtrl = obj.GetComponent<MoveCtrl>();

            if (obj.GetComponent<TadaLib.ActionStd.TadaRigidbody2D>().IsGround)
            {
                // 着地エフェクト
                if (_landEffectPrefab != null)
                {
                    // ジャンプの先行入力が入っていたらエフェクトは出さない(ジャンプエフェクトとかぶってしまうため)
                    if (!InputUtil.IsButtonDown(obj, ButtonCode.Jump, 0.1f - Time.unscaledDeltaTime))
                    {
                        //Effect.Util.Play(_landEffectPrefab, obj.transform.position);
                    }
                }

                if (Mathf.Abs(moveCtrl.Velocity.x) > 0.5f)
                {
                    StateRun.ChangeState(obj);
                    return;
                }

                StateIdle.ChangeState(obj);
            }

            // 落下直後ならジャンプできるかもしれない
            if (StateJump.TryChangeState(obj, StateJump.JumpPowerKind.Medium))
            {
                return;
            }

            // 下入力を押しているときは最高速度を大きくする
            // 高速落下対応
            var axisY = InputUtil.GetAxis(obj, AxisCode.Vertical);
            var maxSpeedRateY = Mathf.Lerp(1.0f, _maxSpeedRateYByDownInput, Mathf.Max(0.0f, -axisY));
            moveCtrl.MaxVelocityRateYState = maxSpeedRateY;
            // ただし、上限突破中は重力を弱くして徐々に加速するようにする
            var gravityRate = 1.0f;
            if (moveCtrl.Velocity.y < -moveCtrl.MaxVelocityIgnoreRate.y)
            {
                gravityRate = 0.5f;
            }
            moveCtrl.GravityRateState = gravityRate;
        }
        #endregion

        #region privateメソッド
        #endregion

        #region privateフィールド
        [SerializeField]
        float _maxSpeedRateYByDownInput = 1.5f;
        [SerializeField]
        ParticleSystem _landEffectPrefab;
        #endregion
    }
}