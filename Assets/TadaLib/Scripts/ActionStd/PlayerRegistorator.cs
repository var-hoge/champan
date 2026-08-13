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
            // 対戦シーンでは、席の確定だけでなくこのキャラが Spawn されるまで待つ。
            //
            // Fusion はシーン上の NetworkObject を Spawn するときに有効化する。
            // Spawn より先に隠すと、その有効化で戻されてしまい、
            // CPU 無しの設定でも空席のキャラが出続けていた。
            //
            // 待つのは対戦シーンだけにする。
            // キャラセレクトのキャラは入場するまで無効で Fusion に登録されないため、
            // そこで Spawn を待つと終わらず、頭上の UI が出なくなる。
            var binder = GetComponent<App.Network.NetworkPlayerBinder>();
            var isMatchScene = App.Network.NetworkSession.IsInMatchScene(gameObject);

            bool IsSpawnedIfNeeded()
            {
                if (!isMatchScene || binder == null)
                {
                    return true;
                }

                return binder.Object != null && binder.Object.IsValid;
            }

            var isTimeout = await UniTask
                .WaitUntil(() =>
                    App.Network.NetworkGameLauncher.Instance != null
                    && App.Network.NetworkGameLauncher.Instance.IsMatchReady
                    && IsSpawnedIfNeeded())
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
            var playerIdx = GetComponent<DataHolder>().PlayerIdx;

            if (
                App.Cpu.CpuManager.Instance.IsCpu(playerIdx) &&
                App.GameMatchManager.Instance.IsExistCpu is false
                )
            {
                // ネットワーク対戦では消さずに隠す。
                //
                // 権威を持たない台が NetworkObject を破棄すると同期が壊れる。
                // ここでやりたいのは「出さない」ことだけなので、無効にすれば足りる。
                if (App.Network.NetworkSession.IsOnline)
                {
                    gameObject.SetActive(false);

                    // 隠したものが戻されていたら、待つ対象がまた足りていない。
                    // 黙って直らないより、気づけるようにしておく。
                    WarnIfShownAgainAsync(playerIdx).Forget();
                    return;
                }

                Destroy(gameObject);
                return;
            }

            PlayerManager.RegisterPlayer(gameObject, GetComponent<DataHolder>().PlayerIdx);
        }

        /// <summary>
        /// 隠したキャラが戻されていたら知らせる
        ///
        /// Fusion は Spawn のときにシーン上の NetworkObject を有効化する。
        /// 隠すのがそれより先だと戻されてしまう。
        /// 一度その形で不具合になっているため、再発したら分かるようにしておく。
        /// </summary>
        async UniTask WarnIfShownAgainAsync(int playerIdx)
        {
            await UniTask.Delay(System.TimeSpan.FromSeconds(1.0));

            // 破棄されていたら見る必要がない
            if (this == null || gameObject.activeSelf is false)
            {
                return;
            }

            Debug.LogWarning(
                $"[PlayerRegistorator] 席={playerIdx} のキャラを隠したはずが戻されています。"
                + $" 隠す前に待つ対象が足りていない可能性があります");
        }
        #endregion
    }
}