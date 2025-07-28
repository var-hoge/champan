using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;

namespace App.Actor.Player.Hit
{
    /// <summary>
    /// Manager
    /// </summary>
    public class Manager
        : BaseManagerProc<Manager>
    {
        #region 定義
        public struct HitResult
        {
            public bool IsLeftHit;
            public float LeftHitSpeed;

            public bool IsRightHit;
            public float RightHitSpeed;

            public bool IsButtomHit;
            public float ButtomHitSpeed;

            public bool IsTopHit;
            public float TopHitSpeed;

            public Vector2 RhsCenterPosition;
        }
        #endregion

        #region プロパティ
        #endregion

        #region メソッド
        public void Register(HitCollider obj)
        {
            _colliders.Add(obj);
        }
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
            _history = new List<List<float>>();
            for (int idx = 0; idx < ColliderCountMax; ++idx)
            {
                _history.Add(new List<float>());
                for (int j = 0; j < ColliderCountMax; ++j)
                {
                    _history[idx].Add(-10.0f);
                }
            }
        }

        void Update()
        {
            if (GameSequenceManager.Instance != null)
            {
                if (GameSequenceManager.Instance.PhaseKind == GameSequenceManager.Phase.AfterBattle)
                {
                    // ゲーム終了後は何もしない
                    return;
                }
            }

            List<HitResult> results = new();

            foreach (var lhs in _colliders)
            {
                var result = new HitResult();

                foreach (var rhs in _colliders)
                {
                    if (lhs == null)
                    {
                        continue;
                    }
                    if (lhs.IsEnabled is false)
                    {
                        continue;
                    }

                    if (rhs == null)
                    {
                        continue;
                    }
                    if (rhs.IsEnabled is false)
                    {
                        continue;
                    }

                    if (lhs == rhs)
                    {
                        continue;
                    }

                    var isCollide = Vector2.Distance(lhs.CenterPos, rhs.CenterPos) <= (lhs.Radius + rhs.Radius);

                    if (isCollide)
                    {
                        var dir = rhs.CenterPos - lhs.CenterPos;
                        if (dir.sqrMagnitude < 0.003f)
                        {
                            // 番号によって挙動を変える
                            // @memo: 重なり続けるのを回避するため
                            if (lhs.PlayerIdx == 0)
                            {
                                result.IsButtomHit = true;
                            }
                            else if (lhs.PlayerIdx == 1)
                            {
                                result.IsTopHit = true;
                            }
                            else if (lhs.PlayerIdx == 2)
                            {
                                result.IsLeftHit = true;
                            }
                            else if (rhs.PlayerIdx == 3)
                            {
                                result.IsRightHit = true;
                            }
                        }
                        else
                        {
                            var velocity = rhs.Velocity;


                            // @memo: deg: [-180, 180]
                            //        deg = 0 のときに x, y = (1, 0)
                            //        deg = 180 のときに x, y = (-1, 0)
                            //        deg = 90 のときに x, y = (0, 1)
                            var deg = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

                            if (deg < 45.0f && deg > -45.0f)
                            {
                                result.IsRightHit = true;
                                result.RightHitSpeed = Mathf.Max(result.RightHitSpeed, Mathf.Max(0.0f, -velocity.x));
                            }
                            else if (deg > 135.0f || deg < -135.0f)
                            {
                                result.IsLeftHit = true;
                                result.LeftHitSpeed = Mathf.Max(result.LeftHitSpeed, Mathf.Max(0.0f, velocity.x));
                            }
                            else if (deg >= 45.0f && deg <= 135.0f)
                            {
                                if (Time.time - _history[lhs.PlayerIdx][rhs.PlayerIdx] < _interval)
                                {
                                    // 連続で当たりすぎ
                                    continue;
                                }

                                result.IsTopHit = true;
                                result.TopHitSpeed = Mathf.Max(result.TopHitSpeed, Mathf.Max(0.0f, -velocity.y));
                            }
                            else
                            {
                                if (Time.time - _history[lhs.PlayerIdx][rhs.PlayerIdx] < _interval)
                                {
                                    // 連続で当たりすぎ
                                    continue;
                                }

                                result.IsButtomHit = true;
                                result.ButtomHitSpeed = Mathf.Max(result.ButtomHitSpeed, Mathf.Max(0.0f, velocity.y));
                            }
                        }

                        _history[lhs.PlayerIdx][rhs.PlayerIdx] = Time.time;
                        result.RhsCenterPosition = rhs.CenterPos;
                    }
                }

                results.Add(result);
            }

            for (int idx = 0; idx < results.Count; ++idx)
            {
                _colliders[idx].ReflectHitResult(results[idx]);
            }
        }
        #endregion

        #region private フィールド
        int ColliderCountMax => Player.Constant.PlayerCountMax;

        List<HitCollider> _colliders = new();

        List<List<float>> _history = new List<List<float>>();
        float _interval = 0.1f;
        #endregion

        #region private メソッド
        #endregion
    }
}