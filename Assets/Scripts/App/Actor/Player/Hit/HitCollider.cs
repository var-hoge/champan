using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using DG.Tweening;
using App.Ui.Main;

namespace App.Actor.Player.Hit
{
    /// <summary>
    /// HitCollider
    /// </summary>
    public class HitCollider
        : MonoBehaviour
        , IScaleChanger
    {
        #region プロパティ
        public bool IsEnabled => gameObject != null && GetComponent<DataHolder>().IsValidDummyPlayerPos is false;

        public Vector2 CenterPos => (Vector2)(_root.transform.position + _offset);

        public float Radius => _radius;

        public Vector2 Velocity => GetComponent<DataHolder>().Velocity;

        public int PlayerIdx => GetComponent<DataHolder>().PlayerIdx;
        #endregion

        #region メソッド
        public void ReflectHitResult(in Manager.HitResult result)
        {
            if (result.IsButtomHit)
            {
                // ジャンプ
                State.StateJump.ChangeState(transform.gameObject, State.StateJump.JumpPowerKind.Spring);
            }
            
            if (result.IsLeftHit || result.IsRightHit)
            {
                // 左右吹き飛び
                // あまり派手にしないようにする

                var strength01 = TadaLib.Util.InterpUtil.Remap(Mathf.Abs(CenterPos.x - result.RhsCenterPosition.x), 0.0f, Radius * 2.0f, 1.0f, 0.0f);

                var dir = Mathf.Sign(CenterPos.x - result.RhsCenterPosition.x);

                var addSpeedX = TadaLib.Util.InterpUtil.Linier(0.0f, 20.0f, strength01);

                var addVelX = addSpeedX * dir;

                transform.position += Vector3.right * (addVelX * Time.deltaTime);
                GetComponent<DataHolder>().PushedDir = dir > 0.0f ? 1 : -1;
            }
            
            if (result.IsTopHit)
            {
                // 踏まれた
                GetComponent<MoveCtrl>().SetVelocityForceY(GetComponent<MoveCtrl>().Velocity.y - 10.0f);

                GetComponent<EmotionCtrl>().NotifyStepedOn();

                if (_seq != null && _seq.IsActive() && !_seq.IsComplete())
                {
                    _seq.Kill();
                }
                _seq = DOTween.Sequence();
                _seq.Append(DOTween.To(
                    () => ViewScaleRate,
                    rate => ViewScaleRate = rate,
                    new Vector3(1.0f, 0.5f, 1.0f),
                    0.3f
                    ).SetEase(Ease.OutElastic)
                    );
                _seq.Append(DOTween.To(
                    () => ViewScaleRate,
                    rate => ViewScaleRate = rate,
                    new Vector3(1.0f, 1.0f, 1.0f),
                    0.1f
                    ).SetEase(Ease.Linear)
                    );
            }
        }
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
            Manager.Instance.Register(this);
        }
        #endregion

        #region IScaleChangerの実装
        public Vector3 ScaleRate { private set; get; } = Vector3.one;

        public Vector3 ViewScaleRate { private set; get; } = Vector3.one;
        #endregion

        #region privateフィールド
        [SerializeField]
        Transform _root;

        [SerializeField]
        Vector3 _offset;

        [SerializeField]
        float _radius = 0.5f;

        Sequence? _seq = null;
        #endregion

        #region privateメソッド
        #endregion
    }
}