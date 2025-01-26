using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using Cysharp.Threading.Tasks;
using DG.Tweening;

namespace Ui
{
    /// <summary>
    /// GameBeginUi
    /// </summary>
    public class TitleUiManager
        : MonoBehaviour
    {
        #region プロパティ
        #endregion

        #region メソッド
        private void Start()
        {
            Staging().Forget();  
        }
        #endregion

        #region privateフィールド
        #endregion

        #region privateメソッド
        public async UniTask Staging()
        {
            // 最低 2 秒は待つ

            await UniTask.WaitForSeconds(2.0f);

            // 誰かがボタンを押したら次へ
            var gameController = GameController.Instance;
            bool isPushed = false;

            void OnActoinTrigged()
            {
                isPushed = true;
            }

            for (int idx = 0; idx < gameController.MaxPlayerCount; ++idx)
            {
                gameController.GetPlayerInput(idx).GetComponent<PlayerInputHandler>().OnAction += OnActoinTrigged;
            }

            while (!isPushed)
            {
                await UniTask.Yield();
            }

            // TODO: UI が動く

            // コールバック解除
            for (int idx = 0; idx < gameController.MaxPlayerCount; ++idx)
            {
                gameController.GetPlayerInput(idx).GetComponent<PlayerInputHandler>().OnAction -= OnActoinTrigged;
            }

            // 次のシーンへ

            TadaLib.Scene.TransitionManager.Instance.StartTransition("CharaSelect", 0.5f, 0.5f);
        }
        #endregion
    }
}