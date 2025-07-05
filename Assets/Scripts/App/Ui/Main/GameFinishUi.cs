using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using App;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using KanKikuchi.AudioManager;
using NUnit.Framework.Internal;
using Unity.VisualScripting;

namespace Ui.Main
{
    /// <summary>
    /// GameFinishUi
    /// </summary>
    public class GameFinishUi
        : MonoBehaviour
    {
        #region プロパティ
        #endregion

        #region メソッド
        public async UniTask Staging(SimpleAnimation animation)
        {
            _canvas.gameObject.SetActive(true);
            _canvas.GetComponent<CanvasGroup>().alpha = 0.0f;
            _ = _canvas.GetComponent<CanvasGroup>().DOFade(1.0f, 0.3f);

            animation.Play("GameFinish");

            await UniTask.WaitForSeconds(1.0f);

            _rematchButton.gameObject.SetActive(true);
            _meinMenuButton.gameObject.SetActive(true);

            await UniTask.WaitForSeconds(0.5f);

            // クリックまで待つ
            var inputManager = TadaLib.Input.PlayerInputManager.Instance;
            var isRematch = true;
            while (true)
            {
                var isEnd = false;
                for (int idx = 0; idx < inputManager.MaxPlayerCount; ++idx)
                {
                    if (inputManager.InputProxy(idx).IsPressed(TadaLib.Input.ButtonCode.Action))
                    {
                        isRematch = true;
                        isEnd = true;
                        break;
                    }

                    if (inputManager.InputProxy(idx).IsPressed(TadaLib.Input.ButtonCode.Cancel))
                    {
                        isRematch = false;
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

            GameMatchManager.Instance.ResetPlayersWinCount();

            if (isRematch)
            {
                // シーン遷移
                TadaLib.Scene.TransitionManager.Instance.StartTransition("Main", 0.5f, 0.5f);
            }
            else
            {
                // シーン遷移
                TadaLib.Scene.TransitionManager.Instance.StartTransition("Title", 0.5f, 0.5f);
            }
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        Canvas _canvas;

        [SerializeField]
        GameObject _rematchButton;

        [SerializeField]
        GameObject _meinMenuButton;
        #endregion

        #region privateメソッド
        #endregion
    }
}