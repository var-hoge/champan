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
using KanKikuchi.AudioManager;

namespace App.Ui.Main
{
    /// <summary>
    /// GameBeginUi
    /// </summary>
    public class ResultUiManager
        : MonoBehaviour
    {
        #region プロパティ
        #endregion

        #region メソッド
        private void Start()
        {
            BGMManager.Instance.Play(BGMPath.RESULT_SCREEN);
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
            var inputManager = TadaLib.Input.PlayerInputManager.Instance;
            bool isPushed = false;

            while (!isPushed)
            {
                var isEnd = false;
                for (int idx = 0; idx < inputManager.MaxPlayerCount; ++idx)
                {
                    if (inputManager.InputProxy(idx).IsPressed(TadaLib.Input.ButtonCode.Action))
                    {
                        isEnd = true;
                        break;
                    }
                }

                if (isEnd)
                {
                    break;
                }

                await UniTask.Yield();
            }

            // TODO: UI が動く

            // 次のシーンへ

            TadaLib.Scene.TransitionManager.Instance.StartTransition("Title", 0.5f, 0.5f);
        }
        #endregion
    }
}