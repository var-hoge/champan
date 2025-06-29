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
using UnityEngine.U2D;

namespace App.Ui.Main
{
    /// <summary>
    /// GameBeginUi
    /// </summary>
    public class GameBeginUi
        : MonoBehaviour
    {
        #region プロパティ
        #endregion

        #region メソッド
        public async UniTask CountDown()
        {
            BGMManager.Instance.Stop();

            await UniTask.WaitForSeconds(1.0f);

            // TODO: ガウシアンブラーをかける

            _ = _canvas.GetComponent<CanvasGroup>().DOFade(1.0f, 0.3f);

            _canvas.gameObject.SetActive(true);

            _main.gameObject.SetActive(false);

            await UniTask.WaitForSeconds(0.5f);

            _main.gameObject.SetActive(true);

            // ルール説明
            {
                _main.SetSprite(_descriptionSprite);
                _main.rectTransform.localScale = Vector3.one * 5.0f;
                _ = _main.rectTransform.DOScale(1.0f, 0.3f).SetEase(Ease.OutQuart);

                _effect.SetActive(true);

                await UniTask.WaitForSeconds(1.6f);

                _effect.SetActive(false);
            }

            SEManager.Instance.Play(SEPath.COUNTDOWN);
            foreach (var sprite in _countDownSprites)
            {
                _main.SetSprite(sprite);
                _main.rectTransform.localScale = Vector3.one * 5.0f;
                _ = _main.rectTransform.DOScale(1.0f, 0.3f).SetEase(Ease.OutQuart);

                _effect.SetActive(true);

                await UniTask.WaitForSeconds(1.4f);

                _effect.SetActive(false);
            }

            // GO
            {
                BGMManager.Instance.Play(BGMPath.FIGHT);
                _main.SetSprite(_descriptionSprite);
                _main.rectTransform.localScale = Vector3.one * 5.0f;
                _ = _main.rectTransform.DOScale(1.0f, 0.3f).SetEase(Ease.OutQuart);

                _effect.SetActive(true);

                await UniTask.WaitForSeconds(0.8f);
            }

            _ = _canvas.GetComponent<CanvasGroup>().DOFade(0.0f, 0.3f);

            GameSequenceManager.Instance.PhaseKind = GameSequenceManager.Phase.Battle;
        }
        #endregion

        #region privateフィールド
        [SerializeField]
        Canvas _canvas;

        [SerializeField]
        UnityEngine.UI.Image _main;

        [SerializeField]
        Sprite _descriptionSprite;

        [SerializeField]
        List<Sprite> _countDownSprites;

        [SerializeField]
        Sprite _goSprite;

        [SerializeField]
        GameObject _effect;
        #endregion

        #region privateメソッド
        #endregion
    }
}