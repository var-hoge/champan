using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;

namespace App.Actor.Gimmick.Crown
{
    /// <summary>
    /// SimpleRotator
    /// </summary>
    public class SimpleRotator
        : MonoBehaviour
    {
        #region プロパティ
        public float SpeedRate { get; set; } = 1.0f;
        #endregion

        #region メソッド
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
            _deg = transform.localEulerAngles.z;
        }

        void Update()
        {
            _deg += _rotateDegPerSec * Time.deltaTime * SpeedRate;
            transform.localEulerAngles = new Vector3(0.0f, 0.0f, _deg);
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        float _rotateDegPerSec = 72.0f;

        [SerializeField]
        bool _isCcw = false;

        float _deg = 0.0f;
        #endregion

        #region privateメソッド
        #endregion
    }
}