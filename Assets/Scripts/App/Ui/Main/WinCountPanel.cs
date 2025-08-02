using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using AnnulusGames.SceneSystem;
using Cysharp.Threading.Tasks;
using System.Linq;
using System;
using TadaLib.Scene;

namespace App.Ui.Main
{
    /// <summary>
    /// WinCountPanel
    /// </summary>
    public class WinCountPanel
        : MonoBehaviour
    {
        public void ShowReachTextIfNeed()
        {
            if (GameMatchManager.Instance.IsExistWinner)
            {
                // 勝者がいる場合は出さない
                return;
            }

            var playerCount = 4; // @todo: マネージャーから取得
            var remainPlayerCount = playerCount;

            var childCount = transform.childCount;
            for (int idx = 0; idx < childCount; idx++)
            {
                var child = transform.GetChild(idx);
                var winCountUnit = child.GetComponent<WinCountUnit>();
                if (winCountUnit == null)
                {
                    continue;
                }

                var playerIdx = playerCount - remainPlayerCount;
                --remainPlayerCount;

                if (GameMatchManager.Instance.IsExistCpu is false)
                {
                    if (Cpu.CpuManager.Instance.IsCpu(playerIdx))
                    {
                        // 存在しないキャラ
                        continue;
                    }
                }

                if (GameMatchManager.Instance.IsReachPlayer(playerIdx))
                {
                    winCountUnit.ShowReachText();
                }

                if (remainPlayerCount == 0)
                {
                    break;
                }
            }
        }

        void OnEnable()
        {
            Appear();
        }

        [SerializeField]
        RectTransform _continueButton;

        [SerializeField]
        float _continueAppearWaitDurationSec = 4.0f;

        async void Appear()
        {
            var playerCount = 4; // @todo: マネージャーから取得
            var remainPlayerCount = playerCount;

            var childCount = transform.childCount;
            for (int idx = 0; idx < childCount; idx++)
            {
                var child = transform.GetChild(idx);
                var winCountUnit = child.GetComponent<WinCountUnit>();
                if (winCountUnit == null)
                {
                    continue;
                }

                var playerIdx = playerCount - remainPlayerCount;
                --remainPlayerCount;

                if (GameMatchManager.Instance.IsExistCpu is false)
                {
                    if (Cpu.CpuManager.Instance.IsCpu(playerIdx))
                    {
                        // 存在しないキャラ
                        continue;
                    }
                }

                var isWinPlayer = GameSequenceManager.WinnerPlayerIdx == playerIdx;
                winCountUnit.Setup(playerIdx, isWinPlayer);
                winCountUnit.gameObject.SetActive(true);

                if (remainPlayerCount == 0)
                {
                    break;
                }
            }

            // 演出が終わるまで待つ
            await UniTask.WaitForSeconds(_continueAppearWaitDurationSec);

            //// continue の表示
            //_continueButton.gameObject.SetActive(true);
        }
    }
}