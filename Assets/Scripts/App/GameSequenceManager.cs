using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using Cysharp.Threading.Tasks;

namespace App
{
    /// <summary>
    /// GameSequenceManager
    /// </summary>
    public class GameSequenceManager
        : TadaLib.Util.SingletonMonoBehaviour<GameSequenceManager>
    {
        [SerializeField] private int TargetFrameRate = 60;

        #region 型定義
        public enum Phase
        {
            BeforeBattle,
            Battle,
            AfterBattle,
        }
        #endregion

        #region プロパティ
        public Phase PhaseKind { get; set; } = Phase.BeforeBattle;
        public static int WinnerPlayerIdx = 0;
        #endregion

        #region static メソッド
        #endregion

        #region メソッド
        public void GameOver()
        {
            WinnerPlayerIdx = Actor.Gimmick.Bubble.Bubble.LastCrownRidePlayerIdx;

            var gameMatchManager = GameMatchManager.Instance;
            gameMatchManager.AddWinScore(WinnerPlayerIdx);
            if (gameMatchManager.TryGetWinner(out var playerIdx))
            {
                Debug.Assert(playerIdx == WinnerPlayerIdx);
                // ゲーム終了！
                _gameEndUi.GameEnd(GetComponent<SimpleAnimation>(), WinnerPlayerIdx).Forget();
            }
            else
            {
                // ラウンドが続く
                _gameEndUi.GameEndWithContinue(GetComponent<SimpleAnimation>(), WinnerPlayerIdx).Forget();
            }

        }
        #endregion

        #region MonoBehavior の実装

        void Awake()
        {
            Application.targetFrameRate = TargetFrameRate;
        }

        void Start()
        {
            _gameBeginUi.CountDown().Forget();
        }
        #endregion

        #region private メソッド
        #endregion

        #region private フィールド
        [SerializeField]
        App.Ui.Main.GameBeginUi _gameBeginUi;

        [SerializeField]
        App.Ui.Main.GameEndUi _gameEndUi;
        #endregion
    }
}