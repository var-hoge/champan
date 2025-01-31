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
            BGMManager.Instance.Play(BGMPath.TITLE_SCREEN);
        }
        #endregion

        #region privateフィールド
        [SerializeField] private GameObject _startOnImage;
        [SerializeField] private GameObject _startOffImage;
        [SerializeField] private GameObject _exitOnImage;
        [SerializeField] private GameObject _exitOffImage;
        #endregion

            #region privateメソッド
        public async UniTask Staging()
        {
            // 最低 2 秒は待つ

            await UniTask.WaitForSeconds(2.0f);

            var gameController = GameController.Instance;
            var inputHandler = gameController.GetPlayerInput(0).GetComponent<PlayerInputHandler>();
            var isPushed = false;
            var isStartSelected = true;
            inputHandler.OnAction += OnActoinTrigged;
            inputHandler.OnMove += OnMove;

            while (true)
            {
                if (isPushed)
                {
                    if (isStartSelected)
                    {
                        inputHandler.OnAction -= OnActoinTrigged;
                        inputHandler.OnMove -= OnMove;
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
                isStartSelected = !isStartSelected;
                _startOnImage.SetActive(isStartSelected);
                _startOffImage.SetActive(!isStartSelected);
                _exitOnImage.SetActive(!isStartSelected);
                _exitOffImage.SetActive(isStartSelected);
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