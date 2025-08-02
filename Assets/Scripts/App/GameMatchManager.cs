using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using TadaLib.Util;
using Unity.VisualScripting;

namespace App
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
        public int WinCountToMatchFinish { private set; get; } = 3;

        /// <summary>
        /// CPUが試合に出場するかどうか
        /// </summary>
        public bool IsExistCpu { private set; get; } = true;

        /// <summary>
        /// 勝ち点がリーチのプレイヤーがいるかどうか
        /// </summary>
        public bool IsExistReachPlayer
        {
            get
            {
                if (_winCounts.Count == 0)
                {
                    // セットアップ前
                    return false;
                }

                for (int idx = 0; idx < _winCounts.Count; ++idx)
                {
                    if (_winCounts[idx] + 1 == WinCountToMatchFinish)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// スコアを獲得したプレイヤーがいるかどうか
        /// </summary>
        public bool IsExistScoreGottenPlayer
        {
            get
            {
                for (int idx = 0; idx < _winCounts.Count; ++idx)
                {
                    if (_winCounts[idx] >= 1)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public int TotalWinCount
        {
            get
            {
                var sum = 0;
                for (int idx = 0; idx < _winCounts.Count; ++idx)
                {
                    sum += _winCounts[idx];
                }

                return sum;
            }
        }

        public bool IsExistWinner
        {
            get
            {
                for (int idx = 0; idx < _winCounts.Count; ++idx)
                {
                    if (_winCounts[idx] == WinCountToMatchFinish)
                    {
                        return true;
                    }
                }

                return false;

            }
        }
        #endregion

        /// <summary>
        /// CPU の有無の設定
        /// </summary>
        public void SetIsExistCpu(bool isExist)
        {
            IsExistCpu = isExist;
        }

        /// <summary>
        /// ゲームに勝利するための勝ち点の設定
        /// </summary>
        /// <param name="count"></param>
        #region メソッド
        public void SetWinCountToMatchFinish(int count)
        {
            Debug.Assert(count >= 1);
            WinCountToMatchFinish = count;
        }

        /// <summary>
        /// 指定したプレイヤーの勝ち点を追加
        /// </summary>
        /// <param name="playerIdx"></param>
        public void AddWinScore(int playerIdx)
        {
            _winCounts[playerIdx]++;
        }

        /// <summary>
        /// 各プレイヤーの勝ち点をリセット
        /// </summary>
        public void ResetPlayersWinCount()
        {
            for (int idx = 0; idx < _winCounts.Count; ++idx)
            {
                _winCounts[idx] = 0;
            }
        }

        /// <summary>
        /// 存在するなら、ゲームに勝利したプレイヤー番号を取得
        /// </summary>
        /// <param name="playerIdx"></param>
        /// <returns>勝者が存在するなら true, 存在しないなら false</returns>
        public bool TryGetWinner(out int playerIdx)
        {
            for (int idx = 0; idx < _winCounts.Count; ++idx)
            {
                if (_winCounts[idx] == WinCountToMatchFinish)
                {
                    playerIdx = idx;
                    return true;
                }
            }

            playerIdx = -1;
            return false;
        }

        /// <summary>
        /// プレイヤーの勝ち点を取得
        /// </summary>
        /// <param name="playerIdx"></param>
        /// <returns></returns>
        public int GetWinCount(int playerIdx)
        {
            return _winCounts[playerIdx];
        }

        /// <summary>
        /// 指定したプレイヤーがリーチプレイヤーかどうか
        /// </summary>
        /// <param name="playerIdx"></param>
        /// <returns></returns>
        public bool IsReachPlayer(int playerIdx)
        {
            return _winCounts[playerIdx] == WinCountToMatchFinish - 1;
        }
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
            for (int idx = 0; idx < Actor.Player.Constant.PlayerCountMax; ++idx)
            {
                _winCounts.Add(0);
            }
        }
        #endregion

        #region private フィールド
        List<int> _winCounts = new List<int>();
        #endregion

        #region private メソッド
        #endregion
    }
}