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
    /// Manager
    /// </summary>
    public class Manager
        : BaseManagerProc<Manager>
    {
        #region プロパティ
        public Setting Setting => _setting;
        public int ShieldValue { get; set; }
        public Bubble.Bubble CrownBubble { get; set; }

        public int LastCrownRidePlayerIdx { get; set; } = 0;

        public bool IsFirstCrownSetup { get; set; } = true;
        #endregion

        #region メソッド
        protected override void Awake()
        {
            base.Awake();

            ShieldValue = _setting.InitShieldValue;
        }
        #endregion

        #region private メソッド
        [SerializeField]
        Setting _setting;
        #endregion

        #region private フィールド
        #endregion
    }
}