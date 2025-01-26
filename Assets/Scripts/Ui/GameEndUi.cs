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
    public class GameEndUi
        : MonoBehaviour
    {
        #region プロパティ
        #endregion

        #region メソッド
        public async UniTask GameEnd()
        {
            // TODO: ガウシアンブラーをかける

            _canvas.gameObject.SetActive(true);

            _main.gameObject.SetActive(false);

            _background.rectTransform.localPosition = Vector3.down * 10.0f;

            _ = _background.rectTransform.DOLocalMoveY(0.0f, 0.1f);

            await UniTask.WaitForSeconds(0.5f);

            _main.gameObject.SetActive(true);

            // TODO: ガウシアンぶらーを切る

            _canvas.gameObject.SetActive(false);
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        Canvas _canvas;

        [SerializeField]
        UnityEngine.UI.Image _background;

        [SerializeField]
        UnityEngine.UI.Image _main;
        #endregion

        #region privateメソッド
        #endregion
    }
}