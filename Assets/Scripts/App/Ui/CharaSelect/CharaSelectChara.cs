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
using App.Actor;
using KanKikuchi.AudioManager;

namespace App.Ui.CharaSelect
{
    /// <summary>
    /// 
    /// </summary>
    public class CharaSelectChara
        : MonoBehaviour
    {
        #region プロパティ
        #endregion

        #region メソッド
        public void OnSelected(System.Action onAnimCompleted)
        {
            SEManager.Instance.Play(_sePath);
            _body.rectTransform.DOKill();
            _body.rectTransform.localScale = _localScaleDefault;
            _body.rectTransform.localPosition = _localPositionDefault;
            _body.rectTransform.localEulerAngles = _localEulerAnglesDefault;

            _body.rectTransform.DOPunchScale(Vector3.one * 0.5f, 0.15f)
                .OnComplete(() =>
                {
                    _body.sprite = _selectedSprite;
                    _body.rectTransform.sizeDelta = _selectedSprite.textureRect.size;
                    _body.rectTransform.localScale = _localScaleDefault * 1.3f;
                    _body.rectTransform.localEulerAngles = new Vector3(0.0f, 0.0f, Random.Range(0.0f, 360.0f));
                    _body.rectTransform.DOShakePosition(0.20f, 5.0f, 5, fadeOut: true);
                    _body.rectTransform.DOScale(_body.rectTransform.localScale * 1.2f, 0.25f).SetEase(Ease.OutQuint);
                    _body.rectTransform.DOShakeRotation(0.20f, 20.0f, 30, fadeOut: true);

                    onAnimCompleted?.Invoke();
                });
        }

        public void OnCancelSelected()
        {
            _body.rectTransform.DOKill();
            _body.sprite = _unselectedSprite;
            _body.rectTransform.sizeDelta = _unselectedSprite.textureRect.size;
            _body.rectTransform.localScale = _localScaleDefault;
            _body.rectTransform.localPosition = _localPositionDefault;
            _body.rectTransform.localEulerAngles = _localEulerAnglesDefault;

            _body.rectTransform.DOPunchScale(Vector3.one * 0.2f, 0.1f);
        }

        public RectTransform GetPickIconTransform(int playerIdx)
        {
            return transform.GetChild(playerIdx + 1).GetComponent<RectTransform>();
        }
        #endregion

        #region privateフィールド

        [SerializeField]
        UnityEngine.UI.Image _body;

        [SerializeField]
        Sprite _selectedSprite;

        [SerializeField]
        string _sePath;

        Sprite _unselectedSprite;
        Vector3 _localScaleDefault;
        Vector3 _localPositionDefault;
        Vector3 _localEulerAnglesDefault;
        #endregion

        #region privateメソッド
        void Start()
        {
            _unselectedSprite = _body.sprite;
            _localScaleDefault = _body.rectTransform.localScale;
            _localPositionDefault = _body.rectTransform.localPosition;
            _localEulerAnglesDefault = _body.rectTransform.localEulerAngles;
        }
        #endregion
    }
}