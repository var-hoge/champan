﻿using System.Collections;
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

            _isMoveDisabled = true;

            _body.rectTransform.DOPunchScale(Vector3.one * 0.5f, 0.15f)
                .OnComplete(() =>
                {
                    _body.SetSprite(_selectedSprite);
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
            _isMoveDisabled = false;

            _body.rectTransform.DOKill();
            _body.SetSprite(_unselectedSprite);
            _body.rectTransform.localScale = _localScaleDefault;
            _body.rectTransform.localPosition = _localPositionDefault;
            _body.rectTransform.localEulerAngles = _localEulerAnglesDefault;

            _body.rectTransform.DOPunchScale(Vector3.one * 0.2f, 0.1f);
        }

        public RectTransform GetPickIconTransform(int playerIdx)
        {
            return transform.GetChild(playerIdx + 1).GetComponent<RectTransform>();
        }

        public void ChangeSprite(Sprite sprite)
        {
            _isMoveDisabled = true;
            _body.SetSprite(sprite);
        }

        public void SetRotateZero()
        {
            _body.rectTransform.localEulerAngles = Vector3.zero;
        }
        #endregion

        #region privateフィールド

        [SerializeField]
        UnityEngine.UI.Image _body;

        [SerializeField]
        Sprite _selectedSprite;

        [SerializeField]
        string _sePath;

        [SerializeField]
        float _moveAmount = 30.0f;
        [SerializeField]
        Vector2 _moveDir = Vector3.up;
        [SerializeField]
        float _moveSpeed = 1.0f;

        bool _isMoveDisabled = false;
        float _moveDurationSec = 0.0f;

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

        private void Update()
        {
            if (_isMoveDisabled)
            {
                return;
            }

            _moveDurationSec += Time.deltaTime * _moveSpeed;
            var rate = Mathf.Sin(_moveDurationSec);

            var pos = _localPositionDefault + (Vector3)(rate * _moveDir.normalized * _moveAmount);
            _body.rectTransform.localPosition = pos;
        }
        #endregion
    }
}