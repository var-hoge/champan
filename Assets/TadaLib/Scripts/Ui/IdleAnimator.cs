using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;

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
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
            IsEnabled = _defaultEnabled;
        }

        void Update()
        {
            if (IsEnabled is false)
            {
                return;
            }
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        bool _defaultEnabled = true;

        [SerializeField]
        float _animRate = 1.0f;

        float _durationSec = 0.0f;
        #endregion

        #region privateメソッド
        #endregion
    }
}