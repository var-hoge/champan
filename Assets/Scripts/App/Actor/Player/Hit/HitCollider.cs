using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using DG.Tweening;

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
        public bool IsEnabled => GetComponent<DataHolder>().IsValidDummyPlayerPos is false;

        public Vector2 CenterPos => (Vector2)(_root.transform.position + _offset);

        public float Radius => _radius;

        public Vector2 Velocity => GetComponent<DataHolder>().Velocity;
        #endregion

        #region メソッド
        public void ReflectHitResult(in Manager.HitResult result)
        {
            if (result.IsButtomHit)
            {
                // ジャンプ
                State.StateJump.ChangeState(transform.gameObject, State.StateJump.JumpPowerKind.Spring);
            }
            else if (result.IsLeftHit || result.IsRightHit)
            {
                // 左右吹き飛び
                var speed = result.IsLeftHit ? result.LeftHitSpeed : result.RightHitSpeed;
                speed = TadaLib.Util.InterpUtil.Remap(speed, 0.0f, 20.0f, 12.0f, 24.0f);

                var velX = result.IsRightHit ? -speed : speed;
                GetComponent<MoveCtrl>().SetVelocityForceX(velX);
            }
            else if (result.IsTopHit)
            {
                // 踏まれた
                GetComponent<MoveCtrl>().SetVelocityForceY(GetComponent<MoveCtrl>().Velocity.y - 10.0f);

                if(_seq != null && _seq.IsActive() && !_seq.IsComplete())
                {
                    _seq.Kill();
                }
                _seq = DOTween.Sequence();
                _seq.Append(DOTween.To(
                    () => ViewScaleRate,
                    rate => ViewScaleRate = rate,
                    new Vector3(1.0f, 0.4f, 1.0f),
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