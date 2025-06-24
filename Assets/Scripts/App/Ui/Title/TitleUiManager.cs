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

namespace App.Ui.Title
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
            BGMManager.Instance.Play(BGMPath.TITLE_SCREEN);
        }
        #endregion

        #region privateフィールド
        [SerializeField] private Transform _start;
        [SerializeField] private GameObject _startOnImage;
        [SerializeField] private GameObject _startOffImage;
        [SerializeField] private Transform _exit;
        [SerializeField] private GameObject _exitOnImage;
        [SerializeField] private GameObject _exitOffImage;
        #endregion

            #region privateメソッド
        public async UniTask Staging()
        {
            // 最低 2 秒は待つ
            await UniTask.WaitForSeconds(1f);
            SEManager.Instance.Play(SEPath.TITLE_SCREEN_04);
            await UniTask.WaitForSeconds(1f);

            var inputProxy = TadaLib.Input.PlayerInputManager.Instance.InputProxy(0);
            var isPushed = false;
            var isStartSelected = true;
            var isInputValid = true;
            inputProxy.OnAction += OnActoinTrigged;
            inputProxy.OnMove += OnMove;

            while (true)
            {
                if (isPushed)
                {
                    if (isStartSelected)
                    {
                        // SE再生
                        SEManager.Instance.Play(SEPath.MENU_VALIDATION);
                        inputProxy.OnAction -= OnActoinTrigged;
                        inputProxy.OnMove -= OnMove;
                        TadaLib.Scene.TransitionManager.Instance.StartTransition("CharaSelect", 0.5f, 0.5f);
                        break;
                    }
                    else
                    {
#if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
#else
                        Application.Quit();
#endif
                    }
                }
                await UniTask.Yield();
            }

            void OnActoinTrigged()
            {
                isPushed = true;
            }

            void OnMove(Vector2 value)
            {
                if (!isInputValid || value.x == 0)
                {
                    isInputValid = true;
                    return;
                }

                isInputValid = false;
                isStartSelected = !isStartSelected;
                // 表示イメージの切替
                _startOnImage.SetActive(isStartSelected);
                _startOffImage.SetActive(!isStartSelected);
                _exitOnImage.SetActive(!isStartSelected);
                _exitOffImage.SetActive(isStartSelected);
                // 文字サイズの設定
                var (startValue, exitvalue) = isStartSelected ? (1.1f, 1f) : (1f, 1.1f);
                _start.DOScale(Vector3.one * startValue, 0.1f);
                _exit.DOScale(Vector3.one * exitvalue, 0.1f);
                // SE再生
                SEManager.Instance.Play(SEPath.MENU_NAVIGATION);
            }
        }

        // ロールバックできるように、もとの実装を残している
        //public async UniTask Staging()
        //{
        //    // 最低 2 秒は待つ

        //    await UniTask.WaitForSeconds(2.0f);

        //    // 誰かがボタンを押したら次へ
        //    var gameController = GameController.Instance;
        //    bool isPushed = false;

        //    void OnActoinTrigged()
        //    {
        //        isPushed = true;
        //    }

        //    for (int idx = 0; idx < gameController.MaxPlayerCount; ++idx)
        //    {
        //        gameController.GetPlayerInput(idx).GetComponent<PlayerInputHandler>().OnAction += OnActoinTrigged;
        //    }

        //    while (!isPushed)
        //    {
        //        await UniTask.Yield();
        //    }

        //    // TODO: UI が動く

        //    // コールバック解除
        //    for (int idx = 0; idx < gameController.MaxPlayerCount; ++idx)
        //    {
        //        gameController.GetPlayerInput(idx).GetComponent<PlayerInputHandler>().OnAction -= OnActoinTrigged;
        //    }

        //    // 次のシーンへ

        //    TadaLib.Scene.TransitionManager.Instance.StartTransition("CharaSelect", 0.5f, 0.5f);
        //}


        #endregion
    }
}