using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TadaLib.ProcSystem;
using TadaLib.Extension;
using TadaLib.ActionStd;
using UniRx;
using TadaLib.Ui;
using Cysharp.Threading.Tasks;
using KanKikuchi.AudioManager;
using System.Threading.Tasks;
using DG.Tweening;

namespace App.Ui.Credit
{
    /// <summary>
    /// CreditUiManager
    /// </summary>
    public class CreditUiManager
        : MonoBehaviour
    {
        #region プロパティ
        #endregion

        #region メソッド
        #endregion

        #region MonoBehavior の実装
        void Start()
        {
            Staging().Forget();
        }
        #endregion

        #region private フィールド
        [SerializeField]
        Button _backButton;
        #endregion

        #region private メソッド
        async UniTask Staging()
        {
            _backButton.GetComponent<RectTransform>().localScale = Vector3.zero;
            
            await UniTask.WaitForSeconds(0.2f);

            _backButton.GetComponent<RectTransform>().DOScale(Vector3.one, 0.15f);
            _backButton.OnSelected();

            await UniTask.WaitForSeconds(0.2f);

            while (true)
            {
                var isFinish = false;
                foreach (var input in TadaLib.Input.PlayerInputManager.Instance.InputProxies)
                {
                    if (input.IsPressedTrigger(TadaLib.Input.ButtonCode.Action))
                    {
                        isFinish = true;
                        break;
                    }
                }

                if (isFinish)
                {
                    break;
                }

                await UniTask.Yield();
            }

            TadaLib.Scene.TransitionManager.Instance.StartTransition("Title", 0.3f, 0.3f);
        }
        #endregion
    }
}