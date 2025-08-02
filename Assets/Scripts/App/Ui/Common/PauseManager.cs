using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;

namespace App.Ui.Common
{
    /// <summary>
    /// PauseManager
    /// </summary>
    public class PauseManager
        : BaseManagerProc<PauseManager>
        , IProcManagerUpdate
    {
        #region プロパティ
        #endregion

        #region static メソッド
        #endregion

        #region メソッド
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
        }
        #endregion

        #region IProcManagerUpdate の実装
        public void OnUpdate()
        {
            // @memo: ポーズ画面を用意したいが、コストがかかる
            //        とりあえず Enter 長押しでタイトルに戻れるようにする

            if (Input.GetKey(KeyCode.Return))
            {
                _timeSecToReturnToTitle += Time.unscaledDeltaTime;
            }
            else
            {
                _timeSecToReturnToTitle = 0;
            }

            if (_timeSecToReturnToTitle >= _needTimeSecToReturnToTitle)
            {
                if (TadaLib.Scene.TransitionManager.Instance.IsTransitioning is false)
                {
                    TadaLib.Scene.TransitionManager.Instance.StartTransition("Title", 0.2f, 0.3f, 0.3f);
                }

                _timeSecToReturnToTitle = 0.0f;
            }

            // ESC キーでゲーム終了
            if (Input.GetKeyDown(KeyCode.Escape))
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }
        #endregion

        #region private フィールド
        float _needTimeSecToReturnToTitle = 1.0f;
        float _timeSecToReturnToTitle = 0.0f;
        #endregion

        #region private メソッド
        #endregion
    }
}