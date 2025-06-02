using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using TadaLib.Input;

namespace App.Cpu
{
    /// <summary>
    /// CpuInput
    /// </summary>
    public class CpuInput
        : BaseProc
        , IProcPostMove
        , TadaLib.Input.IInput
    {
        #region TadaLib.Input.IInput の実装
        public bool ActionEnabled { get; set; }

        // 入力状態をリセットする
        public void ResetInput()
        {

        }

        /// <summary>
        /// 指定したボタンが入力されたかを取得する
        /// </summary>
        /// <param name="code">ボタン</param>
        /// <param name="precedeSec">先行入力時間</param>
        /// <returns></returns>
        public bool GetButtonDown(ButtonCode code, float precedeSec)
        {
            if (_nextInput.pressJump && !_nextInputPrev.pressJump)
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// 指定したボタンが入力されているかを取得する
        /// </summary>
        /// <param name="code">ボタン</param>
        /// <param name="precedeSec">先行入力時間</param>
        /// <returns></returns>
        public bool GetButton(ButtonCode code, float precedeSec)
        {
            return _nextInput.pressJump;
        }

        /// <summary>
        /// 指定したボタンの入力が離されたかを取得する
        /// </summary>
        /// <param name="code">ボタン</param>
        /// <param name="precedeSec">先行入力時間</param>
        /// <returns></returns>
        public bool GetButtonUp(ButtonCode code, float precedeSec)
        {
            if (!_nextInput.pressJump && _nextInputPrev.pressJump)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 過去の入力フラグを全て立てる
        /// </summary>
        /// <param name="code"></param>
        public void ForceFlagOnHistory(ButtonCode code)
        {

        }

        /// <summary>
        /// 過去の入力フラグを全て降ろす
        /// </summary>
        /// <param name="code"></param>
        public void ForceFlagOffHistory(ButtonCode code)
        {

        }

        /// <summary>
        /// 指定したボタンが入力されているかを取得する
        /// </summary>
        /// <param name="code">ボタン</param>
        /// <returns></returns>
        public float GetAxis(AxisCode code)
        {
            return code switch
            {
                AxisCode.Horizontal => _nextInput.axis.x,
                AxisCode.Vertical => _nextInput.axis.y,
                _ => 0.0f
            };
        }
        #endregion

        #region プロパティ
        #endregion

        #region メソッド
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
            _jumpWaitTimer = new TadaLib.Util.Timer(JumpWaitTime);
            _jumppingTimer = new TadaLib.Util.Timer(0.2f);
            _nextInput = new InputData();
            _nextInputPrev = new InputData();
        }
        #endregion

        #region IProcPostMove の実装
        public void OnPostMove()
        {
            _nextInputPrev = _nextInput;
            _nextInput = CalcNextInput();
        }
        #endregion

        #region privateフィールド
        struct InputData
        {
            public bool pressJump;
            public Vector2 axis;
        }

        [SerializeField]
        Vector2 _jumpWaitDurationSec = new Vector2(0.1f, 0.6f);

        Vector2 _targetBubblePosition;
        bool _hasTargetBubble = false;

        TadaLib.Util.Timer _jumpWaitTimer;
        TadaLib.Util.Timer _jumppingTimer; // ジャンプ後に数秒間ジャンプボタンを押す

        InputData _nextInput;
        InputData _nextInputPrev;
        #endregion

        #region privateメソッド
        InputData CalcNextInput()
        {
            var playerIdx = GetComponent<Actor.Player.DataHolder>().PlayerIdx;
            var cpuViewData = CpuViewDataManager.Instance.GetCpuViewData(playerIdx);

            if (_hasTargetBubble)
            {
                // 目標のバブルが消滅したかどうかをチェックする
                var isExistTargetBubble = false;
                foreach (var bubblePos in cpuViewData.bubblePositions)
                {
                    if (Vector2.Distance(bubblePos, _targetBubblePosition) < 1.0f)
                    {
                        isExistTargetBubble = true;
                        break;
                    }
                }

                if (!isExistTargetBubble)
                {
                    _hasTargetBubble = false;
                }
            }

            // どのバブルを狙うか決める
            if (!_hasTargetBubble)
            {
                _targetBubblePosition = SelectTargetBubble(cpuViewData);
                _hasTargetBubble = true;
            }

            var nextInput = new InputData();

            // ジャンプ入力
            if (cpuViewData.isGround)
            {
                // ジャンプ継続
                if (!_jumppingTimer.IsTimout)
                {
                    _jumppingTimer.Advance(Time.deltaTime);
                    nextInput.pressJump = true;
                }
                else
                {
                    // ジャンプまで待つ
                    _jumpWaitTimer.Advance(Time.deltaTime);

                    if (_jumpWaitTimer.IsTimout)
                    {
                        nextInput.pressJump = true;
                        _jumpWaitTimer.TimeReset(JumpWaitTime);
                        _jumppingTimer.TimeReset();
                    }
                    else
                    {
                        nextInput.pressJump = false;
                    }
                }
            }
            else
            {
                // ジャンプは押しっぱなし
                nextInput.pressJump = true;
            }

            // 左右入力
            if (transform.position.x < _targetBubblePosition.x)
            {
                nextInput.axis.x = 1.0f;
            }
            else
            {
                nextInput.axis.x = -1.0f;
            }

            // TODO: シャボン玉に乗っているときの挙動を改善
            if (cpuViewData.isGround)
            {
                nextInput.axis.x *= 0.4f;

            }
            nextInput.axis.y = 0.0f;

            return nextInput;
        }

        Vector2 SelectTargetBubble(in CpuViewData cpuViewData)
        {
            float CalcReachHeightDiff(float distanceX, float velocityY, in Vector2 maxVelocity, float gravity)
            {
                // Bubble に辿り着くまでの時間。加速度が速いので常に MaxSpeed だと考える。
                var t = distanceX / maxVelocity.x;

                // Bubble にたどり着くまでに移動する量(y)
                // 最高速を考慮するのが難しいため、考慮しない
                var moveY = velocityY * t - gravity * t * t;

                return moveY;
            }

            // 良いバブルを狙う
            float CalcScore(float reachY, float crownDistance, bool isCrown)
            {
                var score = 0.0f;

                if (reachY <= Random.Range(0.0f, 2.0f))
                {
                    // 届くと判断した場合
                    score += 5.0f - reachY * 0.25f;

                    // crown なら最大得点
                    if (isCrown)
                    {
                        score += 50.0f;
                    }
                }

                // crown に近いものを目指す
                score += (20.0f - Mathf.Clamp(crownDistance, 0.0f, 20.0f)) * 0.5f;

                return score;
            }


            Vector2 targetBubble = Vector2.zero;
            float maxScore = 0.0f;
            foreach (var bubblePos in cpuViewData.bubblePositions)
            {
                var velocityY = cpuViewData.velocity.y;
                if (cpuViewData.isGround)
                {
                    // 地面にいるならジャンプ力を Y 速度として扱う
                    velocityY = cpuViewData.jumpPower;
                }

                var diff = bubblePos - (Vector2)transform.position;
                var crownDiff = cpuViewData.crownPosition - bubblePos;
                var isCrown = crownDiff.sqrMagnitude < 0.1f;

                var reachY = CalcReachHeightDiff(diff.x, velocityY, cpuViewData.maxVelocity, cpuViewData.gravity) - diff.y;
                var score = CalcScore(reachY, crownDiff.magnitude, isCrown);

                if (score > maxScore)
                {
                    targetBubble = bubblePos;
                    maxScore = score;
                }
            }

            return targetBubble;
        }

        float JumpWaitTime => UnityEngine.Random.Range(_jumpWaitDurationSec.x, _jumpWaitDurationSec.y);
        #endregion

        #region Monobehaviorの実装
        void OnDrawGizmos()
        {
#if UNITY_EDITOR
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(_targetBubblePosition, 0.2f);
#endif
        }
        #endregion
    }
}