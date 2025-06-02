using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using TadaLib.Util;

namespace Scripts
{
    /// <summary>
    /// GameMatchManager
    /// </summary>
    public class GameMatchManager
        : SingletonMonoBehaviour<GameMatchManager>
    {
        #region プロパティ
        /// <summary>
        /// ゲームに勝利するための勝ち点
        /// </summary>
        public int WinCountToMatchFinish { private set; get; }
        #endregion

        #region メソッド
        public void SetWinCountToMatchFinish(int count)
        {
            WinCountToMatchFinish = count;
        }

        #endregion

        #region MonoBehavior の実装
        void Start()
        {
            for (int idx = 0; idx < global::Actor.Player.Constant.PlayerCountMax; ++idx)
            {
                _winCounts.Add(0);
            }
        }

        void Update()
        {
        }
        #endregion

        #region private フィールド
        List<int> _winCounts = new List<int>();
        #endregion

        #region private メソッド
        #endregion
    }
}