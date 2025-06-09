using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;

namespace App.Cpu
{
    /// <summary>
    /// CpuManager
    /// </summary>
    public class CpuManager
        : BaseManagerProc<CpuManager>
    {
        #region プロパティ
        #endregion

        #region static メソッド
        #endregion

        #region メソッド
        /// <summary>
        /// CPU 人数の設定
        /// </summary>
        /// <param name="count"></param>
        public void SetCpuCount(int count)
        {
            Debug.Assert(count <= Actor.Player.Constant.PlayerCountMax);
            SetupIfNeeded();

            for (int idx = 0; idx < Actor.Player.Constant.PlayerCountMax; ++idx)
            {
                _isCpuList[idx] = false;
            }
            // 末尾から CPU を詰めていく
            for (int idx = 0; idx < count; ++idx)
            {
                _isCpuList[Actor.Player.Constant.PlayerCountMax - 1 - idx] = true;
            }
        }

        /// <summary>
        /// 指定したプレイヤー番号が CPU かどうか取得
        /// </summary>
        /// <param name="playerIdx"></param>
        /// <returns></returns>
        public bool IsCpu(int playerIdx)
        {
            SetupIfNeeded();

            return _isCpuList[playerIdx];
        }
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
            SetupIfNeeded();
        }
        #endregion

        #region private フィールド
        [SerializeField]
        CpuSetting _setting;
        List<bool> _isCpuList = null;
        #endregion

        #region private メソッド
        void SetupIfNeeded()
        {
            if (_isCpuList != null)
            {
                return;
            }

            _isCpuList = new List<bool>()
            {
                _setting.IsCpuPlayer0,
                _setting.IsCpuPlayer1,
                _setting.IsCpuPlayer2,
                _setting.IsCpuPlayer3,
            };
        }
        #endregion
    }
}