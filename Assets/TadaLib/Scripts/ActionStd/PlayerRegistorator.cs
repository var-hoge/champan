using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using Cysharp.Threading.Tasks;
using App.Actor.Player;

namespace TadaLib.ActionStd
{
    /// <summary>
    /// PlayerRegistorator
    /// </summary>
    public class PlayerRegistorator
        : MonoBehaviour
    {
        #region プロパティ
        #endregion

        #region メソッド
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
#if UNITY_EDITOR
            if (PlayerManager.Instance is null)
            {
                Assert.IsTrue(false, "PlayerManagerのGameObjectがシーン内にありません。");
                return;
            }
#endif
            // ネットワーク対戦では、どの席が CPU かは席の割り当てが済むまで確定しない
            if (App.Network.NetworkSession.IsOnline)
            {
                SetupAfterSeatsReady().Forget();
                return;
            }

            Setup();
        }
        #endregion

        #region privateフィールド
        #endregion

        #region privateメソッド
        async UniTask SetupAfterSeatsReady()
        {
            var isTimeout = await UniTask
                .WaitUntil(() =>
                    App.Network.NetworkGameLauncher.Instance != null
                    && App.Network.NetworkGameLauncher.Instance.IsMatchReady)
                .TimeoutWithoutException(System.TimeSpan.FromSeconds(15.0));

            if (isTimeout)
            {
                Debug.LogWarning("[PlayerRegistorator] 席の確定を待てなかったため、現在の設定で処理します");
            }

            // 待っている間に破棄されている場合がある
            if (this == null)
            {
                return;
            }

            Setup();
        }

        void Setup()
        {
            if (
                App.Cpu.CpuManager.Instance.IsCpu(GetComponent<DataHolder>().PlayerIdx) &&
                App.GameMatchManager.Instance.IsExistCpu is false
                )
            {
                Destroy(gameObject);
                return;
            }

            PlayerManager.RegisterPlayer(gameObject, GetComponent<DataHolder>().PlayerIdx);
        }
        #endregion
    }
}