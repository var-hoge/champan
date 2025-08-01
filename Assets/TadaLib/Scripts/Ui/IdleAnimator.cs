using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using DG.Tweening;

namespace TadaLib.Ui
{
    /// <summary>
    /// IdleAnimation
    /// </summary>
    public class IdleAnimator
        : MonoBehaviour
    {
        #region プロパティ
        public bool IsEnabled { get; set; } = true;
        #endregion

        #region メソッド
        public void SetBasePos(Vector3 pos)
        {
            _basePos = pos;
        }

        public void ResetBasePos()
        {
            _basePos = GetComponent<RectTransform>().localPosition;
        }
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
            IsEnabled = _defaultEnabled;

            if (_basePos is null)
            {
                ResetBasePos();
            }
        }

        void Update()
        {
            if (IsEnabled is false && _enabledForAnim is false)
            {
                return;
            }

            _moveDurationSec += Time.deltaTime * _animRate;
            var rate = Mathf.Sin(_moveDurationSec);

            if (_moveAmount != 0.0f)
            {
                var pos = _basePos.Value + (Vector3)(rate * _moveDir.normalized * _moveAmount);
                GetComponent<RectTransform>().localPosition = pos;
            }

            if(_rotZAmount != 0.0f)
            {
                var angles = GetComponent<RectTransform>().localEulerAngles;
                angles.z = rate * _rotZAmount;
                GetComponent<RectTransform>().localEulerAngles = angles;
            }
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        bool _defaultEnabled = true;

        [SerializeField]
        float _animRate = 1.2f;

        [SerializeField]
        float _moveAmount = 12.0f;
        [SerializeField]
        Vector2 _moveDir = Vector3.up;

        [SerializeField]
        float _rotZAmount = 0.0f;

        [SerializeField]
        bool _enabledForAnim = false;

        float _moveDurationSec = 0.0f;

        Vector3? _basePos;
        #endregion

        #region privateメソッド
        #endregion
    }
}