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
            Title,
            InGameBeforeBattle,
            InGameBattle,
            InGameAfterBattle,
            Result,
        }
        #endregion

        #region プロパティ
        public Phase PhaseKind { private set; get; }
        #endregion

        #region static メソッド
        #endregion

        #region メソッド
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
        #endregion
    }
}