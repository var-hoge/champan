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
        public int ExShieldValue { get; set; }
        public bool IsShieldDestroyed => ShieldValue == 0 && ExShieldValue == 0;
        public int InitShieldValue { private set; get; }
        public bool DoFakeFinishStaging
        {
            get
            {
                if (_doFaleFinishStaging is false)
                {
                    return false;
                }

                return ShieldValue + ExShieldValue == 1;
            }
        }
        bool _doFaleFinishStaging = false;

        public Bubble.Bubble CrownBubble { get; set; }

        public int LastCrownRidePlayerIdx { get; set; } = 0;

        public bool IsFirstCrownSetup { get; set; } = true;

        #endregion

        #region メソッド
        protected override void Awake()
        {
            base.Awake();

            ShieldValue = _setting.InitShieldValue;

            if (Random.Range(0, 4) <= 0)
            {
                ShieldValue--;
            }

            ExShieldValue = 0;
            if (Random.Range(0, 4) <= 0)
            {
                ExShieldValue++;
            }

            InitShieldValue = ShieldValue;

            _doFaleFinishStaging = false;

            Debug.Log($"ShieldValue/Ex: {ShieldValue}/{ExShieldValue}");
        }

        private void Start()
        {
            if (GameMatchManager.Instance.WinCountToMatchFinish != 1 && GameMatchManager.Instance.IsExistReachPlayer)
            {
                _doFaleFinishStaging = Random.Range(0, 8) <= 0;
            }

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