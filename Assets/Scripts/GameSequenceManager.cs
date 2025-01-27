using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using Cysharp.Threading.Tasks;

namespace Scripts
{
    /// <summary>
    /// GameSequenceManager
    /// </summary>
    public class GameSequenceManager
        : TadaLib.Util.SingletonMonoBehaviour<GameSequenceManager>
    {
        #region 型定義
        public enum Phase
        {
            BeforeBattle,
            Battle,
            AfterBattle,
        }
        #endregion

        #region プロパティ
        public Phase PhaseKind = Phase.BeforeBattle;
        public static int WinnerPlayerIdx = 0;
        #endregion

        #region static メソッド
        #endregion

        #region メソッド
        public void GameOver()
        {
            WinnerPlayerIdx = Bubble.LastRidePlayerIdx;
            _gameEndUi.GameEnd(GetComponent<SimpleAnimation>()).Forget();
        }
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
            _gameBeginUi.CountDown().Forget();
        }
        #endregion

        #region private メソッド
        #endregion

        #region private フィールド
        [SerializeField]
        Ui.GameBeginUi _gameBeginUi;

        [SerializeField]
        Ui.GameEndUi _gameEndUi;
        #endregion
    }
}