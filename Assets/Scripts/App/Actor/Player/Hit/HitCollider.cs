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
        : BaseProc
        , IProcPostMove
        , IScaleChanger
    {
        #region プロパティ
        public bool IsEnabled => gameObject != null && gameObject.activeInHierarchy && GetComponent<DataHolder>().IsValidDummyPlayerPos is false;

        public Vector2 CenterPos => (Vector2)(_root.transform.position + _offset);

        public float Radius => _radius;

        public Vector2 Velocity => GetComponent<DataHolder>().Velocity;

        public int PlayerIdx => GetComponent<DataHolder>().PlayerIdx;
        #endregion

        #region メソッド
        public void ReflectHitResult(in Manager.HitResult result)
        {
            if (_posCache is null)
            {
                _posCache = transform.position;
            }

            if (result.IsButtomHit)
            {
                // ジャンプ
                State.StateJump.ChangeState(transform.gameObject, State.StateJump.JumpPowerKind.Spring);
            }

            static void ReflectLeftOrRightHitResult(HitCollider obj, in Manager.HitResult result, bool isLeft)
            {
                // 左右吹き飛び
                // あまり派手にしないようにする

                var rhsCenterPos = isLeft ? result.RhsCenterPositionLeftHit : result.RhsCenterPositionRightHit;
                var strength01 = TadaLib.Util.InterpUtil.Remap(Mathf.Abs(obj._posCache.Value.x - rhsCenterPos.x), 0.0f, obj.Radius * 2.0f, 1.0f, 0.0f);

                var dir = Mathf.Sign(obj._posCache.Value.x - rhsCenterPos.x);

                var addSpeedX = TadaLib.Util.InterpUtil.Linier(0.0f, 20.0f, strength01);

                var addVelX = addSpeedX * dir;

                obj.transform.position += Vector3.right * (addVelX * Time.deltaTime);
                obj._moveDiffX += addVelX * Time.deltaTime;
            }

            if (result.IsLeftHit)
            {
                ReflectLeftOrRightHitResult(this, result, isLeft: true);
            }

            if (result.IsRightHit)
            {
                ReflectLeftOrRightHitResult(this, result, isLeft: false);
            }

            if (result.IsTopHit)
            {
                // 踏まれた
                //
                // 自分の台のキャラなら、そのまま踏まれた動きをする。
                // 相手の台のキャラなら、動かす権利がないので相手に任せ、
                // 踏まれたことだけを伝える。
                // (相手の台では、そのキャラを踏んだ判定が出ないことがある)
                // 表情はどの台でも出す
                PlayStepedOnEmotion();

                // 拡縮と動きは、そのキャラを動かしている台だけが行う。
                // 他の台では、そこで再生された拡縮がそのまま配られてくる。
                // 各台で別々に再生すると、配られた値と食い違ってちらつく。
                if (!Network.NetworkSession.IsOnline
                    || Network.SeatInput.IsLocalSeat(PlayerIdx))
                {
                    PlayStepedOnSquash();
                    PlayStepedOnMove();
                }

                // 相手の台では、そのキャラが踏まれた判定が出ないことがある
                Network.NetworkMatchState.Instance?.NotifyStepedOn(PlayerIdx);
            }
        }

        /// <summary>
        /// 踏まれたときの動き
        ///
        /// 踏んだ側の跳ね返りは、踏んだ側の台が自分で行う。
        /// </summary>
        public void PlayStepedOnMove()
        {
            // 自分で判定した直後に知らせが届くと、二重に効いてしまう
            if (Time.time - _latestStepedOnMoveTime < StepedOnMoveIntervalSec)
            {
                return;
            }

            _latestStepedOnMoveTime = Time.time;

            GetComponent<MoveCtrl>().SetVelocityForceY(GetComponent<MoveCtrl>().Velocity.y - 10.0f);
        }

        /// <summary>
        /// 踏まれたときの表情
        ///
        /// 表情は配っていないため、見ている台それぞれで出す。
        /// </summary>
        public void PlayStepedOnEmotion()
        {
            GetComponent<EmotionCtrl>().NotifyStepedOn();
        }

        /// <summary>
        /// 踏まれたときの拡縮
        ///
        /// 再生するのは、そのキャラを動かしている台だけ。
        /// 結果は TotalScaleCtrl を通って他の台に配られる。
        /// </summary>
        public void PlayStepedOnSquash()
        {
            {
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

        #region privateフィールド
        /// <summary>
        /// 踏まれた動きを二重に効かせない間隔
        /// </summary>
        const float StepedOnMoveIntervalSec = 0.2f;

        float _latestStepedOnMoveTime = -10.0f;
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
            if (Cpu.CpuManager.Instance.IsCpu(PlayerIdx))
            {
                if (GameMatchManager.Instance.IsExistCpu is false)
                {
                    return;
                }
            }
            Manager.Instance.Register(this);
        }
        #endregion

        #region IPostMove の実装
        public void OnPostMove()
        {
            if (Mathf.Abs(_moveDiffX) > 0.01f)
            {
                GetComponent<DataHolder>().PushedDir = _moveDiffX > 0.0f ? 1 : -1;
            }
            _moveDiffX = 0.0f;
            _posCache = null;
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
        Vector2? _posCache;
        float _moveDiffX = 0.0f;
        #endregion

        #region privateメソッド
        #endregion
    }
}